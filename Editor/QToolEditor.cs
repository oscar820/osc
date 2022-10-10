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
using System.Threading.Tasks;
using UnityEditor.Callbacks;

namespace QTool
{
    public static class QToolEditor
    {
		[MenuItem("QTool/翻译/查看翻译语言信息")]
		public static void LanguageTest()
		{
			Debug.LogError(QTool.QTranslate.QTranslateData.ToString());
			GUIUtility.systemCopyBuffer = QTool.QTranslate.QTranslateData.ToString();
		}
		[MenuItem("QTool/翻译/生成自动翻译文件")]
		public static async void AutoTranslate()
		{
			var newData = new QDataList();
			newData.SetTitles(QTranslate.QTranslateData.TitleRow.ToArray());
			QDictionary<string, string> keyCache = new QDictionary<string, string>();
			
			for (int rowIndex = 0; rowIndex < QTranslate.QTranslateData.Count; rowIndex++)
			{
				var data = QTranslate.QTranslateData[rowIndex];
				for (int i = 2; i < data.Count; i++)
				{
					keyCache.Clear();
					var text =  data[1];
					keyCache[ "%".GetHashCode().ToString()] = "%";
					text = text.Replace("%", "[" + "%".GetHashCode() + "]");
					text = text.ForeachBlockValue('{', '}', (key) => { var value = key.GetHashCode().ToString();keyCache[value] ="{"+ key+"}";   return "["+value+"]"; });
					text = text.ForeachBlockValue('<', '>', (key) => { var value = key.GetHashCode().ToString(); keyCache[value] ="<" +key+">"; return "["+value+"]"; });
					var key = QTranslate.QTranslateData.TitleRow[i];

					if (!text.IsNullOrEmpty() && data[i].IsNullOrEmpty() && !QTranslate.QTranslateData.TitleRow[i].IsNullOrEmpty()&&!key.IsNullOrEmpty()&& QTranslate.TranslateKeys.ContainsKey(key))
					{  
						var language = QTranslate.GetTranslateKey(key);
						var newLine = newData[data[0]];
						newLine[1] = data[1];
					    var translateText = "#" + await text.NetworkTranslateAsync(language.WebAPI);
						translateText = translateText.ForeachBlockValue('[', ']', (key) => keyCache.ContainsKey(key)?keyCache[key]:key );
						newLine[i] = translateText;
						Debug.Log("翻译" + language.Key + " " + rowIndex + "/" + QTranslate.QTranslateData.Count + " " + " [" + data[1] + "]=>[" + newLine[i] + "]");
					}
				}
			}
			Debug.LogError(newData.ToString());
			newData.Save(QDataList.GetModPath( nameof(QTranslate.QTranslateData),nameof(AutoTranslate)));
		}
		[MenuItem("QTool/工具/运行时信息")]
		public static void BaseTest()
		{
			Debug.LogError(nameof(QPoolManager)+"信息 \n"+QPoolManager.Pools.ToOneString());
			Debug.LogError(nameof(QId) + "信息 \n" + QId.InstanceIdList.ToOneString());
		}
		[MenuItem("QTool/清空缓存/清空全部缓存")]
        public static void ClearMemery()
        {
            ClearPlayerPrefs();
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
		
		[MenuItem("Assets/QTool/添加对象引用Id")]
		public static void AddObjectReferenceId()
		{
			if (Selection.objects.Length > 0)
			{
				foreach (var obj in Selection.objects)
				{
					var id = QIdObject.GetId(obj);
				}
			}
		}
		public static string BuildPath => Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Assets")) + "Builds/" + EditorUserBuildSettings.activeBuildTarget + "/" + PlayerSettings.productName + "_v" + PlayerSettings.bundleVersion.Replace(".", "_");
		
		[PostProcessBuild]
		public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
		{
			switch (target)
			{
				case BuildTarget.StandaloneWindows:
				case BuildTarget.StandaloneWindows64:
					{
						var tempPath = pathToBuiltProject.SplitStartString(".exe") + "_BackUpThisFolder_ButDontShipItWithYourGame";
						if (Directory.Exists(tempPath))
						{
							Directory.Delete(tempPath, true);
						}
					}
					break;
				default:
					break;
			}
			var DirectoryPath = Path.GetDirectoryName(pathToBuiltProject);
			Debug.Log("移动打包文件"+ DirectoryPath + "到：" + BuildPath);
			QFileManager.Copy(DirectoryPath, BuildPath);
			var versions = PlayerSettings.bundleVersion.Split('.');
			if (versions.Length > 0)
			{
				versions[versions.Length - 1] = (int.Parse(versions[versions.Length - 1]) + 1).ToString();
			}
			if (!EditorUserBuildSettings.development)
			{
				PlayerSettings.bundleVersion = versions.ToOneString(".");
			
				QEventManager.Trigger("游戏版本", PlayerSettings.bundleVersion);
			}
		}
		#region OldBuild

		//		public static string Build( string[] scenes,BuildOptions options= BuildOptions.None)
		//		{
		//			BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
		//			if (!BuildPipeline.isBuildingPlayer)
		//			{
		//				var startTime = DateTime.Now;


		//#if Addressable
		//				UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent(out var result);
		//				if(string.IsNullOrWhiteSpace(result.Error))
		//				{
		//					QDebug.Log("Addressable Build 完成 ：" + result.Duration+"s");
		//				}
		//				else
		//				{
		//					Debug.LogError("Addressable Build 失败 ："+result.Error);
		//					return "";
		//				}
		//#endif
		//				var buildOption = new BuildPlayerOptions
		//				{
		//					scenes = scenes,
		//					locationPathName = GetBuildPath(),
		//					target = buildTarget,
		//					options = options,
		//				};
		//				var buildInfo = BuildPipeline.BuildPlayer(buildOption);
		//				if (buildInfo.summary.result == BuildResult.Succeeded)
		//				{
		//					QDebug.Log("打包成功" + buildOption.locationPathName);
		//					QDebug.Log("打包用时：" + Math.Ceiling((DateTime.Now - startTime).TotalMinutes) + " 分钟");
		//					var versions = PlayerSettings.bundleVersion.Split('.');
		//					if (versions.Length > 0)
		//					{
		//						versions[versions.Length - 1] = (int.Parse(versions[versions.Length - 1]) + 1).ToString();
		//					}
		//					if(!options.HasFlag(BuildOptions.Development))
		//					{
		//						PlayerSettings.bundleVersion = versions.ToOneString(".");
		//						QEventManager.Trigger("游戏版本", PlayerSettings.bundleVersion);
		//					}
		//					var tempPath = buildOption.locationPathName.SplitStartString(".exe") + "_BackUpThisFolder_ButDontShipItWithYourGame";
		//					if (Directory.Exists(tempPath))
		//					{
		//						Directory.Delete(tempPath, true); 
		//					}
		//					return buildOption.locationPathName;
		//				}
		//				else
		//				{
		//					Debug.LogError("打包失败 "+GetBuildPath());
		//				}
		//			}
		//			return "";
		//		}

		//      [MenuItem("QTool/打包/打包发布版")]
		//      public static void BuildRun()
		//{
		//	var sceneList = new List<string>();
		//	foreach (var scene in EditorBuildSettings.scenes)
		//	{
		//		sceneList.AddCheckExist(scene.path);
		//	}
		//	PlayerPrefs.SetString("QToolBuildPath", Build(sceneList.ToArray()));
		//	RunBuild();
		//}
		//[MenuItem("QTool/打包/打包开发版")]
		//public static void BuildDevelopmentRun()
		//{
		//	var sceneList = new List<string>();
		//	foreach (var scene in EditorBuildSettings.scenes)
		//	{
		//		sceneList.AddCheckExist(scene.path);
		//	}
		//	PlayerPrefs.SetString("QToolBuildPath", Build(sceneList.ToArray(), BuildOptions.Development));
		//	RunBuild();
		//}
		//[MenuItem("QTool/打包/打包当前场景")]
		//public static void BuildRandRunScene()
		//{
		//	PlayerPrefs.SetString("QToolBuildPath", Build(new string[] { SceneManager.GetActiveScene().path }, BuildOptions.Development));
		//	RunBuild();
		//}
		//[MenuItem("QTool/打包/运行测试包")]
		//private static void RunBuild()
		//{
		//	var path = PlayerPrefs.GetString("QToolBuildPath", GetBuildPath());
		//	try
		//	{
		//		System.Diagnostics.Process.Start(path);
		//	}
		//	catch (Exception e)
		//	{
		//		Debug.LogError("运行：" + path + "出错：\n" + e);
		//	}
		//}


		#endregion

	}
}

