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
		static string stashVersion = "version";
		private static void AddHeaderGUI(Editor editor)
		{
			if (!editor.target.IsAsset())
				return;
			var path = AssetDatabase.GetAssetPath(editor.target);
			if (path.EndsWith("unity_builtin_extra")) return;
			if (GUILayout.Button(new GUIContent("同步更改"), GUILayout.Width(80)))
			{
				PullAndCommitPush(path);
			}
			if (GUILayout.Button(new GUIContent("贮藏"), GUILayout.Width(80)))
			{
				stashVersion = GetCurrentVersion(path);
				Debug.LogError(Stash(path));
			}
			if (GUILayout.Button(new GUIContent("还原贮藏"), GUILayout.Width(80)))
			{
				Debug.LogError(Stash(path,true));
				Debug.LogError( Checkout(path, stashVersion));
			}
		}

		static string CheckPathRun(string commond, string path)
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
			return CheckPathRun(nameof(Add).ToLower() + " " + Path.GetFullPath(path), path);
		}
		static string Checkout(string path,string version=null)
		{
			if (string.IsNullOrEmpty(version))
			{
				return CheckPathRun(nameof(Checkout).ToLower() + " -- " + Path.GetFullPath(path), path);
			}
			else
			{
				return CheckPathRun(nameof(Checkout).ToLower()+ " "+version+ " -- " + Path.GetFullPath(path), path);
			}
		}
		static string GetCurrentVersion(string path)
		{
			return CheckPathRun("log -1 --pretty=oneline" , path).SplitStartString(" ");
			
		}
		static bool Pull(string path)
		{
			var result = CheckPathRun(nameof(Pull).ToLower() + " origin", path);
			if (result.StartsWith("fatal")|| result.Contains("error"))
			{
				var parentPath = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
				Debug.LogError("同步出错 " + result);
				var mergeErrorFile = result.GetBlockValue("error: Your local changes to the following files would be overwritten by merge:", "Please commit your changes or stash them before you merge.");
				commitList.Clear();
				foreach (var fileInfo in mergeErrorFile.Trim().Split('\n'))
				{
					commitList.Add(new QFileState(fileInfo,parentPath));
				}
				if (QVersionControlWindow.MergeError(commitList))
				{
					var version = GetCurrentVersion(path);
					foreach (var info in commitList)
					{
						if (!info.select)
						{
							Debug.Log("放弃本地更改 " + info + " " + Checkout(info.path));
						}
						else
						{
							Checkout(info.path);
						}
					}
					Debug.Log("保留本地更改 " + Stash(path));
				
					var pullResult = Pull(path);
					foreach (var info in commitList)
					{
						if (info.select)
						{
							Checkout(info.path, version);
						}
					}
					Debug.Log("还原本地更改 " + Stash(path,true));
					return pullResult;
				}
				else
				{
					return false;
				}
			}
			else
			{
				Debug.Log("同步 " + result);
			}
			return true;
		}

		static void Push(string path)
		{
			Debug.Log("上传更改 " + CheckPathRun(nameof(Push).ToLower() + " origin", path));
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
				commitList.Add(new QFileState(info,path));
			}
			if (commitList.Count == 0) return "";
			var commitInfo = QVersionControlWindow.Commit(commitList);
			if (string.IsNullOrWhiteSpace(commitInfo) || commitList.Count == 0) return "";
			foreach (var info in commitList)
			{
				if (!info.select) continue;
				switch (info.state)
				{
					case "??":
						Debug.Log("新增 " + info.path);
						Add(info.path);
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
		static string Stash(string path,bool pop=false)
		{
			if (pop)
			{
				return CheckPathRun(nameof(Stash).ToLower()+" pop",path);
			}
			else
			{
				return CheckPathRun(nameof(Stash).ToLower(), path);
			}
			
		}
		static void PullAndCommitPush(string path)
		{
			if (Pull(path))
			{
				var commitResul = Commit(path);
				if (commitResul.StartsWith("error"))
				{
					Debug.LogError(commitResul);
					return;
				}
				if (string.IsNullOrWhiteSpace(commitResul))
				{
					Debug.Log("无本地更新");
				}
				else
				{
					Debug.Log("提交更改 " + commitResul);
					Push(path);
				}
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
	
	public class QFileState
	{
		public string state;
		public string path;
		public bool select = true;
		public QFileState(string initInfo,string parentPath)
		{
			if(initInfo.Trim().SplitTowString(" ", out var start, out var end))
			{
				state = start;
				path = (parentPath + "/" + end).Replace('/', '\\');
				select = true;
			}
			else
			{
				path = (parentPath + "/" + start).Replace('/', '\\');
				select = false;
			}
		}
		public override string ToString()
		{
			return path;
		}
	}

	public class QVersionControlWindow : EditorWindow
	{
		public static QVersionControlWindow Instance { private set; get; }
		public static string Commit(List<QFileState> commitList)
		{
			if (Instance == null)
			{
				Instance = GetWindow<QVersionControlWindow>();
				Instance.minSize = new Vector2(200, 130);
			}
			Instance.titleContent = new GUIContent("提交本地更改");
			Instance.fileList.Clear();
			Instance.fileList.AddRange(commitList);
			Instance.commitInfo = "";
			Instance.confirm = false;
			Instance.ShowModal();
			return Instance.confirm?Instance.commitInfo:"";
		}
		public static bool MergeError(List<QFileState> mergeErrorList)
		{
			if (Instance == null)
			{
				Instance = GetWindow<QVersionControlWindow>();
				Instance.minSize = new Vector2(200, 130);
			}
			Instance.titleContent = new GUIContent("解决文件冲突");
			Instance.fileList.Clear();
			Instance.fileList.AddRange(mergeErrorList);
			Instance.commitInfo = "";
			Instance.confirm = false;
			Instance.ShowModal();
			return Instance.confirm;
		}
		public List<QFileState> fileList = new List<QFileState>();
		public string commitInfo { get; private set; }
		bool confirm;
		Vector2 scrollPos = Vector2.zero;
		private void OnGUI()
		{ 
			using (var scroll=new GUILayout.ScrollViewScope(scrollPos,QGUITool.BackStyle))
			{
				foreach (var file in fileList)
				{
					using (new GUILayout.HorizontalScope())
					{
						file.select = GUILayout.Toggle(file.select, "");
						GUILayout.Label(file.path);
					}
				}
				scrollPos=scroll.scrollPosition ;
			}
		
			if(titleContent.text.Contains("提交")){
				commitInfo = EditorGUILayout.TextField(commitInfo);
				if (GUILayout.Button("提交选中文件"))
				{
					if (string.IsNullOrWhiteSpace(commitInfo))
					{
						EditorUtility.DisplayDialog("提交信息错误", "提交信息不能为空", "确认");
					}
					else
					{
						confirm = true;
						Close();
					}
				}
			}
			else
			{
				if (GUILayout.Button("保留选中文件"))
				{
					confirm = true;
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
