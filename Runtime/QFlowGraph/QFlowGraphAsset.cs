using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.FlowGraph
{
  
    public class QFlowGraphAsset : ScriptableObject
    {
        public List<QIdObject> ObjList;
		[SerializeField]
		public QFlowGraph Graph;
        public void Init(string qsmStr)
        {
			Graph= qsmStr.ParseQData(Graph);
			Graph.SerializeString = qsmStr;
		}
        public void Save()
        { 
            try
            {
#if UNITY_EDITOR
				Graph.Name = name;

				FileManager.Save(UnityEditor.AssetDatabase.GetAssetPath(this), Graph.SerializeString);
				if (!Application.isPlaying)
				{
					UnityEditor.AssetDatabase.Refresh();
				}
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogError(name + " 储存出错 :" + e);
                return;
            }

        }
        [System.Serializable]
        public class QObjectRef : IKey<string>
        {
            public string Key { get => id; set => id = value; }
            public string id;
            public UnityEngine.Object obj;
        }
    }
}

