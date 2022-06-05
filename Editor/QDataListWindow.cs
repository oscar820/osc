using QTool.Inspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using QTool.Reflection;
namespace QTool.FlowGraph
{

	public class QDataListWindow : EditorWindow
	{

		[OnOpenAsset(0)]
		public static bool OnOpen(int instanceID, int line)
		{
			//if( EditorUtility.InstanceIDToObject(instanceID) is TextAsset textAsset)
			//{
			//	return Open(textAsset);
			//}
			return false;
		}
		public TextAsset textAsset;
		public Type dataType;
		public static bool Open(TextAsset textAsset)
		{
			if (textAsset == null) return false;
			var path = AssetDatabase.GetAssetPath(textAsset);
			if (path.Contains(QDataList.ResourcesPathRoot) && path.EndsWith(".txt"))
			{
				var window = GetWindow<QDataListWindow>();
				window.minSize = new Vector2(400, 300);
				window.titleContent = new GUIContent(textAsset.name + " - " + nameof(QDataList));
				window.dataType = QReflection.ParseType(path.GetBlockValue(QDataList.ResourcesPathRoot, ".txt").SplitStartString("/"));
				if (window.dataType != null)
				{
					
					
				}
				else
				{

				}
				window.Repaint();
				return true;
			}
			return false;
		}
	}
	
}
