using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace QTool
{
	public static  class QAssetImportManager
	{
		[MenuItem("QTool/资源管理/查找当前场景所有Mesh丢失")]
		static void FindAllMeshNull()
		{
			var meshs= GameObject.FindObjectsOfType<MeshFilter>();
			foreach (var mesh in meshs)
			{
				if (Application.isPlaying? mesh.mesh == null:mesh.sharedMesh==null)
				{
					Debug.LogError(mesh.transform.GetPath()+ " Mesh为null");
				}
			}
		}
		#region 引用查找

		[MenuItem("QTool/资源管理/所有资源格式")]
		static void FindAllAssetExtension()
		{

			QDictionary<string, string> list = new QDictionary<string, string>();
			foreach (var path in AssetDatabase.GetAllAssetPaths())
			{
				if (!path.StartsWith("Assets/")) continue;
				list[Path.GetExtension(path)] = path;
			}
			Debug.LogError(list.ToOneString());
		}
		[MenuItem("QTool/资源管理/查找资源引用 %#&f")]
		static void FindreAssetFerencesMenu()
		{
			if (Selection.assetGUIDs.Length == 0)
			{
				Debug.LogError("请先选择任意一个资源 再查找资源引用");
				return;
			}
			Debug.LogError("开始查找引用[" + Selection.objects.ToOneString(" ")+"]的资源");
			var assetGUIDs = Selection.assetGUIDs;
			var assetPaths = new string[assetGUIDs.Length];
			for (int i = 0; i < assetGUIDs.Length; i++)
			{
				assetPaths[i] = AssetDatabase.GUIDToAssetPath(assetGUIDs[i]);
			}
			var allAssetPaths = AssetDatabase.GetAllAssetPaths();
			Task.Run(async ()=>
			{
				List<Task> tasks = new List<Task>();

				for (int i = 0; i < allAssetPaths.Length; i++)
				{
					var path = allAssetPaths[i];
					tasks.Add(Task.Run(() =>
					{
						var end = Path.GetExtension(path);
						switch (end)
						{
							case ".prefab":
							case ".asset":
							case ".unity":
							case ".mat":
							case ".playable":
								{
									string content = File.ReadAllText(path);
									if (content == null)
									{
										return;
									}

									for (int j = 0; j < assetGUIDs.Length; j++)
									{
										if (content.IndexOf(assetGUIDs[j]) > 0)
										{
											Debug.LogError(path + " 引用 " + assetPaths[j]);
										}
									}
								}break;
							default:
								break;
						}
					}));
				}
				foreach (var task in tasks)
				{
					await task;
				}
				Debug.LogError("查找完成");
			});
		}
		[MenuItem("QTool/资源管理/查找引用的资源")]
		static void FindDependencies()
		{
			if (Selection.assetGUIDs.Length == 0)
			{
				Debug.LogError("请先选择任意一个资源 再查找引用的资源");
				return;
			}
			Debug.LogError("开始查找资源[" + Selection.objects.ToOneString(" ") + "]的引用");
			var assetGUIDs = Selection.assetGUIDs;
			var assetPaths = new string[assetGUIDs.Length];
			for (int i = 0; i < assetGUIDs.Length; i++)
			{
				assetPaths[i] = AssetDatabase.GUIDToAssetPath(assetGUIDs[i]);
			}
			for (int i = 0; i < assetPaths.Length; i++)
			{
				if (string.IsNullOrEmpty(assetPaths[i])) continue;
				foreach (var path in AssetDatabase.GetDependencies(assetPaths))
				{
					Debug.LogError(path + " 被 " + assetPaths[i] + "引用");
				}
			}
			Debug.LogError("查找完成");
		}
		[MenuItem("QTool/资源管理/通过粘贴版Id查找资源")]
		public static void FindAsset()
		{
			try
			{
				var obj = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(GUIUtility.systemCopyBuffer), typeof(UnityEngine.Object));

				Debug.LogError("找到 " + obj);
				Selection.activeObject = obj;
			}
			catch (System.Exception e)
			{
				Debug.LogError("查找出错：" + e);
				throw;
			}
		}


		#endregion
		#region 资源格式


		[MenuItem("QTool/资源管理/删除所有自动图集")]
		public static void DeleteAllAtlas()
		{
			spriteAtlas.Clear();
			foreach (var path in AssetDatabase.GetAllAssetPaths())
			{
				if (path.EndsWith("AutoAtlas.spriteatlas"))
				{
					var assetPath = path.ToAssetPath();
					EditorUtility.DisplayProgressBar("删除自动图集", "删除 " + assetPath, 1);
					AssetDatabase.DeleteAsset(assetPath);
				}
			};
			EditorUtility.ClearProgressBar();
			AssetDatabase.SaveAssets();
		}
		public static QDictionary<string, List<string>> spriteAtlas = new QDictionary<string, List<string>>((key)=>new List<string>());
		[MenuItem("QTool/资源管理/批量设置资源格式")]
		public static void FreshAllImporter()
		{
			bool flag = true;
			spriteAtlas.Clear();
			foreach (var path in AssetDatabase.GetAllAssetPaths())
			{
				if (!flag) return;
				if (!path.StartsWith("Assets/")) continue;
				var assetPath = path.ToAssetPath();
				if(!EditorUtility.DisplayCancelableProgressBar("批量设置资源导入格式", "设置文件 " + assetPath, 1))
				{
					flag = false;
				}
				AssetImporter assetImporter = AssetImporter.GetAtPath(assetPath);
				if (assetImporter is AudioImporter audioImporter)
				{
					ReImportAudio(AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath), audioImporter);
				}
				else if (assetImporter is TextureImporter textureImporter)
				{
					ReImportTexture(AssetDatabase.LoadAssetAtPath<Texture>(assetPath), textureImporter);
				}
			};
			if (flag)
			{
				var old = spriteAtlas;
				spriteAtlas = new QDictionary<string, List<string>>((key) => new List<string>());
				var end = 0;
				foreach (var kv in old)
				{
					if (kv.Value.Count < 5 && (end = kv.Key.IndexOf('\\')) > 0)
					{
						var parentKey = kv.Key.Substring(0, end);
						spriteAtlas[parentKey].AddRange(kv.Value);
					}
					else
					{
						spriteAtlas[kv.Key].AddRange(kv.Value);
					}
				}
				foreach (var kv in spriteAtlas)
				{
					AutoSetAtlasContents(kv.Key, kv.Value);
				}
				EditorUtility.ClearProgressBar();
				AssetDatabase.SaveAssets();
			}
		}
		public static void ReImportAudio(AudioClip audio, AudioImporter audioImporter)
		{
			if (audio == null) return;
			var setting= QToolSetting.Instance;
			AudioImporterSampleSettings audioSetting = default;
			if (audio.length < 1f)
			{
				audioSetting = setting.audioImporterSettings.Get(0);
				audioImporter.preloadAudioData = true;
			}
			else if (audio.length < 3f)
			{
				audioSetting = setting.audioImporterSettings.Get(1);
				audioImporter.preloadAudioData = false;
			}
			else
			{
				audioSetting = setting.audioImporterSettings.Get(2);
				audioImporter.preloadAudioData = false;
			}
			if (!audioImporter.defaultSampleSettings.Equals(audioSetting))
			{
				if (setting.forceToMono)
				{
					audioImporter.forceToMono = true;
				}
				Debug.Log("重新导入音频[" + audioImporter.assetPath + "]");
				audioImporter.defaultSampleSettings = audioSetting;
				//audioImporter.SetOverrideSampleSettings("Standalone", audioSetting);
				//audioImporter.SetOverrideSampleSettings("iPhone", audioSetting);
				//audioImporter.SetOverrideSampleSettings("Android", audioSetting);
				audioImporter.SaveAndReimport();
			}
		}

		public static void ReImportTexture(Texture texture, TextureImporter textureImporter)
		{
			if (texture == null) return;
			var setting = QToolSetting.Instance;
			if (!textureImporter.crunchedCompression)
			{
				Debug.Log("重新导入图片[" + textureImporter.assetPath + "]");


				if (textureImporter.textureType == TextureImporterType.Sprite)
				{
					if (texture.width < 2048 && texture.height < 2048)
					{
						spriteAtlas[textureImporter.assetPath.GetFolderPath()].AddCheckExist(textureImporter.assetPath.Replace('\\', '/'));
					}
				}else
				{
					textureImporter.npotScale = TextureImporterNPOTScale.ToNearest;
				}
				if(textureImporter.textureType== TextureImporterType.Default)
				{
					if(textureImporter.textureShape== TextureImporterShape.Texture2D)
					{
						textureImporter.isReadable = false;
						textureImporter.mipmapEnabled = false;
					}
				}
				textureImporter.crunchedCompression = true;
				textureImporter.textureCompression = TextureImporterCompression.Compressed;
				textureImporter.compressionQuality = setting.compressionQuality;
				textureImporter.SaveAndReimport();
			}
			
		}

		static void AutoSetAtlasContents(string path,List<string> textures)
		{
			path = path + "/AutoAtlas.spriteatlas";
			SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
			if (atlas == null)
			{
				Debug.LogError("创建图集[" + path + "]");
				atlas = new SpriteAtlas();
				// 设置参数 可根据项目具体情况进行设置
				SpriteAtlasPackingSettings packSetting = new SpriteAtlasPackingSettings()
				{
					blockOffset = 1,
					enableRotation = false,
					enableTightPacking = false,
					padding = 2,
				};
				atlas.SetPackingSettings(packSetting);

				SpriteAtlasTextureSettings textureSetting = new SpriteAtlasTextureSettings()
				{
					readable = false,
					generateMipMaps = false,
					sRGB = true,
					filterMode = FilterMode.Bilinear,
				};
				atlas.SetTextureSettings(textureSetting);

				TextureImporterPlatformSettings platformSetting = new TextureImporterPlatformSettings()
				{
					maxTextureSize = 4096,
					format = TextureImporterFormat.Automatic,
					crunchedCompression = true,
					textureCompression = TextureImporterCompression.Compressed,
					compressionQuality = QToolSetting.Instance.compressionQuality,
				};
				atlas.SetPlatformSettings(platformSetting);
				AssetDatabase.CreateAsset(atlas, path);
			}
			foreach (var texPath in textures)
			{
				atlas.Add(AssetDatabase.LoadAllAssetsAtPath(texPath));
			}
		}
		#endregion
	}
}

