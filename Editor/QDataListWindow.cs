using QTool.Inspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using QTool.Reflection;
using QTool;
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
			try
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
						if (Members[i]==null)
						{
							throw new Exception("错误列[" + qdataList.TitleRow[i] + "]");
						}
					}
				}
				else
				{
					qdataList = QDataList.GetData(path);
					typeInfo = null;
				}
			}
			catch (Exception e)
			{
				Debug.LogError("解析QDataList类型[" + typeInfo?.Type + "]出错：\n" + e);
				OpenNull();
			}
			
		}
		private void OnLostFocus()
		{
			if (typeInfo != null)
			{
				objList.ToQDataList(qdataList,typeInfo.Type);
			}
			qdataList?.Save();
		}
		public Rect DrawCell(int x,int y,float widthSize,float heightSize) 
		{
			var width = GUILayout.Width(widthSize);
			var height = GUILayout.Height(heightSize);
			if (y == 0)
			{
				GUILayout.Label(qdataList[y][x], QGUITool.CenterLable, width, height);
			}
			else if(typeInfo==null)
			{
				qdataList[y].SetValueType( qdataList[y][x].Draw("", typeof(string),null,null,null, width,height),typeof(string),x);
			}
			else
			{
				var member = Members[x];
				var obj = objList[y - 1];
				member.Set(obj, member.Get(obj).Draw("", member.Type,null,null,null,width,height));
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
		public void OpenNull()
		{
			typeInfo = null;
			qdataList = null;
			Repaint();
		}
		private void OnGUI()
		{
			if (qdataList==null ) return;
			try
			{
				gridView.DoLayout(Repaint);
			}
			catch (Exception e)
			{
				Debug.LogError("表格出错：" + e);
				OpenNull();
			}
			
		}
	}
	
}
