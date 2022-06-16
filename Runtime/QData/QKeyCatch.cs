using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace QTool
{
	public class QKeyCatch<T,CheckT>
	{
		public Dictionary<string, T> Catch=new Dictionary<string, T>();
		public Dictionary<string, CheckT> CheckInfo = new Dictionary<string, CheckT>();
		public Func<string, CheckT> GetCheckInfo = null;
		public QKeyCatch(Func<string, CheckT> GetCheckInfo)
		{
			this.GetCheckInfo = GetCheckInfo;
		}
		public T Get(string key, Func<string, T> GetValue)
		{
			if (Catch.ContainsKey(key))
			{
				var newInfo = GetCheckInfo(key);
				if (CheckInfo[key]==null||!CheckInfo[key].Equals(newInfo))
				{
					Catch[key] = GetValue(key);
					CheckInfo[key] = newInfo;
				}
			}
			else
			{
				Catch.Add(key, GetValue(key));
				CheckInfo.Add(key, GetCheckInfo(key));
			}
			return Catch[key];
		}
		public void Remove(string key)
		{
			Catch.Remove(key);
			CheckInfo.Remove(key);
		}
		public void Clear()
		{
			Catch.Clear();
			CheckInfo.Clear();
		}
	}
}

