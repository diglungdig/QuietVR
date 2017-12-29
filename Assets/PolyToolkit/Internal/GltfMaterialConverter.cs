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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TiltBrushToolkit;
using UnityEngine;

namespace PolyToolkitInternal {

public class GltfMaterialConverter {
  private static readonly Regex kTiltBrushMaterialRegex = new Regex(
      @".*([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})$");
  private static readonly Regex kTiltBrushShaderRegex = new Regex(
      @".*([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})/");

  /// <summary>
  /// Information about a Unity material generated from a Gltf node.
  /// </summary>
  public struct UnityMaterial {
    /// <summary>
    /// The material to be used in place of the GltfMaterial
    /// </summary>
    public Material material;
    /// <summary>
    /// The material that "material" is based on. This might be the same as
    /// "material", if no customizations were needed.
    /// </summary>
    public Material template;
  }

  /// <summary>
  /// List of NEW Unity materials we have created.
  /// </summary>
  private List<Material> newMaterials = new List<Material>();

  /// <summary>
  /// Cache of Unity materials we have already created for each GltfMaterialBase.
  /// </summary>
  private Dictionary<GltfMaterialBase, UnityMaterial> materials = new Dictionary<GltfMaterialBase, UnityMaterial>();

  private static bool IsTiltBrushHostedUri(string uri) {
    // Will always look like "https://www.tiltbrush.com/shaders/..."
    if (uri.Contains("://")) { return true; }
    return false;
  }

  /// <summary>
  /// Enumerates those Textures associated with local materials, as distinguished
  /// from well-known, global materials like BlocksPaper and Tilt Brush Light.
  /// Textures associated with those latter materials will not be enumerated.
  ///
  /// These are the textures that need UnityEngine.Textures created for them.
  /// </summary>
  public static IEnumerable<GltfTextureBase> NecessaryTextures(GltfRootBase root) {
    foreach (var mat in root.Materials) {
      if (! IsGlobalMaterial(mat)) {
        foreach (var tex in mat.ReferencedTextures) {
          yield return tex;
        }
      }
    }
  }

  /// <summary>
  /// Converts "Necessary" textures textures found in the gltf file.
  /// Coroutine must be fully consumed before generating materials.
  /// </summary>
  /// <seealso cref="GltfMaterialConverter.NecessaryTextures" />
  /// <param name="root">The root of the GLTF file.</param>
  /// <param name="loader">The loader to use to load resources (textures, etc).</param>
  /// <param name="loaded">Mutated to add any textures that were loaded.</param>
  public static IEnumerable LoadTexturesCoroutine(
      GltfRootBase root, IUriLoader loader, List<Texture2D> loaded) {
    foreach (GltfTextureBase gltfTexture in NecessaryTextures(root)) {
      if (IsTiltBrushHostedUri(gltfTexture.SourcePtr.uri)) {
        Debug.LogWarningFormat("Texture {0} uri {1} was considered necessary",
                               gltfTexture.GltfId, gltfTexture.SourcePtr.uri);
        continue;
      }
      foreach (var unused in ConvertTextureCoroutine(gltfTexture, loader)) {
        yield return null;
      }
      if (gltfTexture.unityTexture != null) {
        loaded.Add(gltfTexture.unityTexture);
      }
    }

    // After textures are converted, we don't need the cached RawImage data any more.
    // "Deallocate" it.
    foreach (GltfImageBase image in root.Images) {
      image.data = null;
    }
  }

  /// <summary>
  /// Gets (or creates) the Unity material corresponding to the given GLTF 2 material.
  /// </summary>
  /// <param name="gltfMaterial">The GLTF material.</param>
  /// <returns>The Unity material that correpsonds to the given GLTF2 material.</returns>
  public UnityMaterial? GetMaterial(GltfMaterialBase gltfMaterial) {
    // Have we already converted this material?
    {
      UnityMaterial result;
      if (materials.TryGetValue(gltfMaterial, out result)) {
        return result;
      }
    }

    // Try to look up a global material first.
    Material global;
    if (null != (global = LookUpGlobalMaterial(gltfMaterial))) {
      // Found it.
      var result = new UnityMaterial { material = global, template = global };
      materials[gltfMaterial] = result;
      return result;
    }

    // Ok, we will have to create a new material.
    UnityMaterial? created = ConvertGltfMaterial(gltfMaterial);
    if (created == null) {
      Debug.LogErrorFormat("Failed to look up material {0}", gltfMaterial.name);
    } else {
      var result = created.Value;
      materials[gltfMaterial] = result;
      Debug.Assert(result.material != result.template);
      if (result.material != result.template) {
        newMaterials.Add(result.material);
      }
    }

    return created;
  }

  /// <summary>
  /// Returns a list of new materials that were created as part of the material
  /// conversion process.
  /// </summary>
  public List<Material> GetGeneratedMaterials() {
    return new List<Material>(newMaterials);
  }

  /// <returns>true if there is a global material corresponding to the given glTF material,
  /// false if a material needs to be created for this material.</returns>
  private static bool IsGlobalMaterial(GltfMaterialBase gltfMaterial) {
    // Simple implementation for now
    return LookUpGlobalMaterial(gltfMaterial) != null;
  }

  /// <summary>
  /// Looks up a built-in global material that corresponds to the given GLTF material.
  /// This will NOT create new materials, it will only look up global ones.
  /// </summary>
  /// <param name="gltfMaterial">The material to look up.</param>
  /// <param name="materialGuid">The guid parsed from the material name, or Guid.None</param>
  /// <returns>The global material that corresponds to the given GLTF material,
  /// if found. If not found, null.</returns>
  private static Material LookUpGlobalMaterial(GltfMaterialBase gltfMaterial) {
    // Is this a Blocks gvrss material?
    if (gltfMaterial.TechniqueExtras != null) {
      string surfaceShader = null;
      gltfMaterial.TechniqueExtras.TryGetValue("gvrss", out surfaceShader);

      if (surfaceShader != null) {
        // Blocks material. Look up the mapping in PtSettings.
        Material material = PtSettings.Instance.LookupSurfaceShaderMaterial(surfaceShader);
        if (material != null) {
          return material;
        } else {
          Debug.LogWarningFormat("Unknown gvrss surface shader {0}", surfaceShader);
        }
      }
    }

    // Check if it's a Tilt Brush material.
    Guid guid = ParseGuidFromMaterial(gltfMaterial);
    if (guid != Guid.Empty) {
      // Tilt Brush global material. PBR materials will use unrecognized guids;
      // these will be handled by the caller.
      BrushDescriptor desc;
      if (PtSettings.Instance.brushManifest.BrushesByGuid.TryGetValue(guid, out desc)) {
        return desc.Material;
      }
    }
    return null;
  }

  private UnityMaterial? ConvertGltfMaterial(GltfMaterialBase gltfMat) {
    if (gltfMat is Gltf1Material) {
      return ConvertGltf1Material((Gltf1Material)gltfMat);
    } else if (gltfMat is Gltf2Material) {
      return ConvertGltf2Material((Gltf2Material)gltfMat);
    } else {
      Debug.LogErrorFormat("Unexpected type: {0}", gltfMat.GetType());
      return null;
    }
  }

  /// <summary>
  /// Converts the given glTF1 material to a new Unity material.
  /// This is only possible if the passed material is a Tilt Brush "PBR" material
  /// squeezed into glTF1.
  /// </summary>
  /// <param name="gltfMat">The glTF1 material to convert.</param>
  /// <returns>The result of the conversion, or null on failure.</returns>
  private UnityMaterial? ConvertGltf1Material(Gltf1Material gltfMat) {
    Guid instanceGuid = ParseGuidFromMaterial(gltfMat);
    Guid templateGuid = ParseGuidFromShader(gltfMat);

    BrushDescriptor desc;
    if (!PtSettings.Instance.brushManifest.BrushesByGuid.TryGetValue(templateGuid, out desc)) {
      // If they are the same, there is no template/instance relationship.
      if (instanceGuid != templateGuid) {
        Debug.LogErrorFormat("Unexpected: cannot find template material {0} for {1}",
                             templateGuid, instanceGuid);
      }
      return null;
    }

    TiltBrushGltf1PbrValues tbPbr = gltfMat.values;
    // The default values here are reasonable fallbacks if there is no tbPbr
    Gltf2Material.PbrMetallicRoughness pbr = new Gltf2Material.PbrMetallicRoughness();
    if (tbPbr != null) {
      if (tbPbr.BaseColorFactor != null) {
        pbr.baseColorFactor = tbPbr.BaseColorFactor.Value;
      }
      if (tbPbr.MetallicFactor != null) {
        pbr.metallicFactor = tbPbr.MetallicFactor.Value;
      }
      if (tbPbr.RoughnessFactor != null) {
        pbr.roughnessFactor = tbPbr.RoughnessFactor.Value;
      }
      if (tbPbr.BaseColorTexPtr != null) {
        pbr.baseColorTexture = new Gltf2Material.TextureInfo {
          index = -1,
          texCoord = 0,
          texture = tbPbr.BaseColorTexPtr
        };
      }
      // Tilt Brush doesn't support metallicRoughnessTexture (yet?)
    }
    return CreateNewPbrMaterial(desc.Material, pbr);
  }

  /// <summary>
  /// Converts the given GLTF 2 material to a Unity Material.
  /// This is "best effort": we only interpret SOME, but not all GLTF material parameters.
  /// We try to be robust, and will always try to return *some* material rather than fail,
  /// even if crucial fields are missing or can't be parsed.
  /// </summary>
  /// <param name="gltfMat">The GLTF 2 material to convert.</param>
  /// <returns>The result of the conversion</returns>
  private UnityMaterial? ConvertGltf2Material(Gltf2Material gltfMat) {
    Material baseMaterial; {
      string alphaMode = gltfMat.alphaMode == null ? null : gltfMat.alphaMode.ToUpperInvariant();

      if (!gltfMat.doubleSided) {
        Debug.LogWarning("Not yet supported: single-sided pbr materials");
      }

      switch (alphaMode) {
      case Gltf2Material.kAlphaModeMask:
        Debug.LogWarning("Not yet supported: alphaMode=MASK");
        baseMaterial = PtSettings.Instance.basePbrOpaqueDoubleSidedMaterial;
        break;
      default:
      case Gltf2Material.kAlphaModeOpaque:
        baseMaterial = PtSettings.Instance.basePbrOpaqueDoubleSidedMaterial;
        break;
      case Gltf2Material.kAlphaModeBlend:
        baseMaterial = PtSettings.Instance.basePbrBlendDoubleSidedMaterial;
        break;
      }
    }

    if (gltfMat.pbrMetallicRoughness == null) {
      Debug.LogWarningFormat("Material #{0} has no PBR info.", gltfMat.gltfIndex);
      return null;
    }

    return CreateNewPbrMaterial(baseMaterial, gltfMat.pbrMetallicRoughness);
  }

  // Helper for ConvertGltf{1,2}Material
  private UnityMaterial CreateNewPbrMaterial(
      Material baseMaterial, Gltf2Material.PbrMetallicRoughness pbr) {
    Material mat = UnityEngine.Object.Instantiate(baseMaterial);

    string matName = baseMaterial.name;
    if (matName.StartsWith("Base")) {
      matName = matName.Substring(4);
    }

    mat.SetColor("_BaseColorFactor", pbr.baseColorFactor);
    Texture tex = null;
    if (pbr.baseColorTexture != null) {
      tex = pbr.baseColorTexture.texture.unityTexture;
      mat.SetTexture("_BaseColorTex", tex);
    }
    if (tex != null) {
      matName = string.Format("{0}_{1}", matName, tex.name);
    }
    mat.SetFloat("_MetallicFactor", pbr.metallicFactor);
    mat.SetFloat("_RoughnessFactor", pbr.roughnessFactor);
    mat.name = matName;

    return new UnityMaterial { material = mat, template = baseMaterial };
  }

  private static string SanitizeName(string uri) {
    uri = System.IO.Path.ChangeExtension(uri, "");
    return Regex.Replace(uri, @"[^a-zA-Z0-9_-]+", "");
  }

  /// <summary>
  /// Fills in gltfTexture.unityTexture with a Texture2D.
  /// </summary>
  /// <param name="gltfTexture">The glTF texture to convert.</param>
  /// <param name="loader">The IUriLoader to use for loading image files.</param>
  /// <returns>On completion of the coroutine, gltfTexture.unityTexture will be non-null
  /// on success.</returns>
  private static IEnumerable ConvertTextureCoroutine(
      GltfTextureBase gltfTexture, IUriLoader loader) {
    if (gltfTexture.unityTexture != null) {
      throw new InvalidOperationException("Already converted");
    }

    if (gltfTexture.SourcePtr == null) {
      Debug.LogErrorFormat("No image for texture {0}", gltfTexture.GltfId);
      yield break;
    }

    Texture2D tex;
    if (gltfTexture.SourcePtr.data != null) {
      // This case is hit if the client code hooks up its own threaded
      // texture-loading mechanism.
      var data = gltfTexture.SourcePtr.data;
      tex = new Texture2D(data.colorWidth, data.colorHeight, data.format, true);
      yield return null;
      tex.SetPixels32(data.colorData);
      yield return null;
      tex.Apply();
      yield return null;
    } else {
      byte[] textureBytes;
      using (IBufferReader reader = loader.Load(gltfTexture.SourcePtr.uri)) {
        textureBytes = new byte[reader.GetContentLength()];
        reader.Read(textureBytes, destinationOffset: 0, readStart: 0, readSize: textureBytes.Length);
      }
      tex = new Texture2D(1,1);
      tex.LoadImage(textureBytes, markNonReadable: false);
      yield return null;
    }

    tex.name = SanitizeName(gltfTexture.SourcePtr.uri);
    gltfTexture.unityTexture = tex;
  }

  // Returns the guid that represents this material.
  // The guid may refer to a pre-existing material (like Blocks Paper, or Tilt Brush Light).
  // It may also refer to a dynamically-generated material, in which case the base material
  // can be found by using ParseGuidFromShader.
  private static Guid ParseGuidFromMaterial(GltfMaterialBase gltfMaterial) {
    // Tilt Brush names its gltf materials like:
    //   material_Light-2241cd32-8ba2-48a5-9ee7-2caef7e9ed62

    // .net 3.5 doesn't have Guid.TryParse, and raising FormatException generates
    // tons of garbage for something that is done so often.
    if (!kTiltBrushMaterialRegex.IsMatch(gltfMaterial.name)) {
      return Guid.Empty;
    }
    int start = Mathf.Max(0, gltfMaterial.name.Length - 36);
    if (start < 0) { return Guid.Empty; }
    return new Guid(gltfMaterial.name.Substring(start));
  }

  // Returns the guid found on this material's vert or frag shader, or Empty on failure.
  // This Guid represents the template from which a pbr material was created.
  // For example, BasePbrOpaqueDoubleSided.
  private static Guid ParseGuidFromShader(Gltf1Material material) {
    var technique = material.techniquePtr;
    if (technique == null) { return Guid.Empty; }
    var program = technique.programPtr;
    if (program == null) { return Guid.Empty; }
    var shader = program.vertexShaderPtr ?? program.fragmentShaderPtr;
    if (shader == null) { return Guid.Empty; }
    var match = kTiltBrushShaderRegex.Match(shader.uri);
    if (match.Success) {
      return new Guid(match.Groups[1].Value);
    } else {
      return Guid.Empty;
    }
  }

  /// Returns a BrushDescriptor given a gltf material, or null if not found.
  /// If the material is an instance of a template, the descriptor for that
  /// will be returned.
  public static BrushDescriptor LookupBrushDescriptor(GltfMaterialBase gltfMaterial) {
    Guid guid = ParseGuidFromMaterial(gltfMaterial);
    if (guid == Guid.Empty) {
      return null;
    } else {
      BrushDescriptor desc;
      PtSettings.Instance.brushManifest.BrushesByGuid.TryGetValue(
          guid, out desc);
      if (desc == null) {
        // Maybe it's templated from a pbr material; the template guid
        // can be found on the shader.
        Gltf1Material gltf1Material = gltfMaterial as Gltf1Material;
        if (gltf1Material == null) {
          Debug.LogErrorFormat("Unexpected: glTF2 Tilt Brush material");
          return null;
        }
        Guid templateGuid = ParseGuidFromShader((Gltf1Material)gltfMaterial);
        PtSettings.Instance.brushManifest.BrushesByGuid.TryGetValue(
          templateGuid, out desc);
      }
      return desc;
    }
  }
}

}
