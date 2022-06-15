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
			if (EditorUtility.InstanceIDToObject(instanceID) is TextAsset textAsset)
			{
				return Open(textAsset);
			}
			return false;
		}
		public QSerializeType typeInfo;
		public QDataList qdataList;
		public QGridView gridView;
		public QList<object> objList = new QList<object>();
		public QList<QMemeberInfo> Members = new QList<QMemeberInfo>();
		public static bool Open(TextAsset textAsset)
		{
			if (textAsset == null) return false;
			var path = AssetDatabase.GetAssetPath(textAsset);
			if (path.Contains(nameof(QDataList) + "Assets" + '/') && path.EndsWith(".txt"))
			{
				var window = GetWindow<QDataListWindow>();
				window.minSize = new Vector2(400, 300);
				window.titleContent = new GUIContent(textAsset.name + " - " + nameof(QDataList));
				window.Open(path);
				return true;
			}
			return false;
		}
		public void Open(string path)
		{
			var type = QReflection.ParseType(path.GetBlockValue(nameof(QDataList) + "Assets" + '/', ".txt").SplitStartString("/"));
			if (type != null)
			{
				typeInfo = QSerializeType.Get(type);
				qdataList = QDataList.GetData(path);
				qdataList.ParseQdataList(objList, type);
				for (int i = 0; i < qdataList.TitleRow.Count; i++)
				{
					Members[i] = typeInfo.GetMemberInfo(qdataList.TitleRow[i]);
				}
			}
			else
			{
				qdataList = QDataList.GetData(path);
				typeInfo = null;
			}
		}
		private void OnLostFocus()
		{
			if (typeInfo != null)
			{
				objList.ToQDataList(qdataList,typeInfo.Type);
			}
			qdataList.Save();
		}
		public Rect DrawCell(int x,int y,Vector2 size) 
		{
			if (y == 0)
			{
				GUILayout.Label(qdataList[y][x],GUILayout.Width(size.x),GUILayout.Height(size.y));
			}
			else if(typeInfo==null)
			{
				qdataList[y].SetValueType( qdataList[y][x].Draw("", typeof(string)),typeof(string),x);
			}
			else
			{
				var member = Members[x];
				var obj = objList[y - 1];
				member.Set(obj, member.Get(obj).Draw("", member.Type));
			}
			var rect= GUILayoutUtility.GetLastRect();
			return rect;
		}
		private void OnEnable()
		{
			gridView = new QGridView(DrawCell,()=>new Vector2Int { 
				x=qdataList.TitleRow.Count,
				y=qdataList.Count,
			});
		}
		private void OnGUI()
		{
			if (qdataList==null ) return;
			gridView.DoLayout();
		}
	}
	
}
