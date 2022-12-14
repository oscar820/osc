using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using QTool.Reflection;

namespace QTool
{
	public static class QAnalysis
	{
		public enum QAnalysisEventName
		{
			游戏_开始,
			游戏_结束,
			游戏_暂离,
			游戏_错误,
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
		public static int MinSendCount { get; set; } = 100;
		public static int AutoSendCount { get; set; } =5000;

		[System.Diagnostics.Conditional("UNITY_STANDALONE")]
		public static void Start(string playerId)
		{
			try
			{
				if (QPlayerPrefs.HasKey(EventListKey))
				{
					QPlayerPrefs.GetString(EventListKey).ParseQData(EventList);
				}
			}
			catch (Exception e)
			{
				Debug.LogError("读取记录信息出错：\n" + e);
			}
			sendTask =SendAndClear();
			if (Application.isEditor&& !playerId.StartsWith("Editor"))
			{
				playerId = "Editor_" + playerId;
			}
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
			Trigger(nameof(QAnalysisEventName.游戏_开始),new StartInfo());
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
						if(condition.Contains(nameof(StackOverflowException)))
						{
							if (stackTrace.Length > 300)
							{
								stackTrace = stackTrace.Substring(0, 300);
							}
						}
						errorInfoList.Add(condition);
						Trigger(nameof(QAnalysisEventName.游戏_错误), condition + '\n' + stackTrace);
					}
					break;
			}
		}
		static void OnFocus(bool focus)
		{
			if (!focus)
			{
				Trigger(nameof(QAnalysisEventName.游戏_暂离));
			}
		}
		static bool OnWantsQuit()
		{
			if (stopTask==null)
			{
				stopTask = Stop();
			}
			stopTask.GetAwaiter().OnCompleted(() =>
			{
				Application.Quit();
			});
			return false;
		}
		static Task stopTask = null;
		static Task sendTask = null;

		public static async Task Stop()
		{
			if (sendTask != null)
			{
				await sendTask;
			}
			if (!InitOver)
			{
				return;
			}
			Trigger(nameof(QAnalysisEventName.游戏_结束));
			Application.focusChanged -= OnFocus;
			Application.logMessageReceived -= LogCallback;
			await SendAndClear();
			Application.wantsToQuit -= OnWantsQuit;
			PlayerId = null;
			stopTask = null;
		}
		static string _startKey = null;
		public static string StartKey => _startKey??= nameof(QAnalysis) + "_" + (string.IsNullOrEmpty(QToolSetting.Instance.QAnalysisProject)? Application.productName: QToolSetting.Instance.QAnalysisProject);
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
			if (QPlayerPrefs.HasKey(EventListKey))
			{
				var count = EventList.Count;
				if (count > MinSendCount)
				{
					List<QAnalysisEvent> tempList = new List<QAnalysisEvent>();
					var data = "";
					var id = EventList.QueuePeek()?.eventId;
					lock (EventList)
					{
						tempList.AddRange(EventList);
						data = QPlayerPrefs.GetString(EventListKey);
						EventList.Clear();
						QPlayerPrefs.DeleteKey(EventListKey);
					}
					if (!await QMailTool.SendAsync(QToolSetting.Instance.QAnalysisMail, QToolSetting.Instance.QAnalysisMail.account, StartKey + "_" + SystemInfo.deviceName +"_"+ id, data))
					{
						lock (EventList)
						{
							EventList.AddRange(tempList);
							QPlayerPrefs.SetString(EventListKey, EventList.ToQData());
							Debug.LogWarning("还原信息：\n" + EventList.ToQData());
						}	
					}
				}
			
			}
			sendTask = null;
		}

		static List<QAnalysisEvent> EventList = new List<QAnalysisEvent>();

		[System.Diagnostics.Conditional("UNITY_STANDALONE")]
		public static void Trigger(string eventKey,object value=null)
		{

#if UNITY_EDITOR
			if ((Application.isEditor&& !QPlayerPrefs.HasKey(nameof(QAnalysis) + "_EditorTest")))
			{
				return;
			}
#endif
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
					EventList.Add(eventData);
					QPlayerPrefs.SetString(EventListKey, EventList.ToQData());
					QDebug.Log(StartKey + " 触发事件 " + eventData);
					if (AutoSendCount >= 1 && EventList.Count >= AutoSendCount)
					{
						sendTask = SendAndClear();
					}

				}
				catch (Exception e)
				{
					Debug.LogError(nameof(QAnalysis) + "触发事件 " + eventKey + " " + value + " 出错：\n" + e);
				}

			}
		}
		[System.Diagnostics.Conditional("UNITY_STANDALONE")]
		public static void Trigger(string eventKey,string key, object value)
		{
			Trigger(eventKey, new QKeyValue<string, object>(key, value));
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
			else 
			{
				var childKey = dataKey.SplitEndString(eventKey+"/").Replace("/",".");
				return eventValue.GetValue(childKey);
			}
		}

		public static int SortMethod(QAnalysisEvent a, QAnalysisEvent b)
		{
			return DateTime.Compare(a.eventTime, b.eventTime);
		}
	}

}
