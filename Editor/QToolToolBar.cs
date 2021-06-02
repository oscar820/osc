using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using QTool.Resource;
using QTool.Data;
using QTool.Reflection;
using System.Reflection;

namespace QTool
{
    public static class QToolToolBar
    {
     
        [MenuItem("QTool/Tool/显示日志")]
        public static void SwitchLog()
        {
            ToolDebug.ShowLog = !ToolDebug.ShowLog;
            UnityEngine.Debug.Log(ToolDebug.ShowLog ? "显示"+ToolDebug.Key : "隐藏"+ ToolDebug.Key);
        }
        [MenuItem("QTool/清空缓存/清空全部缓存")]
        public static void ClearMemery()
        {
            ClearPlayerPrefs();
            ClearQData();
            ClearResourcesList();
        }
        [MenuItem("QTool/清空缓存/清空PlayerPrefs")]
        public static void ClearPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
        }
      

        [MenuItem("QTool/清空缓存/清空QData缓存")]
        public static void ClearQData()
        {
            foreach (var type in typeof(QData<>).GetAllTypes())
            {
                type.BaseType.InvokeMember("Clear", BindingFlags.InvokeMethod | BindingFlags.Static| BindingFlags.Public, null,null,new object[0]);

            };
        }
        [MenuItem("QTool/清空缓存/清空ResourcesList缓存")]
        public static void ClearResourcesList()
        {
            foreach (var type in typeof(ResourceList<,>).GetAllTypes())
            {
                type.BaseType.InvokeMember("Clear", BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public, null, null, new object[0]);
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
        [MenuItem("QTool/Tool/运行测试包 %T")]
        public static void RunTest()
        {
            System.Diagnostics.Process.Start(BasePath + WindowsLocalPath);
        }
        [MenuItem("QTool/Tool/打包测试当前场景 %#T")]
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
                    ToolDebug.Log("打包成功" + BasePath+WindowsLocalPath);
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

