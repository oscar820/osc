using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;
namespace QTool.StateMachine
{
    [ScriptedImporter(1, ".qsm")]
    public class QSMImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var qsm= ScriptableObject.CreateInstance<QStateMachineAsset>();
            qsm.Init(File.ReadAllText(ctx.assetPath));
            ctx.AddObjectToAsset(nameof(qsm), qsm); 
            ctx.SetMainObject(qsm);
        }
    }
}