using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Serialization;
using System;
using System.Threading.Tasks;
using QTool.Reflection;
using System.Reflection;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace QTool
{


    public static partial class Tool
    {
		public static string Version => Application.version;
		public static bool IsTestVersion => Application.version.StartsWith("0.");
        static QDictionary<string, Color> KeyColor = new QDictionary<string, Color>();
        public static Color ToColor(this string key, float s = 0.5f, float v = 1f)
        {
            if (string.IsNullOrWhiteSpace(key)) return Color.white;
            var colorKey = key + s + v;
            if (!KeyColor.ContainsKey(colorKey))
            {
                var colorValue = Mathf.Abs(key[0].GetHashCode() % 800) + Mathf.Abs(key.GetHashCode() % 200f);
                KeyColor[colorKey] = Color.HSVToRGB(colorValue / 1000, s, v);
            }
            return KeyColor[colorKey];
        }
		public static Transform GetChild(this Transform transform,string childPath)
		{
			if (childPath.SplitTowString(".", out var start, out var end))
			{
				try
				{
					return transform.GetChild(start).GetChild(end);
				}
				catch (Exception e)
				{
					throw new Exception("路径出错 [" + childPath+"]", e);
				}
			}
			else
			{
				var child = transform.Find(start);
				if (child!=null)
				{
					return child;
				}
				else
				{
					throw new Exception(" 找不到 key [" + start+"]"+childPath);
				}
			}
		}
		public static string RemveChars(this string str,params char[] exceptchars)
		{
			foreach (var c in exceptchars)
			{
				str = str.Replace(c.ToString(), "");
			}
			return str;
		}
		public static string ToIdString(this string str, int length = -1)
		{
			if (length > 0)
			{
				str = str.Substring(0, Math.Min(str.Length, length));
			}
			return str.RemveChars('{','}', '（','）','~', '“','”', '—','。', '…','=','#', ' ', ';', '；', '-', ',', '，', '<', '>', '【', '】', '[', ']', '{', '}', '!', '！', '?', '？', '.', '\'', '‘', '’', '\"', ':', '：');
		}
		public static float ToComputeFloat(this object value)
		{
			if (value == null) return 0;
			if(value is string str)
			{
				if(float.TryParse(str, out var newFloat))
				{
					return newFloat;
				}
				else
				{
					List<string> numbers = new List<string>();
					var newNamber ="";
					for (int i = str.Length-1; i>=0; i--)
					{
						var c = str[i];
						if (char.IsNumber(c))
						{
							newNamber += c;
						}
						else
						{
							if (newNamber.Length > 0)
							{
								numbers.Add(newNamber);
								newNamber = "";
							}
						}
					}
					var sum = 0f;
					for (int i = 0; i < numbers.Count; i++)
					{
						sum += float.Parse(numbers[i]) * Mathf.Pow(10, i * 2);
					}
					return sum;
				}
			}
			return Convert.ToSingle(value);
		}
		public static void RunTimeCheck(string name, System.Action action, Func<int> getLength = null, Func<string> getInfo = null)
        {
            var last = System.DateTime.Now;
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError("【" + name + "】运行出错:" + e);
                return;
            }
            var checkInfo = "【" + name + "】运行时间:" + (System.DateTime.Now - last).TotalMilliseconds;
            if (getLength != null)
            {
                checkInfo += " " + " 大小" + getLength().ToSizeString();
            }
            if (getInfo != null)
            {
                checkInfo += "\n" + getInfo();
            }
            Debug.LogError(checkInfo);
        }

        public static bool PercentRandom(float percent)
        {
            var value = UnityEngine.Random.Range(0, 100);
            return value <= percent;
        }
        public static T RandomGet<T>(this IList<T> list)
        {
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        public static IList<T> Random<T>(this IList<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var cur = list[i];
                list.Remove(cur);
                list.Insert(UnityEngine.Random.Range(0, i), cur);
            }
            return list;
        }
        public static async Task<bool> WaitGameTime(float second, bool ignoreTimeScale = false)
        {
            var startTime = (ignoreTimeScale ? Time.unscaledTime : Time.time);
            return await Wait(() => startTime + second <= (ignoreTimeScale ? Time.unscaledTime : Time.time));
        }

		public static string ToQTimeString(this DateTime time)
		{
			return time.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"); 
		}
		public static Action StopAllWait;

		public static async Task<bool> Wait(Func<bool> flagFunc)
		{
			var WaitStop = false;
			if (flagFunc == null) return Application.isPlaying ;
			Action OnWaitStop = () => { WaitStop = true; };
			StopAllWait += OnWaitStop;
			while (!flagFunc.Invoke())
			{
				await Task.Delay(100);
				if (!Application.isPlaying||WaitStop)
				{
					StopAllWait -= OnWaitStop;
					return false;
				}
			}
			StopAllWait -= OnWaitStop;
			return true;
		}
        internal static void ForeachArray(this Array array, int deep, int[] indexArray, Action<int[]> Call, Action start = null, Action end = null, Action mid = null)
        {
            start?.Invoke();
            var length = array.GetLength(deep);
            for (int i = 0; i < length; i++)
            {
                indexArray[deep] = i;
                if (deep + 1 < indexArray.Length)
                {
                    ForeachArray(array, deep + 1, indexArray, Call, start, end, mid);
                }
                else
                {
                    Call?.Invoke(indexArray);
                }
                if (i < length - 1)
                {

                    mid?.Invoke();
                }

            }
            end?.Invoke();
        }

		public static string BuildString(Action<StringWriter> action)
		{
			using (var writer=new StringWriter())
			{
				action(writer);
				return writer.ToString();
			}
		}
		public static bool NextIs(this StringReader reader, char value)
        {
            if (reader.Peek() == value)
            {
                reader.Read();
                return true;
            }
            return false;
        }
        public static void NextIgnore(this StringReader reader, char value)
        {
            if (reader.Peek() == value)
            {
                reader.Read();
            }
        }
        public static bool IsEnd(this StringReader reader)
        {
            return reader.Peek() < 0;
		}
        /// <summary>
        /// 获取异或校验值
        /// </summary>
        public static byte ToCheckFlag(this byte[] bytes, byte flag = 0)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                flag ^= bytes[i];
            }
            return flag;
        }
		public static bool IsPrefabAsset(this UnityEngine.Object obj)
		{
#if UNITY_EDITOR
			return UnityEditor.PrefabUtility.IsPartOfPrefabAsset(obj);
#else
			return false;
#endif
		}

		public static bool IsPrefabInstance(this UnityEngine.Object obj)
		{
#if UNITY_EDITOR
			var gameObj = obj.GetGameObject();
			return gameObj!=null&&UnityEditor.PrefabUtility.IsAnyPrefabInstanceRoot(gameObj) && !obj.IsPrefabAsset();
#else
            return false;
#endif
		}
		public static bool IsSceneInstance(this UnityEngine.Object obj)
		{
#if UNITY_EDITOR
			if (obj == null)
			{
				return false;
			}
			if (UnityEditor.EditorUtility.IsPersistent(obj))
			{
				return false;
			}
#endif
			return true;
		}
		public static GameObject GetGameObject(this UnityEngine.Object obj)
		{
			if (obj is Component com)
			{
				return com.gameObject;
			}
			if (obj is GameObject gameObject)
			{
				return gameObject;
			}
			else
			{
				return null;
			}
		}
		public static GameObject GetPrefab(this UnityEngine.Object obj)
		{
#if UNITY_EDITOR
			var gameObj= obj.GetGameObject();
			return gameObj == null ? null : UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObj));
#else
            return null;
#endif
		}
		//public static GameObject CreateInstance(this GameObject prefab)
		//{
		//	var obj = GameObject.Instantiate(prefab);
		//	if (obj.transform is RectTransform)
		//	{
		//		(obj.transform as RectTransform).anchoredPosition = (prefab.transform as RectTransform).anchoredPosition;
		//	}
		//	return obj;
		//}
	}
    public class SecondsAverageList
    {
        public float Value
        {
            get; private set;
        }
        QDictionary<double, float> list = new QDictionary<double, float>();
        public float AllSum { private set; get; }
        
        double _lastSumTime=-1;
        float _secondeSum = 0;
        public double StartTime { private set; get; } = -1;
        public double EndTime { private set; get; }
        public float SecondeSum
        {
            get
            {
                if (EndTime == 0) return 0;
                if (EndTime == _lastSumTime)
                {
                    return _secondeSum;
                }
                _lastSumTime = EndTime;
                _secondeSum = 0f;
                foreach (var kv in list)
                {
                    _secondeSum += kv.Value;
                }
                return _secondeSum ;
            }
        }
        int maxCount = -1;
        static double CurTime
        {
            get
            {
                return (DateTime.Now - new DateTime()).TotalSeconds;
            }
        }
		
		public void Push(float value)
        {
            if (StartTime<0)
            {
                StartTime = CurTime;
            }
            AllSum += value;
            list.RemoveAll((kv) => (CurTime - kv.Key) > 1);
            list[CurTime] = value;
            EndTime = CurTime;
            Value = SecondeSum/list.Count;
        }
        public void Clear()
        {
            list.Clear();
            StartTime = -1;
            _lastSumTime = -1;
            _secondeSum = 0;
        }
        public override string ToString()
        {
            return "总记[" + AllSum + "]平均[" + Value + "/s]";
        }
        public  string ToString(Func<float,string> toString)
        {
            return "总记[" + toString(AllSum) + "]平均[" + toString(Value) + "/s]";
        }
    }



}
