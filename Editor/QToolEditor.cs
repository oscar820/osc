using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using QTool.Asset;
using QTool.Reflection;
using System.Reflection;

namespace QTool
{
    public static class QToolEditor
    {
     
        [MenuItem("QTool/清空缓存/清空全部缓存")]
        public static void ClearMemery()
        {
            ClearPlayerPrefs();
            ClearResourcesList();
            ClearPersistentData();
        }
        [MenuItem("QTool/清空缓存/清空PlayerPrefs")]
        public static void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
        }
        [MenuItem("QTool/清空缓存/清空PersistentData")]
        public static void ClearPersistentData()
        {
            Application.persistentDataPath.ClearData();
        }
        [MenuItem("QTool/清空缓存/清空AssetList缓存")]
        public static void ClearResourcesList()
        {
            foreach (var type in typeof(AssetList<,>).GetAllTypes())
            {
                type.InvokeStaticFunction("Clear");
                Debug.LogError("清空" + type.Name);
            };
        }
        public static string BasePath
        {
            get
            {
                return Application.dataPath.Substring(0, Application.dataPath.IndexOf("Assets")) ;
            }
        }
        public static string WindowsLocalPath
        {
            get
            {
                return "Builds/Windows/test.exe";
            }

        }
		public static string GetBuildPath(BuildTarget buildTarget= BuildTarget.StandaloneWindows)
		{
			switch (buildTarget)
			{
				case BuildTarget.StandaloneWindows:
					return "Builds/" + buildTarget + "/"+PlayerSettings.productName+".exe";
				default:
					throw new Exception("不支持快速打包 "+buildTarget+" 平台");
			}
		}
		public static string Build(BuildTarget buildTarget= BuildTarget.StandaloneWindows)
		{
			if (!BuildPipeline.isBuildingPlayer)
			{
				var sceneList = new List<string>();
				sceneList.Add(SceneManager.GetActiveScene().path);
				foreach (var scene in EditorBuildSettings.scenes)
				{
					sceneList.Add(scene.path);
				}
				var buildOption = new BuildPlayerOptions
				{
					scenes = sceneList.ToArray(),
					locationPathName = GetBuildPath(buildTarget),
					target = buildTarget,
					options = BuildOptions.None,
				};
				var buildInfo = BuildPipeline.BuildPlayer(buildOption);
				if (buildInfo.summary.result == BuildResult.Succeeded)
				{
					Debug.Log("打包成功" + buildOption.locationPathName);
					return buildOption.locationPathName;
				}
				else
				{
					Debug.LogError("打包失败");
				}
			}
			return "";
		}
     
        [MenuItem("QTool/工具/打包测试当前场景")]
        private static void BuildRandRun()
        {
			var path = Build();
			if (!string.IsNullOrWhiteSpace(path))
			{
				System.Diagnostics.Process.Start(path);
			}
		}
		[MenuItem("Assets/QTool/添加对象引用Id")]
		public static void AddObjectReferenceId()
		{
			if (Selection.objects.Length > 0)
			{
				foreach (var obj in Selection.objects)
				{
					var id= QObjectReference.GetId(obj);
				}
			}
			
		}
	}
}

