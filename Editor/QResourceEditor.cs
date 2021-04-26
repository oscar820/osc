using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Data;
#if Addressable
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
#endif
namespace QTool.Resource {
    public static class AddressableTool
    {
        public static void AutoGroup(string groupName, string assetPath)
        {
#if Addressable
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings.FindGroup(groupName);
            if (group == null)
            {
                settings.CreateGroup(groupName, false, false, false, new List<AddressableAssetGroupSchema>
                {settings.DefaultGroup.Schemas[0],settings.DefaultGroup.Schemas[1] }, typeof(SchemaType));
            }
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = Path.GetFileNameWithoutExtension(assetPath);
            entry.SetLabel(groupName, true, true);
#endif
        }
    }
    
}
