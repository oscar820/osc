using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace QTool.TileMap {
	[System.Serializable]
	public class QualityLevelObject
	{
		public int minLevel = 0;
		public int maxLevel = 0;
		public GameObject obj;
		public BoolEvent OnActive;
		public bool InLevel(int level)
		{
			return level >= minLevel && level <= maxLevel;
		}
	}
	[ExecuteInEditMode]
	public class QQualityLevel : MonoBehaviour
	{
		#region 质量检测
		public static int curLevel = -1;
		static QQualityLevel()
		{
			QToolManager.OnUpdate += CheckUpdate;
		}
		static void CheckUpdate()
		{
			if (curLevel != QualitySettings.GetQualityLevel())
			{
				curLevel = QualitySettings.GetQualityLevel();
				OnFresh?.Invoke(curLevel);
			}
		}
		public static System.Action<int> OnFresh;
		#endregion
		public List<QualityLevelObject> levelObj = new List<QualityLevelObject>();

#if UNITY_EDITOR
		private void OnValidate()
		{
			Fresh(QualitySettings.GetQualityLevel());
		}
#endif
		private void Awake()
		{
			Fresh(QualitySettings.GetQualityLevel());
			OnFresh += Fresh;
		}
		private void OnDestroy()
		{
			OnFresh -= Fresh;
		}
		void Fresh(int level)
		{
			for (int i = 0; i < levelObj.Count; i++)
			{
				if (levelObj[i].obj != null)
				{
					levelObj[i].obj?.SetActive(levelObj[i].InLevel(level));
				}
				levelObj[i].OnActive?.Invoke(levelObj[i].InLevel(level));
			}
		}
	}

}
