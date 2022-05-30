using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;
using System;

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
			Instance.titleContent = new GUIContent(nameof(QAnalysis)+" - "+Application.productName);
			FreshData();
		}
		static async void FreshData()
		{
			if (QAnalysisData.IsLoading) return;
			await QAnalysisData.FreshData();
			Instance?.Repaint();
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

				var lastRect = GUILayoutUtility.GetLastRect();
				Handles.DrawLine(new Vector3(0, lastRect.yMax), new Vector3(position.xMax, lastRect.yMax));
			}
			using (var playerDataScroll = new GUILayout.ScrollViewScope(viewPos))
			{
				using (new GUILayout.VerticalScope())
				{

					using (new GUILayout.HorizontalScope())
					{
						DrawCell("玩家ID", 200, true, true);
						foreach (var title in QAnalysisData.Instance.TitleList)
						{
							DrawCell(title.Key, title.width, false, true);
						}
						GUILayout.FlexibleSpace();
					}
					using (new GUILayout.HorizontalScope())
					{
						using (new GUILayout.VerticalScope())
						{
							foreach (var data in QAnalysisData.Instance.AnalysisData)
							{
								DrawCell(data.Key, 200, true);
							}
						}

						using (new GUILayout.VerticalScope())
						{
							foreach (var playerData in QAnalysisData.Instance.AnalysisData)
							{
								using (new GUILayout.HorizontalScope())
								{
									foreach (var title in QAnalysisData.Instance.TitleList)
									{
										DrawCell(playerData.AnalysisData[title.Key].ToViewString());
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

	
		public void DrawCell(string value,float width,bool drawXLine,bool drawYLine=false)
		{
			DrawCell(value,width);
			var lastRect = GUILayoutUtility.GetLastRect();
			if (drawXLine)
			{
				Handles.DrawLine(new Vector3(viewPos.x, lastRect.yMax), new Vector3(viewPos.x+position.xMax, lastRect.yMax));
			}
			if (drawYLine)
			{ 
				Handles.DrawLine(new Vector3(lastRect.xMax,viewPos.y + lastRect.yMin ), new Vector3(lastRect.xMax, viewPos.y + position.yMax));;
			}
		}
		public void DrawCell(string value,float width=200)
		{
			GUILayout.Label(value, QGUITool.CenterLable, GUILayout.Width(width), GUILayout.Height(50));
		}
		public bool DrawButton(string name)
		{
			return GUILayout.Button(name, GUILayout.Width(100));
		}
	}
	public class QAnalysisData
	{
		public static QAnalysisData Instance { get; private set; } = Activator.CreateInstance<QAnalysisData>();
		public QList<string, QAnalysisEvent> EventList = new QList<string, QAnalysisEvent>();
		public QAutoList<string, QPlayerData> AnalysisData = new QAutoList<string, QPlayerData>();
		public QAutoList<string, QTitleInfo> TitleList = new QAutoList<string, QTitleInfo>();
		public QMailInfo LastMail=null;
		static QAnalysisData()
		{
			FileManager.Load("QTool/" + QAnalysis.StartKey, "{}").ParseQData(Instance);
		}
		public static bool IsLoading { get; private set; } = false;
		public static async Task FreshData() 
		{
			if (IsLoading) return;
			IsLoading = true;
			await QMailTool.FreshEmails(QToolSetting.Instance.QAnalysisMail, (mailInfo) =>
			{ 
				if (mailInfo.Subject.StartsWith(QAnalysis.StartKey))
				{
					AddEvent(mailInfo.Body.ParseQData<List<QAnalysisEvent>>());
				}
				Instance.LastMail = mailInfo;
			}, Instance.LastMail);

			FileManager.Save("QTool/" + QAnalysis.StartKey, Instance.ToQData());
			IsLoading = false;
		}
		public static void Clear()
		{
			Instance = Activator.CreateInstance<QAnalysisData>();
		}
		public static void AddEvent(List<QAnalysisEvent> newEventList)
		{
			foreach (var eventData in newEventList)
			{
				if (!Instance.TitleList.ContainsKey(eventData.eventKey))
				{
					Instance.TitleList[eventData.eventKey].width = 200;
				}
				Instance.EventList.Add(eventData);
				Instance.AnalysisData[eventData.accountId].Add(eventData);
			}
		}

	}
	public class QTitleInfo:IKey<string>
	{
		public string Key { get; set; }
		public float width;
		public QAnalysisSetting analysisSetting;
		
	}
	public enum QAnalysisMode
	{
		普通,
		计数,
	}
	public class QAnalysisSetting
	{
		public string Key;
		public QAnalysisMode mode = QAnalysisMode.普通;
	}
	public class QAnalysisInfo:IKey<string>
	{
		public string Key { get; set; }
		public object value;
		public DateTime updateTime;
		public string ToViewString()
		{
			return value == null ? updateTime.ToQTimeString() : value.ToString();
		}
		public void SetValue(object newValue, DateTime time)
		{
			updateTime = time;
			value = newValue;
		}
	}
	public class QPlayerData : IKey<string>
	{
		public QAutoList<string, QAnalysisInfo> AnalysisData = new QAutoList<string, QAnalysisInfo>();
		public string Key { get; set; }
		public List<QAnalysisEvent> EventList = new List<QAnalysisEvent>();
		public DateTime UpdateTime;
		public void Add(QAnalysisEvent eventData)
		{
			UpdateTime = eventData.eventTime;
			EventList.Add(eventData);
			AnalysisData[eventData.eventKey].SetValue( eventData.eventValue,eventData.eventTime);
		}
		public override string ToString()
		{
			return Key + "\t" + EventList.ToOneString("\t", (eventData) => eventData.eventKey);
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
		public static GUIStyle CenterLable => _centerLable ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
		static GUIStyle _centerLable;
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
