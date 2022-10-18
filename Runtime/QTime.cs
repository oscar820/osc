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
			QEventManager.Trigger("时间更改信息","TimeScale "+Time.timeScale+"  "+ timeScaleList.ToOneString());
		}
		public static float GetTimeScale(object obj)
		{
			if (timeScaleList.ContainsKey(obj))
			{
				return timeScaleList[obj];
			}
			else
			{
				return 1;
			}
		}

        static QDictionary<object, float> timeScaleList = new QDictionary<object, float>();
        public static void ChangeScale(object obj, float timeScale)
        {
			if (timeScaleList.ContainsKey(obj))
			{
				if (timeScaleList[obj] == timeScale) return;
			}
			else
			{
				if (timeScale == 1) return;
			}
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
			if (timeScaleList.ContainsKey(obj))
			{
				timeScaleList.RemoveKey(obj);
				UpdateTimeScale();
			}
        }
    }
	public class QWaitTime
	{
        public float Time { get; protected set; }
        public float CurTime { get; protected set; }

		public QWaitTime()
		{

		}
		public QWaitTime(float Time, bool startOver = false)
		{
			Reset(Time, startOver);
		}
		public void Clear()
		{
			CurTime = 0;
		}
        public void Over()
        {
            CurTime = Time;
        }
        public void SetCurTime(float curTime)
        {
            CurTime = curTime;
        }
        public void Reset(float time, bool startOver = false)
        {
            this.Time = time;
            Clear();
            if (startOver) Over();
        }
		bool IsOver(out float timeOffset)
		{
			timeOffset =CurTime- Time;
			return timeOffset>0;
		}
        public bool Check(float deltaTime, bool autoClear = true)
        {
			CurTime = CurTime+deltaTime;
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
