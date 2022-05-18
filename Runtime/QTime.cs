using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    public abstract class WaitTimeBase<T>
    {
        public T Time { get; protected set; }
        public T CurTime { get; protected set; }


        public abstract void Clear();
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

        protected abstract void AddTime(T addValue);
        protected abstract bool IsOver(out T timeOffset);
        public bool Check(T deltaTime, bool autoClear = true)
        {
            AddTime(deltaTime);
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
    public class WaitTime : WaitTimeBase<float>
    {
        public WaitTime(float time, bool startOver = false)
        {
            Reset(time, startOver);
        }
        public override void Clear()
        {
            CurTime = 0;
        }
        protected override void AddTime(float addValue)
        {
            CurTime += addValue;
        }

        protected override bool IsOver(out float timeOffset)
        {
            timeOffset = CurTime - Time;
            return timeOffset > 0;
        }
    }
}
