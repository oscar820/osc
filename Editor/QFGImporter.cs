using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;
namespace QTool.FlowGraph
{
    [ScriptedImporter(1, ".qfg")]
    public class QFGImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var qsm= ScriptableObject.CreateInstance<QFlowGraphAsset>();
            qsm.Init(File.ReadAllText(ctx.assetPath));
            ctx.AddObjectToAsset(nameof(qsm), qsm); 
            ctx.SetMainObject(qsm);
        }
    }
}