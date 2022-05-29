using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
					Debug.LogError(nameof(QAnalysis) + "未设置账户ID");
					return false;
				}
				return true;
			}
		}
		public static void Login(string id)
		{
			AccountId = id;
		}
		
	}
	public class QAnalysisInfo
	{
		public string accountId;
		public string infoId;
	}

}
