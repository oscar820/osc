using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace QTool
{
	public static class QAnalysis
	{
		public enum QAnalysisEventName
		{
			游戏开始,
			游戏结束,
			游戏暂离,
			错误日志,
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
		public static int AutoSendCount { get; set; } =100;
		public static void Start(string playerId)
		{
			try
			{
				if (PlayerPrefs.HasKey(EventListKey))
				{
					PlayerPrefs.GetString(EventListKey).ParseQData(EventList);
				}
			}
			catch (Exception e)
			{
				Debug.LogError("读取记录信息出错：\n" + e);
			}
			sendTask =SendAndClear();
			if (playerId == PlayerId)
			{
				Debug.LogError(StartKey+" 已登录" + playerId);
				return;
			}
			PlayerId = playerId;
			if (!InitOver)
			{
				return;
			}
			Trigger(nameof(QAnalysisEventName.游戏开始),new StartInfo());
			errorInfoList.Clear();
			Application.focusChanged += OnFocus;
			Application.logMessageReceived += LogCallback;
			Application.wantsToQuit += OnWantsQuit;
		}
		static List<string> errorInfoList = new List<string>();
		static void LogCallback(string condition, string stackTrace, LogType type)
		{
			switch (type)
			{
				case LogType.Warning:
					break;
				case LogType.Log:
					break;
				case LogType.Error:
					break;
				default:
					if (!errorInfoList.Contains(condition))
					{
						errorInfoList.Add(condition);
						Trigger(nameof(QAnalysisEventName.错误日志), condition + '\n' + stackTrace);
					}
					break;
			}
		}
		static void OnFocus(bool focus)
		{
			if (!focus)
			{
				Trigger(nameof(QAnalysisEventName.游戏暂离));
				if (!Application.isEditor)
				{
					sendTask=SendAndClear();
				}
			}
		}
		static bool OnWantsQuit()
		{
			if (stopTask==null)
			{
				stopTask = Stop();
				stopTask.GetAwaiter().OnCompleted(() =>
				{
					Application.Quit();
				});
			}
			return false;
		}
		static Task stopTask = null;
		static Task sendTask = null;
		public static async Task Stop()
		{
			if (!InitOver)
			{
				return;
			}
			if (sendTask != null)
			{
				await sendTask;
			}
			Trigger(nameof(QAnalysisEventName.游戏结束));
			Application.focusChanged -= OnFocus;
			Application.logMessageReceived -= LogCallback;
			PlayerId = null;
			await SendAndClear();
			Application.wantsToQuit -= OnWantsQuit;
			stopTask = null;
		}
		public static string StartKey => nameof(QAnalysis) + "_" + Application.productName;
		public static string EventListKey => StartKey + "_" + nameof(EventList);
	
		static async Task SendAndClear()
		{
			if (sendTask != null)
			{
				await sendTask;
			}
			if (!QToolSetting.Instance.QAnalysisMail.InitOver)
			{
				Debug.LogError(nameof(QToolSetting.Instance.QAnalysisMail) + " 未设置");
				return;
			}
			if (PlayerPrefs.HasKey(EventListKey))
			{
				var count = EventList.Count;
				if (count > 0)
				{
					List<QAnalysisEvent> tempList = new List<QAnalysisEvent>();
					var data = "";
					lock (EventList)
					{
						tempList.AddRange(EventList);
						data = PlayerPrefs.GetString(EventListKey);
						EventList.Clear();
						PlayerPrefs.DeleteKey(EventListKey);
					}
					if (!await QMailTool.SendAsync(QToolSetting.Instance.QAnalysisMail, QToolSetting.Instance.QAnalysisMail.account, StartKey + "_" + SystemInfo.deviceName + "_" + PlayerId, data))
					{
						lock (EventList)
						{
							EventList.AddRange(tempList);
							PlayerPrefs.SetString(EventListKey, EventList.ToQData());
							Debug.LogWarning("还原信息：\n" + EventList.ToQData());
						}	
					}
				}
			
			}
			sendTask = null;
		}
		
		static List<QAnalysisEvent> EventList = new List<QAnalysisEvent>();
		public static void Trigger(string eventKey,object value=null)
		{
			if (InitOver)
			{
				try
				{
					if (eventKey.Contains("_"))
					{
						eventKey = eventKey.Replace("_", "/");
					}
					var eventData = new QAnalysisEvent
					{ 
						playerId = PlayerId,
						eventKey = eventKey, 
						eventValue = value, 
					};
					lock(EventList){
						EventList.Add(eventData);
						PlayerPrefs.SetString(EventListKey, EventList.ToQData());
					}
					Debug.Log(StartKey + " 触发事件 " + eventData);
					if (AutoSendCount >= 1 && EventList.Count >= AutoSendCount)
					{
						sendTask=SendAndClear();
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
				return null;
			}
			if (eventKey == dataKey)
			{
				return eventValue;
			}
			else if (dataKey.Contains("/"))
			{
				try
				{
					if (eventValue == null)
					{
						return null;
					}
					var memeberKey = dataKey.SplitEndString("/");
					var typeInfo = QSerializeType.Get(eventValue.GetType());
					if (typeInfo == null)
					{
						Debug.LogError("不存在类型：[" +eventValue+ "]");
					}
					var value= typeInfo.Members[memeberKey]?.Get(eventValue);
					return value;
				}
				catch (Exception e)
				{
					Debug.LogError("在 " + eventKey + " 中读取 " + dataKey + " 出错：\n" + e);
					return eventValue;
				}
			}
			else
			{
				return eventValue;
			}
		}

		public static int SortMethod(QAnalysisEvent a, QAnalysisEvent b)
		{
			return DateTime.Compare(a.eventTime, b.eventTime);
		}
	}

}
