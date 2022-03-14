using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
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
        public T defaultValue { private set; get; } = default;
        public QDictionary(T defaultValue)
        {
            this.defaultValue = defaultValue;
        }
        public void Add(TKey key, T value)
        {
            this[key] = value;
        }
        public override void OnCreate(QKeyValue<TKey, T> obj)
        {
            obj.Value = defaultValue;
            base.OnCreate(obj);
        }
    }
    public class QList<TKey, T> : List<T> where T : IKey<TKey>
    {
        [NonSerialized]
        [XmlIgnore]
        protected Dictionary<TKey, T> dic = new Dictionary<TKey, T>();
        public new void Add(T value)
        {
            if (value != null)
            {
                Set(value.Key, value);
            }
        }
        public new void Sort(Comparison<T> comparison)
        {
            dic.Clear();
            base.Sort(comparison);
        }
        public new void Sort(int index, int count, IComparer<T> comparer)
        {
            dic.Clear();
            base.Sort(index, count, comparer);
        }
        public new void Sort()
        {
            dic.Clear();
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
                Debug.LogError("key Ϊ null");
                return false;
            }
            if (dic.ContainsKey(key) && dic[key] != null)
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
                Debug.LogError("key Ϊ null");
                return default;
            }
            if (!dic.ContainsKey(key))
            {
                var value = this.Get<T, TKey>(key);
                if (value != null)
                {
                    dic[key] = value;
                }
                else
                {
                    return default;
                }
            }
            return dic[key];
        }
        public virtual void Set(TKey key, T value)
        {
            if (key == null)
            {
                Debug.LogError("key Ϊ null");
            }
            if (dic.ContainsKey(key))
            {
                dic[key] = value;
            }
            else
            {
                dic.Add(key, value);
            }
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
                base.Remove(obj);
                dic.Remove(obj.Key);
            }
        }
        public void RemoveKey(TKey key)
        {
            Remove(this[key]);
        }
        public new void Clear()
        {
            dic.Clear();
            base.Clear();
            //dic.Clear();
        }
        public new void Reverse(int index, int count)
        {
            dic.Clear();
            base.Reverse(index, count);
        }
        public new void Reverse()
        {
            dic.Clear();
            base.Reverse();
        }
    }
    public class QAutoList<TKey, T> : QList<TKey, T> where T : IKey<TKey>, new()
    {

        public override T Get(TKey key)
        {
            if (!dic.ContainsKey(key))
            {
                dic[key] = this.GetAndCreate<T, TKey>(key, OnCreate);
            }
            return dic[key];
        }
        public virtual void OnCreate(T obj)
        {
            creatCallback?.Invoke(obj);
        }
        public event System.Action<T> creatCallback;
    }
    public static class ArrayTool
    {
     

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
        public static string ToOneString<T>(this ICollection<T> array, string splitChar = "\n")
        {
            var str = "";
            if (array == null)
            {
                return str;
            }
            int i = 0;
            foreach (var item in array)
            {
                str += item + (i != array.Count - 1 ? splitChar : "");
                i++;
            }
            return str;
        }
        public static List<T> Replace<T>(this List<T> array, int indexA, int indexB)
        {
            if (indexA == indexB) return array;
            var temp = array[indexA];
            array[indexA] = array[indexB];
            array[indexB] = temp;
            return array;
        }
        public static bool ContainsKey<T, KeyType>(this ICollection<T> array, KeyType key) where T : IKey<KeyType>
        {
            return array.ContainsKey(key, (item) => item.Key);
        }
        public static bool ContainsKey<T, KeyType>(this ICollection<T> array, KeyType key, Func<T, KeyType> keyGetter)
        {
            if (key == null)
            {
                return false;
            }
            foreach (var value in array)
            {
                if (key.Equals(keyGetter(value)))
                {
                    return true;
                }
            }
            return false;
        }
        public static T Get<T, KeyType>(this ICollection<T> array, KeyType key) where T : IKey<KeyType>
        {
            return array.Get(key, (item) => item.Key);
        }
        public static T Get<T, KeyType>(this ICollection<T> array, KeyType key, Func<T, KeyType> keyGetter)
        {
            if (key == null)
            {
                return default;
            }
            foreach (var value in array)
            {
                if (value == null) continue;
                if (key.Equals(keyGetter(value)))
                {
                    return value;
                }
            }
            return default;
        }
        public static List<T> GetList<T, KeyType>(this ICollection<T> array, KeyType key, List<T> tempList = null) where T : IKey<KeyType>
        {
            var list = tempList == null ? new List<T>() : tempList;
            foreach (var value in array)
            {
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
        public static void RemoveKey<T, KeyType>(this ICollection<T> array, KeyType key) where T : IKey<KeyType>
        {
            var old = array.Get(key);
            if (old != null)
            {
                array.Remove(old);
            }
        }
        public static void Set<T, KeyType>(this ICollection<T> array, KeyType key, T value) where T : IKey<KeyType>
        {
            array.RemoveKey(key);
            value.Key = key;
            array.Add(value);
        }

        public static T GetAndCreate<T, KeyType>(this ICollection<T> array, KeyType key, System.Action<T> creatCallback = null) where T : IKey<KeyType>, new()
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
