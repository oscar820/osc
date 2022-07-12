using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using QTool.Reflection;
using System.IO;
using System.Threading.Tasks;

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

			if (GUILayout.Button(new GUIContent("提交"), GUILayout.Width(50)))
			{
				Commit(path);
			}
			if (GUILayout.Button(new GUIContent("更新"), GUILayout.Width(50)))
			{
				Debug.Log("状态 ");
			}
			GUILayout.EndHorizontal();
		}
		static string PathRun(string commond,string path)
		{
			path = Path.GetFullPath(path);
			RunInfo.Arguments = commond.ToLower()+" " + Path.GetFullPath(path);
			if (!Directory.Exists(path))
			{
				path = Path.GetDirectoryName(path);
			}
			RunInfo.WorkingDirectory = path;
			Debug.Log(RunInfo.ToQData());
			return Tool.ProcessCommand(RunInfo); ;
		}
		static string Add(string path)
		{
			return PathRun(nameof(Add), path);
		}
		static void Commit(string path)
		{
			Task.Run(() =>
			{
				var state = Status(path);
				if (state.StartsWith("fatal")) return;
				path = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
				var lines = state.Split('\n');
				foreach (var info in lines)
				{
					if (info.Trim().SplitTowString(" ", out var start, out var end))
					{
						switch (start)
						{
							case "??":
								Debug.LogError(Add(path + "/" + end));
								break;
							case "M":
								break;
							default:
								Debug.LogError("[" + start + "][" + end + "]");
								break;
						}
					}
				}

				RunInfo.Arguments = nameof(Commit).ToLower();
				RunInfo.WorkingDirectory = path;
				Debug.Log(RunInfo.ToQData());
				Debug.LogError(Tool.ProcessCommand(RunInfo));
				Debug.Log("提交完成");
			});
			

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
			return PathRun(nameof(Status) + " -s",path);
		}
		static void Push()
		{
			
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
