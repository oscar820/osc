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
		public QKeyCache(Func<KeyT, CheckT> GetCheckInfo)
		{
			this.GetCheckInfo = GetCheckInfo;
		}
		public QKeyCache( Func<KeyT, T> GetValue,Func<KeyT, CheckT> GetCheckInfo)
		{
			this.GetValue = GetValue;
			this.GetCheckInfo = GetCheckInfo;
		}
		public bool ContainsKey(KeyT key)
		{
			if (key == null)
			{
				Debug.LogError("key is null");
				return false;
			}
			if (Cache.ContainsKey(key))
			{
				var newInfo = GetCheckInfo(key);
				if (CheckInfo[key] == null || !CheckInfo[key].Equals(newInfo))
				{
					Cache.Remove(key);
				}
			}
			else
			{
				GetValue(key);
			}
			return Cache.ContainsKey(key);
		}
		public void Set(KeyT key,T value) {
			var checkInfo = GetCheckInfo(key);
			if (Cache.ContainsKey(key))
			{
				Cache[key] = value;
				CheckInfo[key] = checkInfo;
			}
			else
			{
				Cache.Add(key, value);
				CheckInfo.Add(key, checkInfo);
			}
		}
		public T Get(KeyT key, Func<KeyT, T> GetValue)
		{
			if (key == null)
			{
				Debug.LogError("key is null");
				return default;
			}
			if (Cache.ContainsKey(key))
			{
				var newInfo = GetCheckInfo(key);
				if (CheckInfo[key]==null||!CheckInfo[key].Equals(newInfo))
				{
					Cache[key] = GetValue(key);
					CheckInfo[key] = newInfo;
				}
			}
			else
			{
				Cache.Add(key, GetValue(key));
				CheckInfo.Add(key, GetCheckInfo(key));
			}
			return Cache[key];
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

