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
	}
	public class QRuntimeValue<T> : IKey<string> 
	{
		public string Key { get; set; }
		public QRuntimeValue()
		{
		}
		public QRuntimeValue(T value)
		{
			baseValue = value;
		}
		public T baseValue = (T)(object)0;
		public T percentValue = (T)(object)1;
		public T Value
		{
			get
			{
				return (T)baseValue.OperaterMultiply(percentValue);
			}
		}
	}
	
}

