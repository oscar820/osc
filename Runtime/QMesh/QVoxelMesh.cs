using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Mesh
{
	public class QVoxelMesh : MonoBehaviour
	{
		QVoxelData voxelData;
		public Material mat;
		private void Awake()
		{
			var voxel2d = new QVoxelData2D();
			voxel2d[0, 0] = 1;
			voxel2d[0, 1] = 1;
			voxel2d[0, 2] = 1;
			voxel2d[0, 3] = 1;
			voxel2d[0, 4] = 1;

			for (short x = -2; x <= 2; x++)
			{
				for (short y = 5; y <= 6; y++)
				{
					voxel2d[x, y] = 2;
				}
			}
			new QVoxelData(voxel2d).GenerateMesh(gameObject, mat);
		}
	}

}
