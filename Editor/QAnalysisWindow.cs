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
		QGridView GridView;
		
		private void OnEnable()
		{
			Instance = this;
			if (GridView == null)
			{

				GridView = new QGridView(GetValue, GetSize, ClickCell);
			}
		}
		public bool ClickCell(int x,int y,int button)
		{
			var change = false;
			if (button == 0)
			{
				if (x == 0&&y>0)
				{
					if (string.IsNullOrWhiteSpace(ViewPlayer))
					{
						ViewChange(ViewEvent, QAnalysisData.Instance.PlayerDataList[y-1].Key );
					}
				}
				else if(x>0&&y==0)
				{
					ViewChange(Titles[x].Key, ViewPlayer);
				}
				
			}
			else if(button==1)
			{
				var menu = new GenericMenu();
				if (x > 0 && y == 0)
				{
					var title = Titles[x];

				
					foreach (var eventKey in QAnalysisData.Instance.DataKeyList)
					{
						menu.AddItem(new GUIContent("数据来源/" + eventKey), eventKey == title.DataSetting.DataKey, () =>
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
							QAnalysisData.Instance.FreshKey(newTitle.Key);
						}
					});

					menu.AddItem(new GUIContent("设置数据列"), false, () =>
					{
						if (QTitleWindow.ChangeTitle(title))
						{
							QAnalysisData.Instance.FreshKey(title.Key);
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
				else
				{
					menu.AddItem(new GUIContent("复制"), false, () =>
					{
						GUIUtility.systemCopyBuffer = GetValue(x, y);
					});
				}
				
				menu.ShowAsContext();
			}
			return change;
		}
		public string CheckToString(object obj)
		{
			if (obj == null) return "";
			if(obj is TimeSpan timeSpan)
			{
				return timeSpan.ToString("hh\\:mm\\:ss");
			}
			else
			{
				return obj.ToString();
			}
		}
		public string GetValue(int x,int y)
		{
			if (x == 0&&y==0)
			{
				return ViewEvent + " " + ViewPlayer;
			}
			if (y == 0)
			{
				return Titles[x].ViewKey;
			}
			if (string.IsNullOrWhiteSpace(ViewPlayer))
			{
				var playerData = QAnalysisData.Instance.PlayerDataList[y - 1];
				if (x == 0)
				{
					return playerData.Key;
				}
				else
				{
					return CheckToString( playerData.AnalysisData[Titles[x].Key].GetValue());
				}
			}
			else
			{
				var playerData = QAnalysisData.Instance.PlayerDataList[ViewPlayer];
				var eventData=QAnalysisData.GetEvent(playerData.EventList[y - 1]);
				if (x == 0)
				{
					return eventData.eventTime.ToString();
				}
				else
				{
					if (eventData.eventKey == Titles[x].DataSetting.EventKey)
					{
						return CheckToString(playerData.AnalysisData[Titles[x].Key].GetValue(eventData.eventId));
					}
					else
					{
						return "";
					}
				}
			}
		}
		public QList<QTitleInfo> Titles = new QList<QTitleInfo>();
		public Vector2Int GetSize()
		{
			Titles.Clear();
			var size = new Vector2Int();
			QAnalysisData.ForeachTitle((title) =>
			{
				size.x++;
				Titles[size.x] = title;
			});
			size.x++;
			if (string.IsNullOrWhiteSpace(ViewPlayer))
			{
				size.y = QAnalysisData.Instance.PlayerDataList.Count+1;
			}
			else
			{
				size.y= QAnalysisData.Instance.PlayerDataList[ViewPlayer].EventList.Count+1;
			}
			return size;
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
		
			using (new GUILayout.HorizontalScope())
			{
				if (QAnalysisData.IsLoading)
				{
					GUI.enabled = false;
				}
				if (ViewInfoStack.Count > 0)
				{
					if (DrawButton( "返回"))
					{
						ViewBack();
					}
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
								writer.Write(playerData.AnalysisData[title.Key].GetValue()?.ToString().ToElement());
								writer.Write("\t");
							});
							writer.Write("\n");
						}
					});
					EditorUtility.DisplayDialog("复制表格数据", "复制数据成功", "确认");
				}
				var lastRect = GUILayoutUtility.GetLastRect();
				Handles.DrawLine(new Vector3(0, lastRect.yMax), new Vector3(position.width, lastRect.yMax));
				if (QAnalysisData.IsLoading)
				{
					GUI.enabled = true;
					GUILayout.Label( "加载中..", QGUITool.BackStyle);
				}
			}
			GridView.DoLayout(Repaint);
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
					if (names.IndexOf(titleInfo.DataSetting.DataKey) < 0)
					{
						titleInfo.DataSetting.DataKey = names[0];
					}
					titleInfo.DataSetting.DataKey = names[EditorGUILayout.Popup("数据来源", names.IndexOf(titleInfo.DataSetting.DataKey), names.ToArray())];
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
						if (string.IsNullOrWhiteSpace(titleInfo.DataSetting.DataKey))
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
		public QMailInfo LastMail = null;
		public static QAnalysisEvent GetEvent(string eventId)
		{
			if (string.IsNullOrEmpty(eventId)) return null;
			return EventList[eventId];
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
			FileManager.Save("QTool/" + QAnalysis.StartKey + "_" + nameof(EventList), EventList.ToQData());
			FileManager.Save("QTool/" + QAnalysis.StartKey + "_" + nameof(TitleList), TitleList.ToQData());
		}
		public static void ForeachTitle(Action<QTitleInfo> action)
		{
			var viewInfo = QAnalysisWindow.Instance.ViewEvent;
			foreach (var title in TitleList)
			{
				if (title.CheckView(viewInfo) || title.DataSetting.TargetKey == viewInfo)
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
		static void AddNewEventList()
		{
			var start = DateTime.Now;
			Debug.Log("对新事件排序" + NewEventList.Count);
			NewEventList.Sort(QAnalysisEvent.SortMethod);
			Debug.Log("排序完成" + (DateTime.Now - start).ToString("hh\\:mm\\:ss") + "开始添加事件" + NewEventList.Count);
			start = DateTime.Now;
			for (var i = 0; i < NewEventList.Count; i++)
			{
				var eventData = NewEventList[i];
				AddEvent(eventData);
				EditorUtility.DisplayProgressBar("添加事件", i + "/" + NewEventList.Count + " " + eventData.eventKey, i * 1f / NewEventList.Count);
			}
			EditorUtility.ClearProgressBar();
			Debug.Log("添加事件" + NewEventList.Count + "完成 " + (DateTime.Now - start).ToString("hh\\:mm\\:ss") + " 总数" + QAnalysisData.EventList.Count);
			NewEventList.Clear();
		}
		public static List<QAnalysisEvent> NewEventList { get; private set; } = new List<QAnalysisEvent>();
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

			try
			{
				NewEventList.Clear();
				await QMailTool.FreshEmails(QToolSetting.Instance.QAnalysisMail, (mailInfo) =>
				{
					if (mailInfo.Subject.StartsWith(QAnalysis.StartKey))
					{
						if (!string.IsNullOrWhiteSpace(mailInfo.Body))
						{
							var list = mailInfo.Body.ParseQData<List<QAnalysisEvent>>();
							foreach (var item in list)
							{
								NewEventList.Add(item);
							}
						}
					}
					Instance.LastMail = mailInfo;
				}, Instance.LastMail);
				AddNewEventList();
				SaveData();
				Debug.Log("保存完成");
			}
			catch (Exception e)
			{

				throw e;
			}
			finally
			{
				IsLoading = false;
			}
		
		}
		
	
		public void FreshKey(string titleKey)
		{
			for (int i = 0; i < PlayerDataList.Count; i++)
			{
				PlayerDataList[i].FreshKey(titleKey);
				EditorUtility.DisplayProgressBar("刷新数据列[" + titleKey + "]", "玩家" + i + "/" + PlayerDataList.Count + ":" + PlayerDataList[i].Key, i * 1f / PlayerDataList.Count);
			}
			EditorUtility.ClearProgressBar();
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
			Instance.DataKeyList.AddCheckExist(eventData.eventKey);
			CheckTitle(eventData.eventKey, eventData.eventValue);
	
			if (eventData.eventValue != null)
			{
				foreach (var memeberInfo in QSerializeType.Get(eventData.eventValue.GetType()).Members)
				{
					var key = eventData.eventKey + "/" + memeberInfo.Key;
					Instance.DataKeyList.AddCheckExist(key);
					CheckTitle(key, eventData.eventValue == null ? null : memeberInfo.Get(eventData.eventValue));
	
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
				title.DataSetting.DataKey = key;
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
			DataSetting.DataKey = eventKey;
			QAnalysisData.Instance.FreshKey(Key);
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
		private string _dataKey;
		public string DataKey
		{
			get
			{
				return _dataKey;
			}
			set
			{
				SetDataKey(value);
			}
		}
		public QAnalysisMode mode = QAnalysisMode.最新数据;
		public void SetDataKey(string key)
		{
			_dataKey = key;
			if (QAnalysisData.Instance.EventKeyList.Contains(_dataKey))
			{
				EventKey= _dataKey;
			}
			else
			{
				var eventKey = "";
				foreach (var eventKeyValue in QAnalysisData.Instance.EventKeyList)
				{
					if (_dataKey.StartsWith(eventKeyValue))
					{
						if (eventKey.Length < eventKeyValue.Length)
						{
							eventKey = eventKeyValue;
						}
					}
				}
				EventKey= eventKey;
			}
			if (EventKey.EndsWith("结束"))
			{
				TargetKey= EventKey.Replace("结束", "开始");
			}
			else if (EventKey.EndsWith("开始"))
			{
				TargetKey= EventKey.Replace("开始", "结束");
			}
			else
			{
				TargetKey= EventKey;
			}
		}
		public string EventKey;
		public string TargetKey;
		public override string ToString()
		{
			return "("+DataKey + " " + mode+")";
		}
	}
	public class QAnalysisInfo : IKey<string>
	{
		public string Key { get; set; }
		public DateTime UpdateTime;
		public QList<string> EventList = new QList<string>();
		public QDictionary<string, object> BufferData = new QDictionary<string, object>();
		public void AddEvent(QAnalysisEvent eventData)
		{

			var setting = QAnalysisData.TitleList[Key].DataSetting;
			switch (setting.mode)
			{
				case QAnalysisMode.最新数据:
					BufferData[eventData.eventId] = eventData.GetValue(setting.DataKey);
					break;
				case QAnalysisMode.起始数据:
					if (BufferData.Count == 0)
					{
						BufferData[eventData.eventId] = eventData.GetValue(setting.DataKey);
					}
					else
					{
						BufferData[eventData.eventId] = BufferData[0].Value;
					}
					break;
				case QAnalysisMode.次数:
					if (BufferData.Count == 0)
					{
						BufferData[eventData.eventId] = 1;
					}
					else
					{
						BufferData[eventData.eventId] = (int)BufferData[BufferData.Count-1].Value +1;
					}
					break;
				case QAnalysisMode.最小值:
					if (BufferData.Count == 0)
					{
						BufferData[eventData.eventId] = eventData.GetValue(setting.DataKey).ToComputeFloat();
					}
					else if ((float)BufferData[BufferData.Count - 1].Value>eventData.GetValue(setting.DataKey).ToComputeFloat())
					{
						BufferData[eventData.eventId] = eventData.GetValue(setting.DataKey).ToComputeFloat();
					}
					else
					{
						BufferData[eventData.eventId] = (float)BufferData[BufferData.Count - 1].Value;
					}
					break;
				case QAnalysisMode.最大值:
					if (BufferData.Count == 0)
					{
						BufferData[eventData.eventId] = eventData.GetValue(setting.DataKey).ToComputeFloat();
					}
					else if ((float)BufferData[BufferData.Count - 1].Value < eventData.GetValue(setting.DataKey).ToComputeFloat())
					{
						BufferData[eventData.eventId] = eventData.GetValue(setting.DataKey).ToComputeFloat();
					}
					else
					{
						BufferData[eventData.eventId] = (float)BufferData[BufferData.Count - 1].Value;
					}
					break;
				case QAnalysisMode.平均值:
					if (BufferData.Count == 0)
					{
						BufferData[eventData.eventId] = eventData.GetValue(setting.DataKey).ToComputeFloat();
					} 
					else
					{   
						BufferData[eventData.eventId]= ((float)BufferData[BufferData.Count - 1].Value + eventData.GetValue(setting.DataKey).ToComputeFloat())/ 2;
					}
					break;
				case QAnalysisMode.求和:
					if (BufferData.Count == 0)
					{
						BufferData[eventData.eventId] = eventData.GetValue(setting.DataKey).ToComputeFloat();
					}
					else
					{
						BufferData[eventData.eventId] = (float)BufferData[BufferData.Count - 1].Value + eventData.GetValue(setting.DataKey).ToComputeFloat();
					}
					break;
				case QAnalysisMode.最新时间:
					BufferData[eventData.eventId] = eventData.eventTime;
					break;
				case QAnalysisMode.起始时间:
					if (BufferData.Count == 0)
					{
						BufferData[eventData.eventId] = eventData.eventTime;
					}
					else
					{
						BufferData[eventData.eventId] = BufferData[0].Value;
					}
					break;
				case QAnalysisMode.最新时长:
					{
						BufferData[eventData.eventId] = GetLastTimeSpan(eventData);
					}
					break;
				case QAnalysisMode.总时长:
					if (BufferData.Count == 0)
					{
						BufferData[eventData.eventId] = TimeSpan.Zero;
					}
					else
					{
						BufferData[eventData.eventId] = (TimeSpan)BufferData[BufferData.Count-1].Value + GetLastTimeSpan(eventData);
					}
					break;
				default:
					break;
			}

			UpdateTime = eventData.eventTime;
			EventList.AddCheckExist(eventData.Key);
		}
		TimeSpan GetLastTimeSpan(QAnalysisEvent eventData)
		{
			var setting = QAnalysisData.TitleList[Key].DataSetting;
			if (EventList.Count > 0)
			{
				var targetData = GetPlayerData().AnalysisData[setting.TargetKey];
				if (setting.EventKey.EndsWith("开始"))
				{
					return  GetTimeSpan(QAnalysisData.GetEvent(EventList.StackPeek()), targetData.GetEndEvent(eventData.eventTime), out var hasend, eventData);
				}
				else if (setting.EventKey.EndsWith("结束"))
				{
					return GetTimeSpan(targetData.GetEndEvent(eventData.eventTime), eventData, out var hasend);
				}
			}
			return TimeSpan.Zero;
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
		TimeSpan GetTimeSpan(QAnalysisEvent startData, QAnalysisEvent endData, out TimeState state, QAnalysisEvent nextData = null)
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
				var pauseData = playerData.AnalysisData[nameof(QAnalysis.QAnalysisEventName.游戏暂离)];
				pauseData.ForeachEvent( (pauseEvent) =>
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
					return GetPlayerData().UpdateTime - startData.eventTime;
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
		
		public void ForeachEvent(Action<QAnalysisEvent> action, QAnalysisEvent endEvent=null)
		{
			if (endEvent == null)
			{
				endEvent = QAnalysisData.GetEvent(EventList.StackPeek());
			}
			foreach (var eventId in EventList)
			{
				var eventData = QAnalysisData.GetEvent(eventId);
				if (eventData != null)
				{
					action(eventData);
					if (eventData.eventId == endEvent.eventId)
					{
						break;
					}
				}
			}
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

	
		public object GetValue(string eventId="")
		{
			
			if (string.IsNullOrWhiteSpace(eventId))
			{
				eventId = EventList.StackPeek();
				if (string.IsNullOrWhiteSpace(eventId))
				{
					return null;
				}
			}
		
			if (BufferData.ContainsKey(eventId))
			{
				return BufferData[eventId];
			}
			else
			{
				Debug.LogError("缺少数据" + Key + "[" + EventList.IndexOf(eventId) + "]");
				return null;
			}
			//var setting = QAnalysisData.TitleList[Key].DataSetting;
			//object freshValue = null;
			//if (EventList.Count > 0)
			//{
			//	switch (setting.mode)
			//	{
			//		//case QAnalysisMode.最新数据:
			//		//	freshValue = endEvent.GetValue(setting.dataKey);
			//		//	break;
			//		//case QAnalysisMode.起始数据:
			//		//	freshValue = QAnalysisData.EventList[EventList.QueuePeek()].GetValue(setting.dataKey);
			//		//	break;
			//		//case QAnalysisMode.最新时间:
			//		//	freshValue = endEvent.eventTime;
			//		//	break;
			//		//case QAnalysisMode.起始时间:
			//		//	freshValue = QAnalysisData.EventList[EventList.QueuePeek()].eventTime;
			//		//	break;
			//		//case QAnalysisMode.次数:
			//		//	{
			//		//		int count = 0;
			//		//		ForeachEvent((eventData) =>
			//		//		{
			//		//			count++;
			//		//		}, endEvent);
			//		//		freshValue = count;
			//		//	}
			//		//	break;
			//		//case QAnalysisMode.最新时长:
			//		//	{
			//		//		var targetData = GetPlayerData().AnalysisData[setting.TargetKey];
			//		//		if (setting.EventKey.EndsWith("开始"))
			//		//		{
			//		//			freshValue = GetTimeSpan(endEvent, targetData.GetEndEvent(GetPlayerData().UpdateTime), out var hasend,QAnalysisData.GetEvent(EventList[EventList.IndexOf(endEvent.eventId)+1]));
			//		//		}
			//		//		else if (setting.EventKey.EndsWith("结束"))
			//		//		{
			//		//			freshValue = GetTimeSpan(targetData.GetEndEvent(endEvent.eventTime), endEvent, out var hasend);
			//		//		}
			//		//		else
			//		//		{
			//		//			EventList.StackPeek();
			//		//			freshValue = TimeSpan.Zero;
			//		//		}
			//		//	}
			//		//	break;
			//		//case QAnalysisMode.总时长:
			//		//	{
			//		//		QAnalysisInfo startInfo = null;
			//		//		QAnalysisInfo endInfo = null;
			//		//		if (setting.EventKey.EndsWith("开始"))
			//		//		{
			//		//			startInfo = this;
			//		//			endInfo = GetPlayerData().AnalysisData[setting.TargetKey];
			//		//		}
			//		//		else if (setting.EventKey.EndsWith("结束"))
			//		//		{
			//		//			startInfo = GetPlayerData().AnalysisData[setting.TargetKey];
			//		//			endInfo = this;
			//		//		}
			//		//		else
			//		//		{
			//		//			freshValue = TimeSpan.Zero;
			//		//		}
			//		//		var starIndex = 0;
			//		//		var endIndex = 0;
			//		//		TimeSpan allTime = default;
			//		//		while (starIndex < startInfo.EventList.Count) 
			//		//		{
			//		//			var startData = QAnalysisData.GetEvent(startInfo.EventList[starIndex]);
							
			//		//			allTime += GetTimeSpan(startData, QAnalysisData.GetEvent( endInfo.EventList[endIndex]),  out var state, QAnalysisData.GetEvent(startInfo.EventList[starIndex + 1]));

			//		//			switch (state)
			//		//			{
			//		//				case TimeState.更新结束时间:
			//		//					endIndex++;
			//		//					break;
			//		//				case TimeState.更新起始时间:
			//		//					starIndex++;
			//		//					break;
			//		//				default:
			//		//					starIndex++;
			//		//					endIndex++;
										
			//		//					break;
			//		//			}
			//		//			if (startData.eventId == endEvent.eventId&&(state== TimeState.暂离时长||state== TimeState.起止时长))
			//		//			{
			//		//				break;
			//		//			}
			//		//		}
			//		//		freshValue = allTime;

			//		//	}
			//		//	break;
			//		//case QAnalysisMode.最大值:
			//		//	{
			//		//		freshValue = endEvent.eventValue; 
			//		//		ForeachEvent((eventData) =>
			//		//		{
			//		//			if (eventData.eventValue.ToComputeFloat() > freshValue.ToComputeFloat())
			//		//			{
			//		//				freshValue = eventData.eventValue;
			//		//			}
			//		//		},endEvent);
			//		//	}
			//		//	break;
			//		//case QAnalysisMode.最小值:
			//		//	{
			//		//		freshValue = endEvent.eventValue;
			//		//		ForeachEvent((eventData) =>
			//		//		{
			//		//			if (eventData.eventValue.ToComputeFloat() < freshValue.ToComputeFloat())
			//		//			{
			//		//				freshValue = eventData.eventValue;
			//		//			}
			//		//		}, endEvent);
			//		//	}
			//		//	break;
			//		//case QAnalysisMode.求和:
			//		//	{
			//		//		var sum = 0f;
			//		//		ForeachEvent( (eventData) =>
			//		//		{
			//		//			sum += eventData.eventValue.ToComputeFloat();
			//		//		}, endEvent);
			//		//		freshValue = sum;
			//		//	}
			//		//	break;
			//		//case QAnalysisMode.平均值:
			//		//	{
			//		//		var sum = 0f;
			//		//		var count = 0;
			//		//		ForeachEvent((eventData) =>
			//		//		{
			//		//			sum += eventData.eventValue.ToComputeFloat();
			//		//			count++;
			//		//		}, endEvent);
			//		//		freshValue = sum / count;
			//		//	}
			//		//	break;
			//		default:
			//			Debug.LogError("缺少数据" + setting.mode+"["+EventList.IndexOf(endEvent.eventId) +"]");
			//			break;
			//	}
			//}
			//BufferData[endEvent.eventId] = freshValue;
			//return null;
		}

		public override string ToString()
		{
			return GetValue()?.ToString();
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
			foreach (var title in QAnalysisData.TitleList)
			{
				if (title.DataSetting.EventKey == eventData.eventKey)
				{
					AnalysisData[title.Key].AddEvent(eventData);
					//AnalysisData[title.Key].changed = true;
				}
				//else if(title.DataSetting.TargetKey == eventData.eventKey)
				//{
				////	AnalysisData[title.Key].changed = true;
				//}
				//else if (eventData.eventKey == nameof(QAnalysis.QAnalysisEventName.游戏暂离))
				//{
				//	switch (title.DataSetting.mode)
				//	{
				//		case QAnalysisMode.总时长:
				//		case QAnalysisMode.最新时长:
				//			{
				//			//	AnalysisData[title.Key].changed = true;
				//			}
				//			break;
				//		default:
				//			break;
				//	}
				//}
			}
		}
		public void FreshKey(string titleKey)
		{
			var info = AnalysisData[titleKey];
			info.EventList.Clear();
			info.BufferData.Clear();
			var setting = QAnalysisData.TitleList[titleKey].DataSetting;
			foreach (var eventId in EventList)
			{
				var eventData = QAnalysisData.EventList[eventId];
				if (eventData.eventKey == setting.EventKey)
				{
					info.AddEvent(eventData);
				}
			}
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
