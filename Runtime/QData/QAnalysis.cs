using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QTool
{
	public static class QAnalysis
	{
		public enum QAnalysisEventName
		{
			游戏开始,
			游戏结束,
			游戏暂离,
		}
		public static string PlayerId { private set; get; }
		public static bool InitOver
		{
			get
			{
				if (string.IsNullOrWhiteSpace(PlayerId))
				{
					Debug.LogError(StartKey + "未设置账户ID");
					return false;
				}
				return true;
			}
		}
		public static int SendCount { get; set; } =30;
		public static void Start(string id)
		{
			SendEventList();
			if (id == PlayerId)
			{
				Debug.LogError(StartKey+" 已登录" + id);
				return;
			}
			PlayerId = id;
			if (!InitOver)
			{
				return;
			}
			Trigger(nameof(QAnalysisEventName.游戏开始),new StartInfo());

			Application.focusChanged += OnFocus;
			Application.quitting += Stop;
		}
		static void OnFocus(bool focus)
		{
			if (!focus)
			{
				Trigger(nameof(QAnalysisEventName.游戏暂离));
				SendEventList();
			}
		}
		public static void Stop() 
		{
			if (!InitOver)
			{
				return;
			}
			Trigger(nameof(QAnalysisEventName.游戏结束));
			SendEventList();
			Application.focusChanged -= OnFocus;
			Application.quitting -= Stop;
			PlayerId = null;
		}
		public static string StartKey => nameof(QAnalysis) + "_" + Application.productName;
		public static string EventListKey => StartKey + "_" + nameof(triggerEventList);
		public static void SendEventList()
		{
			if (!QToolSetting.Instance.QAnalysisMail.InitOver)
			{
				Debug.LogError(nameof(QToolSetting.Instance.QAnalysisMail) + " 未设置");
				return; 
			}
			if (PlayerPrefs.HasKey(EventListKey))
			{
				var data =PlayerPrefs.GetString( EventListKey);
				QMailTool.Send(QToolSetting.Instance.QAnalysisMail, QToolSetting.Instance.QAnalysisMail.account, StartKey + "_" + SystemInfo.deviceName + "_" + PlayerId, data);
				triggerEventList.Clear();
				PlayerPrefs.DeleteKey(EventListKey);
			}
		}
		static List<QAnalysisEvent> triggerEventList = new List<QAnalysisEvent>();
		public static void Trigger(string eventKey,object value=null)
		{
			if (InitOver)
			{
				try
				{
					var eventData = new QAnalysisEvent
					{
						playerId = PlayerId,
						eventKey = eventKey,
						eventValue = value,
					};
					triggerEventList.Add(eventData);
					Debug.Log(StartKey + " 触发事件 " + eventData);
					PlayerPrefs.SetString(EventListKey, triggerEventList.ToQData());
					if (SendCount >= 1 && triggerEventList.Count >= SendCount)
					{
						SendEventList();
					}

				}
				catch (Exception e)
				{
					Debug.LogError(nameof(QAnalysis) + "触发事件 " + eventKey + " " + value + " 出错：\n" + e);
				}
				
			}
		}
		public static void Trigger(string eventKey,string key, object value)
		{
			Trigger(eventKey, new KeyValuePair<string, object>(key, value));
		}
	}
	public class StartInfo
	{
		public RuntimePlatform platform = Application.platform;
		public string version = Application.version;
		public string deviceName = SystemInfo.deviceName;
		public string deviceUniqueIdentifier = SystemInfo.deviceUniqueIdentifier;
		public string os = SystemInfo.operatingSystem;
		public string deviceModel = SystemInfo.deviceModel;
		public string cpu = SystemInfo.processorType;
		public int cpuCount = SystemInfo.processorCount;
		public int cpuFrequency = SystemInfo.processorFrequency;
		public int systemMemorySize = SystemInfo.systemMemorySize;
		public string gpu = SystemInfo.graphicsDeviceName;
		public int gpuMemorySize = SystemInfo.graphicsMemorySize;
	}

	public class QAnalysisEvent:IKey<string>
	{
		public string eventKey;
		public object eventValue;
		public DateTime eventTime = DateTime.Now;
		public string playerId;
		public string eventId = QId.GetNewId();
		[QIgnore]
		public string Key { get => eventId; set => eventId = value; }
	
		public override string ToString()
		{
			return this.ToQData(false);
		}
		public object GetValue(string dataKey)
		{
			if (eventValue == null)
			{
				return eventValue;
			}
			if (dataKey.Contains("/"))
			{
				var memeberKey = dataKey.SplitEndString("/");
				var typeInfo = QSerializeType.Get(eventValue.GetType());
				return typeInfo.Members[memeberKey].Get(eventValue);
			}
			else
			{
				return eventValue;
			}
		}
	}

}
