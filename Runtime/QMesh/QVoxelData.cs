using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QTool.Mesh
{
	public struct QVector2Short
	{
		public short x;
		public short y;
		public QVector2Short(short x, short y)
		{
			this.x = x;
			this.y = y;
		}
	}
	public class QVoxelInfo2D
	{
		public byte color;
		public byte size;
		
	}
	public class QVoxelData2D
	{
		public QList<Color> ColorList = new QList<Color>();
		public QDictionary<QVector2Short, QVoxelInfo2D> Voxels = new QDictionary<QVector2Short, QVoxelInfo2D>((key)=>new QVoxelInfo2D());
		static QVoxelData voxelData = null;
		public byte this[short x, short y] { get => this[new QVector2Short(x, y)];set => this[new QVector2Short(x, y)] = value; }
		public byte this[QVector2Short pos]
		{
			get
			{
				if (Voxels.ContainsKey(pos))
				{
					return Voxels[pos].size;
				}
				return 0;
			}
			set
			{
				if (Voxels.ContainsKey(pos))
				{
					if (value == 0)
					{
						Voxels.Remove(pos);
						return;
					}
				}
				Voxels[pos].size = value;
			}
		}
		
	}
	public class QVoxelData
	{
		public QDictionary<Vector3Int, float> Voxels { get; private set; } = new QDictionary<Vector3Int, float>();
		public QMeshData meshData { private set; get; } = new QMeshData();
		public Vector3Int Max { get; private set; } = Vector3Int.one * int.MinValue;
		public Vector3Int Min { get; private set; } = Vector3Int.one * int.MaxValue;
		public QVoxelData()
		{

		}
		public QVoxelData(QVoxelData2D voxelData2D)
		{
			foreach (var kv in voxelData2D.Voxels)
			{
				for (int i = 0; i < kv.Value.size; i++)
				{
					var pos = new Vector3Int(kv.Key.x, kv.Key.y, 0);
					this[pos + Vector3Int.forward*i] = 1;
					this[pos + Vector3Int.back * i] = 1;
				}
			}
		}
	
		public float this[Vector3Int pos]
		{
			get
			{
				if (Voxels.ContainsKey(pos))
				{
					return Voxels[pos];
				}
				return 0;
			}
			set
			{
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
		public float this[int x,int y,int z] { get => this[new Vector3Int(x, y, z)];set => this[new Vector3Int(x, y, z)] = value; }
		
		public UnityEngine.Mesh GetMesh()
		{
			if (meshData.HasMesh) return meshData.GetMesh();
			return QMarchingCubes.GenerateMeshData(this).GetMesh();
		}
	}

}
