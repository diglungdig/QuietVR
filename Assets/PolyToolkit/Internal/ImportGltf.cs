// Copyright 2017 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

using Semantic = TiltBrushToolkit.BrushDescriptor.Semantic;
using BrushDescriptor = TiltBrushToolkit.BrushDescriptor;
using Debug = UnityEngine.Debug;

using PolyToolkit;
using System.Threading;

namespace PolyToolkitInternal {

public static class ImportGltf {
  /// <summary>
  /// Minimum sane bounding box size to perform automatic fitting to a given size.
  /// If the biggest side of the bounding box is smaller than this, we won't attempt to fit it to
  /// a given size.
  /// </summary>
  private const float MIN_BOUNDING_BOX_SIZE_FOR_SIZE_FIT = 0.001f;

  /// <summary>
  /// Minimum valid magnitude for a normal vector that's considered valid. Technically, all normals
  /// should have magnitude close to 1, but we tolerate other magnitudes. Once we go below this
  /// threshold, however, we consider the normal to be invalid and regenerate it on import.
  /// </summary>
  private const float MIN_VALID_NORMAL_SQRMAGNITUDE = float.Epsilon;  // Only fix normals that are exactly the 0 vector.

  private static readonly JsonSerializer kSerializer = new JsonSerializer {
    ContractResolver = new GltfJsonContractResolver()
  };

  const int kUnityMeshMaxVerts = 65534;
  const float kExtremeSizeMeters = 371f;

  public static Matrix4x4 GltfFromUnity { get; private set; }
  public static Matrix4x4 UnityFromGltf { get; private set; }

  static ImportGltf() {
    //         Rt   Up  Fwd
    // Unity   +x   +y   +z
    // glTF    +x   +y   -z    (or more accurately, Poly)
    Matrix4x4 gltfFromUnity = Matrix4x4.identity;
    gltfFromUnity.m22 = -1;
    GltfFromUnity = gltfFromUnity;
    UnityFromGltf = GltfFromUnity.inverse;
  }

  public class GltfImportResult {
    public GameObject root;
    public List<Mesh> meshes;
    public List<Material> materials;
    public List<Texture2D> textures;
  }

  /// <summary>State data used by the import process.</summary>
  public sealed class ImportState : IDisposable {
    // The parsed gltf; filled in by BeginImport
    internal GltfRootBase root;
    // Which gltf scene to import; filled in by BeginImport
    internal GltfSceneBase desiredScene;
    // Scale factor by which to scale the geometry.
    // Computed by BeginImport().
    internal float scaleFactor = 1.0f;
    // Scale factor by which to scale the top node.
    // Computed by BeginImport().
    internal float nodeScaleFactor = 1.0f;
    // Translation to apply to positions in the geometry.
    // This happens AFTER the scale.
    // Computed by BeginImport().
    internal Vector3 translation = Vector3.zero;

    internal ImportState() {}

    public void Dispose() {
      if (root != null) { root.Dispose(); }
    }
  }

  /// <summary>
  /// Import a gltf model.
  /// If you would like to perform some of the work off the main thread, use
  /// the referenced functions instead.
  /// </summary>
  /// <param name="gltfVersion">The glTF format version to use.</param>
  /// <param name="gltfStream">A stream containing the contents of the .gltf file. Ownership of the stream is transferred; it will be closed after the import.</param>
  /// <param name="uriLoader">For fetching relative URIs referenced by the .gltf file.</param>
  /// <param name="options">The options to import the model with.</param>
  /// <seealso cref="ImportGltf.BeginImport" />
  /// <seealso cref="ImportGltf.EndImport" />
  public static GltfImportResult Import(
      GltfSchemaVersion gltfVersion, TextReader gltfStream, IUriLoader uriLoader,
      PolyImportOptions options) {
    using (var state = BeginImport(gltfVersion, gltfStream, uriLoader, options)) {
      IEnumerable meshCreator;
      GltfImportResult result = EndImport(state, uriLoader, out meshCreator);
      foreach (var unused in meshCreator) {
        // create meshes!
      }
      return result;
    }
  }

  private static GltfRootBase DeserializeGltfRoot(GltfSchemaVersion gltfVersion, JsonTextReader reader) {
    switch (gltfVersion) {
      case GltfSchemaVersion.GLTF1: {
        var gltf1Root = kSerializer.Deserialize<Gltf1Root>(reader);
        if (gltf1Root == null || gltf1Root.nodes == null) {
          throw new Exception("Failed to parse GLTF1. File is empty or in the wrong format.");
        }

        // Some historical Tilt Brush assets use multiple meshes per node, but the importer
        // assumes single-mesh-per-node.
        PostProcessRemoveMultipleMeshes(gltf1Root);
        return gltf1Root;
      }
      case GltfSchemaVersion.GLTF2:
        var gltf2Root= kSerializer.Deserialize<Gltf2Root>(reader);
        if (gltf2Root == null || gltf2Root.nodes == null) {
          throw new Exception("Failed to parse GLTF2. File is empty or in the wrong format.");
        }
        return gltf2Root;
      default:
        throw new ArgumentException("Invalid gltfVersion" + gltfVersion);
    }
  }

  private static void SanityCheckImportOptions(PolyImportOptions options) {
    if (options.rescalingMode == PolyImportOptions.RescalingMode.CONVERT &&
        options.scaleFactor == 0.0f) {
      // If scaleFactor is exactly zero (not just close), it's PROBABLY because of user error,
      // because this is what happens when you do "new PolyImportOptions()" and forget to set
      // scaleFactor. PolyImportOptions is a struct so we can't have a default value.
      throw new Exception("scaleFactor must be != 0 for PolyImportOptions CONVERT mode. " +
          "Did you forget to set scaleFactor?");
    } else if (options.rescalingMode == PolyImportOptions.RescalingMode.FIT &&
        options.desiredSize <= 0.0f) {
      throw new Exception("desiredSize must be > 0 for PolyImportOptions FIT mode. " +
          "Did you forget to set desiredSize?");
    }

  }

  /// <summary>
  /// The portion of <seealso cref="ImportGltf.Import"/> that can be
  /// performed off the main thread. The parameters are the same as for Import.
  /// </summary>
  /// <returns>An object which should be passed to <seealso cref="ImportGltf.EndImport"/> and then disposed</returns>
  public static ImportState BeginImport(
      GltfSchemaVersion gltfVersion, TextReader stream, IUriLoader uriLoader,
      PolyImportOptions options) {

    SanityCheckImportOptions(options);

    using (var reader = new JsonTextReader(stream)) {
      var root = DeserializeGltfRoot(gltfVersion, reader);
      root.Dereference(uriLoader);

      // Extract Google-specific information.
      if (root.asset != null) {
        // Get the generator information so we can tell which app and version generated this file.
        if (root.asset.generator != null) {
          Version version;
          if (GetTiltBrushVersion(root.asset.generator, out version)) {
            root.tiltBrushVersion = version;
          } else if (GetBlocksVersion(root.asset.generator, out version)) {
            root.blocksVersion = version;
          }
        }

        // Allow the glTF to define the limit of PT's data forward-compatibility
        if (root.asset.extras != null) {
          string requiredPtVersion;
          root.asset.extras.TryGetValue("requiredPolyToolkitVersion", out requiredPtVersion);
          if (requiredPtVersion != null) {
            Match match = Regex.Match(requiredPtVersion, @"^([0-9]+)\.([0-9]+)");
            if (match.Success) {
              var required = new Version {
                major = int.Parse(match.Groups[1].Value),
                minor = int.Parse(match.Groups[2].Value)
              };
              if (required > PtSettings.Version) {
                Debug.LogWarningFormat(
                    "This file requires Poly Toolkit {0}; you are currently using {1}",
                    required, PtSettings.Version);
              }
            }
          }
        }
      }

      var state = new ImportState() {
        root = root,
        desiredScene = root.ScenePtr,
      };

      Bounds? sceneBoundsInGltfSpace;
      sceneBoundsInGltfSpace = ComputeSceneBoundingBoxInGltfSpace(root.ScenePtr, approximate:false);
      if (sceneBoundsInGltfSpace != null) {
        // Figure out what scale factor to apply.
        ComputeScaleFactor(options, sceneBoundsInGltfSpace.Value,
                           out state.scaleFactor, out state.nodeScaleFactor);
        if (options.recenter) {
          // The bounding box is calculated in glTF space, so first calculate the translation
          // in glTF space.
          var translationInGltfSpace = -sceneBoundsInGltfSpace.Value.center * state.scaleFactor;
          // Now convert the translation from glTF space to Unity space.
          state.translation = UnityFromGltf * translationInGltfSpace;
        }
      }

      if (uriLoader.CanLoadImages()) {
        foreach (var image in GltfMaterialConverter
                 .NecessaryTextures(root)
                 .Select(tex => tex.SourcePtr)
                 .Where(image => image != null)) {
          // Absolute URIs should now never get through; they will only confuse the uri loader
          if (image.uri.Contains("://")) {
            Debug.LogWarningFormat("Incorrectly considered image {0} as necessary", image.uri);
            continue;
          }
          image.data = uriLoader.LoadAsImage(image.uri);
        }
      }
      CreateMeshPrecursorsFromScene(state);
      return state;
    }
  }

  // Returns a factor to apply directly to geometry, and a factor to apply to the top node.
  // The latter will be non-unit only in extreme circumstances, when we detect that
  // the dimensions of the geometry are so extreme that we need to artificially scale
  // the object's geometry to keep within Unity's modelview scale limits, and as a
  // result have to compensate for the scale at the top node.
  static void ComputeScaleFactor(
      PolyImportOptions options, Bounds boundsInGltfSpace,
      out float directScaleFactor, out float nodeScaleFactor) {
    switch (options.rescalingMode) {
      case PolyImportOptions.RescalingMode.CONVERT:
        directScaleFactor = options.scaleFactor / PtSettings.Instance.sceneUnit.ToMeters();
        nodeScaleFactor = 1;
        float requiredShrink;
        if (ComputeScaleFactorToFit(boundsInGltfSpace, kExtremeSizeMeters, out requiredShrink)) {
          if (requiredShrink < 1) {
            // Object is quite large. Push some extra shrink into the vert data, and compensate
            // by adding some grow to the node. The assumption is that the user will likely try
            // to shrink the object; the extra grow on the node helps keep the modelview scale
            // from getting smaller than Unity's limit of ~6e-5.
            directScaleFactor *= requiredShrink;
            nodeScaleFactor /= requiredShrink;
          } else {
            // Object is smaller than the extreme limit -- no need for any worries.
          }
        }
        return;
      case PolyImportOptions.RescalingMode.FIT:
        // User wants a specific size, so derive it from the bounding box.
        if (!ComputeScaleFactorToFit(boundsInGltfSpace, options.desiredSize, out directScaleFactor)) {
          Debug.LogWarningFormat("Could not automatically resize object; object is too small or empty.");
          directScaleFactor = nodeScaleFactor = 1;
          return;
        }
        nodeScaleFactor = 1;
        return;
      default:
        throw new Exception("Invalid rescaling mode: " + options.rescalingMode);
    }
  }

  static bool GetTiltBrushVersion(string generatorString, out Version version) {
    Match match = Regex.Match(generatorString, @"^Tilt Brush ([0-9]+)\.([0-9]+)");
    if (match.Success) {
      version = new Version {
        major = int.Parse(match.Groups[1].Value),
        minor = int.Parse(match.Groups[2].Value)
      };
      return true;
    }
    version = new Version { major = 0, minor = 0 };
    return false;
  }

  static bool GetBlocksVersion(string generatorString, out Version version) {
    // Some Blocks version have a version number, some of them don't, due to a bug.
    // But they all start with "Blocks".
    Match match = Regex.Match(generatorString, @"^Blocks ([0-9]+)\.([0-9]+)");
    if (match.Success) {
      version = new Version {
        major = int.Parse(match.Groups[1].Value),
        minor = int.Parse(match.Groups[2].Value),
      };
      return true;
    }
    if (generatorString.StartsWith("Blocks")) {
      // "Blocks" without a version number was generated by old versions of Blocks,
      // and can also be generated by debug builds. For these cases, assume the baseline
      // version of blocks.
      version = new Version { major = 1, minor = 0 };
      return true;
    }
    version = new Version { major = 0, minor = 0 };
    return false;
  }


  /// Converts gltf data to MeshPrecursors
  /// This is mildly compute-intenstive, but can happen off the main thread
  static void CreateMeshPrecursorsFromScene(ImportState state) {
    foreach (var node in state.desiredScene.Nodes) {
      CreateMeshPrecursorsFromNode(state, node);
    }
  }

  static void CreateMeshPrecursorsFromNode(ImportState state, GltfNodeBase node) {
    if (node.Mesh != null) {
      CreateMeshPrecursorsFromMesh(state, node.Mesh);
    }

    foreach (var childNode in node.Children) {
      CreateMeshPrecursorsFromNode(state, childNode);
    }
  }

  static void CreateMeshPrecursorsFromMesh(ImportState state, GltfMeshBase mesh) {
    foreach (var prim in mesh.Primitives) {
      if (prim.precursorMeshes == null) {
        prim.precursorMeshes = CreateMeshPrecursorsFromPrimitive(state, prim);
      }
    }
  }

  /// <summary>
  /// Computes the scale factor to make the given bounds fit in a bounding cube whose side
  /// measures <c>desiredSize</c>.
  /// </summary>
  /// <param name="bounds">The bounds.</param>
  /// <param name="desiredSize">The desired size (bounding cube).</param>
  /// <param name="scaleFactor">The resulting scaling factor.</param>
  /// <returns>True if the factor was computed successfully (in which case it is returned in
  /// <c>scaleFactor</c>), false if it failed (in which case <c>scaleFactor</c> is undefined).</returns>
  static bool ComputeScaleFactorToFit(Bounds bounds, float desiredSize, out float scaleFactor) {
    float biggestSide = Math.Max(bounds.size.x, Math.Max(bounds.size.y, bounds.size.z));
    if (biggestSide < MIN_BOUNDING_BOX_SIZE_FOR_SIZE_FIT) {
      scaleFactor = 1.0f;
      return false;
    }
    scaleFactor = desiredSize / biggestSide;
    return true;
  }

  /// <summary>
  /// Converts the result of <seealso cref="ImportGltf.BeginImport" /> to usable
  /// Unity objects. This must be run on the main Unity thread. For large models this can result
  /// in a lot of Unity meshes being created, so an enumerable is returned that does the actual
  /// work of creating unity meshes. This can be pumped all at once, or a few times per frame if
  /// it is desirable to keep the time taken per frame to a minimum.
  /// </summary>
  /// <param name="state">Returned by BeginImport</param>
  /// <param name="uriLoader">For fetching relative URIs referenced by the .gltf file.</param>
  /// <param name="meshCreator">out reference to an enumerable that should be enumerated
  /// to create the unity meshes.</param>
  static public GltfImportResult EndImport(
      ImportState state, IUriLoader uriLoader, out IEnumerable meshCreator) {
    var result = new GltfImportResult {
      root = new GameObject("PolyImport"),
      meshes = new List<Mesh>(),
    };
    meshCreator = CreateGameObjectsFromNodes(state, result, uriLoader);
    return result;
  }


  static IEnumerable CreateGameObjectsFromNodes(
      ImportState state, GltfImportResult result, IUriLoader uriLoader) {
    var loaded = new List<Texture2D>();
    foreach (var unused in GltfMaterialConverter.LoadTexturesCoroutine(
                 state.root, uriLoader, loaded)) {
      yield return null;
    }
    result.textures = loaded;

    GltfMaterialConverter matConverter = new GltfMaterialConverter();
    var rootTransform = result.root.transform;
    Debug.Assert(rootTransform.childCount == 0);
    foreach (var node in state.desiredScene.Nodes) {
      foreach (var unused in
        CreateGameObjectsFromNode(state, rootTransform, node, result, matConverter, state.translation)) {
        yield return null;
      }
    }
    foreach (Transform child in rootTransform) {
      // XXX: do this here instead? 
      // child.localPosition += state.translation;
      child.localScale = child.localScale * state.nodeScaleFactor;
    }
    result.materials = matConverter.GetGeneratedMaterials();
  }

  /// Creates a GameObject tree from node and attaches it to parent.
  /// <param name="translationToApply">Translation to apply to the created node. This translation
  /// is NOT applied recursively to child nodes, it's only applied to the top node.</param>
  static IEnumerable CreateGameObjectsFromNode(
      ImportState state, Transform parent, GltfNodeBase node, GltfImportResult result,
      GltfMaterialConverter matConverter, Vector3 translationToApply) {
    if (node.Mesh == null && !node.Children.Any()) {
      yield break;
    }

    GameObject obj = new GameObject(node.name);
    if (parent != null) {
      obj.transform.SetParent(parent, false);
    }

    if (node.matrix != null) {
      Matrix4x4 unityMatrix = ChangeBasisAndApplyScale(node.matrix.Value, state.scaleFactor);
      Vector3 translation, scale;
      Quaternion rotation;
      MathUtils.DecomposeToTrs(unityMatrix, out translation, out rotation, out scale);
      obj.transform.localPosition = translation;
      obj.transform.localRotation = rotation;
      obj.transform.localScale = scale;
    } else {
      // Default to identity. (obj.localTransform is already identity)
    }

    // Maybe better to have caller apply it?
    obj.transform.localPosition += translationToApply;

    if (node.Mesh != null) {
      foreach(var unused in CreateGameObjectsFromMesh(
          obj.transform, node.Mesh, result, matConverter, allowMeshInParent: true)) {
        yield return null;
      }
    }

    foreach (var childNode in node.Children) {
      foreach (var unused in
               CreateGameObjectsFromNode(state, obj.transform, childNode, result, matConverter, Vector3.zero)) {
        yield return null;
      }
    }
  }

  static IEnumerable CreateGameObjectsFromMesh(
      Transform parent, GltfMeshBase mesh, GltfImportResult result,
      GltfMaterialConverter matConverter, bool allowMeshInParent) {
    // A mesh may have more than one primitive, although in the common case
    // there will be only one. Each needs its own GameObject, because the primitives
    // may have different materials.

    // For more discussion on multiple-mesh vs multiple-primitive,
    // see https://github.com/KhronosGroup/glTF/issues/821
    // and regarding creating an extra node level for primitives and meshes
    // https://github.com/KhronosGroup/glTF/issues/1065
    for (int iP = 0; iP < mesh.PrimitiveCount; ++iP) {
      GltfPrimitiveBase prim = mesh.GetPrimitiveAt(iP);
      GltfMaterialConverter.UnityMaterial? unityMaterial = matConverter.GetMaterial(prim.MaterialPtr);

      if (prim.precursorMeshes == null) {
        continue;
      }

      string primName = (unityMaterial == null) ? "unknown" : unityMaterial.Value.material.name;
      if (mesh.PrimitiveCount > 1) {
        primName += string.Format("_p{0}", iP);
      }

      if (prim.unityMeshes == null) {
        prim.unityMeshes = new List<Mesh>();
        for (int iM = 0; iM < prim.precursorMeshes.Count; ++iM) {
          string meshName = primName;
          if (prim.precursorMeshes.Count > 1) {
            meshName += string.Format("_m{0}", iM);
          }
          Mesh umesh = UnityFromPrecursor(prim.precursorMeshes[iM]);
          umesh.name = meshName;
          prim.unityMeshes.Add(umesh);
          result.meshes.Add(umesh);
          yield return null;
        }
      }

      // Unity meshes may not have > 2^16 verts. Having to break the
      // prim up into multiple meshes is unexpected, since Blocks and TB break
      // up their geometry into chunks much smaller than the Unity limit, and since
      // the exporter they use only supports 16-bit indices
      foreach (var unityMesh in prim.unityMeshes) {
        GameObject obj;
        if (allowMeshInParent && mesh.PrimitiveCount == 1 && prim.unityMeshes.Count == 1) {
          obj = parent.gameObject;
        } else {
          // If more than one, put them all in as children
          obj = new GameObject(unityMesh.name);
          obj.transform.SetParent(parent, false);
        }

        MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
        if (unityMaterial != null) {
          renderer.sharedMaterial = unityMaterial.Value.material;
        }

        MeshFilter filter = obj.AddComponent<MeshFilter>();
        filter.sharedMesh = unityMesh;
      }
    }
  }

  static Mesh UnityFromPrecursor(MeshPrecursor precursor) {
    var mesh = new Mesh();

    mesh.vertices = precursor.vertices;
    if (precursor.normals != null)  { mesh.normals = precursor.normals;   }
    if (precursor.colors != null)   { mesh.colors = precursor.colors;     }
    if (precursor.tangents != null) { mesh.tangents = precursor.tangents; }
    for (int i = 0; i < precursor.uvSets.Length; ++i) {
      if (precursor.uvSets[i] != null) {
        GenericSetUv(mesh, i, precursor.uvSets[i]);
      }
    }
    mesh.triangles = precursor.triangles;

    if (precursor.normals == null) {
      mesh.RecalculateNormals();
    }
    return mesh;
  }

  struct MeshSubset {
    public IntRange vertices;
    public IntRange triangles;
  }

  // Breaks a mesh up into contiguous ranges of triangles and vertices such that:
  // - no range of vertices has size greater than maxSubsetVerts.
  // - triangle ranges are disjoint
  // - triangle ranges together cover the entire range of triangles[]
  //
  // This decomposition is only possible if the topology satisfies certain
  // assumptions (commented inline). It will work for Tilt Brush topology,
  // but is definitely not suitable for arbitrary meshes.
  static IEnumerable<MeshSubset> GenerateMeshSubsets(
      ushort[] triangles, int numVerts, int maxSubsetVerts = kUnityMeshMaxVerts) {

    // Early out if there's no split -- saves time figuring out which verts are used.
    if (numVerts <= maxSubsetVerts) {
      yield return new MeshSubset {
        vertices = new IntRange { min = 0, max = numVerts },
        triangles = new IntRange { min = 0, max = triangles.Length }
      };
      yield break;
    }

    int count = triangles.Length;

    IntRange? vertsUsed = null;
    IntRange? trisUsed = null;

    for (int iTri = 0; iTri < count; /* manual loop advance */) {
      // Assumption #1: triVerts.Size << maxSubsetVerts
      IntRange triVerts; {
        int t0 = triangles[iTri];
        int t1 = triangles[iTri + 1];
        int t2 = triangles[iTri + 2];
        triVerts = new IntRange {
          min = Mathf.Min(t0, t1, t2),
          max = Mathf.Max(t0, t1, t2) + 1
        };
      }

      IntRange newVertsUsed = IntRange.Union(vertsUsed, triVerts);
      // Simplifying assumption #2: indices in triangles[] are mostly monotonic
      IntRange newTrisUsed = IntRange.Union(trisUsed, new IntRange { min=iTri, max=iTri+3 });

      // If tri doesn't fit, emit a subset and re-attempt the triangle.
      if (newVertsUsed.Size > maxSubsetVerts) {
        if (vertsUsed == null) {
          Debug.LogWarningFormat("No forward progress");
          yield break;
        }
        yield return new MeshSubset {
          vertices = vertsUsed.Value,
          triangles = trisUsed.Value
        };

        vertsUsed = null;
        trisUsed = null;
        // Did not consume a triangle; do not advance iTri
      } else {
        vertsUsed = newVertsUsed;
        trisUsed = newTrisUsed;
        iTri += 3;
      }
    }

    if (vertsUsed != null) {
      yield return new MeshSubset {
        vertices = vertsUsed.Value,
        triangles = trisUsed.Value
      };
    }
  }

  // Switches basis from gltf -> Unity.
  // Applies a scale change directly to the transforms + mesh data.
  static Matrix4x4 ChangeBasisAndApplyScale(Matrix4x4 gltfMatrix, float scaleFactor) {
    Matrix4x4 inTargetBasis = UnityFromGltf * gltfMatrix * GltfFromUnity;
    // The similarity transform is the canonical way to make arbitrary
    // transforms to a matrix. If you expand out the scale math, all it
    // does it modify the translation -- which makes sense, since the
    // translation is the only distance-like portion of the 4x4.
    // Doing it this way avoids multiplying then dividing the upper-left 3x3
    // by the same factor (which can introduce perturbations)
#if false
    Matrix4x4 destFromSource = Matrix4x4.Scale(Vector3.one * scaleFactor);
    Matrix4x4 sourceFromDest = Matrix4x4.Scale(Vector3.one / scaleFactor);
    return destFromSource * inTargetBasis * sourceFromDest;
#endif
    inTargetBasis[13] *= scaleFactor;
    inTargetBasis[14] *= scaleFactor;
    inTargetBasis[15] *= scaleFactor;
    return inTargetBasis;
  }

  // Switches texture origin from glTF style (upper-left) to Unity style (lower-left)
  static void ChangeUvBasis(Array data, Semantic semantic) {
    if (semantic == Semantic.XyIsUv ||
        semantic == Semantic.XyIsUvZIsDistance) {
      if (data is Vector2[]) {
        Vector2[] vData = (Vector2[])data;
        for (int i = 0; i < vData.Length; ++i) {
          var tmp = vData[i]; tmp.y = 1 - tmp.y; vData[i] = tmp;
        }
      } else if (data is Vector3[]) {
        Vector3[] vData = (Vector3[])data;
        for (int i = 0; i < vData.Length; ++i) {
          var tmp = vData[i]; tmp.y = 1 - tmp.y; vData[i] = tmp;
        }
      } else if (data is Vector4[]) {
        Vector4[] vData = (Vector4[])data;
        for (int i = 0; i < vData.Length; ++i) {
          var tmp = vData[i]; tmp.y = 1 - tmp.y; vData[i] = tmp;
        }
      }
    }
  }

  // Switches position basis from gltf -> Unity.
  // Applies a scale change directly to the transforms + mesh data.
  static void ChangeBasisAndApplyScale(Array data, Semantic semantic, float scaleFactor) {
    Matrix4x4 basisChange = UnityFromGltf;
    if (semantic == Semantic.Position ||
        semantic == Semantic.Vector) {
      // Assume incoming data is meters
      basisChange *= Matrix4x4.Scale(Vector3.one * scaleFactor);
    } else if (semantic == Semantic.UnitlessVector) {
      // use basisChange as-is
    } else if (semantic == Semantic.XyIsUvZIsDistance && scaleFactor != 1) {
      if (data is Vector3[]) {
        Vector3[] vData = (Vector3[])data;
        for (int i = 0; i < vData.Length; ++i) {
          var tmp = vData[i]; tmp.z *= scaleFactor; vData[i] = tmp;
        }
      } else if (data is Vector4[]) {
        Vector4[] vData = (Vector4[])data;
        for (int i = 0; i < vData.Length; ++i) {
          var tmp = vData[i]; tmp.z *= scaleFactor; vData[i] = tmp;
        }
      } else {
        Debug.LogWarningFormat("Cannot change basis of type {0}", data.GetType());
      }
    } else {
      // no basis-change or unit-change needed
      return;
    }

    if (data is Vector3[]) {
      Vector3[] vData = (Vector3[]) data;
      for (int i = 0; i < vData.Length; ++i) {
        vData[i] = basisChange.MultiplyVector(vData[i]);
      }
    } else if (data is Vector4[]) {
      Vector4[] vData = (Vector4[]) data;
      for (int i = 0; i < vData.Length; ++i) {
        vData[i] = basisChange.MultiplyVector(vData[i]);
      }
    } else {
      Debug.LogWarningFormat("Cannot change basis of type {0}", data.GetType());
    }
  }

  // Annotates GltfPrimitive with materials, precursorMeshes
  static List<MeshPrecursor> CreateMeshPrecursorsFromPrimitive(ImportState state, GltfPrimitiveBase prim) {
    if (prim.mode != GltfPrimitiveBase.Mode.TRIANGLES) {
      Debug.LogWarningFormat("Cannot create mesh from {0}", prim.mode);
      return null;
    }

    BrushDescriptor desc = GltfMaterialConverter.LookupBrushDescriptor(prim.MaterialPtr);

    ushort[] triangles; {
      GltfAccessorBase accessor = prim.IndicesPtr;
      IntRange range = new IntRange { max = accessor.count };
      triangles = (ushort[])GetDataAsArray(accessor, range, null);
    }

    int numVerts = prim.GetAttributePtr("POSITION").count;

    List<MeshPrecursor> meshes = new List<MeshPrecursor>();

    // Tilt Brush particle meshes are sensitive to being broken up.
    // The verts may shift, but the shader requires that (vertexId % 4) not change.
    int maxSubsetVerts = kUnityMeshMaxVerts - 4;
    foreach (var readonlySubset in GenerateMeshSubsets(triangles, numVerts, maxSubsetVerts)) {
      var mesh = new MeshPrecursor();

      // Protect the above invariant. (The copy here is required by C#)
      var subset = readonlySubset;
      subset.vertices.min -= (subset.vertices.min % 4);

      if (desc != null) {
        if (desc.m_bFbxExportNormalAsTexcoord1) {
          // make the gltf look like what the fbx shaders expect
          // normals moved to texcoord1
          prim.ReplaceAttribute("NORMAL", "TEXCOORD_1");
        }
      }

      // Vertex data
      HashSet<string> attributeNames = prim.GetAttributeNames();
      foreach (var semantic in attributeNames) {
        GltfAccessorBase accessor = prim.GetAttributePtr(semantic);
        IntRange attribRange = subset.vertices;

        // Note: some GLTF files incorrectly[?] have accessors with shorter counts than the vertex
        // count. In these cases, be lenient and ignore the missing data.
        if (attribRange.min >= accessor.count) {
          // No data at all for this accessor (entirely out of range).
          // This is a verbose log, not a warning, because it's accepted as "correct".
          PtDebug.LogVerboseFormat("Attribute {0} has no data: wanted range {1}, count was {2}",
              semantic, attribRange, accessor.count);
          // Ignore this attribute.
          continue;
        } else if (attribRange.max > accessor.count) {
          // Attribute is present, but is missing some data.
          // This is a data error, so print a warning.
          Debug.LogWarningFormat("Attribute {0} is missing data: wanted range {1}, count was {2}",
              semantic, attribRange, accessor.count);
          // Cap the read range to what's available.
          attribRange.max = accessor.count;
        }
        // PadArrayToSize() guarantees that, no matter what, the data array will have enough data to cover
        // all the vertices, even if (due to the problems described above) the accessor is missing
        // some elements. The missing elements will be initialized to zero.
        Array data = PadArrayToSize(GetDataAsArray(accessor, attribRange, semantic), subset.vertices.Size);
        switch (semantic) {
        case "POSITION":
          ChangeBasisAndApplyScale(data, Semantic.Position, state.scaleFactor);
          mesh.vertices = (Vector3[]) data;
          break;
        case "NORMAL":
          ChangeBasisAndApplyScale(data, Semantic.UnitlessVector, state.scaleFactor);
          mesh.normals = (Vector3[]) data;
          break;
        case "COLOR": 
        case "COLOR_0": {
          Color[] colors = data as Color[];
          if (colors == null) {
            Debug.LogWarningFormat(
                "Unsupported: color buffer of type {0}", 
                data == null ? "null" : data.GetType().ToString());
            break;
          }

          var desiredSpace = GetDesiredColorSpace(state.root);
          var actualSpace = GetActualColorSpace(state.root);
          if (actualSpace == ColorSpace.Unknown) {
            Debug.LogWarning("Reading color buffer in unknown color space");
            // Guess at something, so we at least offer consistent results.
            // sRGB is the most likely.
            actualSpace = ColorSpace.Srgb;
          }

          if (desiredSpace == ColorSpace.Srgb && actualSpace == ColorSpace.Linear) {
            for (int i = 0; i < colors.Length; ++i) {
              colors[i] = colors[i].gamma;
            }
          } else if (desiredSpace == ColorSpace.Linear && actualSpace == ColorSpace.Srgb) {
            for (int i = 0; i < colors.Length; ++i) {
              colors[i] = colors[i].linear;
            }
          }

          mesh.colors = colors;
          break;
        }
        case "TANGENT":
          ChangeBasisAndApplyScale(data, Semantic.UnitlessVector, state.scaleFactor);
          mesh.tangents = (Vector4[]) data;
          break;
        case "TEXCOORD_0": {
          var ptSemantic = GetTexcoordSemantic(state, accessor, desc, 0);
          ChangeBasisAndApplyScale(data, ptSemantic, state.scaleFactor);
          ChangeUvBasis(data, ptSemantic);
          mesh.uvSets[0] = data;
          break;
        }
        case "TEXCOORD_1": {
          var ptSemantic = GetTexcoordSemantic(state, accessor, desc, 1);
          ChangeBasisAndApplyScale(data, ptSemantic, state.scaleFactor);
          ChangeUvBasis(data, ptSemantic);
          mesh.uvSets[1] = data;
          break;
        }
        case "VERTEXID":
          // This was an attempt to get vertex id in webgl, but it didn't work out.
          // The data is not fully hooked-up in the gltf, and it doesn't make its way to THREE.
          // So: ignore it.
          break;
        default:
          Debug.LogWarningFormat("Unhandled attribute {0}", semantic);
          break;
        }
      }

      {
        int[] triangleSubset = new int[subset.triangles.Size];
        int triangleSubsetLength = triangleSubset.Length;
        Debug.Assert(triangleSubsetLength % 3 == 0);
        // Re-index verts. Also reverse winding since the basis-change matrix has a mirroring.
        // Also change from ushort -> int
        for (int i = 0; i < triangleSubsetLength; i += 3) {
          triangleSubset[i    ] = triangles[subset.triangles.min + i    ] - subset.vertices.min;
          triangleSubset[i + 1] = triangles[subset.triangles.min + i + 2] - subset.vertices.min;
          triangleSubset[i + 2] = triangles[subset.triangles.min + i + 1] - subset.vertices.min;
        }
        mesh.triangles = triangleSubset;
      }

      meshes.Add(mesh);
    }
    foreach (MeshPrecursor mesh in meshes) {
      FixInvalidNormals(mesh);
    }
    return meshes;
  }

  /// <summary>
  /// Pads the given array with the array type's default value so that it's at least
  /// the given size.
  /// </summary>
  /// <param name="array">The array to pad.</param>
  /// <param name="size">The desired size</param>
  private static Array PadArrayToSize(Array array, int size) {
    if (array.Length >= size) return array;
    Array paddedArray = Array.CreateInstance(array.GetType().GetElementType(), size);
    Array.Copy(array, paddedArray, array.Length);
    return paddedArray;
  }

  /// <summary>
  /// Detects and fixes any invalid normals in the given mesh.
  /// Also handles the case where the normals buffer is not the correct length.
  /// </summary>
  private static void FixInvalidNormals(MeshPrecursor mesh) {
    if (mesh.normals == null) {
      // No normals! We have to generate all of them -- but let Unity do it in C++
      return;
    } else if (mesh.normals.Length < mesh.vertices.Length) {
      // Weird case where we don't have normals for all vertices.
      // Leave null so they get regenerated later in C++
      Debug.LogWarning("Unexpected: not enough data in mesh.normals. Regenerating.");
      mesh.normals = null;
      return;
    }

    List<int> invalidNormalIndices = new List<int>();
    for (int i = 0; i < mesh.normals.Length; i++) {
      if (mesh.normals[i].sqrMagnitude < MIN_VALID_NORMAL_SQRMAGNITUDE) {
        invalidNormalIndices.Add(i);
      }
    }

    // If we didn't find any invalid normals, stop here. No more work needed.
    if (invalidNormalIndices.Count == 0) return;

    // For each vertex index i, vertexToTriangle[i] is the index in mesh.triangles
    // where one (arbitrarily chosen, if there's more than one) triangle that contains
    // the vertex is located.
    Dictionary<int,int> vertexToTriangle = new Dictionary<int, int>();

    // Build the map from vertex to containing triangles.
    for (int i = 0; i < mesh.triangles.Length; i++) {
      // Every triplet is a triangle so if we're at index i, the triangle starts at index i - i % 3.
      vertexToTriangle[mesh.triangles[i]] = i - i % 3;
    }

    for (int j = 0; j < invalidNormalIndices.Count; j++) {
      int i = invalidNormalIndices[j];
      // Replace this normal by an automatically computed triangle normal.
      // It might not be right, but it's better than an invalid one.
      int triStart;
      if (!vertexToTriangle.TryGetValue(i, out triStart)) {
        // This vertex doesn't belong to any triangles, which is weird, but in this case
        // we don't have to fix the normal since the vertex doesn't render.
        continue;
      }
      // The triangle to which the vertex belongs is given by the indices triStart, triStart + 1
      // and triStart + 2 in mesh.triangles. So we just have to get the corresponding vertices
      // and calculate the normal:
      mesh.normals[i] = MathUtils.CalculateNormal(
        mesh.vertices[mesh.triangles[triStart]],
        mesh.vertices[mesh.triangles[triStart + 1]],
        mesh.vertices[mesh.triangles[triStart + 2]]);
    }
  }

  // Returns a Semantic which tells us how to manipulate the uv to convert it
  // from glTF conventions to Unity conventions.
  static Semantic GetTexcoordSemantic(
      ImportState state, GltfAccessorBase accessor, BrushDescriptor desc, int uvChannel) {
    // GL and Unity use the convention "texture origin is lower-left"
    // glTF, DX, Metal, and modern APIs use the convention "texture origin is upper-left"
    // We want to match the logic used by the exporter which generated this gltf, down to its bugs.
    // If we don't know which exporter it was, assume it was written correctly.
    if (state.root.tiltBrushVersion == null) {
      // Not Tilt Brush, so we can sort-of safely assume texcoord.xy is a UV coordinate.
      return Semantic.XyIsUv;
    } else {
      // Tilt Brush doesn't use "correct" logic (flip y of every texcoord) because that
      // fails if any importers choose the strategy "flip texture" rather than "flip texcoord.y".
      // Thus it needs to be data-driven.
      if (desc == null) {
        Debug.LogWarning("Unexpected: TB geometry without descriptor");
        return Semantic.Unspecified;
      } else {
        // VERY SUBTLE incorrectness here -- TB pre-15 didn't flip y on Semantic.XyIsUvZIsDistance
        // because of an exporter bug. To be perfectly correct, if version <= 14,
        // this should convert XyIsUvZIsDistance to the (mythical) semantic ZIsDistance.
        if (uvChannel == 0) {
          return desc.m_uv0Semantic;
        } else if (uvChannel == 1) {
          return desc.m_uv1Semantic;
        } else {
          Debug.LogWarningFormat("Unexpected TB texcoord: {0}", uvChannel);
          return Semantic.Unspecified;
        }
      }
    }
  }

  /// Returns the specified range of data from an accessor.
  /// The result is a copy and is safe to mutate.
  ///
  /// semantic -
  ///   Attribute name/semantic, if the accessor is for vertex attributes.
  ///   May be null. Helps determine the return type.
  static unsafe Array GetDataAsArray(GltfAccessorBase accessor, IntRange eltRange, string semantic) {
    const Gltf1Accessor.ComponentType FLOAT = Gltf1Accessor.ComponentType.FLOAT;
    if (accessor.type == "VEC2" && accessor.componentType == FLOAT) {
      var destination = new Vector2[eltRange.Size];
      fixed (void* destPtr = destination) {
        ReadAccessorData(accessor, eltRange, sizeof(Vector2), (IntPtr)destPtr);
      }
      return destination;
    } else if (accessor.type == "VEC3" && accessor.componentType == FLOAT) {
      var destination = new Vector3[eltRange.Size];
      fixed (void* destPtr = destination) {
        ReadAccessorData(accessor, eltRange, sizeof(Vector3), (IntPtr)destPtr);
      }
      return destination;
    } else if (accessor.type == "VEC4" && accessor.componentType == FLOAT) {
      if (semantic.StartsWith("COLOR")) {
        var destination = new Color[eltRange.Size];
        fixed (void* destPtr = destination) {
          ReadAccessorData(accessor, eltRange, sizeof(Color), (IntPtr)destPtr);
        }
        return destination;
      } else {
        var destination = new Vector4[eltRange.Size];
        fixed (void* destPtr = destination) {
          ReadAccessorData(accessor, eltRange, sizeof(Vector4), (IntPtr)destPtr);
        }
        return destination;
      }
    } else if (accessor.type == "SCALAR" && accessor.componentType == FLOAT) {
      var destination = new float[eltRange.Size];
      fixed (void* destPtr = destination) {
        ReadAccessorData(accessor, eltRange, sizeof(float), (IntPtr)destPtr);
      }
      return destination;
    } else if (accessor.type == "SCALAR"
               && accessor.componentType == Gltf1Accessor.ComponentType.UNSIGNED_SHORT) {
      var destination = new ushort[eltRange.Size];
      fixed (void* destPtr = destination) {
        ReadAccessorData(accessor, eltRange, sizeof(ushort), (IntPtr)destPtr);
      }
      return destination;
    } else {
      Debug.LogWarningFormat(
          "Unknown accessor type {0} componentType {1}",
          accessor.type, accessor.componentType);
    }
    return null;
  }

  /// Reads (range.Size * elementLength) bytes from the specified point
  /// in the bufferview, into the specified pointer.
  ///
  /// eltRange      Half-open range of elements to fetch
  /// elementLength Length of a single element, in bytes
  /// destination   Pointer to copy to
  static void ReadAccessorData(
      GltfAccessorBase accessor, IntRange eltRange, int elementLength, IntPtr destination) {
    GltfBufferViewBase view = accessor.BufferViewPtr;

    IntRange viewRange = new IntRange {
      min = view.byteOffset,
      max = view.byteOffset + view.byteLength
    };

    IntRange readRange = new IntRange {
      min = view.byteOffset + accessor.byteOffset + eltRange.min * elementLength,
      max = view.byteOffset + accessor.byteOffset + eltRange.max * elementLength
    };

    if (IntRange.Union(viewRange, readRange) != viewRange) {
      Debug.LogErrorFormat("Read error: {0} doesn't contain {1}", viewRange, readRange);
      return;
    }

    view.BufferPtr.data.Read(destination, readRange.min, readRange.Size);
  }

  static void GenericSetUv(Mesh mesh, int channel, Array array) {
    // This is kind of terrible; Unity's API requires a List<>
    // but under the hood it converts the List<> straight back into a [].
    if (array is Vector2[]) {
      mesh.SetUVs(channel, ((Vector2[]) array).ToList());
    } else if (array is Vector3[]) {
      mesh.SetUVs(channel, ((Vector3[]) array).ToList());
    } else if (array is Vector4[]) {
      mesh.SetUVs(channel, ((Vector4[]) array).ToList());
    } else {
      Debug.LogWarningFormat("Cannot assign {0} to uv{1}", array.GetType(), channel);
    }
  }

  enum ColorSpace { Unknown, Srgb, Linear };

  /// Returns the color space expected by any shaders which consume the vertex color channel.
  static ColorSpace GetDesiredColorSpace(GltfRootBase root) {
    if (root.tiltBrushVersion != null) {
      // Tilt Brush Toolkit shaders want linear color
      return ColorSpace.Linear;
    } else if (root.blocksVersion != null) {
      // Blocks import currently borrows Tilt Brush shaders, which want linear color
      // This may change when Blocks shaders are imported.
      return ColorSpace.Linear;
    }
    return ColorSpace.Unknown;
  }

  /// Returns the color space used by the gltf's vertex color channel.
  static ColorSpace GetActualColorSpace(GltfRootBase root) {
    // There is no metadata in gltf to tell us, but we can infer it based on versions
    if (root.tiltBrushVersion != null) {
      // all known versions of Tilt Brush emit srgb colors to gltf
      return ColorSpace.Srgb;
    } else if (root.blocksVersion != null) {
      // all known versions of Blocks emit srgb colors to gltf
      return ColorSpace.Srgb;
    }
    return ColorSpace.Unknown;
  }

  /// Removes and returns a value from the list
  private static T ListPop<T>(List<T> list) {
    int idx = list.Count - 1;
    if (idx < 0) { throw new InvalidOperationException("empty"); }
    T ret = list[idx];
    list.RemoveAt(idx);
    return ret;
  }

  /// Modifies node tree to ensure that nodes contain at most one mesh
  private static void PostProcessRemoveMultipleMeshes(Gltf1Root root) {
    // Reify the dict iterator because we're going to mutate the dict
    foreach (var keyvalue in root.nodes.ToArray()) {
      Gltf1Node node = keyvalue.Value;
      if (node.meshes == null) { continue; }
      while (node.meshes.Count > 1) {
        string meshName = ListPop(node.meshes);

        string childNodeName;
        for (int i = 0; ; ++i) {
          childNodeName = string.Format("{0}_{1}", meshName, i);
          if (! root.nodes.ContainsKey(childNodeName)) { break; }
        }

        var child = new Gltf1Node {
          name = childNodeName,
          meshes = new List<string> { meshName },
        };

        root.nodes.Add(childNodeName, child);
        if (node.children == null) {
          node.children = new List<string>();
        }
        node.children.Add(child.name);
      }
    }
  }

  /// <summary>
  /// Computes and returns the given scene's bounding box (AABB).
  /// </summary>
  /// <param name="scene">The scene whose bounding box to compute.</param>
  /// <param name="approximate">Computes more quickly, but the result is not perfectly tight</param>
  /// <returns>The bounding box.</returns>
  public static Bounds? ComputeSceneBoundingBoxInGltfSpace(GltfSceneBase scene, bool approximate) {
    Bounds? bounds = null;
    Matrix4x4 matrix = Matrix4x4.identity;
    foreach (GltfNodeBase node in scene.Nodes) {
      EncapsulateNodeInBoundingBox(matrix, node, approximate, ref bounds);
    }
    return bounds;
  }

  private static void EncapsulateNodeInBoundingBox(
      Matrix4x4 rootFromParent, GltfNodeBase node, bool approximate, ref Bounds? bounds) {
    Matrix4x4 rootFromNode = node.matrix != null ? (rootFromParent * node.matrix.Value) : rootFromParent;
    if (node.Mesh != null) {
      foreach (GltfPrimitiveBase prim in node.Mesh.Primitives) {
        if (approximate) {
          EncapsulatePrimInBoundingBoxApproximate(rootFromNode, prim, ref bounds);
        } else {
          EncapsulatePrimInBoundingBoxExact(rootFromNode, prim, ref bounds);
        }
      }
    }
    foreach (GltfNodeBase child in node.Children) {
      EncapsulateNodeInBoundingBox(rootFromNode, child, approximate, ref bounds);
    }
  }

  // Returns the smallest axis-aligned bounding box which contains
  // b's corners transformed by m.
  private static Bounds Transform(Matrix4x4 m, Bounds b) {
    Vector3 halfExtent = b.extents;
    Vector3 mc = m.MultiplyPoint(b.center);
    Vector3 mx = m * new Vector4(halfExtent.x, 0, 0, 1);
    Vector3 my = m * new Vector4(0, halfExtent.y, 0, 1);
    Vector3 mz = m * new Vector4(0, 0, halfExtent.z, 1);
    Bounds mb = new Bounds(center: mc, size: Vector3.zero);
    mb.Encapsulate(mc + mx + my + mz);
    mb.Encapsulate(mc + mx + my - mz);
    mb.Encapsulate(mc + mx - my + mz);
    mb.Encapsulate(mc + mx - my - mz);
    mb.Encapsulate(mc - mx + my + mz);
    mb.Encapsulate(mc - mx + my - mz);
    mb.Encapsulate(mc - mx - my + mz);
    mb.Encapsulate(mc - mx - my - mz);
    return mb;
  }

  private static void EncapsulatePrimInBoundingBoxApproximate(
      Matrix4x4 rootFromNode, GltfPrimitiveBase prim, ref Bounds? bounds) {
    GltfAccessorBase pos = prim.GetAttributePtr("POSITION");
    if (pos.componentType != GltfAccessorBase.ComponentType.FLOAT) { return; }
    if (pos.type != "VEC3") { return; }

    Bounds boundsInNodeSpace = new Bounds();
    if (pos.min != null && pos.min.Count == 3) {
      boundsInNodeSpace.min = new Vector3(pos.min[0], pos.min[1], pos.min[2]);
    }
    if (pos.max != null && pos.max.Count == 3) {
      boundsInNodeSpace.max = new Vector3(pos.max[0], pos.max[1], pos.max[2]);
    }
    Bounds boundsInRootSpace = Transform(rootFromNode, boundsInNodeSpace);
    if (bounds != null) {
      boundsInRootSpace.Encapsulate(bounds.Value.min);
      boundsInRootSpace.Encapsulate(bounds.Value.max);
    }
    bounds = boundsInRootSpace;
  }

  private static void EncapsulatePrimInBoundingBoxExact(
      Matrix4x4 rootFromNode, GltfPrimitiveBase prim, ref Bounds? bounds) {
    GltfAccessorBase vertexAccessor = prim.GetAttributePtr("POSITION");

    IntRange vertRange = new IntRange();
    vertRange.min = 0;
    vertRange.max = vertexAccessor.count - 1;
    Array data = GetDataAsArray(vertexAccessor, vertRange, null);
    if (!(data is Vector3[])) {
      throw new Exception("Vertex accessor didn't convert to Vector3[]. We got: " + data.GetType());
    }
    Vector3[] vertices = (Vector3[]) data;
    foreach (Vector3 vertex in vertices) {
      Vector3 transformed = rootFromNode * vertex;
      if (bounds != null) {
        Bounds newBounds = bounds.Value;
        newBounds.Encapsulate(transformed);
        bounds = newBounds;
      } else {
        bounds = new Bounds(transformed, Vector3.zero);
      }
    }
  }
}

}
