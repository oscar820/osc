using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace QTool
{
	public static  class QAssetImportManager
	{
		[MenuItem("QTool/工具/删除所有自动图集")]
		public static void DeleteAllAtlas()
		{
			spriteAtlas.Clear();
			Application.dataPath.ForeachDirectoryFiles((path) =>
			{
				if (path.EndsWith("AutoAtlas.spriteatlas"))
				{
					var assetPath = path.ToAssetPath();
					EditorUtility.DisplayProgressBar("删除自动图集", "删除 " + assetPath, 1);
					AssetDatabase.DeleteAsset(assetPath);
				}
			});
			EditorUtility.ClearProgressBar();
			AssetDatabase.SaveAssets();
		}
		public static QDictionary<string, List<string>> spriteAtlas = new QDictionary<string, List<string>>((key)=>new List<string>());
		[MenuItem("QTool/工具/批量设置资源格式")]
		public static void FreshAllImporter()
		{
			spriteAtlas.Clear();
			Application.dataPath.ForeachDirectoryFiles((path) =>
			{
				var assetPath = path.ToAssetPath();
				EditorUtility.DisplayProgressBar("批量设置资源导入格式", "设置文件 " + assetPath, 1);
				AssetImporter assetImporter = AssetImporter.GetAtPath(assetPath);
				if (assetImporter is AudioImporter audioImporter)
				{
					ReImportAudio(AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath), audioImporter);
				}
				else if (assetImporter is TextureImporter textureImporter)
				{
					ReImportTexture(AssetDatabase.LoadAssetAtPath<Texture>(assetPath), textureImporter);
				}
			});
			var old = spriteAtlas;
			spriteAtlas = new QDictionary<string, List<string>>((key) => new List<string>());
			foreach (var kv in old)
			{
				if (kv.Value.Count < 5)
				{

					var parentKey = kv.Key.Substring(0, kv.Key.IndexOf('\\'));
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


				if (textureImporter.textureType != TextureImporterType.Sprite)
				{
					textureImporter.npotScale = TextureImporterNPOTScale.ToNearest;
				}
				textureImporter.mipmapEnabled = false;
				textureImporter.isReadable = false;
				textureImporter.crunchedCompression = true;
				textureImporter.textureCompression = TextureImporterCompression.CompressedLQ;
				textureImporter.compressionQuality = setting.compressionQuality;
				textureImporter.SaveAndReimport();
			}
			if (textureImporter.textureType == TextureImporterType.Sprite)
			{
				if (texture.width < 2048 && texture.height < 2048)
				{
					spriteAtlas[textureImporter.assetPath.GetFolderPath()].AddCheckExist(textureImporter.assetPath.Replace('\\', '/'));
				}
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
					textureCompression = TextureImporterCompression.CompressedLQ,
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
	}
}

