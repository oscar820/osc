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
namespace QTool.Asset {
    public static  class AddressableToolEditor
    {
#if Addressable
        [MenuItem("Assets/����/��������Addressable��Դ", priority = 0)]
        public static void AutoAddressableResource()
        {
            if (Selection.activeObject is DefaultAsset)
            {
                var groupName = Selection.activeObject.name;
                var directory = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (EditorUtility.DisplayDialog("�������Addressable��Դ",
                    "���ļ���[" + directory + "] \n�����������ǩΪ[" + groupName + "]����Դ��"
                    , "ȷ��", "ȡ��"))
                {

                    var count = directory.DirectoryFileCount();
                    var index = 1f;
                    directory.ForeachDirectoryFiles((path) =>
                    {
                        EditorUtility.DisplayProgressBar("�������Addressable��Դ", "�����Դ(" + index + "/" + count + ") : " + path, index / count);
                        var key = path.Substring(directory.Length + 1);
                        key = key.Substring(0, key.LastIndexOf('.'));
                        AddressableTool.SetAddresableGroup(path, groupName, key);
                        index++;
                    });
                    EditorUtility.ClearProgressBar();
                }

            }
        }


#endif
    }

}
