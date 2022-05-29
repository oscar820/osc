using QTool.Inspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QAnalysisTest : MonoBehaviour
	{		
		[ViewButton("刷新数据")]
		public async void FreshData() 
		{
			await QAnalysisData.FreshData();
			Debug.LogError(QAnalysisData.AnalysisData.ToOneString());
		}
	
		[ViewButton("登录")]
		public void Login()
		{
			QAnalysis.Login("TestAccount"+ "_" + Random.Range(1, 4));
		}
		[ViewButton("退出")]
		public void Logout()
		{
			QAnalysis.logout();
		}
	}

}
