using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QTool.Mesh
{

	public class QVoxelData
	{
		public QDictionary<Vector3Int, float> Voxels { get; private set; } = new QDictionary<Vector3Int, float>();
		public QMeshData meshData { private set; get; } = new QMeshData();
		public Vector3Int Max { get; private set; } = Vector3Int.one * int.MinValue;
		public Vector3Int Min { get; private set; } = Vector3Int.one * int.MaxValue;
		public float this[int x, int y, int z]
		{
			get
			{
				var pos =new Vector3Int(x, y, z);
				if (Voxels.ContainsKey(pos))
				{
					return Voxels[pos];
				}
				return 0;
			}
			set
			{
				var pos = new Vector3Int(x, y, z);
				if (Voxels.ContainsKey(pos))
				{
					if (Voxels[pos] == value) return;	
				}
				else
				{
					Max = Vector3Int.Max(Max, pos);
					Min = Vector3Int.Min(Min, pos);
				}
				if (meshData.HasMesh)
				{
					meshData.Clear();
				}
				Voxels[pos] = value;
			}
		}
		public UnityEngine.Mesh GetMesh(bool hasBorder=true)
		{
			if (meshData.HasMesh) return meshData.GetMesh();
			return QMarchingCubes.GenerateMesh(this, hasBorder);
		}
	}

}
