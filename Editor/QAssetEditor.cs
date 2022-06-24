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

		public const string AddressableResources = nameof(AddressableResources);
		[MenuItem("QTool/工具/批量生成AddressableResources资源")]
		public static void AutoAddressableResources()
		{
			var root = "Assets/" + AddressableResources;
			root.ForeachDirectory((directory) =>
			{
				var groupName = directory.SplitEndString(root + "/").SplitStartString("/");
				var count = directory.DirectoryFileCount();
				var index = 1f;
				directory.ForeachDirectoryFiles((path) =>
				{
					EditorUtility.DisplayProgressBar("批量添加Addressable资源", "添加资源(" + index + "/" + count + ") : " + path, index / count);
					var key = path.SplitEndString(root + "/");
					key = key.Substring(0, key.LastIndexOf('.'));
					AddressableTool.SetAddresableGroup(path, groupName, key);
					index++;
				});
				EditorUtility.ClearProgressBar();
			});
		}
#endif
    }

}
