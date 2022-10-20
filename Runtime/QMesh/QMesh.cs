using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Mesh
{
	public static class QMesh
	{
		public static void CombineMeshs(GameObject root, MeshRenderer[] meshes)
		{
			var childs = root.GetComponentsInChildren<Transform>(true);
			var matList = new List<Material>();
			var combineInfos = new List<CombineInstance>();
			foreach (var meshObj in meshes)
			{
				var mesh = meshObj.GetComponent<MeshFilter>()?.mesh;
				matList.AddRange(meshObj.sharedMaterials);
				CombineInstance combine = new CombineInstance();
				combine.mesh = mesh;
				combineInfos.Add(combine);
			}
			root.GetComponent<MeshRenderer>(true).materials = matList.ToArray();
			var filter = root.GetComponent<MeshFilter>(true);
			filter.sharedMesh = new UnityEngine.Mesh();
			filter.sharedMesh.CombineMeshes(combineInfos.ToArray(), false, false);
		}
		public static void CombineSkinedMeshs(this GameObject skeleton, SkinnedMeshRenderer[] meshes)
		{
			var childs = skeleton.GetComponentsInChildren<Transform>(true);
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
			var meshRenderer = skeleton.GetComponent<SkinnedMeshRenderer>();
			if (meshRenderer == null)
			{
				meshRenderer = skeleton.AddComponent<SkinnedMeshRenderer>();
			}
			meshRenderer.sharedMesh = new UnityEngine.Mesh();
			meshRenderer.sharedMesh.CombineMeshes(combineInfos.ToArray(), false, false);
			meshRenderer.bones = bones.ToArray();
			meshRenderer.materials = matList.ToArray();
		}
	}

}
