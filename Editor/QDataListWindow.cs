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
		DateTime lastTime = DateTime.MinValue;
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
				lastTime = FileManager.GetLastWriteTime(path);
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
				PlayerPrefs.SetString(nameof(QDataListWindow) + "_LastPath",path);
			}
			catch (Exception e)
			{
				Debug.LogError("解析QDataList类型[" + typeInfo?.Type + "]出错：\n" + e);
				OpenNull();
			}
			
		}
		private void OnLostFocus()
		{
			if (gridView.HasChanged&&! QEidtCellWindow.IsShow)
			{
				if (typeInfo != null)
				{
					objList.ToQDataList(qdataList, typeInfo.Type);
				} 
				qdataList?.Save();
				lastTime = DateTime.Now;
				gridView.HasChanged = false;
			}
		}
		internal bool AutoOpen = true;
		private void OnFocus()
		{
			var key = nameof(QDataListWindow) + "_LastPath";
			if (PlayerPrefs.HasKey(key))
			{
				var path = PlayerPrefs.GetString(key);
				if (FileManager.GetLastWriteTime(path) > lastTime)
				{
					Open(path);
				}
			}
		}

		private void OnEnable()
		{
			gridView = new QGridView(GetValue, ()=>new Vector2Int { 
				x=qdataList.TitleRow.Count,
				y=qdataList.Count,
			});
			gridView.EditCell = EditCell;
			gridView.AddAt = AddAt;
			gridView.RemoveAt = RemoveAt;
			gridView.SetStringValue = SetValue;
		}
		public void AddAt(int y)
		{
			qdataList.CreateAt(QSerializeType.Get(typeof(QDataList)), y);
			if (objList != null)
			{
				objList.CreateAt(QSerializeType.Get(typeof(List<object>)),y-1);
			}
		}
		public void RemoveAt(int y)
		{
			qdataList.RemoveAt(y);
			if (objList != null)
			{
				objList?.RemoveAt(y-1);
			}
		}
		public string GetValue(int x,int y)
		{
			if (y == 0||typeInfo==null)
			{
				return qdataList[y][x];
			}
			else
			{
				var member = Members[x];
				var obj = objList[y - 1];
				return member.Get(obj)?.ToQDataType(member.Type,false).Trim('\"');
			}
		}
		public void SetValue(int x, int y,string value)
		{
			if (y == 0 || typeInfo == null)
			{
				qdataList[y][x] = value;
			}
			else
			{
				var member = Members[x];
				var obj = objList[y - 1];
				member.Set(obj, value.ParseQDataType(member.Type, false));
			}
		}
		public bool EditCell(int x,int y)
		{
			if (y == 0)
			{
				return false;
			}
			else if (typeInfo == null)
			{
				qdataList[y].SetValueType( QEidtCellWindow.Show(qdataList[y].Key+"."+qdataList.TitleRow[x],qdataList[y][x], typeof(string),out var changed, Members[x].MemeberInfo), typeof(string),x);
				return changed;
			}
			else
			{
				var member = Members[x];
				var obj = objList[y - 1];
				member.Set(obj,QEidtCellWindow.Show((obj as IKey<string>).Key+"."+member.ViewName,member.Get(obj), member.Type,out var changed, Members[x].MemeberInfo));
				return changed;
			}
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
				if(e is UnityEngine.ExitGUIException)
				{
					Debug.LogWarning(e);
				}
				else
				{
					Debug.LogError("表格出错：" + e);
					OpenNull();
				}
			}
			
		}
	}
	
}
