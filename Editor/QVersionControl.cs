using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using QTool.Reflection;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
namespace QTool
{

	[InitializeOnLoad]
	public static class QVersionControl
	{
		static QVersionControl()
		{
			UnityEditor.Editor.finishedDefaultHeaderGUI += AddHeaderGUI;
			//Selection.selectionChanged += () =>
			//{
			//	var target = Selection.activeObject;
			//};
		}
		//static QDictionary<UnityEngine.Object, string> StateCache = new QDictionary<UnityEngine.Object, string>();
		private static void AddHeaderGUI(Editor editor)
		{
			if (!editor.target.IsAsset())
				return;
			var path = AssetDatabase.GetAssetPath(editor.target);
			if (path.EndsWith("unity_builtin_extra")) return;
			GUILayout.BeginHorizontal();

			if (GUILayout.Button(new GUIContent("同步更改"), GUILayout.Width(80)))
			{
				PullAndCommitPush(path,"提交测试");
			}
			GUILayout.EndHorizontal();
		}
		
		static string CheckPathRun(string commond,string path)
		{
			path = Path.GetFullPath(path);
			RunInfo.Arguments = commond;
			if (!Directory.Exists(path))
			{
				path = Path.GetDirectoryName(path);
			}
			RunInfo.WorkingDirectory = path;
			return Tool.ProcessCommand(RunInfo); ;
		}
		static string Add(string path)
		{
			return CheckPathRun(nameof(Add).ToLower()+" "+ Path.GetFullPath(path), path);
		}
		static List<string> fileList = new List<string>();
		static void Pull(string path)
		{
			Debug.Log("同步 "+ CheckPathRun(nameof(Pull).ToLower() + " origin", path));
		}
	
		static void Push(string path)
		{
			Debug.Log("上传更改 "+ CheckPathRun(nameof(Push).ToLower() + " origin" , path));
		}
		static string Commit(string path, string commitInfo)
		{
			if (string.IsNullOrWhiteSpace(commitInfo))
			{
				throw new Exception("上传信息不能为空");
			}

			var statusInfo = Status(path);
			if (statusInfo.StartsWith("fatal")) return "";
			path = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
			var lines = statusInfo.Split('\n');
			fileList.Clear();
			foreach (var info in lines)
			{
				if (info.Trim().SplitTowString(" ", out var start, out var end))
				{
					var filePath = (path + "/" + end).Replace('/', '\\');
					switch (start)
					{
						case "??":
							Debug.Log("新增 " + end);
							Add(filePath);
							fileList.AddCheckExist(end);
							break;
						case "D":
							Debug.Log("删除 " + end);
							fileList.AddCheckExist(end);
							break;
						case "A":
							Debug.Log("新增 " + end);
							fileList.AddCheckExist(end);
							break;
						case "M":
							Debug.Log("更改 " + end);
							fileList.AddCheckExist(end);
							break;
						default:
							break;
					}
				}
			}
			if (fileList.Count > 0)
			{
				RunInfo.Arguments = nameof(Commit).ToLower() + " " + fileList.ToOneString(" ") + " -m " + commitInfo;
				RunInfo.WorkingDirectory = path;
				return Tool.ProcessCommand(RunInfo);
			}
			else
			{
				return "";
			}
		}

		static void PullAndCommitPush(string path, string commitInfo)
		{
			Pull(path);
			var commitResul= Commit(path, commitInfo);
			if (string.IsNullOrWhiteSpace(commitResul))
			{
				Debug.Log("无本地更新");
			}
			else
			{
				Debug.Log("提交更改" + commitResul);
				Push(path);
			}
		}
		static System.Diagnostics.ProcessStartInfo RunInfo = new System.Diagnostics.ProcessStartInfo("Git")
		{
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError=true,
			UseShellExecute = false,
		};
		public static string Status(string path)
		{
			return CheckPathRun(nameof(Status).ToLower() + " -s "+ Path.GetFullPath( path), path);
		}
		

		//public const string COMMAND_TORTOISE_LOG = @"/command:log /path:{0} /findtype:0 /closeonend:0";
		//public const string COMMAND_TORTOISE_PULL = @"/command:pull /path:{0} /closeonend:0";
		//public const string COMMAND_TORTOISE_COMMIT = @"/command:commit /path:{0} /closeonend:0";
		//public const string COMMAND_TORTOISE_PUSH = @"/command:push /path:{0} /closeonend:0";
		//public const string COMMAND_TORTOISE_STASHSAVE = @"/command:stashsave /path:{0} /closeonend:0";
		//public const string COMMAND_TORTOISE_STASHPOP = @"/command:stashpop /path:{0} /closeonend:0";
		//public static string tortoiseGitPath = @"E:\TortoiseGit\bin\TortoiseGitProc.exe";

		////[MenuItem("TortoiseGit/StashSave")]
		//public static void GitAssetsStushSave()
		//{
		//	//TortoiseGit.GitCommand(GitType.StashSave, Application.dataPath, tortoiseGitPath);
		//}

		////[MenuItem("TortoiseGit/StashPop")]
		//public static void GitAssetsStushPop()
		//{
		//	//TortoiseGit.GitCommand(GitType.StashPop, Application.dataPath, tortoiseGitPath);
		//}

		//[MenuItem("TortoiseGit/Push")]
		//public static void GitAssetPush()
		//{
		//	//TortoiseGit.GitCommand(GitType.Push, Application.dataPath, tortoiseGitPath);
		//	ProcessCommand(COMMAND_TORTOISE_PUSH);
		//}

		//[MenuItem("TortoiseGit/Log")]
		//public static void GitAssetsLog()
		//{
		//	ProcessCommand(COMMAND_TORTOISE_LOG);
		//}

		//[MenuItem("TortoiseGit/Pull")]
		//public static void GitAssetsPull()
		//{
		//	//TortoiseGit.GitCommand(GitType.Pull, Application.dataPath, tortoiseGitPath);
		//	ProcessCommand(COMMAND_TORTOISE_PULL);
		//}

		//[MenuItem("TortoiseGit/Commit")]
		//public static void GitAssetsCommit()
		//{
		//	//TortoiseGit.GitCommand(GitType.Commit, Application.dataPath, tortoiseGitPath);
		//	ProcessCommand(COMMAND_TORTOISE_COMMIT);
		//}
	}
}
