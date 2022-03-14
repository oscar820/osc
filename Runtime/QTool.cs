using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Serialization;
using System;
using System.Threading.Tasks;

namespace QTool
{
   
    
    public static partial class Tool
    {
        public static void RunTimeCheck(string name, System.Action action, Func<int> getLength = null)
        {
            var last = System.DateTime.Now;
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError("【" + name + "】运行出错:"+e);
                return;
            }
            Debug.LogError("【" + name + "】运行时间:" + (System.DateTime.Now - last).TotalMilliseconds + (getLength == null ? "" : " 长度" + getLength().ToSizeString()));
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
        public static async Task<bool> DelayGameTime(float second, bool ignoreTimeScale = false)
        {
            var startTime = (ignoreTimeScale ? Time.unscaledTime : Time.time);

            while (startTime + second > (ignoreTimeScale ? Time.unscaledTime : Time.time) && Application.isPlaying)
            {
                await Task.Yield();
            }
            return Application.isPlaying;
        }
        public static async Task<bool> Wait(Func<bool> flagFunc)
        {
            if (flagFunc == null) return Application.isPlaying;
            while (!flagFunc.Invoke() && Application.isPlaying)
            {
                await Task.Yield();
            }
            return Application.isPlaying;
        }
        public static bool TryParse<T>(this string str,out T t)
        {
            if(TryParse(str, typeof(T), out var obj))
            {
                t = (T)obj;
                return true;
            }
            else
            {
                t = default;
                return false;
            }
            
        }
        public static bool TryParse(this string str, Type type, out object obj)
        {
            switch (type.Name)
            {
                case nameof(System.Object):
                case nameof(String):
                    obj = str;
                    return true;
                case nameof(Int16):
                case nameof(Int32):
                case nameof(Int64):
                    if (int.TryParse(str, out var intValue))
                    {
                        obj = intValue;
                        return true;
                    }
                    break;
                case nameof(Single):
                case nameof(Double):
                    if (float.TryParse(str, out var floatValue))
                    {
                        obj = floatValue;
                        return true;
                    }
                    break;
                case nameof(Boolean):
                    if(bool.TryParse(str,out var boolValue))
                    {
                        obj = boolValue;
                        return true;
                    }
                    break;
                default:
                    break;
            }
            obj = null;
            return false;
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
    }
    public class SecondsAverageList
    {
        public float Value
        {
            get; private set;
        }
        QDictionary<float, float> list = new QDictionary<float, float>();
        public float AllSum { private set; get; }
        float _lastSumTime=-1;
        float _secondeSum = 0;
        public float EndTime
        {
            get
            {
                if (list.Count == 0) return 0;
                return list.StackPeek().Value;
            }
        }
        public float SecondeSum
        {
            get
            {
                var endTime = EndTime;
                if (endTime == 0) return 0;
                if (endTime == _lastSumTime)
                {
                    return _secondeSum;
                }
                _lastSumTime = endTime;
                _secondeSum = 0f;
                foreach (var kv in list)
                {
                    _secondeSum += kv.Value;
                }
                return _secondeSum ;
            }
        }
        int maxCount = -1;
        public void Push(float value)
        {
            AllSum += value;
            list.RemoveAll((kv) => (Time.time - kv.Key) > 1);
            list[Time.time]= value;
            Value = SecondeSum/list.Count;
        }
        public void Clear()
        {
            list.Clear();
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