using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace QTool
{
	public class QKeyCache<KeyT,T,CheckT>
	{
		public Dictionary<KeyT, T> Cache=new Dictionary<KeyT, T>();
		public Dictionary<KeyT, CheckT> CheckInfo = new Dictionary<KeyT, CheckT>();
		public Func<KeyT, CheckT> GetCheckInfo = null;
		public Func<KeyT, T> GetValue = null;
		public QKeyCache(Func<KeyT, CheckT> GetCheckInfo = null, Func<KeyT, T> GetValue=null)
		{
			this.GetValue = GetValue;
			this.GetCheckInfo = GetCheckInfo;
		}
		public void Set(KeyT key,T value) {

			var checkInfo = GetCheckInfo(key);
			Cache.Set(key, value);
			CheckInfo.Set(key, checkInfo);
		}
		public T Get(KeyT key, Func<KeyT, T> GetValueFunc)
		{
			if (key == null)
			{
				Debug.LogError("key is null");
				return default;
			}
			if (Cache.ContainsKey(key)&&Cache[key]!=null)
			{
				var newInfo = GetCheckInfo(key);
				if (!CheckInfo.ContainsKey(key)||!CheckInfo[key].Equals(newInfo))
				{
					Cache.Set(key, GetValueFunc(key));
					CheckInfo.Set(key, newInfo);
				}
				return Cache[key];
			}
			else
			{
				Cache.Set(key, GetValueFunc(key));
				CheckInfo.Set(key, GetCheckInfo(key));
				return Cache[key];
			}
		}
		public T Get(KeyT key)
		{
			return Get(key, GetValue);
		}
		public void Remove(KeyT key)
		{
			Cache.Remove(key);
			CheckInfo.Remove(key);
		}
		public void Clear()
		{
			Cache.Clear();
			CheckInfo.Clear();
		}
	}
}

