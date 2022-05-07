using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace QTool.Flow
{
    public class QFlowGraphAsset : ScriptableObject
    {
        public string stringValue;
        public QFlowGraph Graph =>_graph??=Load();
        QFlowGraph _graph;
        public void Init(string qsmStr)
        {
            this.stringValue = qsmStr;
        }
        public QFlowGraph Load()
        {
            return stringValue.ParseQData<QFlowGraph>().Init();
        }
        public void Save()
        {
            this.stringValue = Graph.ToQData();
            FileManager.Save(AssetDatabase.GetAssetPath(this), this.stringValue);
            AssetDatabase.Refresh();
        }
    }
}

