using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Reflection;
namespace QTool
{
	public class QRuntimeData: QRuntimeData<float>
	{

	}
	public class QRuntimeData<T> 
	{
		public QAutoList<string, QRuntimeValue<T>> Values = new QAutoList<string, QRuntimeValue<T>>();
		public T this[string key]
		{
			get
			{
				return Values[key].Value;
			}
		}
		public void Clear()
		{
			Values.Clear();
		}
	}
	public class QRuntimeValue<T> : IKey<string> 
	{
		public string Key { get; set; }
		public QRuntimeValue()
		{
		}
		public QRuntimeValue(T value)
		{
			BaseValue = value;
		}
		public T BaseValue { get; set; } = 0.ConvertTo<T>();
		public T MultiplyValue { get; set; } = 1.ConvertTo<T>();
		public T Value
		{
			get
			{
				return (T)BaseValue.OperaterMultiply(MultiplyValue);
			}
		}
	}
	
}

