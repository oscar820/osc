using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Data;
using System;
using System.Threading.Tasks;
using UnityEngine.U2D;
using UnityEditor.U2D;
#if Addressable
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
#endif
namespace QTool.Asset {
	public static class AddressableToolEditor
	{
		[MenuItem("QTool/资源管理/查找当前场景所有Mesh丢失")]
		static void FindAllMeshNull()
		{
			var meshs = GameObject.FindObjectsOfType<MeshFilter>();
			foreach (var mesh in meshs)
			{
				if (Application.isPlaying ? mesh.mesh == null : mesh.sharedMesh == null)
				{
					Debug.LogError(mesh.transform.GetPath() + " Mesh为null");
				}
			}
		}
		#region 引用查找
		[MenuItem("QTool/资源管理/复制Id")]
		static void CopyID()
		{
			if (Selection.assetGUIDs.Length == 1)
			{
				if (Selection.activeObject != null)
				{
					GUIUtility.systemCopyBuffer = Selection.assetGUIDs[0];
					Debug.LogError("复制 " + Selection.activeObject.name + " Id[" + GUIUtility.systemCopyBuffer + "]");
				}
			}
			else
			{
				Debug.LogError("选中过多");
			}
		}
		[MenuItem("QTool/资源管理/使用粘贴板Id替换当前Id")]
		static void RepleaceID()
		{
			if (Selection.assetGUIDs.Length == 1)
			{
				var target = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(GUIUtility.systemCopyBuffer), typeof(UnityEngine.Object));

				if (Selection.activeObject != null && target != null && Selection.activeObject != target)
				{
					if (Selection.activeObject.GetType() != target.GetType())
					{
						Debug.LogError(Selection.activeObject.name + " : " + target.name + " 类型不匹配");
						return;
					}
					var oldPath = AssetDatabase.GetAssetPath(Selection.activeObject);
					if (EditorUtility.DisplayDialog("资源替换", "确定将" + oldPath + "替换为" + AssetDatabase.GetAssetPath(target), "确定", "取消"))
					{
						Debug.LogError("将" + oldPath + "替换为" + AssetDatabase.GetAssetPath(target));
						var oldId = Selection.assetGUIDs[0];
						var newId = GUIUtility.systemCopyBuffer;
						foreach (var path in AssetDatabase.GetAllAssetPaths())
						{
							if (!path.StartsWith("Assets/") || path == oldPath) continue;

							var end = Path.GetExtension(path);
							switch (end)
							{
								case ".prefab":
								case ".asset":
								case ".unity":
								case ".mat":
								case ".playable":
									{
										var text = QFileManager.Load(path);
										if (text.Contains(oldId))
										{
											Debug.LogError("更改[" + path + "]引用资源");
											QFileManager.Save(path, text.Replace(oldId, newId));
										}
									}
									break;
								default:
									break;
							}

						}
					}

				}
			}
			else
			{
				Debug.LogError("选中过多");
			}
		}
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
			Debug.LogError("开始查找引用[" + Selection.objects.ToOneString(" ", (obj) => obj.name) + "]的资源");
			var assetGUIDs = Selection.assetGUIDs;
			var assetPaths = new string[assetGUIDs.Length];
			for (int i = 0; i < assetGUIDs.Length; i++)
			{
				assetPaths[i] = AssetDatabase.GUIDToAssetPath(assetGUIDs[i]);
			}
			var allAssetPaths = AssetDatabase.GetAllAssetPaths();
			Task.Run(async () =>
			{
				List<Task> tasks = new List<Task>();

				for (int i = 0; i < allAssetPaths.Length; i++)
				{
					var path = allAssetPaths[i];
					if (!path.StartsWith("Assets/")) continue;
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
							case ".shadergraph":
							case ".shadersubgraph":
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
								}
								break;
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
			Debug.LogError("开始查找资源[" + Selection.objects.ToOneString(" ", (obj) => obj.name) + "]的引用");
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


		//[MenuItem("QTool/资源管理/删除所有自动图集")]
		//public static void DeleteAllAtlas()
		//{
		//	foreach (var path in AssetDatabase.GetAllAssetPaths())
		//	{
		//		if (path.EndsWith("AutoAtlas.spriteatlas"))
		//		{
		//			var assetPath = path.ToAssetPath();
		//			Debug.LogError("删除 " + assetPath);
		//			AssetDatabase.DeleteAsset(assetPath);
		//		}
		//	};
		//	EditorUtility.ClearProgressBar();
		//	AssetDatabase.SaveAssets();
		//}
		//public static QDictionary<string, List<string>> spriteAtlas = new QDictionary<string, List<string>>((key) => new List<string>());
		[MenuItem("QTool/资源管理/批量设置资源格式")]
		public static void FreshAllImporter()
		{
			var paths = AssetDatabase.GetAllAssetPaths();
			foreach (var path in paths)
			{
				if (!path.StartsWith("Assets/")) continue;
				AssetImporter assetImporter = AssetImporter.GetAtPath(path);
				if (assetImporter is AudioImporter audioImporter)
				{
					ReImportAudio(AssetDatabase.LoadAssetAtPath<AudioClip>(path), audioImporter);
				}
				else if (assetImporter is TextureImporter textureImporter)
				{
					ReImportTexture(AssetDatabase.LoadAssetAtPath<Texture>(path), textureImporter);
				}
			};
			EditorUtility.ClearProgressBar();
			AssetDatabase.SaveAssets();
		}
		public static void ReImportAudio(AudioClip audio, AudioImporter audioImporter)
		{
			if (audio == null) return;
			var setting = QToolSetting.Instance;
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
		public readonly static List<int> TextureSize = new List<int> {1,4,8,16 ,32, 64,128,256,512,1024,2048,4096 };
		public static void ReImportTexture(Texture texture, TextureImporter textureImporter)
		{
			if (texture == null) return;
			var setting = QToolSetting.Instance;
			if (!textureImporter.crunchedCompression|| (textureImporter.maxTextureSize>texture.width&&textureImporter.maxTextureSize>texture.height))
			{
				Debug.Log("重新导入图片[" + textureImporter.assetPath + "]");

				if(textureImporter.maxTextureSize > texture.width && textureImporter.maxTextureSize > texture.height)
				{
					for (int i = 0; i < TextureSize.Count - 1 && textureImporter.maxTextureSize > TextureSize[i]; i++)
					{
						var minSize = TextureSize[i];
						var maxSize = TextureSize[i + 1];
						if (texture.width >= minSize || texture.height >= minSize)
						{
							if (texture.width <= maxSize && texture.height <= maxSize)
							{
								textureImporter.maxTextureSize = minSize;
								Debug.LogError(texture + "  " + nameof(textureImporter.maxTextureSize) + " : " + minSize);
								break;
							}
						}
					}
				}
				
				if (textureImporter.textureType != TextureImporterType.Sprite)
				{
					textureImporter.npotScale = TextureImporterNPOTScale.ToSmaller;
				}
				if (textureImporter.textureType == TextureImporterType.Default)
				{
					if (textureImporter.textureShape == TextureImporterShape.Texture2D)
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

		//static void AutoSetAtlasContents(string path, List<string> textures)
		//{
		//	path = path + "/AutoAtlas.spriteatlas";
		//	SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
		//	if (atlas == null)
		//	{
		//		Debug.LogError("创建图集[" + path + "]");
		//		atlas = new SpriteAtlas();
		//		// 设置参数 可根据项目具体情况进行设置
		//		SpriteAtlasPackingSettings packSetting = new SpriteAtlasPackingSettings()
		//		{
		//			blockOffset = 1,
		//			enableRotation = false,
		//			enableTightPacking = false,
		//			padding = 2,
		//		};
		//		atlas.SetPackingSettings(packSetting);

		//		SpriteAtlasTextureSettings textureSetting = new SpriteAtlasTextureSettings()
		//		{
		//			readable = false,
		//			generateMipMaps = false,
		//			sRGB = true,
		//			filterMode = FilterMode.Bilinear,
		//		};
		//		atlas.SetTextureSettings(textureSetting);

		//		TextureImporterPlatformSettings platformSetting = new TextureImporterPlatformSettings()
		//		{
		//			maxTextureSize = 4096,
		//			format = TextureImporterFormat.Automatic,
		//			crunchedCompression = true,
		//			textureCompression = TextureImporterCompression.Compressed,
		//			compressionQuality = QToolSetting.Instance.compressionQuality,
		//		};
		//		atlas.SetPlatformSettings(platformSetting);
		//		AssetDatabase.CreateAsset(atlas, path);
		//	}
		//	foreach (var texPath in textures)
		//	{
		//		atlas.Add(AssetDatabase.LoadAllAssetsAtPath(texPath));
		//	}
		//}
		#endregion
#if Addressable

		public const string AddressableResources = nameof(AddressableResources);
		[MenuItem("QTool/资源管理/批量生成AddressableResources资源",false,11)]
		public static void AutoAddressableResources()
		{
			var root = "Assets/" + AddressableResources;
			root.ForeachDirectory((directory) =>
			{
				var groupName = directory.SplitEndString(root + "/").SplitStartString("/");
				var count = directory.DirectoryFileCount();
				var index = 1f;
				directory.ForeachDirectoryFiles((path) =>
				{
					EditorUtility.DisplayProgressBar("批量添加Addressable资源", "添加资源(" + index + "/" + count + ") : " + path, index / count);
					var key = path.SplitEndString(root + "/");
					key = key.Substring(0, key.LastIndexOf('.'));
					AddressableTool.SetAddresableGroup(path, groupName, key);
					index++;
				});
				EditorUtility.ClearProgressBar();
			});
			AssetDatabase.SaveAssets();
		}
#endif
    }

}
