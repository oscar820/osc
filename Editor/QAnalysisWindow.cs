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
		private void OnEnable()
		{
			Instance = this;
		}
		public async void FreshData()
		{
			await QAnalysisData.FreshData();
			Repaint();
		}
		Stack<string> ViewInfoStack = new Stack<string>();
		Stack<string> ViewPlayerStack = new Stack<string>();
		public string ViewEvent=> ViewInfoStack.Count > 0 ? ViewInfoStack.Peek() : "玩家Id";
		public string ViewPlayer => ViewPlayerStack.Count > 0 ? ViewPlayerStack.Peek():"";
		Vector2 viewPos;

		List<string> viewEventList = new List<string>();
		int startIndex = -1; 
		int endIndex = - 1;
		Rect viewRect;
		QList<Rect> elementRect = new QList<Rect>(); 
		public void ViewChange(string newEvent,string newPlayer)
		{
			if (newEvent != ViewEvent || newPlayer != ViewPlayer)
			{
				ViewInfoStack.Push(newEvent);
				ViewPlayerStack.Push(newPlayer);
				FreshView();
			}
		}
		public void FreshView()
		{
			if(!string.IsNullOrEmpty(ViewPlayer))
			{
				var playerData = QAnalysisData.Instance.PlayerDataList[ViewPlayer];
				viewEventList.Clear();
				foreach (var eventId in playerData.EventList)
				{
					var eventData = QAnalysisData.GetEvent(eventId);
					QAnalysisData.ForeachTitle((title) =>
					{
						if (eventData.eventKey == title.DataSetting.EventKey)
						{
							viewEventList.AddCheckExist(eventData.Key);
							return;
						}
					});
				}
			}
		}
		public void ViewBack()
		{
			if (ViewInfoStack.Count > 0 && ViewPlayerStack.Count > 0)
			{
				ViewInfoStack.Pop();
				ViewPlayerStack.Pop();
				FreshView();
			}
		}
		private void OnGUI()
		{
			using (var toolBarHor = new GUILayout.HorizontalScope())
			{
				if (QAnalysisData.IsLoading)
				{
					GUI.enabled = false;
				}
				if (DrawButton("刷新数据"))
				{
					FreshData();
				}
				if (DrawButton("重新获取数据"))
				{
					QAnalysisData.Clear();
					FreshData();
				}
				if (DrawButton("重置数据表"))
				{
					if (EditorUtility.DisplayDialog("重置数据表", "将会清空所有本地信息\n包含列信息设置", "确认","取消"))
					{
						QAnalysisData.Clear(true);
						FreshData();
					}
				}
				if (DrawButton("复制表格数据"))
				{
					GUIUtility.systemCopyBuffer =Tool.BuildString((writer) =>
					{
						writer.Write("玩家Id\t");
						QAnalysisData.ForeachTitle((title) =>
						{
							writer.Write(title.Key.SplitEndString("/"));
							writer.Write("\t");
						});
						writer.Write("\n");
						foreach (var playerData in QAnalysisData.Instance.PlayerDataList)
						{
							writer.Write(playerData.Key + "\t");
							QAnalysisData.ForeachTitle((title) =>
							{
								writer.Write(playerData.AnalysisData[title.Key].value?.ToString().ToElement());
								writer.Write("\t");
							});
							writer.Write("\n");
						}
					});
					EditorUtility.DisplayDialog("复制表格数据", "复制数据成功：\n "+GUIUtility.systemCopyBuffer, "确认");
				}
				var lastRect = GUILayoutUtility.GetLastRect();
				Handles.DrawLine(new Vector3(0, lastRect.yMax), new Vector3(position.width, lastRect.yMax));
				if (QAnalysisData.IsLoading)
				{
					GUI.enabled = true;
					GUILayout.Label( "加载中..", QGUITool.BackStyle);
				}
			}

			{
				using (new GUILayout.VerticalScope())
				{

					using (new GUILayout.HorizontalScope())
					{
						var rect = DrawCell(ViewEvent + " " + ViewPlayer, KeyWidth);
						if (ViewInfoStack.Count > 0)
						{
							var btnRect = rect;
							btnRect.xMax *= 0.2f;
							btnRect.height *= 0.8f;
							if (GUI.Button(btnRect, "返回"))
							{
								ViewBack();
							}
						}

						Handles.DrawLine(new Vector3(0, rect.yMax), new Vector3(position.width, rect.yMax));
						Handles.DrawLine(new Vector3(rect.xMax, rect.yMin), new Vector3(rect.xMax,position.height));

						using ( new GUILayout.ScrollViewScope(new Vector2(viewPos.x, 0),GUIStyle.none,GUIStyle.none,GUILayout.Height(CellHeight)))
						{
							using (new GUILayout.HorizontalScope())
							{
								QAnalysisData.ForeachTitle((title) =>
								{
									var viewKey = title.Key;
									DrawCell(title.ViewKey, title.width, (menu) =>
									{

										foreach (var eventKey in QAnalysisData.Instance.DataKeyList)
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
											if (QTitleWindow.GetNewTitle(out var newTitle))
											{
												QAnalysisData.Instance.AddTitle(newTitle);
												QAnalysisData.Instance.FreshKey(newTitle.Key, true);
											}
										});

										menu.AddItem(new GUIContent("设置数据列"), false, () =>
										{
											if (QTitleWindow.ChangeTitle(title))
											{
												QAnalysisData.Instance.FreshKey(title.Key, true);
											}
										});
										menu.AddItem(new GUIContent("删除数据列"), false, () =>
										{
											if (EditorUtility.DisplayDialog("删除确认", "删除数据列 " + title.Key, "确认", "取消"))
											{
												QAnalysisData.Instance.RemveTitle(title);
											}
										});
									}
									, () =>
									{
										if (QAnalysisData.Instance.EventKeyList.Contains(title.DataSetting.EventKey))
										{
											ViewChange(title.Key, ViewPlayer);
										}
									});
								});
								GUILayout.FlexibleSpace();
							}
							
						}
						GUILayout.FlexibleSpace();
						GUILayout.Space(13);
					}
					

					
				
					using (new GUILayout.HorizontalScope())
					{

						using (new GUILayout.ScrollViewScope(new Vector2(0, viewPos.y), GUIStyle.none, GUIStyle.none, GUILayout.Width(KeyWidth)))
						{
							if (string.IsNullOrWhiteSpace(ViewPlayer))
							{
								using (new GUILayout.VerticalScope())
								{
									for (int i = 0; i < QAnalysisData.Instance.PlayerDataList.Count; i++)
									{

										var data = QAnalysisData.Instance.PlayerDataList[i];
										DrawCell("<size=8>" + data.Key + "</size>", KeyWidth, null, () =>
										{
											if (ViewPlayer != data.Key)
											{
												ViewChange(ViewEvent, data.Key);
											}

										}, i);

									}
								}


							}
							else
							{

								var playerData = QAnalysisData.Instance.PlayerDataList[ViewPlayer];
								using (new GUILayout.VerticalScope())
								{
									for (int i = 0; i < viewEventList.Count; i++)
									{
										var eventData = QAnalysisData.GetEvent(viewEventList[i]);
										DrawCell(eventData.eventTime.ToString(), KeyWidth, null, null, i);
									}
								}
							}

							
							GUILayout.Space(13);
						}
						if (Event.current.type == EventType.Repaint)
						{
							viewRect = GUILayoutUtility.GetLastRect();
						}
						GUILayout.Space(6);
						using (var playerDataScroll = new GUILayout.ScrollViewScope(viewPos))
						{
							if (string.IsNullOrWhiteSpace(ViewPlayer))
							{
								{
									using (new GUILayout.VerticalScope())
									{
									
										for (int i = 0;i < QAnalysisData.Instance.PlayerDataList.Count; i++)
										{
											if (i >= startIndex && i <= endIndex)
											{
												var playerData = QAnalysisData.Instance.PlayerDataList[i];
												using (new GUILayout.HorizontalScope())
												{
													QAnalysisData.ForeachTitle((title) =>
													{
														DrawCell(playerData.AnalysisData[title.Key].value, title.width);
													});
													GUILayout.FlexibleSpace();
												}
											}
											else
											{
												DrawCell(null, 100);
											}
										}

									}
								}
							}
							else
							{
								{
									var playerData = QAnalysisData.Instance.PlayerDataList[ViewPlayer];
									using (new GUILayout.VerticalScope())
									{
										for (int i = 0; i < viewEventList.Count; i++)
										{
											if (i >= startIndex && i <= endIndex)
											{
												var eventData = QAnalysisData.GetEvent(viewEventList[i]);
												using (new GUILayout.HorizontalScope())
												{
													QAnalysisData.ForeachTitle((title) =>
													{
														if (eventData.eventKey == title.DataSetting.EventKey)
														{
															DrawCell(playerData.AnalysisData[title.Key].GetFreshValue(eventData.eventTime), title.width);
														}
														else if (viewEventList.Contains(eventData.Key))
														{
															DrawCell(null, title.width);
														}
													});
													GUILayout.FlexibleSpace();
												}
											}
											else
											{
												DrawCell(null, 100);

											}
											
										}
									}

								}
							}
							viewPos = playerDataScroll.scrollPosition;
						}
					}
				}
			}
			if (Event.current.type == EventType.Repaint)
			{
				startIndex = -1;
				endIndex = -1;
			}

		}

		public Rect DrawCell(object value,float width,Action<GenericMenu> menu=null ,Action cilck=null,int index=-1)
		{
			var showStr = value?.ToString();
			if(value!=null&&value is TimeSpan timeSpan)
			{
				showStr = timeSpan.ToString("hh\\:mm\\:ss");
			}
			GUILayout.Label(showStr, QGUITool.CenterLable, GUILayout.Width(width), GUILayout.Height(CellHeight));
			var lastRect = GUILayoutUtility.GetLastRect();
			var lastColor = Handles.color;
			Handles.color = Color.gray;
			Handles.DrawLine(new Vector3(lastRect.xMin, lastRect.yMax), new Vector3(lastRect.xMax, lastRect.yMax));
			Handles.DrawLine(new Vector3(lastRect.xMax, lastRect.yMin), new Vector3(lastRect.xMax, lastRect.yMax));
			Handles.color = lastColor;
			if (menu == null)
			{
				menu = (m) => m.AddItem(new GUIContent("复制"), false, () =>
				{
					GUIUtility.systemCopyBuffer = value?.ToString();
				});
			}
			lastRect.MouseMenuClick(menu, cilck);
			if (index >= 0)
			{
				if (Event.current.type == EventType.Repaint)
				{
					elementRect[index] = lastRect;
				}
				else
				{
					var offset = new Vector2(0,viewPos.y- viewRect.y);
					if (startIndex < 0)
					{
						if (viewRect.Contains(elementRect[index].center - offset))
						{
							startIndex = Math.Max(index - 2, 0);
							endIndex = startIndex;
						}
					}
					else
					{
						
						if (viewRect.Contains(elementRect[index].center - offset))
						{
							endIndex = Math.Max(endIndex, index + 1);
						}
					}
				}
			}
			return lastRect;
		}
		const float CellHeight=36;
		const float KeyWidth = 260;
		public bool DrawButton(string name)
		{
			return GUILayout.Button(name, GUILayout.Width(100));
		}
	}
	public class QTitleWindow : EditorWindow
	{
		public static QTitleWindow Instance { private set; get; }
		public static bool GetNewTitle(out QTitleInfo newInfo)
		{
			if (Instance == null)
			{
				Instance = GetWindow<QTitleWindow>();
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
				Instance = GetWindow<QTitleWindow>();
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
				var names = QAnalysisData.Instance.DataKeyList;
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
							EditorUtility.DisplayDialog("错误的列名", "不能为空", "确认");
							return;
						}
						if (Instance.create&&QAnalysisData.TitleList.ContainsKey(titleInfo.Key))
						{
							EditorUtility.DisplayDialog("错误的列名", "已存在列名" + titleInfo.Key, "确认");
							return;
						}
						if (string.IsNullOrWhiteSpace(titleInfo.DataSetting.dataKey))
						{
							EditorUtility.DisplayDialog("错误的事件名", "不能为空", "确认");
							return;
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
		static public QList<string, QAnalysisEvent> EventList = new QList<string, QAnalysisEvent>();
		public QAutoList<string, QPlayerData> PlayerDataList = new QAutoList<string, QPlayerData>();
		static public QAutoList<string, QTitleInfo> TitleList = new QAutoList<string, QTitleInfo>();
		public List<string> EventKeyList = new List<string>();
		public List<string> DataKeyList = new List<string>();
		public QMailInfo LastMail=null;
		public static QAnalysisEvent GetEvent(string eventId)
		{
			if (string.IsNullOrEmpty(eventId)) return null;
			return EventList[eventId];
		}
		void FreshChangedList()
		{
			foreach (var player in PlayerDataList)
			{
				player.FreshChangedList();
			}
		}
		static QAnalysisData()
		{
			Load();
		}
		public static void Load()
		{
			FileManager.Load("QTool/" + QAnalysis.StartKey, "{}").ParseQData(Instance);
			FileManager.Load("QTool/" + QAnalysis.StartKey + "_" + nameof(EventList)).ParseQData(EventList);
			FileManager.Load("QTool/" + QAnalysis.StartKey + "_" + nameof(TitleList)).ParseQData(TitleList);
		}
		public static void SaveData()
		{
			FileManager.Save("QTool/" + QAnalysis.StartKey, Instance.ToQData());
			FileManager.Save("QTool/" + QAnalysis.StartKey+	"_"	+ nameof(EventList),EventList.ToQData());
			FileManager.Save("QTool/" + QAnalysis.StartKey + "_" + nameof(TitleList),TitleList.ToQData());
		}
		public static void ForeachTitle(Action<QTitleInfo> action)
		{
			var viewInfo = QAnalysisWindow.Instance.ViewEvent;
			foreach (var title in TitleList)
			{
				if (title.CheckView(viewInfo)||title.DataSetting.TargetKey==viewInfo)
				{
					action(title);
				}
			}
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
		static List<QAnalysisEvent> newList = new List<QAnalysisEvent>();
		public static float AutoFreshTime { get; set; } = 120f;
		public static async Task FreshData() 
		{
			if (IsLoading) return;
			if (!QToolSetting.Instance.QAnalysisMail.InitOver)
			{
				Debug.LogError(nameof(QToolSetting.Instance.QAnalysisMail) + " 未设置");
				return;
			}
			IsLoading = true;

			newList.Clear();
			await QMailTool.FreshEmails(QToolSetting.Instance.QAnalysisMail, (mailInfo) =>
			{ 
				if (mailInfo.Subject.StartsWith(QAnalysis.StartKey))
				{
					newList.AddRange(mailInfo.Body.ParseQData<List<QAnalysisEvent>>());
				}
				Instance.LastMail = mailInfo;
			}, Instance.LastMail);
			newList.Sort(QAnalysisEvent.SortMethod);
			foreach (var eventData in newList)
			{
				AddEvent(eventData);
			}
			Instance.FreshChangedList();
			SaveData();
			IsLoading = false;
		}
		
	
		public void FreshKey(string titleKey,bool freshEventList=false)
		{
			foreach (var playerData in PlayerDataList)
			{
				playerData.FreshKey(titleKey, freshEventList);
			}
			SaveData();
		}
		public static void Clear(bool clearAll=false)
		{
			if (clearAll)
			{
				TitleList.Clear(); 
			}
			EventList.Clear();
			Instance = Activator.CreateInstance<QAnalysisData>();
		}
	
		public static void AddEvent(QAnalysisEvent eventData)
		{
			if (EventList.ContainsKey(eventData.Key)) return;
			if (eventData.eventKey.Contains("_"))
			{
				eventData.eventKey = eventData.eventKey.Replace("_", "/");
			}
			Instance.EventKeyList.AddCheckExist(eventData.eventKey);
			CheckTitle(eventData.eventKey, eventData.eventValue);
			Instance.DataKeyList.AddCheckExist(eventData.eventKey);
	
			if (eventData.eventValue != null)
			{
				Debug.LogError("event " + eventData.eventKey);
				foreach (var memeberInfo in QSerializeType.Get(eventData.eventValue.GetType()).Members)
				{
					var key = eventData.eventKey + "/" + memeberInfo.Key;
					Debug.LogError("添加变量[" + key+"]");
					CheckTitle(key, eventData.eventValue == null ? null : memeberInfo.Get(eventData.eventValue));
					Instance.DataKeyList.AddCheckExist(key);
				}
			}
			EventList.Add(eventData);
			Instance.PlayerDataList[eventData.playerId].Add(eventData);
		}
		static void CheckTitle(string key,object value)
		{
			if (!TitleList.ContainsKey(key))
			{
				var title =TitleList[key];
				title.DataSetting.dataKey = key;
				if (value != null && Type.GetTypeCode(value.GetType()) != TypeCode.Object)
				{
					title.DataSetting.mode = QAnalysisMode.最新数据;
				}
				else
				{
					title.DataSetting.mode = QAnalysisMode.次数;
				}
			}
		}
	}
	public class QTitleInfo:IKey<string>
	{
		public string Key { get; set; }
		string _viewKey = null;
		public string ViewKey => _viewKey ??= Key.SplitEndString("/") + "\n<size=8>" + DataSetting + "</size>";
		public float width = 100;


		public QAnalysisSetting DataSetting = new QAnalysisSetting();
		public void ChangeMode(string modeKey)
		{
			_viewKey = null;
			DataSetting.mode=(QAnalysisMode) Enum.Parse(typeof(QAnalysisMode), modeKey);
			QAnalysisData.Instance.FreshKey(Key);
		}
		public void ChangeEvent(string eventKey)
		{
			_viewKey = null;
			DataSetting.dataKey = eventKey;
			QAnalysisData.Instance.FreshKey(Key,true);
		}
		public bool CheckView(string viewInfo)
		{
			if (viewInfo == "玩家Id")
			{
				if (Key.Contains("/")) return false;
			}
			else if (!Key.StartsWith(viewInfo))
			{
				return false;
			}
			return true;
		}
	}
	public enum QAnalysisMode
	{
		最新数据,
		起始数据,
		次数,
		最小值,
		最大值,
		平均值,
		求和,

		最新时间,
		起始时间,
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
				if (!QAnalysisData.Instance.EventKeyList.Contains(dataKey)&&dataKey.Contains("/"))
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
				if (EventKey.EndsWith("结束"))
				{
					return EventKey.Replace("结束", "开始");
				}
				else if (EventKey.EndsWith("开始"))
				{
					return EventKey.Replace("开始", "结束");
				}
				else
				{
					return EventKey;
				}
			}
		}
		public override string ToString()
		{
			return "("+dataKey + " " + mode+")";
		}
	}
	public class QAnalysisInfo : IKey<string>
	{
		public string Key { get; set; }
		public object value;
		public DateTime UpdateTime;
		public QList<string> EventList = new QList<string>();
		public QDictionary<DateTime, object> TimeData = new QDictionary<DateTime, object>();
		public void AddEvent(QAnalysisEvent eventData)
		{
			UpdateTime = eventData.eventTime;
			EventList.AddCheckExist(eventData.Key);
		}
		QPlayerData GetPlayerData()
		{
			return QAnalysisData.Instance.PlayerDataList[QAnalysisData.GetEvent(EventList.StackPeek()).playerId];
		}
		enum TimeState
		{
			起止时长,
			暂离时长,
			更新结束时间,
			更新起始时间
		}
		static TimeSpan GetTimeSpan(QAnalysisEvent startData, QAnalysisEvent endData,DateTime endTime, out TimeState state, QAnalysisEvent nextData = null)
		{
			if (startData == null)
			{
				state = TimeState.更新起始时间;
				return TimeSpan.Zero;
			}
			if (endData == null|| endData.eventTime <= startData.eventTime)
			{
				var playerData = QAnalysisData.Instance.PlayerDataList[startData.playerId];
				QAnalysisEvent LastPauseEvent = null;
				var pauseCount = 0;
				playerData.AnalysisData[nameof(QAnalysis.QAnalysisEventName.游戏暂离)].ForeachEvent(endTime, (pauseEvent) =>
				{
					pauseCount++;
					if (pauseEvent.eventTime > startData.eventTime)
					{
						if (nextData == null || pauseEvent.eventTime < nextData.eventTime)
						{
							LastPauseEvent = pauseEvent;
						}
						else
						{
							return;
						}
					}
				});
				if (LastPauseEvent != null)
				{
					state = TimeState.暂离时长;
					return LastPauseEvent.eventTime - startData.eventTime;
				}
				else if (pauseCount == 0)
				{
					state = TimeState.暂离时长;
					return endTime - startData.eventTime;
				}
				else
				{
					state = TimeState.更新起始时间;
					return TimeSpan.Zero;
				}
			}
			else
			{
				if (nextData == null || endData.eventTime < nextData.eventTime)
				{
					state = TimeState.起止时长;
					return endData.eventTime - startData.eventTime;
				}
				else
				{
					state = TimeState.更新结束时间;
					return TimeSpan.Zero;
				}
			}
		}
		
		public void ForeachEvent(DateTime dateTime,Action<QAnalysisEvent> action)
		{
			foreach (var eventId in EventList)
			{
				var eventData = QAnalysisData.GetEvent(eventId);
				if (eventData != null&&eventData.eventTime<=dateTime)
				{
					action(eventData);
				}
			}
		}
		public void FreshMode(DateTime endTime)
		{
			value =GetFreshValue(endTime);
		}
		public QAnalysisEvent GetEndEvent(DateTime endTime)
		{
			for (int i = EventList.Count-1; i >=0; i--)
			{
				var eventData = QAnalysisData.GetEvent(EventList[i]);
				if (eventData.eventTime <= endTime)
				{
					return eventData;
				}
			}
			return null;
		}
		public object GetFreshValue(DateTime endTime)
		{
			if (TimeData.ContainsKey(endTime))
			{
				return TimeData[endTime];
			}
			var setting = QAnalysisData.TitleList[Key].DataSetting;
			object freshValue = null;
			if (EventList.Count > 0)
			{
				switch (setting.mode)
				{
					case QAnalysisMode.最新数据:
						ForeachEvent(endTime, (eventData) =>
						{
							freshValue = eventData.GetValue(setting.dataKey);
						});
						break;
					case QAnalysisMode.起始数据:
						freshValue = QAnalysisData.EventList[EventList.QueuePeek()].GetValue(setting.dataKey);
						break;
					case QAnalysisMode.最新时间:
						ForeachEvent(endTime, (eventData) =>
						{
							freshValue = eventData.eventTime;
						});
						break;
					case QAnalysisMode.起始时间:
						freshValue = QAnalysisData.EventList[EventList.QueuePeek()].eventTime;
						break;
					case QAnalysisMode.次数:
						{
							int count = 0;
							ForeachEvent(endTime, (eventData) =>
							{
								count++;
							});
							freshValue = count;
						}
						break;
					case QAnalysisMode.最新时长:
						{
							var targetData = GetPlayerData().AnalysisData[setting.TargetKey];
							if (setting.EventKey.EndsWith("开始"))
							{
								freshValue = GetTimeSpan(GetEndEvent(endTime), targetData.GetEndEvent(endTime),endTime, out var hasend);
							}
							else if (setting.EventKey.EndsWith("结束"))
							{
								freshValue = GetTimeSpan(targetData.GetEndEvent(endTime), GetEndEvent(endTime),endTime, out var hasend);
							}
							else
							{
								EventList.StackPeek();
								freshValue = TimeSpan.Zero;
							}
						}
						break;
					case QAnalysisMode.总时长:
						{
							QAnalysisInfo startInfo = null;
							QAnalysisInfo endInfo = null;
							if (setting.EventKey.EndsWith("开始"))
							{
								startInfo = this;
								endInfo = GetPlayerData().AnalysisData[setting.TargetKey];
							}
							else if (setting.EventKey.EndsWith("结束"))
							{
								startInfo = GetPlayerData().AnalysisData[setting.TargetKey];
								endInfo = this;
							}
							else
							{
								freshValue = TimeSpan.Zero;
							}
							var starIndex = 0;
							var endIndex = 0;
							TimeSpan allTime = default;
							while (starIndex < startInfo.EventList.Count) 
							{
								var startData = QAnalysisData.GetEvent(startInfo.EventList[starIndex]);
								if (startData.eventTime > endTime)
								{
									break;
								}
								allTime += GetTimeSpan(startData, QAnalysisData.GetEvent( endInfo.EventList[endIndex]),endTime, out var state, QAnalysisData.GetEvent(startInfo.EventList[starIndex + 1]));

								switch (state)
								{
									case TimeState.更新结束时间:
										endIndex++;
										break;
									case TimeState.更新起始时间:
										starIndex++;
										break;
									default:
										starIndex++;
										endIndex++;
										break;
								}
							}
							freshValue = allTime;

						}
						break;
					case QAnalysisMode.最大值:
						{
							freshValue = GetEndEvent(endTime).eventValue; 
							ForeachEvent(endTime, (eventData) =>
							{
								if (eventData.eventValue.ToComputeFloat() > freshValue.ToComputeFloat())
								{
									freshValue = eventData.eventValue;
								}
							});
						}
						break;
					case QAnalysisMode.最小值:
						{
							freshValue = GetEndEvent(endTime).eventValue;
							ForeachEvent(endTime, (eventData) =>
							{
								if (eventData.eventValue.ToComputeFloat() < freshValue.ToComputeFloat())
								{
									freshValue = eventData.eventValue;
								}
							});
						}
						break;
					case QAnalysisMode.求和:
						{
							var sum = 0f;
							ForeachEvent(endTime, (eventData) =>
							{
								sum += eventData.eventValue.ToComputeFloat();
							});
							freshValue = sum;
						}
						break;
					case QAnalysisMode.平均值:
						{
							var sum = 0f;
							var count = 0;
							ForeachEvent(endTime, (eventData) =>
							{
								sum += eventData.eventValue.ToComputeFloat();
								count++;
							});
							freshValue = sum / count;
						}
						break;
					default:
						break;
				}
			}
			TimeData[endTime] = freshValue;
			return freshValue;
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
		[QIgnore]
		public List<string> FreshKeyList = new List<string>();
		public void FreshChangedList()
		{
			foreach (var key in FreshKeyList)
			{
				AnalysisData[key].TimeData.Clear();
				AnalysisData[key].FreshMode(UpdateTime);
			}
			FreshKeyList.Clear();
		}
		public void Add(QAnalysisEvent eventData)
		{
			UpdateTime = eventData.eventTime;
			EventList.AddCheckExist(eventData.eventId);
			foreach (var title in QAnalysisData.TitleList)
			{
				if (title.DataSetting.EventKey == eventData.eventKey)
				{
					AnalysisData[title.Key].AddEvent(eventData);
					FreshKeyList.AddCheckExist(title.Key);
				}
				else if(title.DataSetting.TargetKey == eventData.eventKey)
				{
					FreshKeyList.AddCheckExist(title.Key);
				}
				else if (eventData.eventKey == nameof(QAnalysis.QAnalysisEventName.游戏暂离))
				{
					switch (title.DataSetting.mode)
					{
						case QAnalysisMode.总时长:
						case QAnalysisMode.最新时长:
							{
								FreshKeyList.AddCheckExist(title.Key);
							}
							break;
						default:
							break;
					}
				}
			}
		}
		public void FreshKey(string titleKey,bool freshEventList)
		{
			var info = AnalysisData[titleKey];
			info.TimeData.Clear();
			if (freshEventList)
			{
				info.EventList.Clear();
				var setting = QAnalysisData.TitleList[titleKey].DataSetting;
				foreach (var eventId in EventList)
				{
					var eventData = QAnalysisData.EventList[eventId];
					if (eventData.eventKey == setting.EventKey)
					{
						info.EventList.AddCheckExist(eventData.eventId);
					}
				}
			}
			info.FreshMode(UpdateTime);
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
