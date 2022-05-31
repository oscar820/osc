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
		public static string AccountId { private set; get; }
		public static bool InitOver
		{
			get
			{
				if (string.IsNullOrWhiteSpace(AccountId))
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
			if (id == AccountId)
			{
				Debug.LogError(StartKey+" 已登录" + id);
				return;
			}
			AccountId = id;
			if (!InitOver)
			{
				return;
			}
			Trigger("游戏开始",new StartInfo());
		}
	
		public static void Stop() 
		{
			if (!InitOver)
			{
				return;
			}
			Trigger("游戏结束");
			SendEventList();
			AccountId = null;
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
				QMailTool.Send(QToolSetting.Instance.QAnalysisMail, QToolSetting.Instance.QAnalysisMail.account, StartKey + "_" + SystemInfo.deviceName + "_" + AccountId, data);
				triggerEventList.Clear();
				PlayerPrefs.DeleteKey(EventListKey);
			}
		}
		static List<QAnalysisEvent> triggerEventList = new List<QAnalysisEvent>();
		public static void Trigger(string eventKey,object value=null)
		{
			if (InitOver)
			{
				var eventData=new QAnalysisEvent
				{
					playerId = AccountId,
					eventKey=eventKey,
					eventValue=value,
				};
				triggerEventList.Add(eventData);
				Debug.Log(StartKey + " 触发事件 " + eventData);
				PlayerPrefs.SetString(EventListKey, triggerEventList.ToQData());
				if (SendCount >= 1 && triggerEventList.Count>= SendCount)
				{
					SendEventList();
				}
			}
		}
		
	}
	public class StartInfo
	{
		public RuntimePlatform platform = Application.platform;
		public string version = Application.version;
		public string deviceName = SystemInfo.deviceName;
		public string deviceUniqueIdentifier = SystemInfo.deviceUniqueIdentifier;
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
			return eventKey + " " + eventTime.ToQTimeString() +" "+playerId;
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
