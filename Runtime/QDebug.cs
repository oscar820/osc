using System;
using UnityEngine;

namespace QTool
{
    public class QToolDebug : QDebug<QToolDebug>
    {

    }
    public class QDebug<T> where T : QDebug<T>
    {
        public static bool ShowLog
        {
            get
            {
                if (!QDataList.QToolSetting.ContainsKey(Key)&&Application.isEditor)
                {
                    QDataList.QToolSetting[Key].SetValue(true);
                }
                return QDataList.QToolSetting[Key].GetValue<bool>();
            }
        }
        public static string Key
        {
            get
            {
                return typeof(T).Name;
            }
        }
        public static void Log(object log)
        {
            if (ShowLog)
            {
                Debug.Log(Key + ":" + log);
            }
        }
        public static void Log(Func<object> log)
        {
            if (ShowLog)
            {
                Debug.Log(Key + ":" + log?.Invoke());
            }
        }
        public static void LogWarning(object log)
        {
            if (ShowLog)
            {
                Debug.LogWarning(Key + ":" + log);
            }
        }
        public static void LogWarning(Func<object> log)
        {
            if (ShowLog)
            {
                Debug.LogWarning(Key + ":" + log?.Invoke());
            }
        }
    }
}