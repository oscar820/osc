using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using QTool;
public class NewBehaviourScript : IPreprocessBuildWithReport,IPostprocessBuildWithReport
{
	public int callbackOrder { get { return 0; } }

	public System.DateTime startTime;
	public void OnPreprocessBuild(BuildReport report)
	{
		startTime = System.DateTime.Now;
		var versions = PlayerSettings.bundleVersion.Split('.');
		if (versions.Length > 0)
		{
			versions[versions.Length - 1] = (int.Parse( versions[versions.Length - 1]) + 1).ToString();
		}
		PlayerSettings.bundleVersion = versions.ToOneString(".");
	}
	public void OnPostprocessBuild(BuildReport report)
	{
		Debug.LogError("打包花费时间：" + (int)(System.DateTime.Now - startTime).TotalMinutes + "分钟");
	}
}
