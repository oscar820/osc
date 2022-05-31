using QTool.Inspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QAnalysisTest : MonoBehaviour
	{		
	
	
		[ViewButton("登录")]
		public void Login()
		{
			QAnalysis.Start("TestAccount"+ "_" + Random.Range(1, 10)); 
		}
		[ViewButton("退出")]
		public void Logout()
		{
			QAnalysis.Stop();
		}
	}

}
