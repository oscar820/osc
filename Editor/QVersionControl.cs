using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using QTool.Reflection;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor.PackageManager.UI;
using UnityEditor.PackageManager;

namespace QTool
{
	public class QPackageManager : IPackageManagerExtension
	{
		static UnityEditor.PackageManager.PackageInfo CurInfo;
		static Button freshButton = new Button(()=> {
			Debug.LogError("重新拉取 " + CurInfo.name);
			Client.Add(CurInfo.git.revision);
		});
		public VisualElement CreateExtensionUI()
		{
			return freshButton;
		}

		public void OnPackageAddedOrUpdated(UnityEditor.PackageManager.PackageInfo packageInfo)
		{
		}

		public void OnPackageRemoved(UnityEditor.PackageManager.PackageInfo packageInfo)
		{
		}

		public void OnPackageSelectionChange(UnityEditor.PackageManager.PackageInfo packageInfo)
		{
			CurInfo = packageInfo;
			freshButton.text = "重新拉取Git包 " + packageInfo.displayName;
			freshButton.SetEnabled(packageInfo.git != null);
		}
	}

	[InitializeOnLoad]
	public static class QVersionControl
	{
		static QVersionControl()
		{
			PackageManagerExtensions.RegisterExtension(new QPackageManager());
			UnityEditor.Editor.finishedDefaultHeaderGUI += AddHeaderGUI;
		}
		private static void AddHeaderGUI(Editor editor)
		{
			if (!editor.target.IsAsset())
				return;
			var path = AssetDatabase.GetAssetPath(editor.target);
			if (path.EndsWith("unity_builtin_extra")) return;
			GUILayout.Space(10);
			if (GUILayout.Button(new GUIContent("同步更改"), GUILayout.Width(80)))
			{
				PullAndCommitPush(path);
			}
			GUILayout.Space(10);
		}

		static string CheckPathRun(string commond, string path,bool window=false)
		{
			try
			{
				path = Path.GetFullPath(path);
				if (Path.HasExtension(path))
				{
					path = Path.GetDirectoryName(path);
				}
			}
			catch (Exception e)
			{
				Debug.LogError(path+" " +e.ToString());
			}
			
			return Tool.ProcessCommand("git", commond,path,window);
		}
		static string Add(string addPath,string folderPath)
		{
			return CheckPathRun(nameof(Add).ToLower() + " \"" + addPath + "\"", folderPath);
		}
		static string Checkout(string path,string folderPath, string version = null)
		{
			string result = "";
			if (string.IsNullOrEmpty(version))
			{
				result=CheckPathRun(nameof(Checkout).ToLower() + " -- \"" + path + "\"", folderPath);
				
			}
			else
			{
				result= CheckPathRun(nameof(Checkout).ToLower() + " " + version + " -- \"" +path+ "\"", folderPath);

				return result;
			}
			if (CheckResult(result))
			{
				return result;
			}
			else
			{
				return CheckPathRun("clean -f \"" +path + "\"", folderPath);
			}
		}
		static string GetCurrentVersion(string path)
		{
			return (CheckPathRun("log -1 --pretty=oneline", path)).SplitStartString(" ");

		}
		static bool CheckInit(string path)
		{
			

			if (!PlayerPrefs.HasKey(nameof(CheckInit)))
			{
			
				var result = CheckPathRun("config --global core.quotepath false", path);
				var name =CheckPathRun("config user.name", path);
				if (string.IsNullOrWhiteSpace(name))
				{
					EditorUtility.ClearProgressBar();
					if (!InputTextWindow.Show("设置Git用户名", out name))
					{
						return false;
					}
					CheckPathRun("config --global user.name \"" + name + "\"", path);
				}
				PlayerPrefs.SetInt(nameof(CheckInit), 1);
			}
			
			return true;

		}
		public static string Pull(string path,bool confim=true)
		{

			var rootPath =(CheckPathRun("rev-parse --git-dir", path)).Trim().SplitStartString("/.git");
			if (rootPath.EndsWith( ".git"))
			{
				rootPath = path;
			}
			if (!CheckResult(path))
			{
				return path;
			}
			
			if (!CheckInit(path))
			{
				return "error 取消设置git基础信息";
			}
			var result =CheckPathRun(nameof(Pull).ToLower() + " origin", path,true);
			var mergerTip = "Your local changes to the following files would be overwritten by merge:";
			var untrackedTip = "error: The following untracked working tree files would be overwritten by merge:";
			if (!CheckResult(result)||result.Contains(mergerTip))
			{
				if(!result.Contains(mergerTip) &&!result.Contains(untrackedTip))
				{
					EditorUtility.DisplayDialog("拉取更新出错", result, "确认");
					return result;
				}
				var mergeErrorFile = result.GetBlockValue(mergerTip, "Please commit your changes or stash them before you merge.");
				
				mergeErrorFile+= result.GetBlockValue(untrackedTip, "Please move or remove them before you merge.");
		
				commitList.Clear();
				foreach (var fileInfo in mergeErrorFile.Trim().Split('\n'))
				{
					commitList.Add(new QFileState(false,fileInfo));
				}
				EditorUtility.ClearProgressBar();
				if (QVersionControlWindow.MergeError(commitList))
				{
					var version =GetCurrentVersion(path);
					var useStash = false;
					var files = "";
					foreach (var info in commitList)
					{
						if (!info.select)
						{
							Debug.LogError("放弃本地更改 " + info + " " +(Checkout(info.path, rootPath)));
						}else
						{
							files += info + " ";
							useStash = true;
						}
					}
					if (useStash)
					{
						QDebug.Log("保留本地更改 " +(StashPush(files, rootPath)));
					}
					var pullResult =Pull(path);
					if (useStash)
					{
						foreach (var info in commitList)
						{
							if (info.select)
							{
								Debug.LogError("放弃远端更改 " + info + " " + (Checkout(info.path, rootPath, version)));
							}
						}
						QDebug.Log("还原本地更改 " + (StashPop(rootPath)));
					} 
					return pullResult;
				}
				else
				{
					return "error 取消";
				}

			}
			else
			{
				if (confim)
				{
					EditorUtility.DisplayDialog("拉取更新完成 ", "拉取更新成功", "确认");
				}
				return result;
			}
		}
		static bool CheckResult(string path)
		{
			if (path.Contains("error") || path.Contains("fatal"))
			{
				return false;
			}
			return true;
		}
		static string Push(string path)
		{
			return CheckPathRun(nameof(Push).ToLower() + " origin master", path);
		}

		static List<QFileState> commitList = new List<QFileState>();
		static async Task AddCommitList(string path)
		{
			var statusInfo =Status(path);
			if (statusInfo.StartsWith("fatal")) return;
			var lines = statusInfo.Split('\n');
			foreach (var info in lines)
			{
				if (string.IsNullOrWhiteSpace(info)) continue;
				commitList.Add(new QFileState(true,info));
			}
		}
		static string Commit(string path)
		{
			commitList.Clear();
			AddCommitList(path);
			//if (path.StartsWith("Assets"))
			//{
			//	AddCommitList(path + ".meta");
			//}
			if (commitList.Count == 0) return "";
			EditorUtility.ClearProgressBar();
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
						Add(info.path,path);
						break;
					default:
						break;
				}
				QDebug.Log(info.state + "  " + info);
			}
			EditorUtility.ClearProgressBar();
			if (commitList.Count > 0)
			{
				return CheckPathRun(nameof(Commit).ToLower() + " " + commitList.ToOneString(" ") + " -m \"" + commitInfo + '\"', path);
			}
			else
			{
				return "";
			}
		}
		static string StashPush(string files,string path)
		{
			return CheckPathRun("stash push " + files.Trim() + " -a", path);
		}
		static string StashPop(string path)
		{
			return CheckPathRun("stash pop",path);
		}
		public static void PullAndCommitPush(string path,bool commit=true)
		{
			EditorUtility.DisplayProgressBar("同步更新", "拉取远端更新中...", 0.2f);
			var resultInfo =Pull(path);
			if (CheckResult(resultInfo)&&commit)
			{
				EditorUtility.DisplayProgressBar("同步更新", "检测本地更改", 0.5f);
				resultInfo =Commit(path);
				if (resultInfo.StartsWith("error")|| resultInfo.Contains("fatal"))
				{
					EditorUtility.DisplayDialog("提交更新失败", resultInfo, "确认");
				}
				else
				{
					EditorUtility.DisplayProgressBar("同步更新", "同步更改中...", 0.7f);
					resultInfo =Push(path);
					EditorUtility.DisplayProgressBar("同步更新", "更新完毕",0.9f);
					if (!CheckResult(resultInfo))
					{
						EditorUtility.DisplayDialog("提交更新失败", resultInfo, "确认");
					}
				}

			}
			EditorUtility.ClearProgressBar();
			if (!CheckResult(resultInfo) && EditorUtility.DisplayDialog("提交更新失败", resultInfo, "重试", "取消"))
			{
				PullAndCommitPush(path,commit);
			}
			EditorUtility.ClearProgressBar();
			UnityEditor.PackageManager.Client.Resolve();
			AssetDatabase.Refresh();
		}
	
		public static string Status(string path)
		{
			return CheckPathRun(nameof(Status).ToLower() + " -s "+"\""+Path.GetFullPath( path)+"\"", path);
		}
		[MenuItem("QTool/Git/全局拉取更新")]
		public static void AllPull()
		{
			var path = Directory.GetCurrentDirectory();
			PullAndCommitPush(path,false);
		}
		[MenuItem("QTool/Git/全局同步更新")]
		public static void AllPush()
		{
			var path = Directory.GetCurrentDirectory();
			PullAndCommitPush(path);
		}
		[MenuItem("QTool/Git/以粘贴版信息初始化仓库")]
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
			QDebug.Log(CheckPathRun("init", path));
			QDebug.Log(CheckPathRun("remote add origin \"" + GUIUtility.systemCopyBuffer + "\"", path));
			GitIgnoreFile();
			QDebug.Log(CheckPathRun(nameof(Add).ToLower() + " .", path));
			QDebug.Log(CheckPathRun(nameof(Commit).ToLower() + " -m 初始化", path));
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
		public static void GitIgnoreFile()
		{
			QFileManager.Save(".gitignore", @"# This .gitignore file should be placed at the root of your Unity project directory
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
		public string viewString;
		public QFileState(bool hasState, string initInfo)
		{
			try
			{
				if (hasState)
				{
					initInfo.Trim().SplitTowString(" ", out var start, out var end);
					state = start;
					end = end.Trim().Trim('\"');
					path = end.Trim();
					select = true;


				}
				else
				{
					initInfo = initInfo.Trim('\"');
					path = initInfo.Trim();
					select = false;
				}
				switch (state)
				{//We are<b>not</ b > amused
					case "MM":
					case "M": viewString= "<color=#99ff99><b>修改</b> " + path + "</color>"; break;
					case "??":
					case "A": viewString = "<color=#9999ff><b>新增</b> " + path + "</color>"; break;
					case "D": viewString = "<color=#ff9999><b>删除</b> " + path + "</color>"; break;
					default:
						viewString = "<color=red>"+state + " " + path+ "</color>";
						break;
				}
			}
			catch (Exception e)
			{

				Debug.LogError("路径出错 "+ initInfo + " \n" + e);
			}

		}
		public override string ToString()
		{
			return "\""+ path+"\"";
		}
	}
	public class InputTextWindow : EditorWindow
	{
		public static InputTextWindow Instance { private set; get; }
	
		public static bool Show(string name,out string text)
		{
			if (Instance == null)
			{
				Instance = GetWindow<InputTextWindow>();
				Instance.minSize = new Vector2(200, 70);
				Instance.maxSize = new Vector2(200, 70);
			}
			Instance.titleContent = new GUIContent(name);
			text = "";
			Instance.ShowModal();
			text = Instance.text;
			return Instance.confirm;
		}
		bool confirm;
		string text;
		private void OnGUI()
		{
			text = EditorGUILayout.TextField(text);
			if (GUILayout.Button("确认"))
			{
				if (string.IsNullOrWhiteSpace(text))
				{
					EditorUtility.DisplayDialog(titleContent.text+"错误", titleContent.text+ "不能为空", "确认");
				}
				else
				{
					confirm = true;
					Close();
				}
			}

			if (GUILayout.Button("取消"))
			{
				text = "";
				Close();
			}
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
						if (GUILayout.Button(file.viewString,QGUITool.CenterLable))
						{
							file.select =! file.select;
						}
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
