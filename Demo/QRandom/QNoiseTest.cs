using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Mesh;
namespace QTool.Noise
{

	public class QNoiseTest : MonoBehaviour
	{
		public Material material;
		public int size = 30;
		void Start()
		{
			QNoise fractal = new ValueNoise();	
			var marching = new QMarchingCubes();
			var voxels = new QVoxelData(size);
			for (int x = 0; x < size; x++)
			{
				for (int y = 0; y < size; y++)
				{
					for (int z = 0; z < size; z++)
					{
						float u = x / (size - 1.0f);
						float v = y / (size - 1.0f);
						float w = z / (size - 1.0f);
						voxels[x, y, z] = fractal[u, v, w];
					}
				}
			}
			List<Vector3> verts = new List<Vector3>();
			List<Vector3> normals = new List<Vector3>();
			List<int> indices = new List<int>();
			marching.Generate(voxels.Voxels, verts, indices);
			CreateMesh(verts, normals, indices, -Vector3.one*size/2f);
		}


		private void CreateMesh(List<Vector3> verts, List<Vector3> normals, List<int> indices, Vector3 position)
		{
			UnityEngine.Mesh mesh = new UnityEngine.Mesh();
			mesh.SetVertices(verts);
			mesh.SetTriangles(indices, 0);
			if (normals.Count > 0)
				mesh.SetNormals(normals);
			else
				mesh.RecalculateNormals();
			mesh.RecalculateBounds();
			GameObject go = new GameObject("Mesh");
			go.transform.parent = transform;
			go.AddComponent<MeshFilter>();
			go.AddComponent<MeshRenderer>();
			go.GetComponent<Renderer>().material = material;
			go.GetComponent<MeshFilter>().mesh = mesh;
			go.transform.localPosition = position;
			Debug.LogError("mesh " + mesh.vertices.Length);
		}
	}
}
