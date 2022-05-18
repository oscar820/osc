using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.FlowGraph
{
  
    public class QFlowGraphAsset : ScriptableObject
    {
        public List<QObjectReference> ObjList;
        public string stringValue;
        public QFlowGraph Graph =>_graph??=
            Load();
        QFlowGraph _graph;
        public void Init(string qsmStr)
        {
            this.stringValue = qsmStr;
        }
        public QFlowGraph Load()
        {
            try
            {
                return stringValue.ParseQData<QFlowGraph>().Init();
            }
            catch (System.Exception e)
            {
                Debug.LogError(name + " 读取出错 " + e);
                return null;
            }
        }
        public void Save()
        {
            if (Graph == null) return;
            try
            {
                this.stringValue = Graph.ToQData();
#if UNITY_EDITOR
                FileManager.Save(UnityEditor.AssetDatabase.GetAssetPath(this), this.stringValue);
                UnityEditor.AssetDatabase.Refresh();
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

