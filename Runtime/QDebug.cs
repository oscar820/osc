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
                if (!QDataTable.QToolSetting.ContainsKey(Key)&&Application.isEditor)
                {
                    QDataTable.QToolSetting[Key].SetValue(false);
                }
                return QDataTable.QToolSetting[Key].GetValue<bool>();
            }
        }
        public static string Key
        {
            get
            {
                return typeof(T).Name;
            }
        }
        public static void Log(Func<string> log)
        {
            if (ShowLog)
            {
                Debug.Log(Key + ":" + log?.Invoke());
            }
        }
        public static void LogWarning(Func<string> log)
        {
            if (ShowLog)
            {
                Debug.LogWarning(Key + ":" + log?.Invoke());
            }
        }
    }
}