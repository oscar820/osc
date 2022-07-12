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
			if (GUILayout.Button(new GUIContent("同步更改"), GUILayout.Width(80)))
			{
				PullAndCommitPush(path);
			}
		}

		static string CheckPathRun(string commond, string path)
		{
			try
			{

				path = Path.GetFullPath(path);
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
			}
			RunInfo.Arguments = commond;
			if (File.Exists(path))
			{
				path = Path.GetDirectoryName(path);
			}
			RunInfo.WorkingDirectory =path;
			Debug.Log("git "+ RunInfo.Arguments);
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
				Debug.LogError("同步出错 " + result);
				var mergeErrorFile = result.GetBlockValue("error: Your local changes to the following files would be overwritten by merge:", "Please commit your changes or stash them before you merge.");
				commitList.Clear();
				foreach (var fileInfo in mergeErrorFile.Trim().Split('\n'))
				{
					commitList.Add(new QFileState(false,fileInfo,path));
				}
				if (QVersionControlWindow.MergeError(commitList))
				{
					var version = GetCurrentVersion(path);
					var useStash = false;
					foreach (var info in commitList)
					{
						if (!info.select)
						{
							Debug.LogError("放弃本地更改 " + info + " " + Checkout(info.path));
						}else
						{
							useStash = true;
						}
					}
					if (useStash)
					{
						Debug.Log("保留本地更改 " + Stash(path));
					}
					var pullResult = Pull(path);
					if (useStash)
					{
						foreach (var info in commitList)
						{
							if (info.select)
							{
								Debug.LogError("放弃远端更改 " + info + " " + Checkout(info.path, version));
							}
						}
						Debug.Log("还原本地更改 " + Stash(path, true));
					} 
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
			Debug.Log("上传更改 " + CheckPathRun(nameof(Push).ToLower() + " origin master", path));
		}

		static List<QFileState> commitList = new List<QFileState>();
		static void AddCommitList(string path)
		{
			var statusInfo = Status(path);
			Debug.Log("commit " + statusInfo);
			if (statusInfo.StartsWith("fatal")) return;
			var lines = statusInfo.Split('\n');
			foreach (var info in lines)
			{
				if (string.IsNullOrWhiteSpace(info)) continue;
				commitList.Add(new QFileState(true,info, path));
			}
		}
		static string Commit(string path)
		{
			commitList.Clear();
			AddCommitList(path);
			if(File.Exists(path + ".meta"))
			{
				AddCommitList(path + ".meta");
			}
			if (commitList.Count == 0) return "";
			var commitInfo = QVersionControlWindow.Commit(commitList);
			if (string.IsNullOrWhiteSpace(commitInfo) || commitList.Count == 0) return "";
			commitList.RemoveAll((obj) => !obj.select);
			for (int i = 0; i < commitList.Count; i++)
			{
				var info = commitList[i];
				if (!info.select) continue;
				EditorUtility.DisplayProgressBar("提交更改", "提交 " + info.path +" "+(i+1)+"/"+commitList.Count, i*1f/commitList.Count);
				switch (info.state)
				{
					case "??":
					//	Add(info.path);
						break;
					default:
						break;
				}
				Debug.Log(info.state + "  " + info);
			}
			EditorUtility.ClearProgressBar();
			if (commitList.Count > 0)
			{
				//Debug.LogError(nameof(Commit).ToLower() + " " + commitList.ToOneString(" ") + " - m \"" + commitInfo + '\"');
				var info= CheckPathRun(nameof(Commit).ToLower() + " " + commitList.ToOneString(" ") + " -m \"" + commitInfo + '\"', path);
				if (info.StartsWith("error"))
				{
					return info;
				}
				return commitInfo;
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
				return CheckPathRun(nameof(Stash).ToLower()+ " -a", path);
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
			CreateNoWindow = false,
			RedirectStandardOutput = true,
			RedirectStandardError=true,
			UseShellExecute = false,
		};
		public static string Status(string path)
		{
			return CheckPathRun(nameof(Status).ToLower() + " -s "+ Path.GetFullPath(path), path);
		}
		[MenuItem("QTool/工具/Git/拉取更新")]
		static void AllPull()
		{
			var path = Directory.GetCurrentDirectory();
			Debug.Log(Pull(path));
		}
		[MenuItem("QTool/工具/Git/以粘贴版信息初始化仓库")]
		static  void AllInit()
		{
			if(string.IsNullOrWhiteSpace(GUIUtility.systemCopyBuffer))
			{
				EditorUtility.DisplayDialog("粘贴板信息为空", " Git仓库远端地址不能为空", "确认");
				return;

			}
			else
			{
				if (!EditorUtility.DisplayDialog("创建Git远端同步库", "以粘贴板信息 "+ GUIUtility.systemCopyBuffer+" 为远端地址创建Git仓库", "确认","取消"))
				{
					return;
				}
			}
			var path = Directory.GetCurrentDirectory();
			Debug.Log(CheckPathRun("init", path));
			Debug.Log(CheckPathRun("remote add origin \"" + GUIUtility.systemCopyBuffer + "\"", path));
			GitIgnoreFile();
			Debug.Log(CheckPathRun(nameof(Add).ToLower() + " .", path));
			Debug.Log(CheckPathRun(nameof(Commit).ToLower() + " -m 初始化", path));
			_ =Task.Run(() =>
			{
				Push(path);
			});
			//if (Directory.Exists(path+"\\.git"))
			//{
			//	Debug.LogError("Git已初始化");
			//	return;
			//}
		
			//Push(path);
			//}

		}
		#region 忽略文件
		[MenuItem("QTool/忽略文件")]
		public static void GitIgnoreFile()
		{
			FileManager.Save(".gitignore", @"# This .gitignore file should be placed at the root of your Unity project directory
#
# Get latest from https://github.com/github/gitignore/blob/main/Unity.gitignore
#
/[Ll]ibrary/
/[Tt]emp/
/[Oo]bj/
/[Bb]uild/
/[Bb]uilds/
/[Ll]ogs/
/[Uu]ser[Ss]ettings/

# MemoryCaptures can get excessive in size.
# They also could contain extremely sensitive data
/[Mm]emoryCaptures/

# Recordings can get excessive in size
/[Rr]ecordings/

# Uncomment this line if you wish to ignore the asset store tools plugin
# /[Aa]ssets/AssetStoreTools*

# Autogenerated Jetbrains Rider plugin
/[Aa]ssets/Plugins/Editor/JetBrains*

# Visual Studio cache directory
.vs/

# Gradle cache directory
.gradle/

# Autogenerated VS/MD/Consulo solution and project files
ExportedObj/
.consulo/
*.csproj
*.unityproj
*.sln
*.suo
*.tmp
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db

# Unity3D generated meta files
*.pidb.meta
*.pdb.meta
*.mdb.meta

# Unity3D generated file on crash reports
sysinfo.txt

# Builds
*.apk
*.aab
*.unitypackage
*.app

# Crashlytics generated file
crashlytics-build.properties

# Packed Addressables
/[Aa]ssets/[Aa]ddressable[Aa]ssets[Dd]ata/*/*.bin*

# Temporary auto-generated Android Assets
/[Aa]ssets/[Ss]treamingAssets/aa.meta
/[Aa]ssets/[Ss]treamingAssets/aa/*");
		}
		#endregion

	}

	public class QFileState
	{
		public string state;
		public string path;
		public bool select = true;
		public QFileState(bool hasState, string initInfo, string parentPath)
		{
			try
			{
				if (hasState)
				{
					initInfo.Trim().SplitTowString(" ", out var start, out var end);
					state = start;
					end = end.Trim().Trim('\"');
					path = Path.GetFullPath(parentPath.EndsWith(end) ? parentPath : (parentPath + "/" + end));
					select = true;


				}
				else
				{
					initInfo = initInfo.Trim('\"');
					path = Path.GetFullPath(parentPath.EndsWith(initInfo) ? parentPath : (parentPath + "/" + initInfo));
					select = false;
				}
			}
			catch (Exception e)
			{

				Debug.LogError("路径出错 " + parentPath + "   " + initInfo + " \n" + e);
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
