using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Data;
using System;
#if Addressable
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
#endif
namespace QTool.Resource {
    public static class AddressableTool
    {
#if Addressable
        [MenuItem("Assets/����/��������Addressable��Դ",priority =0)]
        public static void AutoAddressableResource()
        {
            if(Selection.activeObject is DefaultAsset)
            {
                var name = Selection.activeObject.name;
                var directory = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (EditorUtility.DisplayDialog("�������Addressable��Դ", 
                    "���ļ���["+ directory + "] \n�����������ǩΪ[" + name + "]����Դ��"
                    , "ȷ��", "ȡ��"))
                {
                   
                    directory.DirectoryForeachFiles((path) =>
                    {
                        SetAddresableGroup(path,name);
                    });
                }
               
            }
        }
        public static void SetAddresableGroup(string assetPath,string groupName)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings.FindGroup(groupName);
            if (group == null)
            {

                group=settings.CreateGroup(groupName, false, false, false, new List<AddressableAssetGroupSchema>
                {settings.DefaultGroup.Schemas[0],settings.DefaultGroup.Schemas[1] }, typeof(SchemaType));
            }
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = Path.GetFileNameWithoutExtension(assetPath);
            entry.SetLabel(groupName, true, true);

        }
#endif
    }

}
