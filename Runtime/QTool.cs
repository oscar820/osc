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
                default:
                    break;
            }
            Debug.LogError("[" + str + "]无法解析成类型[" + type.Name + "]");
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
   
    



}