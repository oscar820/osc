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
		}
		private static void AddHeaderGUI(Editor editor)
		{
			if (!editor.target.IsAsset())
				return;
			var path = AssetDatabase.GetAssetPath(editor.target);
			if (path.EndsWith("unity_builtin_extra")) return;
			GUILayout.BeginHorizontal();

			if (GUILayout.Button(new GUIContent("同步更改"), GUILayout.Width(80)))
			{
				PullAndCommitPush(path);
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
		static void Pull(string path)
		{
			Debug.Log("同步 "+ CheckPathRun(nameof(Pull).ToLower() + " origin", path));
		}
	
		static void Push(string path)
		{
			Debug.Log("上传更改 "+ CheckPathRun(nameof(Push).ToLower() + " origin" , path));
		}

		static List<QFileState> commitList = new List<QFileState>();
		static string Commit(string path)
		{
			var statusInfo = Status(path); 
			if (statusInfo.StartsWith("fatal")) return "";
			path = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
			var lines = statusInfo.Split('\n');
			commitList.Clear();
			foreach (var info in lines)
			{ 
				if (string.IsNullOrWhiteSpace(info)) continue;
				commitList.Add(new QFileState(info));
			}
			var commitInfo = QCommitWindow.Show(commitList);
			if (string.IsNullOrWhiteSpace(commitInfo)) return"";
			foreach (var info in commitList)
			{
				var filePath = (path + "/" + info.path).Replace('/', '\\');
				switch (info.state)
				{
					case "??":
						Debug.Log("新增 " + info.path);
						Add(filePath);
						break;
					case "D":
						Debug.Log("删除 " + info.path);
						break;
					case "A":
						Debug.Log("新增 " + info.path);
						break;
					case "M":
						Debug.Log("更改 " + info.path);
						break;
					default:
						break;
				}
			}
			if (commitList.Count > 0)
			{
				RunInfo.Arguments = nameof(Commit).ToLower() + " " + commitList.ToOneString(" ") + " -m " + commitInfo;
				RunInfo.WorkingDirectory = path;
				return Tool.ProcessCommand(RunInfo);
			}
			else
			{
				return "";
			}
		}

		static void PullAndCommitPush(string path)
		{
			Pull(path);
			var commitResul= Commit(path);
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

	}
	
	public struct QFileState
	{
		public string state;
		public string path;
		public QFileState(string initInfo)
		{
			initInfo.Trim().SplitTowString(" ", out var start, out var end);
			state = start;
			path = end;
		}
		public override string ToString()
		{
			return path;
		}
	}

	public class QCommitWindow : EditorWindow
	{
		public static QCommitWindow Instance { private set; get; }
		public static string Show(List<QFileState> commitList)
		{
			if (Instance == null)
			{
				Instance = GetWindow<QCommitWindow>();
				Instance.minSize = new Vector2(300, 130);
			}
			Instance.titleContent = new GUIContent("提交本地更改");
			Instance.commitList = commitList;
			Instance.fileList.Clear();
			Instance.fileList.AddRange(commitList);
			Instance.commitInfo = "";
			Instance.ShowModal();
			return Instance.commitInfo;
		}
		public List<QFileState> fileList = new List<QFileState>();
		public List<QFileState> commitList = new List<QFileState>();
		public string commitInfo { get; private set; }
		Vector2 scrollPos = Vector2.zero;
		private void OnGUI()
		{
			commitInfo = GUILayout.TextArea(commitInfo, GUILayout.Height(60));
			using (var scroll=new GUILayout.ScrollViewScope(scrollPos,QGUITool.BackStyle))
			{
				foreach (var file in fileList)
				{
					using (new GUILayout.HorizontalScope())
					{
						if (GUILayout.Toggle(commitList.Contains(file), "")){
							commitList.AddCheckExist(file);
						}else
						{
							commitList.Remove(file);
						}
						GUILayout.Label(file.path);
					}
				}
				scrollPos=scroll.scrollPosition ;
			}
			if (GUILayout.Button("提交"))
			{
				if (string.IsNullOrWhiteSpace(commitInfo))
				{
					EditorUtility.DisplayDialog("提交信息错误", "提交信息不能为空", "确认");
				}
				else
				{
					Close();
				}
			}
			if (GUILayout.Button("取消"))
			{
				commitInfo = "";
				Close();
			}
		}
	}
}
