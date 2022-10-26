using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
namespace QTool.Mesh
{
	
	public class QMeshData
	{
		public List<Vector3> vertices = new List<Vector3>();
		public List<Vector3> normals = new List<Vector3>();
		public List<Color> colors = new List<Color>();
		public List<int> triangles = new List<int>();
		public List<Vector2> uvs = new List<Vector2>();
		public void Clear()
		{
			vertices.Clear();
			normals.Clear();
			colors.Clear();
			triangles.Clear();
			uvs.Clear();
			mesh = null;
		}
		public bool HasMesh => mesh != null;
		UnityEngine.Mesh mesh=null;
		public UnityEngine.Mesh GetMesh()
		{
			if (mesh != null) return mesh;
			mesh = new UnityEngine.Mesh();
			mesh.vertices = vertices.ToArray();
			mesh.uv = uvs.ToArray();
			mesh.colors = colors.ToArray();
			mesh.triangles = triangles.ToArray();
			if (normals.Count == 0)
			{
				mesh.RecalculateNormals();
			}
			else
			{
				mesh.normals = normals.ToArray();
			}
			return mesh;
		}
	}
	public static class QMeshTool
	{

		
		public static void CombineMeshs(this MeshRenderer root, MeshRenderer[] meshes = null)
		{
			bool deleteOld = false;
			if (meshes == null)
			{
				meshes = root.GetComponentsInChildren<MeshRenderer>();
				deleteOld = true;
			}
			var matList = new List<Material>();
			var combineInfos = new List<CombineInstance>();
			foreach (var meshObj in meshes)
			{
				if (meshObj == root) continue;
				var mesh = meshObj.GetComponent<MeshFilter>()?.sharedMesh;
				matList.AddRange(meshObj.sharedMaterials);
				CombineInstance combine = new CombineInstance();
				combine.transform = Matrix4x4.TRS(meshObj.transform.localPosition, meshObj.transform.rotation, meshObj.transform.localScale);
				combine.mesh = mesh;
				combineInfos.Add(combine);
			}
			root.sharedMaterials = matList.ToArray();
			var filter = root.GetComponent<MeshFilter>(true);
			filter.sharedMesh = new UnityEngine.Mesh();
			filter.sharedMesh.CombineMeshes(combineInfos.ToArray(), false, true);
			Debug.Log(root + " " + nameof(CombineMeshs) + " 顶点数:" + filter.sharedMesh.vertices.Length);
			if (deleteOld)
			{
				foreach (var mesh in meshes)
				{
					if (mesh != null)
					{
						mesh.gameObject.CheckDestory();
					}
				}
			}
		}
		public static void CombineMeshs(this SkinnedMeshRenderer root, SkinnedMeshRenderer[] meshes)
		{
			var childs = root.GetComponentsInChildren<Transform>(true);
			var matList = new List<Material>();
			var combineInfos = new List<CombineInstance>();
			var bones = new List<Transform>();
			foreach (var skinedMesh in meshes)
			{
				matList.AddRange(skinedMesh.sharedMaterials);
				for (int sub = 0; sub < skinedMesh.sharedMesh.subMeshCount; sub++)
				{
					CombineInstance combine = new CombineInstance();
					combine.mesh = skinedMesh.sharedMesh;
					combine.subMeshIndex = sub;
					combineInfos.Add(combine);
				}
				foreach (var bone in skinedMesh.bones)
				{
					bones.Add(childs.Get(bone.name, (trans) => trans.name));
				}
			}
			root.sharedMesh = new UnityEngine.Mesh();
			root.sharedMesh.CombineMeshes(combineInfos.ToArray(), false, false);
			root.bones = bones.ToArray();
			root.materials = matList.ToArray();
		}
	}
}
