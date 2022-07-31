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
		private void OnGUI()
		{

			using (new GUILayout.HorizontalScope())
			{
			
				if (ViewInfoStack.Count > 0)
				{
					if (DrawButton("返回"))
					{
						ViewBack();
					}
				}
				if (QAnalysisData.IsLoading)
				{
					GUI.enabled = false;
				}
				var editorTest = PlayerPrefs.HasKey(nameof(QAnalysis) + "_EditorTest");

				var newValue = GUILayout.Toggle(editorTest, "编辑器测试", GUILayout.Width(100));
				if (newValue != editorTest)
				{
					if (newValue)
					{
						PlayerPrefs.SetString(nameof(QAnalysis) + "_EditorTest", "true");
					}
					else
					{
						PlayerPrefs.DeleteKey(nameof(QAnalysis) + "_EditorTest");
					}
				}
				QAnalysisData.Setting.StartVersion = EditorGUILayout.TextField(QAnalysisData.Setting.StartVersion,GUILayout.Width(100));
				GUILayout.Label("事件：" + QAnalysisData.EventList.Count, GUILayout.Width(120));
				GUILayout.Label("玩家：" + QAnalysisData.Instance.PlayerDataList.Count, GUILayout.Width(80));
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
					if (EditorUtility.DisplayDialog("重置数据表", "将会清空所有本地信息\n包含列信息设置", "确认", "取消"))
					{
						QAnalysisData.Clear(true);
						FreshData();
					}
				}
				if (DrawButton("复制表格数据"))
				{
					GUIUtility.systemCopyBuffer = GridView.Copy();
					EditorUtility.DisplayDialog("复制表格数据", "复制数据成功", "确认");
				}
				var lastRect = GUILayoutUtility.GetLastRect();
				Handles.DrawLine(new Vector3(0, lastRect.yMax), new Vector3(position.width, lastRect.yMax));
				if (QAnalysisData.IsLoading)
				{
					GUI.enabled = true;
					QGUITool.ProgressBar("加载中..   " + QAnalysisData.LoadingInfo, QAnalysisData.LoadingRate);
					
				}
			}
			GridView.DoLayout(Repaint);
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
			else if(obj is DateTime dateTime&& dateTime.Ticks ==0)
			{
				return "";
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
				return ViewInfoStack.Count == 0 ? Titles[x].Key : Titles[x].GetViewKey(ViewEvent); 
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
				var eventData= viewEventList[y - 1];
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
				size.y= viewEventList .Count+ 1;
			}
			return size;
		}
		public async void FreshData()
		{
			await QAnalysisData.FreshData(Repaint);
		}
		Stack<string> ViewInfoStack = new Stack<string>();
		Stack<string> ViewPlayerStack = new Stack<string>();
		public string ViewEvent=> ViewInfoStack.Count > 0 ? ViewInfoStack.Peek() : "玩家Id";
		public string ViewPlayer => ViewPlayerStack.Count > 0 ? ViewPlayerStack.Peek():"";
		Vector2 viewPos;

		List<QAnalysisEvent> viewEventList = new List<QAnalysisEvent>();
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
							viewEventList.AddCheckExist(eventData);
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
	public class QAnalysisDataSetting
	{
		public string StartVersion = "0.1";
		public QList<string, QTitleInfo> TitleList = new QList<string, QTitleInfo>();
	}
	public class QAnalysisData
	{
		public static QAnalysisData Instance { get; private set; } = Activator.CreateInstance<QAnalysisData>();
		static public QList<string, QAnalysisEvent> EventList = new QList<string, QAnalysisEvent>();
		public QAutoList<string, QPlayerData> PlayerDataList = new QAutoList<string, QPlayerData>();
		public static QAnalysisDataSetting Setting = new QAnalysisDataSetting();
		public static QList<string, QTitleInfo> TitleList => Setting.TitleList;
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
			EditorUtility.DisplayProgressBar("数据读取", "读取解析数据 QTool/" + QAnalysis.StartKey, 0.2f);
			QFileManager.Load("QTool/" + QAnalysis.StartKey, "{}").ParseQData(Instance);
			EditorUtility.DisplayProgressBar("数据读取","读取事件表 "+"QTool/" + QAnalysis.StartKey, 0.5f);
			QFileManager.Load("QTool/" + QAnalysis.StartKey + "_" + nameof(EventList)).ParseQData(EventList);
			EditorUtility.DisplayProgressBar("数据读取", "读取解析数据设置 "+ "QTool/" + QAnalysis.StartKey, 0.9f);
			QFileManager.Load("QTool/" + QAnalysis.StartKey + "_" + nameof(Setting)).ParseQData(Setting);
			EditorUtility.ClearProgressBar();
		}
		public static void SaveData()
		{
			EditorUtility.DisplayProgressBar("数据储存", "储存解析数据 QTool/" + QAnalysis.StartKey, 0.2f);
			QFileManager.Save("QTool/" + QAnalysis.StartKey, Instance.ToQData());
			EditorUtility.DisplayProgressBar("数据储存", "储存事件表 " + "QTool/" + QAnalysis.StartKey, 0.5f);
			QFileManager.Save("QTool/" + QAnalysis.StartKey + "_" + nameof(EventList), EventList.ToQData());
			EditorUtility.DisplayProgressBar("数据储存", "储存解析数据设置 " + "QTool/" + QAnalysis.StartKey, 0.9f);
			QFileManager.Save("QTool/" + QAnalysis.StartKey + "_" + nameof(Setting), Setting.ToQData());
			EditorUtility.ClearProgressBar();
		}
		public static void ForeachTitle(Action<QTitleInfo> action)
		{
			var viewInfo = QAnalysisWindow.Instance.ViewEvent;
			foreach (var title in TitleList)
			{
				if (title.CheckView(viewInfo))
				{
					action(title);
				}
			}
		}
		public static bool IsLoading { get; private set; } = false;
		public static string LoadingInfo { get; private set; } = "";
		public static float LoadingRate { get; private set; } = 0;
		public static void SetLoadingInfo(string title, string info, float rate)
		{

			LoadingInfo = title + " " + info;
			LoadingRate = rate;
			//EditorUtility.DisplayProgressBar(title, info, rate);
		}
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
		static QDictionary<string, float> playerVersion = new QDictionary<string, float>();
		static float _startV = 0;
		static float startV;
		//static void AddNewEventList()
		//{
		//	QDictionary<string, float> playerVersion = new QDictionary<string, float>();
		//	var start = DateTime.Now;
		//	QDebug.Log("对新事件排序" + NewEventList.Count);
		//	QAnalysisData.SetLoadingInfo("事件排序", "数量" + NewEventList.Count, 0);
		//	NewEventList.Sort(QAnalysisEvent.SortMethod);
		//	QDebug.Log("排序完成" + (DateTime.Now - start).ToString("hh\\:mm\\:ss") + "开始添加事件" + NewEventList.Count);
		//	Queue<QAnalysisEvent> queue = new Queue<QAnalysisEvent>(NewEventList);
		//	NewEventList.Clear();
		//	start = DateTime.Now;
		//	var length = queue.Count;
		//	var i = 0;
		//	while (queue.Count>0)
		//	{
		//		var eventData = queue.Dequeue();
		//		var version = playerVersion[eventData.playerId];
		//		if ("游戏/开始".Equals(eventData.eventKey))
		//		{
		//			version = ((StartInfo)eventData.eventValue).version.ToComputeFloat();
		//			playerVersion[eventData.playerId] = version;
		//		}
		//		if (version >= startV)
		//		{
		//			AddEvent(eventData);
		//		}
		//		if (i % QAnalysis.AutoSendCount == 0)
		//		{
		//			SetLoadingInfo("添加事件", i + "/" + length + " " + eventData.eventKey, i * 1f / length);
		//		}
		//		i++;
		//	}
		//	QDebug.Log("添加事件" + length + "完成 " + (DateTime.Now - start).ToString("hh\\:mm\\:ss") + " 总数" + QAnalysisData.EventList.Count);
		//	EditorUtility.ClearProgressBar();
		//}
		//static Queue<QAnalysisEvent> NewEventList { get;  set; } = new Queue<QAnalysisEvent>();
		static List<Task> ParseTasks = new List<Task>();
		//static QDictionary<string, List< Task>> PlayerTasks = new QDictionary<string,List< Task>>();
		static DateTime loadingStartTime;
		public static async Task<bool> FreshData(Action action) 
		{
			if (IsLoading) return true;
			loadingStartTime = DateTime.Now;
			if (!QToolSetting.Instance.QAnalysisMail.InitOver)
			{
				Debug.LogError(nameof(QToolSetting.Instance.QAnalysisMail) + " 未设置");
				return true;
			}
			IsLoading = true;
			LoadingInfo = "";
			LoadingRate = 0;
			startV = Setting.StartVersion.ToComputeFloat();
			try
			{
				var loadOver= await QMailTool.FreshEmails(QToolSetting.Instance.QAnalysisMail,(mailInfo) =>
				{
					Instance.LastMail = mailInfo;
					if (mailInfo.Subject.StartsWith(QAnalysis.StartKey))
					{
						if (!string.IsNullOrWhiteSpace(mailInfo.Body))
						{
							var list = mailInfo.Body.ParseQData<List<QAnalysisEvent>>();
							var playerData = Instance.PlayerDataList[list.StackPeek().playerId];
							var task = Task.Run(() =>
							{
								try
								{
									for (int i = 0; i < list.Count; i++)
									{
										var eventData = list[i];
										var version = playerVersion[eventData.playerId];
										SetLoadingInfo("添加玩家数据[" + eventData.playerId + "]", i + "/" + list.Count + " " + eventData.eventKey, i * 1f / list.Count);
										if ("游戏/开始".Equals(eventData.eventKey))
										{
											version = ((StartInfo)eventData.eventValue).version.ToComputeFloat();
											playerVersion[eventData.playerId] = version;
										}
										if (version >= startV)
										{
											AddEvent(eventData);
										}
									}
								}
								catch (Exception e)
								{
									Debug.LogError("添加玩家数据出错 " + e);
								}
								
							});
							ParseTasks.Add(task);
						}
					}
				}, Instance.LastMail,500);
				foreach (var task in ParseTasks)
				{
					await QTask.Wait(() => { action(); return task.IsCompleted; });
				}
				ParseTasks.Clear();
				foreach (var player in Instance.PlayerDataList)
				{
					_=Task.Run(player.ParseEventBuffer);
				}
				foreach (var player in Instance.PlayerDataList)
				{
					await QTask.Wait(() => { if (player.EventBuffer.Count > 0) { action(); SetLoadingInfo("解析玩家数据[" + player.Key + "]", player.EventBuffer.Count + "/" + player.BufferCount + " " + player.EventBuffer.QueuePeek().eventKey, player.EventBuffer.Count * 1f / player.BufferCount); } return player.EventBuffer.Count == 0; });
				}
				action();
				SaveData();
				QDebug.Log("刷新数据完成 保存数据 用时: " + (DateTime.Now - loadingStartTime).ToString("hh\\:mm\\:ss"));
				return loadOver;
			}
			catch (Exception e)
			{
				Debug.LogError(e);
			}
			finally
			{
				IsLoading = false;
			}
			return true;

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
			EventList.Add(eventData);
			Instance.PlayerDataList[eventData.playerId].Add(eventData);
			if (!Instance.EventKeyList.Contains(eventData.eventKey))
			{
				Instance.EventKeyList.Add(eventData.eventKey);
				Instance.DataKeyList.Add(eventData.eventKey);
				CheckTitle(eventData.eventKey, eventData.eventValue);
			}
		}
		static void CheckTitle(string key,object value)
		{
			if (TitleList.ContainsKey(key)) return;
			var title = new QTitleInfo();
			TitleList.Set(key, title);
			if (title.DataSetting.mode== QAnalysisMode.更新时间)
			{
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
			var tKey = key;
			var startKey = "";
			while(tKey.SplitTowString("/",out var start,out var end))
			{
				if (string.IsNullOrEmpty(startKey))
				{
					startKey = start;
				}
				else
				{
					startKey += "/" + start;
				}
				if (!TitleList.ContainsKey(startKey))
				{
					var spaceTitle = new QTitleInfo();
					TitleList.Set(startKey, spaceTitle);
					spaceTitle.DataSetting.DataKey = "";
					spaceTitle.DataSetting.mode = QAnalysisMode.更新时间;
				}
				tKey = end;
			}

			if (value != null)
			{
				foreach (var memeberInfo in QSerializeType.Get(value.GetType()).Members)
				{
					var memeberKey = key + "/" + memeberInfo.Key;
					Instance.DataKeyList.AddCheckExist(memeberKey);
					CheckTitle(memeberKey,value== null ? null : memeberInfo.Get(value));

				}
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
			DataSetting.DataKey = eventKey;
			QAnalysisData.Instance.FreshKey(Key);
		}
		public bool CheckView(string viewInfo)
		{
			if (viewInfo == Key) return DataSetting.mode!= QAnalysisMode.更新时间;
			if (viewInfo == "玩家Id")
			{
				if (!Key.Contains("/")) return true;
			}
			else
			{
				var index = Key.IndexOf(viewInfo+"/") ;
				if (index == 0 && !Key.Substring(index + viewInfo.Length+1).Contains("/"))
				{
					return true;
				}
			}
			return false;
		}
		public string GetViewKey(string viewEvent)
		{
			if (Key == viewEvent) return Key.SplitEndString("/");
			if (Key.Contains(viewEvent))
			{
				return Key.SplitEndString(viewEvent ).TrimStart('/');
			}
			else
			{
				return Key;
			}
		}
	}
	public enum QAnalysisMode
	{
		更新时间,
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
		public QAnalysisMode mode = QAnalysisMode.更新时间;
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
				for (int i = 0; i < QAnalysisData.Instance.EventKeyList.Count; i++)
				{
					var eventKeyValue = QAnalysisData.Instance.EventKeyList[i];
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
						BufferData[eventData.eventId] = (int)BufferData[BufferData.Count - 1].Value + 1;
					}
					break;
				case QAnalysisMode.最小值:
					if (BufferData.Count == 0)
					{
						BufferData[eventData.eventId] = eventData.GetValue(setting.DataKey).ToComputeFloat();
					}
					else if ((float)BufferData[BufferData.Count - 1].Value > eventData.GetValue(setting.DataKey).ToComputeFloat())
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
						BufferData[eventData.eventId] = ((float)BufferData[BufferData.Count - 1].Value + eventData.GetValue(setting.DataKey).ToComputeFloat()) / 2;
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
						BufferData[eventData.eventId] = GetTimeSpan(eventData);
						EventList.AddCheckExist(eventData.Key);
					}
					break;
				case QAnalysisMode.总时长:
					if (BufferData.Count == 0)
					{
						BufferData[eventData.eventId] = GetTimeSpan(eventData);
					}
					else
					{
						BufferData[eventData.eventId] = (TimeSpan)BufferData[BufferData.Count - 1].Value + GetTimeSpan(eventData);
					}
					EventList.AddCheckExist(eventData.Key);
					break;
				default:
					UpdateTime = eventData.eventTime;
					return;
			}

			UpdateTime = eventData.eventTime;
		}
		TimeSpan GetTimeSpan(QAnalysisEvent eventData)
		{
			var setting = QAnalysisData.TitleList[Key].DataSetting;
			var targetData = GetPlayerData(eventData).AnalysisData[setting.TargetKey];
			if (setting.EventKey.EndsWith("开始"))
			{
				return GetTimeSpan(QAnalysisData.GetEvent(EventList.StackPeek()), targetData.GetEndEvent(eventData.eventTime), out var hasend, eventData);
			}
			else if (setting.EventKey.EndsWith("结束"))
			{
				var startEvent = targetData.GetEndEvent(eventData.eventTime);
				var nextEvent = startEvent!=null? QAnalysisData.GetEvent( targetData.EventList[targetData.EventList.IndexOf(startEvent.eventId) + 1]):null;
				return GetTimeSpan(startEvent, eventData, out var hasend, nextEvent);
			}
			return TimeSpan.Zero;
		}
		QPlayerData GetPlayerData(QAnalysisEvent eventData)
		{
			return QAnalysisData.Instance.PlayerDataList[eventData.playerId];
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
				var pauseData = playerData.AnalysisData[nameof(QAnalysis.QAnalysisEventName.游戏_暂离)];
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
					return GetPlayerData(startData==null?endData:startData).UpdateTime - startData.eventTime;
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
				if (BufferData.Count==0)
				{
					return QAnalysisData.TitleList[Key].DataSetting.mode== QAnalysisMode.更新时间? (object)UpdateTime: null;
				}
				else
				{
					return BufferData.StackPeek().Value;
				}
			}
		
			if (BufferData.ContainsKey(eventId))
			{
				return BufferData[eventId];
			}
			else
			{
				return null;
			}
		
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

		public List<QAnalysisEvent> EventBuffer = new List<QAnalysisEvent>();
		public int BufferCount = 0;
		public void ParseEventBuffer()
		{
			EventBuffer.Sort(QAnalysisEvent.SortMethod);
			BufferCount = EventBuffer.Count;
			while (EventBuffer.Count>0)
			{
				try
				{
					var eventData = EventBuffer.Dequeue();
					UpdateTime = eventData.eventTime;
					EventList.AddCheckExist(eventData.eventId);
					for (int i = 0; i < QAnalysisData.TitleList.Count; i++)
					{
						var title = QAnalysisData.TitleList[i];
						if (title.DataSetting.EventKey == eventData.eventKey)
						{
							AnalysisData[title.Key].AddEvent(eventData);
						}
						else if (title.DataSetting.mode == QAnalysisMode.更新时间 && eventData.eventKey.StartsWith(title.Key))
						{
							AnalysisData[title.Key].AddEvent(eventData);
						}
					}
				}
				catch (Exception e)
				{
					Debug.LogError(Key + "添加事件出错 ：" + e);
				}
			}
		}
		public void Add(QAnalysisEvent eventData)
		{
			lock (EventBuffer)
			{
				EventBuffer.Enqueue(eventData);
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
		public static void ProgressBar(string info, float progress)
		{
			GUILayout.Box("", BackStyle);
			var lastRect = GUILayoutUtility.GetLastRect();
			var rateRect = lastRect;
			rateRect.width *= QAnalysisData.LoadingRate;
			if (progress > 0)
			{
				SetColor(Color.green);
				GUI.Box(rateRect, "", CellStyle);
				RevertColor();
			}
			GUI.Label(lastRect, info, CenterLable);
		}
		public static GUIStyle TitleLable => _titleLabel ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
		static GUIStyle _titleLabel;
		public static GUIStyle CenterLable => _centerLable ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter,richText=true };
		static GUIStyle _centerLable;
		public static GUIStyle TextArea => _textArea ??= new GUIStyle(EditorStyles.textField) { alignment = TextAnchor.MiddleCenter };
		static GUIStyle _textArea;
		public static GUIStyle LeftLable => _leftLable ??= new GUIStyle(EditorStyles.label) { richText = true };
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
