using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QToolSetting : InstanceScriptable<QToolSetting>
	{
		public QMailAccount QAnalysisMail;
		private void OnValidate()
		{
			QAnalysisMail?.Init();
		}
	}
}
