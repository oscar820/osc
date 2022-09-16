using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using QTool.Binary;
#if UNITY_SWITCH
using UnityEngine.Switch;
#endif
namespace QTool
{
    public static class QFileManager
    {
        public static T QDataCopy<T>(this T target)
        {
			return target.ToQData().ParseQData<T>();
		}
		public static string ToAssetPath(this string path)
        {
            return "Assets" + path.SplitEndString(Application.dataPath);
        }
        public static void ForeachDirectoryFiles(this string rootPath, Action<string> action)
        {
            ForeachFiles(rootPath, action);
            ForeachDirectory(rootPath, (path) =>
            {
                path.ForeachDirectoryFiles(action);
            });
        }
        public static int DirectoryFileCount(this string rootPath)
        {
            var count = rootPath.FileCount();
            rootPath.ForeachDirectory((path) =>
            {
                count += rootPath.FileCount();
            });
            return count;
        }
        public static int FileCount(this string rootPath)
        {
            return ExistsDirectory(rootPath) ? Directory.GetFiles(rootPath).Length / 2 : 0;
        }
        public static void ForeachAllDirectoryWith(this string rootPath, string endsWith, Action<string> action)
        {
            rootPath.ForeachDirectory((path) =>
            {
                if (path.EndsWith(endsWith))
                {
                    action?.Invoke(path.Replace('\\', '/'));
                }
                else
                {
                    path.ForeachAllDirectoryWith(endsWith, action);
                }
            });
        }
		public static bool ExistsFile(this string path)
		{
#if UNITY_SWITCH
			if (CheckPath(ref path))
			{
				nn.fs.EntryType entryType = 0;
				nn.Result result = nn.fs.FileSystem.GetEntryType(ref entryType, path);
				if (nn.fs.FileSystem.ResultPathNotFound.Includes(result))
				{
					return false;
				}
				result.abortUnlessSuccess();
				return true;
			}
			else
#endif
			{
				return File.Exists(path);
			}
			

		}
		public static bool ExistsDirectory(this string path)
		{
			return Directory.Exists(path);
		}
		public static void ForeachDirectory(this string rootPath, Action<string> action)
        {
            if (ExistsDirectory(rootPath))
            {
                var paths = Directory.GetDirectories(rootPath);
                foreach (var path in paths)
                {
					if (string.IsNullOrWhiteSpace(path))
                    {
                        continue;
					}
					action?.Invoke(path.Replace('\\', '/'));
                }
            }
            else
            {
                Debug.LogError("错误" + " 不存在文件夹" + rootPath);
            }
        }
        public static void ForeachFiles(this string rootPath, Action<string> action)
        {
            if (ExistsDirectory(rootPath))
            {
                var paths = Directory.GetFiles(rootPath);
                foreach (var path in paths)
                {
                    if (string.IsNullOrWhiteSpace(path) || path.EndsWith(".meta"))
                    {
                        continue;
                    }
                    action?.Invoke(path.Replace('\\', '/'));
                }
            }
            else
            {
                Debug.LogError("错误" + " 不存在文件" + rootPath);
            }
        }

        public static Dictionary<string, XmlSerializer> xmlSerDic = new Dictionary<string, XmlSerializer>();
        public static XmlSerializer GetSerializer(Type type, params Type[] extraTypes)
        {
            var key = type.FullName;
            foreach (var item in extraTypes)
            {
                key += " " + item.FullName;
            }
            if (xmlSerDic.ContainsKey(key))
            {
                return xmlSerDic[key];
            }
            else
            {
                XmlSerializer xz = new XmlSerializer(type, extraTypes);
                xmlSerDic.Add(key, xz);
                return xz;
            }
        }
		public static string QXmlSerialize<T>(T t, params Type[] extraTypes)
		{
			using (StringWriter sw = new StringWriter())
			{
				if (t == null)
				{
					Debug.LogError("序列化数据为空" + typeof(T));
					return null;
				}
				GetSerializer(typeof(T), extraTypes).Serialize(sw, t);
				return sw.ToString();
			}
		}
		public static T QXmlDeserialize<T>(string s, params Type[] extraTypes)
		{
			using (StringReader sr = new StringReader(s))
			{
				try
				{
					XmlSerializer xz = GetSerializer(typeof(T), extraTypes);
					return (T)xz.Deserialize(sr);
				}
				catch (Exception e)
				{
					Debug.LogError("Xml序列化出错：\n" + e);
					return default;
				}
			}
		}
		public const string ResourcesRoot = "Assets/Resources/";
		public static DateTime GetLastWriteTime(string path)
		{
			
			try
			{
#if UNITY_SWITCH
				return DateTime.MinValue;
#else
				if (Application.isPlaying && path.StartsWith(ResourcesRoot))
				{
#if UNITY_EDITOR
					return File.GetLastWriteTime(path);
#else
					return DateTime.MinValue;
#endif
				}
				else
				{
					return File.GetLastWriteTime(path);
				}
#endif
			}
			catch (Exception e)
			{
				Debug.LogError(e);
				return DateTime.MinValue;
			}
	
		}
   
		public static void LoadAll(string path,Action<string,string> action, string defaultValue = "")
		{
			if (path.StartsWith(ResourcesRoot))
			{
				try 
				{ 
					var loadPath = path.SplitEndString(ResourcesRoot).SplitStartString(".");
					var texts= Resources.LoadAll<TextAsset>(loadPath);
					foreach (var text in texts)
					{
						action(text.text,
#if UNITY_EDITOR
							UnityEditor.AssetDatabase.GetAssetPath(text)

#else
							text.name
#endif
							); 
					} 
				}
				catch (Exception e)
				{
					Debug.LogWarning(e);
				}
			}
			else
			{
				if (ExistsFile(path))
				{
					action(Load(path, defaultValue), path);
				}
				else
				{
					path.ForeachDirectoryFiles((filePath) =>
					{
						action(Load(filePath, defaultValue),filePath);
					});
				}
			}
		}
   
        public static void ClearData(this string path)
        {
            var directoryPath = GetFolderPath(path);
            Directory.Delete(directoryPath, true);
         
        }
        /// <summary>
        /// 获取文件夹路径
        /// </summary>
        public static string GetFolderPath(this string path)
        {
            return Path.GetDirectoryName(path);
        }
    
     
   
		public static void SaveQData<T>(string path, T data)
		{
			Save(path, data.ToQData());
		}
		public static T LoadQData<T>(string path)
		{
			return Load(path).ParseQData<T>();
		}
		public static Texture2D LoadPng(string path, int width = 128, int height = 128)
		{
			var bytes = File.ReadAllBytes(path);
			Texture2D tex = new Texture2D(width, height);
			tex.LoadImage(bytes);
			return tex;
		}
		public static bool CheckPath(ref string path)
		{
			bool rightPath = true;
#if UNITY_SWITCH
			if (rightPath = Application.platform== RuntimePlatform.Switch&&  !path.StartsWith(Application.streamingAssetsPath))
			{
				path = nameof(QFileManager) + ":/" + path.Replace('/', '_').Replace('\\', '_').Replace('.', '_');
				
				Debug.LogError("转换路径 " + path);
				if (!ExistsFile(path))
				{
					UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();
					var result = nn.fs.File.Create(path, 1024*1024*10);
					result.abortUnlessSuccess();
					UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();
					Debug.LogWarning("自动创建文件 " + path);
				}
			}
			else
#endif
			{
				var directoryPath = Path.GetDirectoryName(path);
				if (!string.IsNullOrWhiteSpace(directoryPath) && !ExistsDirectory(directoryPath))
				{
					Debug.LogWarning("自动创建文件夹 " + directoryPath);
					Directory.CreateDirectory(directoryPath);
				}
			}
			return rightPath;
		}
		public static bool Save(string path, byte[] bytes,bool checkUpdate=false)
		{
			CheckPath(ref path);
			try
			{
#if UNITY_SWITCH
				if(Application.platform== RuntimePlatform.Switch)
				{
					if (path.StartsWith(Application.streamingAssetsPath))
					{
						Debug.LogError("Switch不支持写入路径 " + path);
					}
					else
					{
						Notification.EnterExitRequestHandlingSection(); ;
						nn.Result result = nn.fs.File.Open(ref fileHandle, path, nn.fs.OpenFileMode.Write);
						result.abortUnlessSuccess();
						result = nn.fs.File.Write(fileHandle, 0, bytes, bytes.LongLength, nn.fs.WriteOption.Flush);
						result.abortUnlessSuccess();
						nn.fs.File.Close(fileHandle);
						result = nn.fs.FileSystem.Commit(nameof(QFileManager));
						result.abortUnlessSuccess();
						Notification.LeaveExitRequestHandlingSection();
					}
				}
				else

#endif
				{
					if (checkUpdate)
					{
						var oldData = Load(path);
						if (!string.IsNullOrWhiteSpace(oldData) && oldData.GetHashCode() == bytes.GetHashCode())
						{
							return false;
						}
					}
				}
				

				File.WriteAllBytes(path, bytes);

				return true;
			}
			catch (Exception e)
			{
				Debug.LogError("向路径写入数据出错" + e);

				return false;
			}
		}
		public static bool Save(string path, string data, bool checkUpdate = false)
		{
			if(Application.platform== RuntimePlatform.Switch)
			{
				return Save(path, data.GetBytes(), checkUpdate);
			}
			else
			{
				CheckPath(ref path);
				File.WriteAllText(path, data);
				return true;
			}
		}
		public static byte[] LoadBytes(string path)
		{
#if UNITY_SWITCH
			if(Application.platform== RuntimePlatform.Switch)
			{
				CheckPath(ref path);
				nn.Result result = nn.fs.File.Open(ref fileHandle, path, nn.fs.OpenFileMode.Read);
				result.abortUnlessSuccess();
				long fileSize = 0;
				result = nn.fs.File.GetSize(ref fileSize, fileHandle);
				result.abortUnlessSuccess();
				byte[] data = new byte[fileSize];
				result = nn.fs.File.Read(fileHandle, 0, data, fileSize);
				result.abortUnlessSuccess();

				nn.fs.File.Close(fileHandle);
				return data;
			}
			else	
#endif
			{
				return File.ReadAllBytes(path);
			}

		}
		public static string Load(string path, string defaultValue = "")
		{
			try
			{
				if (path.StartsWith(ResourcesRoot))
				{
					var text = Resources.Load<TextAsset>(path.SplitEndString(ResourcesRoot).SplitStartString("."));
					if (text == null)
					{
						return defaultValue;
					}
					else
					{
						return text.text;
					}
				}
				else
				{
					if(Application.platform== RuntimePlatform.Switch)
					{
						return LoadBytes(path).GetString();
					}
					else
					{
						CheckPath(ref path);
						return File.ReadAllText(path);
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogError("加载文件出错[" + path + "]" + e);
				return defaultValue;
			}
		}
	
		public static void SavePng(Texture2D tex, string path)
		{
			var bytes = tex.EncodeToPNG();
			if (bytes != null)
			{
				File.WriteAllBytes(path, bytes);
			}
		}
	

 

        public static string SelectOpenPath(string title = "打开文件", string extension = "obj", string directory = "Assets")
        {
            var dialog = new FileDialog
            {
                title = title,
                initialDir = directory,
                // defExt = extension,
                filter = "(." + extension + ")\0*." + extension + "\0",
            };
            if (FileDialog.GetOpenFileName(dialog))
            {
                return dialog.file;
            }
            return "";
        }
        public static string SelectSavePath(string title = "保存文件", string directory= "Assets", string defaultName="newfile", string extension = "obj" )
        {
            var dialog = new FileDialog
            {
                title = title,
                initialDir = directory,
                file=defaultName,
                //  defExt = extension,
                filter = "(." + extension + ")\0*." + extension + "\0",
            };
            if (FileDialog.GetSaveFileName(dialog))
            {
                if (dialog.file.EndsWith("." + extension))
                {
                    return dialog.file + "." + extension;
                }
                return  dialog.file;
            }
            return "";
        }
#region SwitchData
#if UNITY_SWITCH
		private static nn.account.Uid userId;
		private static nn.fs.FileHandle fileHandle = new nn.fs.FileHandle();
		[RuntimeInitializeOnLoadMethod()]
		public static void InitSwitch()
		{	
			if(Application.platform== RuntimePlatform.Switch)
			{
				nn.account.Account.Initialize();
				nn.account.UserHandle userHandle = new nn.account.UserHandle();
				if (!nn.account.Account.TryOpenPreselectedUser(ref userHandle))
				{
					nn.Nn.Abort("Failed to open preselected user.");
				}
				nn.Result result = nn.account.Account.GetUserId(ref userId, userHandle);
				result.abortUnlessSuccess();
				result = nn.fs.SaveData.Mount(nameof(nn.fs.SaveData.Mount), userId);
				result.abortUnlessSuccess();
				Debug.LogError("Init SwitchData Over");
			}
		
		}
#endif
#endregion
	}



#region WindowsData

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public class FileDialog
	{
		public int structSize = 0;
		public IntPtr dlgOwner = IntPtr.Zero;
		public IntPtr instance = IntPtr.Zero;
		public String filter = null;
		public String customFilter = null;
		public int maxCustFilter = 0;
		public int filterIndex = 0;
		public String file = null;
		public int maxFile = 0;
		public String fileTitle = null;
		public int maxFileTitle = 0;
		public String initialDir = null;
		public String title = null;
		public int flags = 0;
		public short fileOffset = 0;
		public short fileExtension = 0;
		public String defExt = null;
		public IntPtr custData = IntPtr.Zero;
		public IntPtr hook = IntPtr.Zero;
		public String templateName = null;
		public IntPtr reservedPtr = IntPtr.Zero;
		public int reservedInt = 0;
		public int flagsEx = 0;
		public FileDialog()
		{
			structSize = Marshal.SizeOf(this);
			file = new string(new char[256]);
			maxFile = file.Length;
			fileTitle = new string(new char[64]);
			maxFileTitle = fileTitle.Length;
			flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;
		}
		[DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
		public static extern bool GetOpenFileName([In, Out] FileDialog ofd);
		[DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
		public static extern bool GetSaveFileName([In, Out] FileDialog ofd);
	}
#endregion
}
