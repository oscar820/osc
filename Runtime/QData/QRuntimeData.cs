using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Reflection;
namespace QTool
{
	public class QRuntimeData
	{
		public QDictionary<string, QRuntimeValue> Values = new QDictionary<string, QRuntimeValue>();
		public float this[string key]
		{
			get
			{
				if (Values.ContainsKey(key))
				{
					return Values[key].Value;
				}
				else
				{
					return 0;
				}
			}
		}
		public void Clear()
		{
			Values.Clear();
		}
	}
	
	public class QRuntimeValue
	{
		public QRuntimeValue(float value)
		{
			BaseValue = value;
		}
		public QValue BaseValue { get; set; } = 0f;
		public QValue PercentValue { get; set; } = 1;
		public float Value
		{
			get
			{
				return BaseValue * PercentValue;
			}
		}
	}
	public struct QValue
	{

		private float a;
		private float b;
		private float t;

		public QValue(float value)
		{
			a = value * 0.5f;
			b = value * 0.5f;
			t = 0.5f;
		}
		public float Value
		{
			get
			{
				return a / t + b / (1 - t);
			}
			set
			{
				if (value == Value) return;
				t = Random.Range(0.2f, 0.8f);
				a = value * t;
				b = value - a;
			}
		}
		public static implicit operator QValue(float value)
		{
			return new QValue(value);
		}
		public static implicit operator float(QValue value)
		{
			return value.Value;
		}
	}
}

