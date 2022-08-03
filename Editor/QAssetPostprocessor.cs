using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace QTool
{
	public static  class QAssetImportManager
	{

		[MenuItem("QTool/工具/批量设置资源格式")]
		public static void FreshAllImporter()
		{
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
			EditorUtility.ClearProgressBar();
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
				Debug.LogError("重新导入音频[" + audioImporter.assetPath + "]");
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
				Debug.LogError("重新导入图片[" + textureImporter.assetPath + "]");
				textureImporter.mipmapEnabled = false;
				textureImporter.isReadable = false;
				textureImporter.npotScale = TextureImporterNPOTScale.ToNearest;
				textureImporter.filterMode = FilterMode.Bilinear;
				textureImporter.crunchedCompression = true;
				textureImporter.compressionQuality = 50;
				textureImporter.SaveAndReimport();
			}
			
		}
	}
}

