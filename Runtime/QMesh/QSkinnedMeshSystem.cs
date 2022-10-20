using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
namespace QTool.Mesh
{
	[RequireComponent(typeof(Animator))]
	public class QSkinnedMeshSystem : MonoBehaviour
	{
		public List<GameObject> modelRoot;
		[QEnum("get_" + nameof(SkinnedMesh))]
		public List<string> meshKeys;
		[HideInInspector]
		public List<SkinnedMeshRenderer> skinnedMesh;
		public List<SkinnedMeshRenderer> SkinnedMesh => skinnedMesh;
		[HideInInspector]
		public Transform rootBone;
	

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
				QMesh.CombineSkinedMeshs(gameObject, meshs.ToArray());
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
	
}

