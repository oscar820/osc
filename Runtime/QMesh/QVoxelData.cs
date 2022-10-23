using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QTool.Mesh
{
	public class QVoxelData
	{
		public QVoxelData(int size) : this(size, size, size) { }
		public QVoxelData(int width, int height, int depth)
		{
			Voxels = new float[width, height, depth];
		}
		public int Width => Voxels.GetLength(0);
		public int Height => Voxels.GetLength(1);
		public int Depth => Voxels.GetLength(2);

		public float this[int x, int y, int z]
		{
			get { return Voxels[x, y, z]; }
			set { Voxels[x, y, z] = value; }
		}
		public float[,,] Voxels { get; private set; }

		public float GetVoxel(float u, float v, float w)
		{
			float x = u * (Width - 1);
			float y = v * (Height - 1);
			float z = w * (Depth - 1);

			int xi = (int)Mathf.Floor(x);
			int yi = (int)Mathf.Floor(y);
			int zi = (int)Mathf.Floor(z);

			float v000 = GetVoxel(xi, yi, zi);
			float v100 = GetVoxel(xi + 1, yi, zi);
			float v010 = GetVoxel(xi, yi + 1, zi);
			float v110 = GetVoxel(xi + 1, yi + 1, zi);

			float v001 = GetVoxel(xi, yi, zi + 1);
			float v101 = GetVoxel(xi + 1, yi, zi + 1);
			float v011 = GetVoxel(xi, yi + 1, zi + 1);
			float v111 = GetVoxel(xi + 1, yi + 1, zi + 1);

			float tx = Mathf.Clamp01(x - xi);
			float ty = Mathf.Clamp01(y - yi);
			float tz = Mathf.Clamp01(z - zi);
			float v0 = Lerp(v000, v100, v010, v110, tx, ty);
			float v1 = Lerp(v001, v101, v011, v111, tx, ty);
			return QLerp.LerpTo(v0, v1, tz);
		}

		public Vector3 GetNormal(int x, int y, int z)
		{
			var n = GetFirstDerivative(x, y, z);


			return n.normalized * -1;
		}

		public Vector3 GetNormal(float u, float v, float w)
		{
			var n = GetFirstDerivative(u, v, w);

			return n.normalized * -1;
		}

		public Vector3 GetFirstDerivative(int x, int y, int z)
		{
			float dx_p1 = GetVoxel(x + 1, y, z);
			float dy_p1 = GetVoxel(x, y + 1, z);
			float dz_p1 = GetVoxel(x, y, z + 1);

			float dx_m1 = GetVoxel(x - 1, y, z);
			float dy_m1 = GetVoxel(x, y - 1, z);
			float dz_m1 = GetVoxel(x, y, z - 1);

			float dx = (dx_p1 - dx_m1) * 0.5f;
			float dy = (dy_p1 - dy_m1) * 0.5f;
			float dz = (dz_p1 - dz_m1) * 0.5f;

			return new Vector3(dx, dy, dz);
		}

		public Vector3 GetFirstDerivative(float u, float v, float w)
		{
			const float h = 0.005f;
			const float hh = h * 0.5f;
			const float ih = 1.0f / h;

			float dx_p1 = GetVoxel(u + hh, v, w);
			float dy_p1 = GetVoxel(u, v + hh, w);
			float dz_p1 = GetVoxel(u, v, w + hh);

			float dx_m1 = GetVoxel(u - hh, v, w);
			float dy_m1 = GetVoxel(u, v - hh, w);
			float dz_m1 = GetVoxel(u, v, w - hh);

			float dx = (dx_p1 - dx_m1) * ih;
			float dy = (dy_p1 - dy_m1) * ih;
			float dz = (dz_p1 - dz_m1) * ih;

			return new Vector3(dx, dy, dz);
		}


		private static float Lerp(float v00, float v10, float v01, float v11, float tx, float ty)
		{
			return QLerp.LerpTo(QLerp.LerpTo(v00, v10, tx), QLerp.LerpTo(v01, v11, tx), ty);
		}

	}

}
