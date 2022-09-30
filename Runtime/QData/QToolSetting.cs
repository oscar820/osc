using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace QTool
{
	public class QToolSetting : InstanceScriptable<QToolSetting>
	{
		public QMailAccount QAnalysisMail;
		public string QAnalysisProject;
		public string danmuRoomId= "55336";
#if UNITY_EDITOR
		[QName("音频强制单声道")]
		public bool forceToMono = true;
		[QName("音频导入设置(时长顺序为 [1s,3s,长音频])")]
		public AudioImporterSampleSettings[] audioImporterSettings = new AudioImporterSampleSettings[]
		{
			new AudioImporterSampleSettings
			{
				loadType= AudioClipLoadType.DecompressOnLoad,
				compressionFormat= AudioCompressionFormat.ADPCM,
				quality=0.8f,
			},
			new AudioImporterSampleSettings
			{
				loadType= AudioClipLoadType.CompressedInMemory,
				compressionFormat= AudioCompressionFormat.Vorbis,
				quality=0.8f,
			}
			,
			new AudioImporterSampleSettings
			{
				loadType= AudioClipLoadType.Streaming,
				compressionFormat= AudioCompressionFormat.Vorbis,
				quality=0.8f,
			}
		};
		[QName("图片压缩质量")]
		[Range(0,100)]
		public int compressionQuality = 50;
		[QName("图集大小")]
		public int AtlasSize = 4096;
#endif
		private void OnValidate()
		{
			QAnalysisMail?.Init();
		}
	}
}
