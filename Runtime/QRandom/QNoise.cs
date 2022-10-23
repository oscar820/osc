using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Noise
{

	public abstract class QNoise
	{
		public int Seed { get; set; }
		public abstract float this[float x] { get; }
		public abstract float this[float x, float y] { get; }
		public abstract float this[float x, float y, float z] { get; }
	}


	public class ValueNoise : QNoise
	{
		private QRandomTable table { get; set; }

		public ValueNoise(int seed=0)
		{
			table = new QRandomTable(seed);
		}
		public override float this[float x]
		{
			get
			{
				int ix0;
				float fx0;
				float s, n0, n1;

				ix0 = (int)Mathf.Floor(x);   
				fx0 = x - ix0;         

				s = Fade(fx0);

				n0 = table[ix0];
				n1 = table[ix0 + 1];

				float n = Lerp(s, n0, n1) /table.Max;
				n = n * 2.0f - 1.0f;

				return n ;
			}
		}

		public override float this[float x,float y]
		{
			get
			{
				int ix0, iy0;
				float fx0, fy0, s, t, nx0, nx1, n0, n1;

				ix0 = (int)Mathf.Floor(x);  
				iy0 = (int)Mathf.Floor(y);   

				fx0 = x - ix0;              
				fy0 = y - iy0;              

				t = Fade(fy0);
				s = Fade(fx0);

				nx0 = table[ix0, iy0];
				nx1 = table[ix0, iy0 + 1];

				n0 = Lerp(t, nx0, nx1);

				nx0 = table[ix0 + 1, iy0];
				nx1 = table[ix0 + 1, iy0 + 1];

				n1 = Lerp(t, nx0, nx1);

				float n = Lerp(s, n0, n1)/table.Max;
				n = n * 2.0f - 1.0f;

				return n ;
			}
		}


		public override float this[float x, float y,float z]
		{
			get
			{
				int ix0, iy0, iz0;
				float fx0, fy0, fz0;
				float s, t, r;
				float nxy0, nxy1, nx0, nx1, n0, n1;

				ix0 = (int)Mathf.Floor(x); 
				iy0 = (int)Mathf.Floor(y);  
				iz0 = (int)Mathf.Floor(z);  
				fx0 = x - ix0;             
				fy0 = y - iy0;            
				fz0 = z - iz0;             

				r = Fade(fz0);
				t = Fade(fy0);
				s = Fade(fx0);

				nxy0 = table[ix0, iy0, iz0];
				nxy1 = table[ix0, iy0, iz0 + 1];
				nx0 = Lerp(r, nxy0, nxy1);

				nxy0 = table[ix0, iy0 + 1, iz0];
				nxy1 = table[ix0, iy0 + 1, iz0 + 1];
				nx1 = Lerp(r, nxy0, nxy1);

				n0 = Lerp(t, nx0, nx1);

				nxy0 = table[ix0 + 1, iy0, iz0];
				nxy1 = table[ix0 + 1, iy0, iz0 + 1];
				nx0 = Lerp(r, nxy0, nxy1);

				nxy0 = table[ix0 + 1, iy0 + 1, iz0];
				nxy1 = table[ix0 + 1, iy0 + 1, iz0 + 1];
				nx1 = Lerp(r, nxy0, nxy1);

				n1 = Lerp(t, nx0, nx1);

				float n = Lerp(s, n0, n1) / table.Max;
				n = n * 2.0f - 1.0f;

				return n ;
			}
		}


		private float Fade(float t) { return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f); }

		private float Lerp(float t, float a, float b) { return a + t * (b - a); }

	}
}
