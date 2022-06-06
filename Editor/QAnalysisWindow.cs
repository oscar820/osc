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
		public string ViewInfo = "玩家Id";
		Vector2 viewPos;
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
				Handles.DrawLine(new Vector3(0, lastRect.yMax), new Vector3(position.xMax, lastRect.yMax));
				if (QAnalysisData.IsLoading)
				{
					GUI.enabled = true;
					GUILayout.Label( "加载中..", QGUITool.BackStyle);
				}
			}
		
			using (var playerDataScroll = new GUILayout.ScrollViewScope(viewPos))
			{
				using (new GUILayout.VerticalScope())
				{

					using (new GUILayout.HorizontalScope())
					{
						var rect= DrawCell(ViewInfo, 250, true, true);
						if (ViewInfo != "玩家Id")
						{
							rect.xMax *= 0.2f;
							rect.height *= 0.8f;
							if (GUI.Button(rect,"返回"))
							{
								ViewInfo = "玩家Id";
							}
						}
						QAnalysisData.ForeachTitle((title) =>
						{
							var viewKey = title.Key;
							DrawCell(title.ViewKey, title.width, false, true, (menu) => {

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

								menu.AddItem(new GUIContent("设置数据列"), false, () => {
									if (QTitleWindow.ChangeTitle(title))
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
							}
							, () => {
								foreach (var newTitle in QAnalysisData.Instance.TitleList)
								{
									if (newTitle.Key.StartsWith(title.Key + "/"))
									{
										ViewInfo = title.Key;
										break;
									}
								}
								
							});
						});
						GUILayout.FlexibleSpace();
					}
					using (new GUILayout.HorizontalScope())
					{
						using (new GUILayout.VerticalScope())
						{
							foreach (var data in QAnalysisData.Instance.PlayerDataList)
							{
								DrawCell("<size=8>"+data.Key+"</size>", 250, true,false);
							}
						}
						using (new GUILayout.VerticalScope())
						{
							foreach (var playerData in QAnalysisData.Instance.PlayerDataList)
							{
								using (new GUILayout.HorizontalScope())
								{
									QAnalysisData.ForeachTitle((title) =>
									{
										DrawCell(playerData.AnalysisData[title.Key].value, title.width);
									});
									GUILayout.FlexibleSpace();
								}
							}
						}
						viewPos = playerDataScroll.scrollPosition;
					}
				}
			}
		
			
		}

	
		public Rect DrawCell(string value,float width,bool drawXLine,bool drawYLine,Action<GenericMenu> menu=null,Action cilck=null)
		{
			DrawCell(value,width);
			var lastRect = GUILayoutUtility.GetLastRect();

			var lastColor = Handles.color;
			if (!drawXLine || !drawYLine)
			{
				Handles.color = Color.grey;
			}
			if (drawXLine)
			{
				Handles.DrawLine(new Vector3(viewPos.x, lastRect.yMax), new Vector3(viewPos.x+position.xMax, lastRect.yMax));
			}
			if (drawYLine)
			{
				Handles.DrawLine(new Vector3(lastRect.xMax, viewPos.y + lastRect.yMin), new Vector3(lastRect.xMax, viewPos.y + position.yMax)); 
			}
			if (!drawXLine || !drawYLine)
			{
				Handles.color = lastColor;
			}
			if (menu != null)
			{
				lastRect.MouseMenuClick(menu, cilck);
			}
			return lastRect;
		}
		public void DrawCell(object value,float width)
		{
			GUILayout.Label(value?.ToString(), QGUITool.CenterLable, GUILayout.Width(width), GUILayout.Height(36));
		}
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
						if (Instance.create&&QAnalysisData.Instance.TitleList.ContainsKey(titleInfo.Key))
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
		public QList<string, QAnalysisEvent> EventList = new QList<string, QAnalysisEvent>();
		public QAutoList<string, QPlayerData> PlayerDataList = new QAutoList<string, QPlayerData>();
		public QAutoList<string, QTitleInfo> TitleList = new QAutoList<string, QTitleInfo>();
		public List<string> EventKeyList = new List<string>();
		public List<string> DataKeyList = new List<string>();
		public QMailInfo LastMail=null;
		public static QAnalysisEvent GetEvent(string eventId)
		{
			if (string.IsNullOrEmpty(eventId)) return null;
			return Instance.EventList[eventId];
		}
		static QAnalysisData()
		{
			FileManager.Load("QTool/" + QAnalysis.StartKey, "{}").ParseQData(Instance);
			_=AutoFreshData();
		}
		public static void ForeachTitle(Action<QTitleInfo> action, string viewInfo=null)
		{
			if (string.IsNullOrEmpty(viewInfo))
			{
				viewInfo = QAnalysisWindow.Instance.ViewInfo;
			}
			foreach (var title in Instance.TitleList)
			{
				if (title.CheckView(viewInfo))
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
		static async Task AutoFreshData()
		{
			while (Application.isEditor&& AutoFreshTime>0)
			{
				await FreshData();
				await Task.Delay((int)(AutoFreshTime * 1000));
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
					newList.Clear();
					AddEvent(mailInfo.Body.ParseQData(newList));
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
		public static void Clear(bool clearTitleSetting=false)
		{
			if (clearTitleSetting)
			{
				Instance = Activator.CreateInstance<QAnalysisData>();
			}
			else
			{
				var titleInfo = Instance.TitleList;
				var eventKeyList = Instance.EventKeyList;
				Instance = Activator.CreateInstance<QAnalysisData>();
				Instance.EventKeyList = eventKeyList;
				Instance.TitleList = titleInfo;
			}
		
		}
	
		public static void AddEvent(List<QAnalysisEvent> newEventList)
		{
			foreach (var eventData in newEventList)
			{
				if (eventData.eventKey.Contains("_"))
				{
					eventData.eventKey = eventData.eventKey.Replace("_", "/");
				}
				Instance.EventKeyList.AddCheckExist(eventData.eventKey);
				CheckTitle(eventData.eventKey, eventData.eventValue);
				Instance.DataKeyList.AddCheckExist(eventData.eventKey);
				if (eventData.eventValue != null)
				{
					foreach (var memeberInfo in QSerializeType.Get(eventData.eventValue.GetType()).Members)
					{
						var key = eventData.eventKey + "/" + memeberInfo.Key;
						CheckTitle(key, eventData.eventValue==null?null:memeberInfo.Get(eventData.eventValue));
						Instance.DataKeyList.AddCheckExist(key);
					}
				}
				Instance.EventList.Add(eventData);
				Instance.PlayerDataList[eventData.playerId].Add(eventData);
			}
		}
		static void CheckTitle(string key,object value)
		{
			if (!Instance.TitleList.ContainsKey(key))
			{
				var title = Instance.TitleList[key];
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
			else
			{
				if (!Key.StartsWith(viewInfo)) return false;
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
				switch (mode)
				{
					case QAnalysisMode.最新时长:
					case QAnalysisMode.总时长:
						if (EventKey.EndsWith("结束"))
						{
							return EventKey.Replace("结束", "开始");
						}
						else if(EventKey.EndsWith("开始"))
						{
							return EventKey.Replace("开始", "结束");
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
	public class QAnalysisInfo : IKey<string>
	{
		public string Key { get; set; }
		public object value;
		public DateTime UpdateTime;
		public QList<string> EventList = new QList<string>();
		public void AddEvent(QAnalysisEvent eventData)
		{
			UpdateTime = eventData.eventTime;
			EventList.AddCheckExist(eventData.Key);
			FreshMode();
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
		static TimeSpan GetTimeSpan(string startId, string endId,out TimeState state, string nextId = null)
		{
			QAnalysisEvent startData = QAnalysisData.GetEvent(startId);
			QAnalysisEvent endData = QAnalysisData.GetEvent(endId);
			QAnalysisEvent nextData = QAnalysisData.GetEvent(nextId);
			if (endData == null)
			{
				var playerData = QAnalysisData.Instance.PlayerDataList[startData.playerId];
				QAnalysisEvent LastPauseEvent = null;
				foreach (var pauseEventId in playerData.AnalysisData[nameof(QAnalysis.QAnalysisEventName.游戏暂离)].EventList)
				{
					var pauseEvent = QAnalysisData.GetEvent(pauseEventId);
					if (pauseEvent.eventTime > startData.eventTime)
					{
						if (nextData == null || pauseEvent.eventTime < nextData.eventTime)
						{
							LastPauseEvent = pauseEvent;
						}
						else
						{
							break;
						}
					}
				}
				if (LastPauseEvent != null)
				{
					state = TimeState.暂离时长;
					return LastPauseEvent.eventTime - startData.eventTime;
				}
				else
				{
					state = TimeState.更新起始时间;
					return TimeSpan.Zero;
				}
			}
			else
			{
				if (endData.eventTime > startData.eventTime)
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
				else
				{
					state = TimeState.更新起始时间;
					return TimeSpan.Zero;
				}
			}
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
						var targetData = GetPlayerData().AnalysisData[setting.TargetKey];
						if (setting.EventKey.EndsWith("开始"))
						{
							value= GetTimeSpan(EventList.StackPeek(), targetData.EventList.StackPeek(),out var hasend);
						}
						else if(setting.EventKey.EndsWith("结束"))
						{
							value= GetTimeSpan(targetData.EventList.StackPeek(),EventList.StackPeek(), out var hasend);
						}
						else
						{
							value = TimeSpan.Zero;
						}
					}
					break;
				case QAnalysisMode.总时长:
					{
						QAnalysisInfo startInfo=null;
						QAnalysisInfo endInfo = null;
						if (setting.EventKey.EndsWith("开始"))
						{
							startInfo = this;
							endInfo=GetPlayerData().AnalysisData[setting.TargetKey];
						}
						else if (setting.EventKey.EndsWith("结束"))
						{
							startInfo = GetPlayerData().AnalysisData[setting.TargetKey];
							endInfo = this ;
						}
						else
						{
							value = TimeSpan.Zero;
						}
						var starIndex = 0;
						var endIndex = 0;
						TimeSpan allTime = default;
						while (starIndex<startInfo.EventList.Count)
						{
							allTime+= GetTimeSpan(startInfo.EventList[starIndex], endInfo.EventList[endIndex], out var state, startInfo.EventList[starIndex + 1]);
						
							switch (state)
							{
								case TimeState.更新结束时间:
									endIndex++;
									//if (endIndex > endInfo.EventList.Count)
									//{
									//	break;
									//}
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
						value = allTime;

					}
					break;
				case QAnalysisMode.最大值:
					{
						value = QAnalysisData.GetEvent(EventList.StackPeek()).eventValue;
						foreach (var eventId in EventList)
						{
							var eventData = QAnalysisData.GetEvent(eventId);
							if (eventData.eventValue.ToComputeFloat() > value.ToComputeFloat())
							{
								value = eventData.eventValue;
							}
						}
					}break;
				case QAnalysisMode.最小值:
					{
						value = QAnalysisData.GetEvent(EventList.StackPeek()).eventValue;
						foreach (var eventId in EventList)
						{
							var eventData = QAnalysisData.GetEvent(eventId);
							if (eventData.eventValue.ToComputeFloat() < value.ToComputeFloat())
							{
								value = eventData.eventValue;
							}
						}
					}
					break;
				case QAnalysisMode.求和:
					{
						var sum = 0f;
						foreach (var eventId in EventList)
						{
							var eventData = QAnalysisData.GetEvent(eventId);
							sum+= eventData.eventValue.ToComputeFloat();
						}
						value = sum;
					}
					break;
				case QAnalysisMode.平均值:
					{
						var sum = 0f;
						foreach (var eventId in EventList)
						{
							var eventData = QAnalysisData.GetEvent(eventId);
							sum += eventData.eventValue.ToComputeFloat();
						}
						value = sum/EventList.Count;
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
				else if (eventData.eventKey == nameof(QAnalysis.QAnalysisEventName.游戏暂离))
				{
					switch (title.DataSetting.mode)
					{
						case QAnalysisMode.总时长:
						case QAnalysisMode.最新时长:
							{
								AnalysisData[title.Key].FreshMode();
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
