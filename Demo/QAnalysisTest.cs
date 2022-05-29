using QTool.Inspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QAnalysisTest : MonoBehaviour
	{
		public string email;
		public string pass ;
		[ViewButton("邮件获取测试")]
		public void EmailReceiveTest() 
		{
			QMailTool.GetEmails(email,pass);
		}
	}

}
