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
    public static class QToolToolBar
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
        [MenuItem("QTool/工具/运行测试包 %T")]
        public static void RunTest()
        {
            System.Diagnostics.Process.Start(BasePath + WindowsLocalPath);
        }
        [MenuItem("QTool/工具/打包测试当前场景 %#T")]
        public static void TestBuild()
        {
            if (!BuildPipeline.isBuildingPlayer)
            {
                var buildOption = new BuildPlayerOptions
                {
                    scenes = new string[] { SceneManager.GetActiveScene().path },
                    locationPathName = WindowsLocalPath,
                    target = BuildTarget.StandaloneWindows,
                    options = BuildOptions.None,
                };
                var buildInfo = BuildPipeline.BuildPlayer(buildOption);
                if (buildInfo.summary.result == BuildResult.Succeeded)
                {
                    QToolDebug.Log(()=>"打包成功" + BasePath+WindowsLocalPath);
                    System.Diagnostics.Process.Start(BasePath + WindowsLocalPath);
                }
                else
                {
                   Debug.LogError("打包失败");
                }
            }
        }
       
    }
}

