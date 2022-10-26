using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Mesh
{
	public class QVoxelMesh : MonoBehaviour
	{
		public GameObject target;
		[QName("刷新")]
		public void Refresh()
		{
			gameObject.GetComponent<MeshRenderer>(true).CombineMeshs(target.GetComponentsInChildren<MeshRenderer>());
		}
	}

}
