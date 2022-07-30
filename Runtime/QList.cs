using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using QTool.Inspector;
using QTool.Reflection;
namespace QTool
{
    public interface IKey<KeyType>
    {
        KeyType Key { get; set; }
    }
    [System.Serializable]
    public class QKeyValue<TKey, T> : IKey<TKey>
    {
        public TKey Key { get; set; }
        public T Value { get; set; }
        public QKeyValue()
        {

        }
        public QKeyValue(TKey key, T value)
        {
            Key = key;
            Value = value;
        }
        public override string ToString()
        {
            return "{" + Key + ":" + Value + "}";
        }
    }
    public class QDictionary<TKey, T> : QAutoList<TKey, QKeyValue<TKey, T>>
    {
        public new T this[TKey key]
        {
            get
            {
                var keyValue = base[key];
                return keyValue.Value;
            }
            set
            {
                base[key].Value = value;
            }
        }
        public QDictionary()
        {

		}
		public Func<TKey, T> createAction { private set; get; }
		public T defaultValue { private set; get; } = default;
        public QDictionary(T defaultValue)
        {
            this.defaultValue = defaultValue;
		}
		public QDictionary( Func<TKey,T> createAction)
		{
			this.createAction = createAction;
		}
		public void Add(TKey key, T value)
        {
            this[key] = value;
        }
		public void Set(TKey key, T value)
		{
			this[key] = value;
		}
		public T Get(TKey key,T defaultValue)
		{
			if (!ContainsKey(key))
			{
				Add(key, defaultValue);
;			}
			return this[key];
		}
		public override void OnCreate(QKeyValue<TKey, T> obj)
        {
			if (createAction != null)
			{
				obj.Value = createAction(obj.Key);
			}
			else
			{
				obj.Value = defaultValue;
			}
            base.OnCreate(obj);
        }
    }
    public class QList<T> : List<T>
    {
        public new T this[int index]
        {
            get
            {
                if (index < Count)
                {
                    return base[index];
                }
                else
                {
                    return default;
                }
            }
            set
            {
                for (int i = Count; i <= index; i++)
                {
                    Add(default);
                }
                base[index] = value;
            }
        }
    }
    public class QList<TKey, T> : QList<T> where T : IKey<TKey>
    {
		[NonSerialized]
		[XmlIgnore]
		public QKeyCache<TKey, T, int> Cache = new QKeyCache<TKey, T, int>();
		public QList()
		{
			Cache.GetCheckInfo = (Key) => Count;
		}
		
		public new void Add(T value)
        {
            if (value != null)
            {
                Set(value.Key, value);
            }
        }
        public new void Sort(Comparison<T> comparison)
        {
			Cache.Clear();
			base.Sort(comparison);
        }
        public new void Sort(int index, int count, IComparer<T> comparer)
        {
			Cache.Clear();
            base.Sort(index, count, comparer);
        }
        public new void Sort()
        {
			Cache.Clear();
            base.Sort();
        }
        public new bool Contains(T value)
        {
            return base.Contains(value);
        }
        public bool ContainsKey(TKey key)
        {
            if (key == null)
            {
                Debug.LogError("key is null");
                return false;
            }
            if (Cache.Cache.ContainsKey(key) && Cache.Cache[key] != null)
            {
                return true;
            }
            else
            {
                return this.ContainsKey<T, TKey>(key);
            }
        }
        public virtual T Get(TKey key)
        {
            if (key == null)
            {
                Debug.LogError("key is null");
                return default;
            }
			return Cache.Get(key,(key) =>
			{
				return this.Get<T, TKey>(key); 
			});
        }
        public virtual void Set(TKey key, T value)
        {
            if (key == null)
            {
                Debug.LogError("key is null");
            }
			Cache.Remove(key);
            this.Set<T, TKey>(key, value);
        }
        public void Remove(TKey key)
        {
            RemoveKey(key);
        }
        List<TKey> keyList = new List<TKey>();
        public new void RemoveAll(Predicate<T> match)
        {
            keyList.Clear();
            if (match != null)
            {
                foreach (var item in this)
                {
                    if (item == null) return;
                    if (match(item))
                    {
                        keyList.Add(item.Key);
                    }
                }
            }
            foreach (var key in keyList)
            {
                RemoveKey(key);
            }
        }
        public T this[TKey key]
        {
            get
            {
                return Get(key);
            }
            set
            {
                Set(key, value);
            }
        }
        public new void Remove(T obj)
        {
            if (obj != null)
			{
				Cache.Remove(obj.Key);
				base.Remove(obj);
            }
        }
        public void RemoveKey(TKey key)
        {
            Remove(this[key]);
        }
        public new void Clear()
        {
			Cache.Clear();
            base.Clear();
        }
        public new void Reverse(int index, int count)
        {
			Cache.Clear();
            base.Reverse(index, count);
        }
        public new void Reverse()
        {
			Cache.Clear();
            base.Reverse();
        }
    }
    public class QAutoList<TKey, T> : QList<TKey, T> where T : IKey<TKey>, new()
    {

        public override T Get(TKey key)
        {
			return Cache.Get(key, (key) =>
			{
				return this.GetAndCreate<T, TKey>(key, OnCreate);
			});
        }
        public virtual void OnCreate(T obj)
        {
            creatCallback?.Invoke(obj);
        }
        public event System.Action<T> creatCallback;
    }
    public static class ArrayTool
    {
        public static string SplitEndString(this string str,string splitStart)
        {
            if (str.Contains(splitStart)){

                return str.Substring(str.LastIndexOf(splitStart)+splitStart.Length);
            }
            else
            {
                return str;
            }
		}
		public static string SplitStartString(this string str, string splitStart)
		{
			if (str.Contains(splitStart))
			{

				return str.Substring(0,str.IndexOf(splitStart));
			}
			else
			{
				return str;
			}
		}
		public static string ForeachBlockValue(this string value, char startChar, char endChar, Func<string, string> action)
		{
			if (string.IsNullOrEmpty(value)) { return value; }
			var start = value.IndexOf(startChar);
			if (start < 0) return value;
			var end = value.IndexOf(endChar, start+1);
			if (start < 0 || end < 0) return value;
			while (start >= 0 && end >= 0)
			{
				var key = value.Substring(start + 1, end - start - 1);
				var result = action(key);
				value = value.Substring(0, start) + result + value.Substring(end + 1);
				end += result.Length - key.Length - 2;
				start = value.IndexOf(startChar, end+1);
				if (start < 0) break;
				end = value.IndexOf(endChar, start);
			}
			return value;
		}
		public static string GetBlockValue(this string value, char startChar, char endChar)
		{
			var start = value.IndexOf(startChar)+1;
			var end = value.IndexOf(endChar,start);
			if (end >= 0)
			{
				return value.Substring(start, end - start);
			}
			else
			{
				return value.Substring(start);
			}
		}
		public static string GetBlockValue(this string value,string startStr,string endStr)
		{
			var index = value.IndexOf(startStr);
			if (index < 0)
			{
				return "";
			}
			var start =index+ startStr.Length;
			
			var end = value.IndexOf(endStr,start);

			if (end >= 0)
			{
				return value.Substring(start, end - start);
			}
			else
			{
				return value.Substring(start);
			}
		}
		public static bool SplitTowString(this string str, string splitStart,out string start,out string end)
		{
			
			if (str.Contains(splitStart))
			{
				var startIndex = str.IndexOf(splitStart);
				start = str.Substring(0, startIndex);
				end = str.Substring(startIndex + 1);
				return true;
			}
			else
			{
				start = str;
				end = "";
				return false;
			}
		}
		public static string ToSizeString(this string array)
        {
            return array.Length.ToSizeString();
        }
        public static string ToSizeString(this IList array)
        {
            return array.Count.ToSizeString();
        }
        public static int RemoveNull<T>(this List<T> array)
        {
            return array.RemoveAll(obj => obj == null);
        }
        public static int RemoveSpace(this List<string> array)
        {
            return array.RemoveAll(obj => string.IsNullOrWhiteSpace(obj));
        }
        public static string ToSizeString(this float byteLength)
        {
            return ToSizeString((long)byteLength);
        }

        public static string ToSizeString(this int byteLength)
        {
            return ToSizeString((long)byteLength);
        }

        public static string ToSizeString(this long longLength)
        {
            string[] Suffix = { "Byte", "KB", "MB", "GB", "TB" };
            int i = 0;
            double dblSByte = longLength;
            if (longLength > 1024)
                for (i = 0; (longLength / 1024) > 0; i++, longLength /= 1024)
                    dblSByte = longLength / 1024.0;
            if (i == 0)
            {
                return dblSByte.ToString("f0") + "" + Suffix[i];
            }
            else
            {
                return dblSByte.ToString("f1") + "" + Suffix[i];
            }
        }
        public static string ToOneString<T>(this ICollection<T> array, string splitChar = "\n",Func<T,string> toString=null)
        {
            if (array == null)
            {
                return "";
            }
			return Tool.BuildString((writer) =>
			{
				int i = 0;
				if (toString == null)
				{
					foreach (var item in array)
					{
						writer.Write(item);
						if (i < array.Count - 1)
						{
							writer.Write(splitChar);
						}
						i++;
					}
				}
				else
				{
					foreach (var item in array)
					{
						writer.Write(toString(item));
						if (i < array.Count - 1)
						{
							writer.Write(splitChar);
						}
						i++;
					}
				}

			});
        }
        public static IList<T> Replace<T>(this IList<T> array, int indexA, int indexB)
        {
            if (indexA == indexB) return array;
            var temp = array[indexA];
            array[indexA] = array[indexB];
            array[indexB] = temp;
            return array;
        }
        public static IList CreateAt(this IList list,QSerializeType typeInfo, int index=-1)
        {
			var newObj = typeInfo.ElementType.CreateInstance(index < 0 ? null : list[index],true);
			if (index < 0)
			{
				index = 0;
			}
            if (list.IsFixedSize)
            {
                if (typeInfo.ArrayRank == 1)
                {
                    var newList= typeInfo.Type.CreateInstance(null,false, list.Count + 1) as IList;
                 
                    for (int i = 0; i < index; i++)
                    {
                        newList[i] = list[i];
                    }
                    newList[index] = newObj;
                    for (int i = index+1; i < newList.Count; i++)
                    {
                        newList[i] = list[i-1];
                    }
                    return newList;
                }
            }
            else
            {
                list.Add(newObj);
            }
            return list;

        }
        public static IList RemoveAt(this IList list,QSerializeType typeInfo,  int index)
        {
            if (list.IsFixedSize)
            {
                if (typeInfo.ArrayRank == 1)
                {
                    var newList = typeInfo.Type.CreateInstance(null,false, list.Count -1) as IList;

                    for (int i = 0; i < index; i++)
                    {
                        newList[i] = list[i];
                    }
                    for (int i = index ; i < newList.Count; i++)
                    {
                        newList[i] = list[i +1];
                    }
                    return newList;
                }
            }
            else
            {
                list.RemoveAt(index);
            }
            return list;

        }
        public static bool ContainsKey<T, KeyType>(this IList<T> array, KeyType key) where T : IKey<KeyType>
        {
            return array.ContainsKey(key, (item) => item.Key);
        }
		public static void CheckSet<TKey,TValue>(this IDictionary<TKey,TValue> dic, TKey key,TValue value) 
		{
			if (dic.ContainsKey(key))
			{
				dic[key] = value;
			}
			else
			{
				dic.Add(key, value);
			}
		}
		public static bool ContainsKey<T, KeyType>(this IList<T> array, KeyType key, Func<T, KeyType> keyGetter)
        {
            if (key == null)
            {
                return false;
            }
			for (int i = 0; i < array.Count; i++)
			{
				var value = array[i];
				if (value == null) continue;
				if (key.Equals(keyGetter(value)))
                {
                    return true;
                }
            }
            return false;
        }
        public static T Get<T>(this IList<T> array,int index) 
        {
            if (index < 0 || index >= array.Count) return default;
            return array[index];
        }
        public static T Get<T, KeyType>(this IList<T> array, KeyType key) where T : IKey<KeyType>
        {
            return array.Get(key, (item) => item.Key);
        }
        public static T Get<T, KeyType>(this IList<T> array, KeyType key, Func<T, KeyType> keyGetter)
        {
            if (key == null)
            {
                return default;
            }
			for (int i = 0; i < array.Count; i++)
			{
				var value = array[i];
				if (value == null) continue;
                if (key.Equals(keyGetter(value)))
                {
                    return value;
                }
            }
            return default;
        }
        public static List<T> GetList<T, KeyType>(this IList<T> array, KeyType key, List<T> tempList = null) where T : IKey<KeyType>
        {
            var list = tempList == null ? new List<T>() : tempList;
			for (int i = 0; i < array.Count; i++)
			{
				var value = array[i];
				if (key.Equals(value.Key))
				{
					list.Add(value);
				}
			}
          
            return list;
        }
        public static T StackPeek<T>(this IList<T> array)
        {
            if (array == null || array.Count == 0)
            {
                return default;
            }
            return array[array.Count - 1];
        }
        public static T QueuePeek<T>(this IList<T> array)
        {
            if (array == null || array.Count == 0)
            {
                return default;
            }
            return array[0];
        }
        public static object[] ToObjects<T>(this IList<T> array)
        {
            var objs = new object[array.Count];
            for (int i = 0; i < array.Count; i++)
            {
                objs[i] = array[i];
            }
            return objs;
        }
        public static void Enqueue<T>(this IList<T> array, T obj)
        {
            array.Add(obj);
        }
        public static void Push<T>(this IList<T> array, T obj)
        {
            array.Add(obj);
        }
        public static T Pop<T>(this IList<T> array)
        {
            if (array == null || array.Count == 0)
            {
                return default;
            }
            var obj = array.StackPeek();
            array.RemoveAt(array.Count - 1);
            return obj;
        }
        public static T Dequeue<T>(this IList<T> array)
        {
            if (array == null || array.Count == 0)
            {
                return default;
            }
            var obj = array.QueuePeek();
            array.RemoveAt(0);
            return obj;
        }
        public static void AddCheckExist<T>(this IList<T> array, params T[] objs)
        {
            foreach (var obj in objs)
            {
                if (!array.Contains(obj))
                {
                    array.Add(obj);
                }
            }
        }
        public static void RemoveKey<T, KeyType>(this IList<T> array, KeyType key) where T : IKey<KeyType>
        {
            var old = array.Get(key);
            if (old != null)
            {
                array.Remove(old);
            }
        }
        public static void Set<T, KeyType>(this IList<T> array, KeyType key, T value) where T : IKey<KeyType>
        {
            array.RemoveKey(key);
            value.Key = key;
            array.Add(value);
        }

        public static T GetAndCreate<T, KeyType>(this IList<T> array, KeyType key, System.Action<T> creatCallback = null) where T : IKey<KeyType>, new()
        {
            var value = array.Get(key);
            if (value != null)
            {
                return value;
            }
            else
            {
                var t = new T { Key = key };
                creatCallback?.Invoke(t);
                array.Add(t);
                return t;
            }
        }
    }
}
