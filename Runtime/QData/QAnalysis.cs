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
		public static void Login(string id)
		{
			if (!QToolSetting.Instance.QAnalysisMail.InitOver)
			{
				Debug.LogError(nameof(QToolSetting.Instance.QAnalysisMail) + " 未设置");
				return;
			}
			SendEventList();
			if (id == AccountId)
			{
				Debug.LogError(StartKey+" 已登录" + id);
				return;
			}
			AccountId = id;
			Trigger(nameof(Login)); 
		}
		public static void logout() 
		{
			if (!InitOver)
			{
				return;
			}
			Trigger(nameof(logout));
			SendEventList();
			AccountId = null;
		}
		public static string StartKey => nameof(QAnalysis) + "_" + Application.productName;
		public static string EventListKey => StartKey + "_" + nameof(triggerEventList);
		public static void SendEventList()
		{
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
					accountId = AccountId,
					eventKey=eventKey,
					evventValue=value,
				};
				triggerEventList.Add(eventData);
				Debug.Log(StartKey + " 触发事件 " + eventData);
				PlayerPrefs.SetString(EventListKey, triggerEventList.ToQData());
			}
		}
	
		
		
	}
	public static class QAnalysisData
	{
		public static QDictionary<string, List<QAnalysisEvent>> Data = new QDictionary<string, List<QAnalysisEvent>>();
		static QAnalysisData()
		{
			QMailTool.OnReceiveMail += (mailInfo) =>
			{
				if (mailInfo.Subject.StartsWith(QAnalysis.StartKey))
				{
					AddEvent(mailInfo.Body.ParseQData<List<QAnalysisEvent>>());
				}
			};
		}
		public static async Task FreshData()
		{
			await QMailTool.FreshEmails(QToolSetting.Instance.QAnalysisMail);
		}
		public static void AddEvent(List<QAnalysisEvent> eventList)
		{
			foreach (var eventData in eventList)
			{
				var playerData = Data[eventData.accountId];
				if (playerData == null)
				{
					playerData = new List<QAnalysisEvent>();
					Data[eventData.accountId] = playerData;
				}
			}
		}

	}

	public class QAnalysisEvent
	{
		public string eventKey;
		public object evventValue;
		public DateTime eventTime = DateTime.Now;
		public string accountId;
		public string eventId = QId.GetNewId();
		public override string ToString()
		{
			return this.ToQData();
		}
	}

}
