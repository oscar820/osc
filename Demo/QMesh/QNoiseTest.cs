using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Mesh;
namespace QTool.Noise
{

	public class QNoiseTest : MonoBehaviour
	{
		public Material material;
		public int size = 50;
		public void NoiseTest()
		{
			QNoise fractal = new ValueNoise();

			var voxels = new QVoxelData();
			for (int x = 0; x < size; x++)
			{
				for (int y = 0; y < size; y++)
				{
					for (int z = 0; z < size; z++)
					{
						float u = x / (size - 1.0f)*5;
						float v = y / (size - 1.0f)*5;
						float w = z / (size - 1.0f)*5;
						voxels[x, y, z] = fractal[u,v,w];
					}
				}
			}
			voxels.GenerateMesh(gameObject,material);
		}
		void Start()
		{
			NoiseTest();
			return;
			var voxels = new QVoxelData();
			for (int x = 0; x < 10; x++)
			{
				for (int y = 0; y < 10; y++)
				{
					for (int z = 0; z < 10; z++)
					{
						if (x <= 6 && x >= 4 && y <= 6 && y >= 4 && z <= 6 && z >= 4)
						{
							if (x == 6 || y == 6||z==6)
							{
								voxels[x, y, z] = 10;
							}
							else
							{

								voxels[x, y, z] = 1;
							}
						}
						else
						{
							voxels[x, y, z] = 0;
						}

					}
				}
			}
			voxels.GenerateMesh(gameObject,material);
		}


	
	}
}
