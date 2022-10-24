using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QTool.Mesh
{

	public class QVoxelData
	{
		private QDictionary<int, QDictionary<int, QDictionary<int, float>>> voxels =
			new QDictionary<int, QDictionary<int, QDictionary<int, float>>>(
				(index) => new QDictionary<int, QDictionary<int, float>>(
					(index) => new QDictionary<int, float>((index) => 0)));
		public QMeshData meshData { private set; get; } = new QMeshData();
		public Vector3Int Max { get; private set; } = Vector3Int.one * int.MinValue;
		public Vector3Int Min { get; private set; } = Vector3Int.one * int.MaxValue;
		public float this[int x, int y, int z]
		{
			get
			{
				if (voxels.ContainsKey(x))
				{
					var xList = voxels[x];
					if (xList.ContainsKey(y))
					{
						var yList = xList[y];
						if (yList.ContainsKey(z))
						{
							return yList[z];
						}
					}
				}
				return 0;
			}
			set
			{
				if (!voxels.ContainsKey(x)|| !voxels.ContainsKey(y)||!voxels.ContainsKey(z))
				{
					var v3 = new Vector3Int(x, y, z);
					Max = Vector3Int.Max(Max, v3);
					Min = Vector3Int.Min(Min, v3);
				}
				voxels[x][y][z] = value;
			}
		}
		
		public void Foreach(System.Action<int, int, int, float> action)
		{
			foreach (var x in voxels)
			{
				foreach (var y in x.Value)
				{
					foreach (var z in y.Value)
					{
						action(x.Key, y.Key, z.Key, z.Value);
					}
				}
			}
		}



	}

}
