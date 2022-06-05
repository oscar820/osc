using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Reflection;
namespace QTool
{
    public static class QTime
    {
        public static void Clear()
        {
            timeScaleList.Clear();
            UpdateTimeScale();
        }
        public static event System.Action<float> OnScaleChange;

        private static void UpdateTimeScale()
        {
            var value = 1f;
            foreach (var kv in timeScaleList)
            {
                value *= kv.Value;
            }
            Time.timeScale = value;
            OnScaleChange?.Invoke(value);
        }

        static QDictionary<object, float> timeScaleList = new QDictionary<object, float>();
        public static void ChangeScale(object obj, float timeScale)
        {
            if (timeScale==1)
            {
				timeScaleList.RemoveKey(obj);
			}
			else
			{
				timeScaleList[obj] = timeScale;
			}
			UpdateTimeScale();
		}
        public static void RevertScale(object obj)
        {
            timeScaleList.RemoveKey(obj);
            UpdateTimeScale();
        }
    }
	public class WaitTime : WaitTime<float>
	{
	}
	public class WaitTime<T>
    {
        public T Time { get; protected set; }
        public T CurTime { get; protected set; }


        public void Clear()
		{
			CurTime = 0.ConvertTo<T>();
		}
        public void Over()
        {
            CurTime = Time;
        }
        public void SetCurTime(T curTime)
        {
            CurTime = curTime;
        }
        public void Reset(T time, bool startOver = false)
        {
            this.Time = time;
            Clear();
            if (startOver) Over();
        }
		bool IsOver(out T timeOffset)
		{
			timeOffset =(T) CurTime.OperaterSubtract( Time);
			return timeOffset.OperaterGreaterThan(0);
		}
        public bool Check(T deltaTime, bool autoClear = true)
        {
			CurTime = (T)CurTime.OperaterAdd(deltaTime);
            if (IsOver(out var timeOffset))
            {
                if (autoClear) { CurTime = timeOffset; }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
