using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.Noise
{

	public class QNoiseTest : MonoBehaviour
	{
		public int size = 512;
		Texture2D texture;

		void Start()
		{

			texture = new Texture2D(size, size);

			QNoise noise = new ValueNoise();


			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float fx = x / (size - 1.0f);
					float fy = y / (size - 1.0f);

					var value=  noise[fx*20, fy*20];
					texture.SetPixel(x, y, new Color(value, value, value, 1));
				}
			}

			texture.Apply();

		}

		void OnGUI()
		{

			Vector2 center = new Vector2(Screen.width / 2, Screen.height / 2);
			Vector2 offset = new Vector2(size / 2, size / 2);

			Rect rect = new Rect();
			rect.min = center - offset;
			rect.max = center + offset;

			GUI.DrawTexture(rect, texture);

		}

	
	}
}
