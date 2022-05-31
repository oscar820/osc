using QTool.Inspector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
namespace QTool
{

	public class QAnalysisWindow : EditorWindow
	{
		public static QAnalysisWindow Instance { private set; get; }
		[MenuItem("QTool/窗口/数据分析")]
		public static void OpenWindow()
		{
			if (Instance == null)
			{
				Instance = GetWindow<QAnalysisWindow>();
				Instance.minSize = new Vector2(400, 300);
			}
			Instance.titleContent = new GUIContent(nameof(QAnalysis) + " - " + Application.productName);
			Instance.Show();
		}
		public async void FreshData()
		{
			await QAnalysisData.FreshData();
			Repaint();
		}
		private void OnFocus()
		{
			FreshData();
		}
		Vector2 viewPos;
		private void OnGUI()
		{
			if (QAnalysisData.IsLoading)
			{
				GUI.enabled = false;
			}
			using (var toolBarHor = new GUILayout.HorizontalScope())
			{
				if (DrawButton("刷新数据"))
				{
					FreshData();
				}
				if (DrawButton("重新获取全部数据"))
				{
					QAnalysisData.Clear();
					FreshData();
				}
				//if (DrawButton("重新生成列信息"))
				//{
				//	QAnalysisData.Clear();
				//	QAnalysisData.Instance.TitleList.Clear();
				//	FreshData();
				//}
				if (DrawButton("复制表格数据"))
				{
					GUIUtility.systemCopyBuffer = QAnalysisData.Copy();
					EditorUtility.DisplayDialog("复制表格数据", "复制数据成功：\n "+GUIUtility.systemCopyBuffer, "确认");
				}
				var lastRect = GUILayoutUtility.GetLastRect();
				Handles.DrawLine(new Vector3(0, lastRect.yMax), new Vector3(position.xMax, lastRect.yMax));
			}
			using (var playerDataScroll = new GUILayout.ScrollViewScope(viewPos))
			{
				using (new GUILayout.VerticalScope())
				{

					using (new GUILayout.HorizontalScope())
					{
						DrawCell("玩家ID", 250, true, true);
						foreach (var title in QAnalysisData.Instance.TitleList)
						{
							DrawCell(title.Key+"\n<size=10>"+title.DataSetting+"</size>", title.width, false, true,(menu)=> {
								
								foreach (var eventKey in QAnalysisData.Instance.EventKeyList)
								{
									menu.AddItem(new GUIContent("数据来源/" + eventKey), eventKey == title.DataSetting.dataKey, () =>
									{
										title.ChangeEvent(eventKey);
									});
								}
								var modes = Enum.GetNames(typeof(QAnalysisMode));
								
								foreach (var mode in modes)
								{
									menu.AddItem(new GUIContent("计算方式/" + mode), mode == title.DataSetting.mode.ToString(), () =>
									{
										title.ChangeMode(mode);
									});
								}
								menu.AddSeparator("");
								menu.AddItem(new GUIContent("新建数据列"), false, () =>
								{
									if (QNewTitleWindow.GetNewTitle(out var newTitle))
									{
										QAnalysisData.Instance.AddTitle(newTitle);
										QAnalysisData.Instance.FreshKey(newTitle.Key, true);
									}
								});
							
								menu.AddItem(new GUIContent("设置数据列"), false, () => {
									if (QNewTitleWindow.ChangeTitle(title))
									{
										QAnalysisData.Instance.FreshKey(title.Key, true);
									}
								});
								menu.AddItem(new GUIContent("删除数据列"), false, () => {
									if (EditorUtility.DisplayDialog("删除确认", "删除数据列 " + title.Key, "确认", "取消"))
									{
										QAnalysisData.Instance.RemveTitle(title);
									}
								});
							});
						}
						GUILayout.FlexibleSpace();
					}
					using (new GUILayout.HorizontalScope())
					{
						using (new GUILayout.VerticalScope())
						{
							foreach (var data in QAnalysisData.Instance.PlayerDataList)
							{
								DrawCell(data.Key, 250, true,false);
							}
						}

						using (new GUILayout.VerticalScope())
						{
							foreach (var playerData in QAnalysisData.Instance.PlayerDataList)
							{
								using (new GUILayout.HorizontalScope())
								{
									foreach (var title in QAnalysisData.Instance.TitleList)
									{
										DrawCell(playerData.AnalysisData[title.Key].value,title.width);
									}
									GUILayout.FlexibleSpace();
								}
							}
						}
						viewPos = playerDataScroll.scrollPosition;
					}
				}
			}
			if (QAnalysisData.IsLoading)
			{
				GUI.enabled = true;
				GUI.Label(new Rect(Vector2.zero,position.size), "加载中..",QGUITool.BackStyle);
			}
			
		}

	
		public void DrawCell(string value,float width,bool drawXLine,bool drawYLine,Action<GenericMenu> menu=null)
		{
			DrawCell(value,width);
			var lastRect = GUILayoutUtility.GetLastRect();
			if (drawXLine)
			{
				Handles.DrawLine(new Vector3(viewPos.x, lastRect.yMax), new Vector3(viewPos.x+position.xMax, lastRect.yMax));
			}
			if (drawYLine)
			{
				if (drawXLine)
				{
					Handles.DrawLine(new Vector3(lastRect.xMax, viewPos.y + lastRect.yMin), new Vector3(lastRect.xMax, viewPos.y + position.yMax)); ;
				}
				else
				{
					var lastColor = Handles.color;
					Handles.color = Color.grey;
					Handles.DrawLine(new Vector3(lastRect.xMax, viewPos.y + lastRect.yMin), new Vector3(lastRect.xMax, viewPos.y + position.yMax)); ;
					Handles.color = lastColor;
				}
				
			}
			if (menu != null)
			{
				lastRect.RightMenu(menu);
			}
		}
		public void DrawCell(object value,float width=200)
		{
			GUILayout.Label(value?.ToString(), QGUITool.CenterLable, GUILayout.Width(width), GUILayout.Height(36));
		}
		public bool DrawButton(string name)
		{
			return GUILayout.Button(name, GUILayout.Width(100));
		}
	}
	public class QNewTitleWindow : EditorWindow
	{
		public static QNewTitleWindow Instance { private set; get; }
		public static bool GetNewTitle(out QTitleInfo newInfo)
		{
			if (Instance == null)
			{
				Instance = GetWindow<QNewTitleWindow>();
				Instance.minSize = new Vector2(300, 100);
				Instance.maxSize = new Vector2(300, 100);
			}
			Instance.titleContent = new GUIContent("新建数据列");
			Instance.confirm = false;
			Instance.create = true;
			Instance.titleInfo = new QTitleInfo();
			Instance.ShowModal();
			newInfo = Instance.titleInfo;
			return Instance.confirm;
		}
		public static bool ChangeTitle(QTitleInfo info)
		{
			if (Instance == null)
			{
				Instance = GetWindow<QNewTitleWindow>();
				Instance.minSize = new Vector2(300, 100);
				Instance.maxSize = new Vector2(300, 100);
			}
			Instance.titleContent = new GUIContent("设置列信息");
			Instance.create = false;
			Instance.confirm = false;
			Instance.titleInfo = info;
			Instance.ShowModal();
			return Instance.confirm;
		}
		bool create = false;
		public QTitleInfo titleInfo = new QTitleInfo();
		public bool confirm = false;
		private void OnGUI()
		{
			using (new GUILayout.VerticalScope())
			{
				titleInfo.Key = (string)titleInfo.Key.Draw("列名", typeof(string));
				var names = QAnalysisData.Instance.EventKeyList;
				if (names.Count > 0)
				{
					if (names.IndexOf(titleInfo.DataSetting.dataKey) < 0)
					{
						titleInfo.DataSetting.dataKey = names[0];
					}
					titleInfo.DataSetting.dataKey = names[EditorGUILayout.Popup("数据来源", names.IndexOf(titleInfo.DataSetting.dataKey), names.ToArray())];
				}
				titleInfo.DataSetting.mode = (QAnalysisMode)titleInfo.DataSetting.mode.Draw("计算方式", typeof(QAnalysisMode));
				titleInfo.width =Mathf.Clamp( (int)((int)titleInfo.width).Draw("列宽", typeof(int)),10,500);
				GUILayout.FlexibleSpace();

				using (new GUILayout.HorizontalScope())
				{
					if (GUILayout.Button("确认"))
					{
						if (string.IsNullOrWhiteSpace(titleInfo.Key))
						{
							if (EditorUtility.DisplayDialog("错误的列名", "不能为空", "确认"))
							{
								return;
							}
						}
						if (Instance.create&&QAnalysisData.Instance.TitleList.ContainsKey(titleInfo.Key))
						{
							if (EditorUtility.DisplayDialog("错误的列名", "已存在列名" + titleInfo.Key, "确认"))
							{
								return;
							}
						}
						if (string.IsNullOrWhiteSpace(titleInfo.DataSetting.dataKey))
						{
							if (EditorUtility.DisplayDialog("错误的事件名", "不能为空", "确认"))
							{
								return;
							}
						}
						confirm = true;
						Close();
					}
					if (GUILayout.Button("取消"))
					{
						confirm = false;
						Close();
					}
				}
			}


		}
	}
	public class QAnalysisData
	{
		public static QAnalysisData Instance { get; private set; } = Activator.CreateInstance<QAnalysisData>();
		public QList<string, QAnalysisEvent> EventList = new QList<string, QAnalysisEvent>();
		public QAutoList<string, QPlayerData> PlayerDataList = new QAutoList<string, QPlayerData>();
		public QAutoList<string, QTitleInfo> TitleList = new QAutoList<string, QTitleInfo>();
		public List<string> EventKeyList = new List<string>();
		public QMailInfo LastMail=null;
		public static QAnalysisEvent GetEvent(string eventId)
		{
			return Instance.EventList[eventId];
		}
		static QAnalysisData()
		{
			FileManager.Load("QTool/" + QAnalysis.StartKey, "{}").ParseQData(Instance);
		}
		public static bool IsLoading { get; private set; } = false;
		public void AddTitle(QTitleInfo newTitle)
		{ 
			TitleList.Add(newTitle);
			FreshKey(newTitle.Key);
		}
		public void RemveTitle(QTitleInfo title)
		{
			if (TitleList.ContainsKey(title.Key))
			{
				TitleList.Remove(title.Key);
				foreach (var playerData in PlayerDataList)
				{
					playerData.AnalysisData.Remove(title.Key);
				}
				SaveData();
			}
		}
		public static async Task FreshData() 
		{
			if (IsLoading) return;
			if (!QToolSetting.Instance.QAnalysisMail.InitOver)
			{
				Debug.LogError(nameof(QToolSetting.Instance.QAnalysisMail) + " 未设置");
				return;
			}
			IsLoading = true;
			await QMailTool.FreshEmails(QToolSetting.Instance.QAnalysisMail, (mailInfo) =>
			{ 
				if (mailInfo.Subject.StartsWith(QAnalysis.StartKey))
				{
					AddEvent(mailInfo.Body.ParseQData<List<QAnalysisEvent>>());
				}
				Instance.LastMail = mailInfo;
			}, Instance.LastMail);

			SaveData();
			IsLoading = false;
		}
		
		public static void SaveData()
		{
			FileManager.Save("QTool/" + QAnalysis.StartKey, Instance.ToQData());
		}
		public void FreshKey(string titleKey,bool freshEventList=false)
		{
			foreach (var playerData in PlayerDataList)
			{
				playerData.FreshKey(titleKey, freshEventList);
			}
			SaveData();
		}
		public static void Clear()
		{
			var titleInfo = Instance.TitleList;
			Instance = Activator.CreateInstance<QAnalysisData>();
			Instance.TitleList = titleInfo;
		}
		public static string Copy()
		{
			var data="玩家ID\t"+ Instance.TitleList.ToOneString("\t", (title) => title.Key)+"\n";
			data += Instance.PlayerDataList.ToOneString("\n", (playerData) => playerData.Key+"\t"+ playerData.AnalysisData.ToOneString("\t"));
			return data;
		}
		public static void AddEvent(List<QAnalysisEvent> newEventList)
		{
			foreach (var eventData in newEventList)
			{
				if (!Instance.TitleList.ContainsKey(eventData.eventKey))
				{
					var title = Instance.TitleList[eventData.eventKey];
					title.DataSetting.dataKey = eventData.eventKey;
					if (eventData.eventValue == null)
					{
						title.DataSetting.mode = QAnalysisMode.最新时间;
					}
					else
					{
						title.DataSetting.mode = QAnalysisMode.最新数据;
					}
				}
				Instance.EventKeyList.AddCheckExist(eventData.eventKey);
				if (eventData.eventValue != null)
				{
					foreach (var memeberInfo in QSerializeType.Get(eventData.eventValue.GetType()).Members)
					{
						Instance.EventKeyList.AddCheckExist(eventData.eventKey+"/"+memeberInfo.Name);
					}
				}
				Instance.EventList.Add(eventData);
				Instance.PlayerDataList[eventData.playerId].Add(eventData);
			}
		}

	}
	public class QTitleInfo:IKey<string>
	{
		public string Key { get; set; }
		public float width = 100;
		public QAnalysisSetting DataSetting = new QAnalysisSetting();
		public void ChangeMode(string modeKey)
		{
			DataSetting.mode=(QAnalysisMode) Enum.Parse(typeof(QAnalysisMode), modeKey);
			QAnalysisData.Instance.FreshKey(Key);
		}
		public void ChangeEvent(string eventKey)
		{
			DataSetting.dataKey = eventKey;
			QAnalysisData.Instance.FreshKey(Key,true);
		}
	}
	public enum QAnalysisMode
	{
		最新数据,
		起始数据,
		最新时间,
		起始时间,
		次数,
		最新时长,
		总时长,
	}
	public class QAnalysisSetting
	{
		public string dataKey; 
		public QAnalysisMode mode = QAnalysisMode.最新数据;
		public string EventKey
		{
			get
			{
				if (dataKey.Contains("/"))
				{
					return dataKey.SplitStartString("/");
				}
				else
				{
					return dataKey;
				}
			}
		}
		public string TargetKey 
		{
			get
			{
				switch (mode)
				{
					case QAnalysisMode.最新时长:
					case QAnalysisMode.总时长:
						if (EventKey.EndsWith("结束"))
						{
							return EventKey.Replace("结束", "开始");
						}
						else
						{
							return EventKey;
						}
					default:
						return EventKey;
				}
			

			}
		}
		public override string ToString()
		{
			return "("+dataKey + " " + mode+")";
		}
	}
	public class QAnalysisInfo:IKey<string>
	{
		public string Key { get; set; }
		public object value;
		public DateTime UpdateTime;
		public List<string> EventList = new List<string>();
		public void AddEvent(QAnalysisEvent eventData)
		{
			UpdateTime = eventData.eventTime;
			EventList.AddCheckExist(eventData.Key);
			FreshMode();
		}
		public DateTime GetTime(DateTime defaulTime )
		{
			if (EventList.Count == 0) return defaulTime;
			return QAnalysisData.GetEvent(EventList.StackPeek()).eventTime;
		}
		public void FreshMode()
		{
			var setting = QAnalysisData.Instance.TitleList[Key].DataSetting;
			if (EventList.Count == 0) { UpdateTime = default; value = null;return; }
			switch (setting.mode)
			{
				case QAnalysisMode.最新数据:
					value = QAnalysisData.Instance.EventList[EventList.StackPeek()].GetValue(setting.dataKey);
					break;
				case QAnalysisMode.起始数据:
					value = QAnalysisData.Instance.EventList[EventList.QueuePeek()].GetValue(setting.dataKey);
					break;
				case QAnalysisMode.最新时间:
					value = QAnalysisData.Instance.EventList[EventList.StackPeek()].eventTime;
					break;
				case QAnalysisMode.起始时间:
					value = QAnalysisData.Instance.EventList[EventList.QueuePeek()].eventTime;
					break;
				case QAnalysisMode.次数:
					value = EventList.Count;
					break;
				case QAnalysisMode.最新时长:
					{
						var endData = QAnalysisData.Instance.EventList[EventList.StackPeek()];
						var playerData = QAnalysisData.Instance.PlayerDataList[endData.playerId];
						var startData = playerData.AnalysisData[setting.TargetKey];
						value = endData.eventTime - startData.GetTime(endData.eventTime);
					}
					break;
				case QAnalysisMode.总时长:
					{
						var eventData = QAnalysisData.Instance.EventList[EventList.StackPeek()];
						var playerData = QAnalysisData.Instance.PlayerDataList[eventData.playerId];
						var startData = playerData.AnalysisData[setting.TargetKey];
						var startIndex = startData.EventList.Count-1;
						var endIndex =EventList.Count-1;
						TimeSpan allTime = default;
						while (startIndex>=0&&endIndex>=0)
						{
							var startTime =QAnalysisData.GetEvent(startData.EventList[startIndex]).eventTime;
							var endTime = QAnalysisData.GetEvent(EventList[endIndex]).eventTime;
							if (startTime <= endTime)
							{
								allTime += endTime - startTime;
								startIndex--;
								endIndex--;
							}
							else
							{
								startIndex--;
							}
						}
						value = allTime;

					}
					break;
				default:
					break;
			}
		}

		public override string ToString()
		{
			return value?.ToString();
		}

	}
	public class QPlayerData : IKey<string>
	{
		public QAutoList<string, QAnalysisInfo> AnalysisData = new QAutoList<string, QAnalysisInfo>();
		public string Key { get; set; }
		public DateTime UpdateTime;
		public List<string> EventList = new List<string>();
		public void Add(QAnalysisEvent eventData)
		{
			UpdateTime = eventData.eventTime;
			EventList.AddCheckExist(eventData.eventId);
			foreach (var title in QAnalysisData.Instance.TitleList)
			{
				if (title.DataSetting.EventKey == eventData.eventKey)
				{
					AnalysisData[title.Key].AddEvent(eventData);
				}
				else if(title.DataSetting.TargetKey == eventData.eventKey)
				{
					AnalysisData[title.Key].FreshMode();
				}
			}
		}
		public void FreshKey(string titleKey,bool freshEventList)
		{
			var info = AnalysisData[titleKey];
			if (freshEventList)
			{
				info.EventList.Clear();
				var setting = QAnalysisData.Instance.TitleList[titleKey].DataSetting;
				foreach (var eventId in EventList)
				{
					var eventData = QAnalysisData.Instance.EventList[eventId];
					if (eventData.eventKey == setting.EventKey)
					{
						info.EventList.AddCheckExist(eventData.eventId);
					}
				}
			}
			info.FreshMode();
		}
	}
	public static class QGUITool
	{
		static Stack<Color> colorStack = new Stack<Color>();
		public static void SetColor(Color newColor)
		{
			colorStack.Push( GUI.color);
			GUI.color = newColor;
		}
		public static void RevertColor()
		{
			GUI.color = colorStack.Pop();
		}
		public static GUIStyle TitleLable => _titleLabel ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
		static GUIStyle _titleLabel;
		public static GUIStyle CenterLable => _centerLable ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter,richText=true };
		static GUIStyle _centerLable;
		public static GUIStyle TextArea => _textArea ??= new GUIStyle(EditorStyles.textField) { alignment = TextAnchor.MiddleCenter };
		static GUIStyle _textArea;
		public static GUIStyle LeftLable => _leftLable ??= new GUIStyle(EditorStyles.label);
		static GUIStyle _leftLable;
		public static GUIStyle RightLabel => _rightLabel ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
		static GUIStyle _rightLabel;
		public static Texture2D NodeEditorBackTexture2D => _nodeEditorBackTexture2D ??= Resources.Load<Texture2D>("NodeEditorBackground");
		static Texture2D _nodeEditorBackTexture2D = null;
		public static Texture2D DotTexture2D => _dotTextrue2D ??= Resources.Load<Texture2D>("NodeEditorDot");
		static Texture2D _dotTextrue2D;
		public static GUIStyle BackStyle => _backStyle ??= new GUIStyle("helpBox") { alignment = TextAnchor.MiddleCenter };
		static GUIStyle _backStyle;

		public static GUIStyle CellStyle => _cellStyle ??= new GUIStyle("GroupBox") { alignment = TextAnchor.MiddleCenter };
		static GUIStyle _cellStyle;
	}
}
