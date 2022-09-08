using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Reflection;
using System.IO;

namespace QTool
{
	public class QRuntimeData
	{
		public QDictionary<string, QRuntimeValue> Values = new QDictionary<string, QRuntimeValue>((key)=>new QRuntimeValue());
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
		public QRuntimeValue()
		{

		}
		public QRuntimeValue(float value)
		{
			OriginValue = value;
		}
		public float OriginValue { get; private set; } = 0f;
		public QValue BaseValue { get; set; } = 0f;
		public QValue PercentValue { get; set; } = 1;
		public float Value
		{
			get
			{
				return (OriginValue+ BaseValue) * PercentValue;
			}
		}
	}
	public struct QValue:IQData
	{

		private float a;
		private float b;

		public QValue(float value)
		{
			a = value * 0.5f;
			b = value * 0.5f;
		}
		public float Value
		{
			get
			{
				return a  + b;
			}
			set
			{
				if (value == Value) return;
				a = value * Random.Range(0.2f, 0.8f);
				b = value - a;
			}
		}

		public void ParseQData(StringReader reader)
		{
			Value = float.Parse(reader.ReadValueString());
		}

		public void ToQData(StringWriter writer)
		{
			writer.Write(Value);
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

