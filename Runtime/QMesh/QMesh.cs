using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
namespace QTool.Mesh
{
	public class QMesh : MonoBehaviour
	{
		public GameObject target;
		[QName("刷新")]
		public void Refresh()
		{
			gameObject.GetComponent<MeshRenderer>(true).CombineMeshs(target.GetComponentsInChildren<MeshRenderer>());
			
		}
	}
	//public class QMeshData
	//{
	//	public List<Vector3> vertices = new List<Vector3>();
	//	public List<Vector3> normals = new List<Vector3>();
	//	public List<Color> colors = new List<Color>();
	//	public List<int> triangles = new List<int>();
	//	public List<Vector2> uvs = new List<Vector2>();
	//	static Vector3[] Normals = new Vector3[] {
	//		Vector3.right,
	//		Vector3.left,
	//		Vector3.up,
	//		Vector3.down,
	//		Vector3.forward,
	//		Vector3.back
	//	};

	//	public static void AddFace(this UnityEngine.Mesh mesh,Vector3 a,Vector3 b,Vector3 c,Vector2 d,Vector3 normal)
	//	{
			
	//	}
	//	public static void CubeMeshWithColor(Vector3 halfSize, Color c, int cidx)
	//	{

	//		Vector3[] verts = new Vector3[] {
	//			new Vector3 (-halfSize.x, -halfSize.y, -halfSize.z),
	//			new Vector3 (-halfSize.x, halfSize.y, -halfSize.z),
	//			new Vector3 (halfSize.x, halfSize.y, -halfSize.z),
	//			new Vector3 (halfSize.x, -halfSize.y, -halfSize.z),
	//			new Vector3 (halfSize.x, -halfSize.y, halfSize.z),
	//			new Vector3 (halfSize.x, halfSize.y, halfSize.z),
	//			new Vector3 (-halfSize.x, halfSize.y, halfSize.z),
	//			new Vector3 (-halfSize.x, -halfSize.y, halfSize.z)
	//		};

	//		int[] indicies = new int[] {
	//			0, 1, 2, //   1
	//			0, 2, 3,
	//			3, 2, 5, //   2
	//			3, 5, 4,
	//			5, 2, 1, //   3
	//			5, 1, 6,
	//			3, 4, 7, //   4
	//			3, 7, 0,
	//			0, 7, 6, //   5
	//			0, 6, 1,
	//			4, 5, 6, //   6
	//			4, 6, 7
	//		};

	//		Color[] colors = new Color[] {
	//		c,
	//		c,
	//		c,
	//		c,
	//		c,
	//		c,
	//		c,
	//		c
	//	};

	//		Vector2[] uvs = new Vector2[] {
	//		new Vector2((cidx - 0.5f) / 256f, 0.5f),
	//		new Vector2((cidx - 0.5f) / 256f, 0.5f),
	//		new Vector2((cidx - 0.5f) / 256f, 0.5f),
	//		new Vector2((cidx - 0.5f) / 256f, 0.5f),
	//		new Vector2((cidx - 0.5f) / 256f, 0.5f),
	//		new Vector2((cidx - 0.5f) / 256f, 0.5f),
	//		new Vector2((cidx - 0.5f) / 256f, 0.5f),
	//		new Vector2((cidx - 0.5f) / 256f, 0.5f)
	//	};

	//		UnityEngine.Mesh mesh = new UnityEngine.Mesh();
	//		mesh.vertices = verts;
	//		mesh.uv = uvs;
	//		mesh.colors = colors;
	//		mesh.triangles = indicies;
	//		mesh.RecalculateNormals();
	//		return mesh;
	//	}
	//}
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
