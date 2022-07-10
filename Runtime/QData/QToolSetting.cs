using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QToolSetting : InstanceScriptable<QToolSetting>
	{
		public QMailAccount QAnalysisMail;
		public string danmuRoomId= "55336";

		private void OnValidate()
		{
			QAnalysisMail?.Init();
		}
	}
}
