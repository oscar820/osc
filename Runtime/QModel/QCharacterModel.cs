using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QTool.Model
{
	[RequireComponent(typeof(Animator))]
	public class QCharacterModel : MonoBehaviour
	{
		public List<GameObject> modelRoot;
		[QReadOnly]
		public List<SkinnedMeshRenderer> skinnedMesh;
		[QReadOnly]
		public Transform rootBone;
		public List<string> meshKeys;

		private void OnValidate()
		{
		
		}
		private void Start()
		{
		}
		[ContextMenu("刷新模型")]
		public void FreshMesh()
		{
			skinnedMesh?.Clear();
			foreach (var model in modelRoot)
			{
				if (model == null) continue;
				CheckBone(model.transform);
				skinnedMesh.AddRange(model.GetComponentsInChildren<SkinnedMeshRenderer>());
			}
			var meshs = new List<SkinnedMeshRenderer>();
		
			foreach (var meshName in meshKeys)
			{
				var mesh = skinnedMesh.Get(meshName, (mesh) => mesh.name);
				if (mesh == null)
				{
					Debug.LogError("找不到网格[" + meshName + "]");
				}
				else
				{
					meshs.Add(mesh);
				}
			}
			if (meshs.Count > 0)
			{
				QModel.CombineMeshs(gameObject, meshs.ToArray());
			}
		}
		public void CheckBone(Transform modelRoot)
		{
			if (this.rootBone == null)
			{
				for (int i = 0; i < modelRoot.childCount; i++)
				{
					var child = modelRoot.GetChild(i);
					if (child != null && child.GetComponent<SkinnedMeshRenderer>() == null)
					{
						rootBone = Instantiate(child, transform);
						rootBone.name = child.name;
						break;
					}
				}
				var animator = GetComponent<Animator>();
				if (animator.avatar == null)
				{
					animator.avatar = modelRoot.GetComponent<Animator>()?.avatar;
				}
			}
		}
	}
	public static class QModel
	{
		public static void CombineMeshs(GameObject skeleton, SkinnedMeshRenderer[] meshes)
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
			meshRenderer.sharedMesh = new Mesh();
			meshRenderer.sharedMesh.CombineMeshes(combineInfos.ToArray(),false,false);
			meshRenderer.bones = bones.ToArray();
			meshRenderer.materials = matList.ToArray();
		}
	}

}

