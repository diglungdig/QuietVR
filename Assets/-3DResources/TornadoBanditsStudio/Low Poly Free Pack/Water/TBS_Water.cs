using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TornadoBanditsStudio.LowPolyFreePack
{
	/// <summary>
	/// Waves generator.
	/// </summary>
	public class TBS_Water : MonoBehaviour 
	{
	   
		[Header ("Waves settings")]
	    public float waveHeight = 1f;
	    public float speed = 1.0f;
	    public float waveLength = 1.0f;

		[Header ("Randomize variables")]
	    public float randomHeight = 0.2f;
	    public float randomSpeed = 5.0f;
	    public float noiseOffset = 20.0f;
	   
	    private Vector3[] baseHeight;
	    private Vector3[] vertices;
	    private List<float> perVertexRandoms = new List<float>();
	    private Mesh mesh;
	   
	    void Awake() 
		{
			//Get the mesh
	        mesh = GetComponent<MeshFilter>().mesh;
	        if (baseHeight == null) 
			{
	            baseHeight = mesh.vertices;
	        }
	 
	        for(int i=0; i < baseHeight.Length; i++) 
			{
	            perVertexRandoms.Add(Random.value * randomHeight);
	        }
	    }
	   
	    void Update () {
	        if (vertices == null) 
			{
	            vertices = new Vector3[baseHeight.Length];
	        }
	       
			//For each vertex in our mesh apply the wave effect
	        for (int i=0;i<vertices.Length;i++) 
			{
	            Vector3 vertex = baseHeight[i];
				//Move the current vertex on x and y.
	            Random.seed = (int)((vertex.x + noiseOffset) * (vertex.x + noiseOffset) + (vertex.y + noiseOffset) * (vertex.y + noiseOffset));
	            vertex.y += Mathf.Sin(Time.time * speed + baseHeight[i].x * waveLength + baseHeight[i].y * waveLength) * waveHeight;
	            vertex.y += Mathf.Sin(Mathf.Cos(Random.value * 1.0f) * randomHeight * Mathf.Cos (Time.time * randomSpeed * Mathf.Sin(Random.value * 1.0f)));

                vertices[i] = vertex;
	        }

			//Set the new vertices and recalculate normals
	        mesh.vertices = vertices;
	        mesh.RecalculateNormals();
	    }
	}
}