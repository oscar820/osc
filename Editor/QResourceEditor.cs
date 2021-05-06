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
namespace QTool.Editor {
    public static class AddressableTool
    {
#if Addressable
        [MenuItem("Assets/����/��������Addressable��Դ",priority =0)]
        public static void AutoAddressableResource()
        {
         
            if(Selection.activeObject is DefaultAsset)
            {
                var groupName = Selection.activeObject.name;
                var directory = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (EditorUtility.DisplayDialog("�������Addressable��Դ", 
                    "���ļ���["+ directory + "] \n�����������ǩΪ[" + groupName + "]����Դ��"
                    , "ȷ��", "ȡ��"))
                {

                    var count = directory.DirectoryFileCount();
                    var index = 1f;
                    directory.ForeachDirectoryFiles((path) =>
                    {
                        EditorUtility.DisplayProgressBar("�������Addressable��Դ", "�����Դ("+ index+"/"+count+") : " + path, index / count);
                        var key = path.Substring(directory.Length + 1);
                        key = key.Substring(0, key.LastIndexOf('.'));
                        SetAddresableGroup(path,groupName, key);
                        index++;
                    });
                    EditorUtility.ClearProgressBar();
                }
               
            }
        }
        public static void SetAddresableGroup(string assetPath,string groupName,string key="")
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
            if (string.IsNullOrWhiteSpace(key))
            {
                entry.address = Path.GetFileNameWithoutExtension(assetPath);
            }
            else
            {
                entry.address = key;
            }
            entry.labels.Clear();
            entry.SetLabel(groupName, true, true);

        }
#endif
    }

}
