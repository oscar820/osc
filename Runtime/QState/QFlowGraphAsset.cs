using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace QTool.Flow
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
                Debug.LogError(name + " ¶ÁÈ¡³ö´í " + e);
                return null;
            }
        }
        public void Save()
        {

            try
            {
                this.stringValue = Graph.ToQData();
                FileManager.Save(AssetDatabase.GetAssetPath(this), this.stringValue);
                AssetDatabase.Refresh();
            }
            catch (System.Exception e)
            {
                Debug.LogError(name + " ´¢´æ³ö´í :" + e);
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

