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
	    static bool IsLoading;
		static async void FreshData()
		{
			if (IsLoading) return;
			IsLoading = true;
			await QAnalysisData.FreshData();
			IsLoading = false;
			Instance?.Repaint();
		}
		Vector2 viewPos;
		private void OnGUI()
		{
			if (IsLoading)
			{
				GUI.enabled = false;
			}
			using (var toolBarVer = new GUILayout.VerticalScope())
			{
				using (var toolBarHor = new GUILayout.HorizontalScope())
				{
					if (DrawButton("刷新数据"))
					{
						FreshData();
					}
					var lastRect = GUILayoutUtility.GetLastRect();
					Handles.DrawLine(new Vector3(0, lastRect.yMax), new Vector3(position.xMax, lastRect.yMax));
				}
				using (var titleHor = new GUILayout.HorizontalScope())
				{
					DrawCell("用户Id");
					var lastRect = GUILayoutUtility.GetLastRect();
					Handles.DrawLine(new Vector3(0, lastRect.yMax), new Vector3(position.xMax, lastRect.yMax));
				}
				using (var playerLeftRight = new GUILayout.HorizontalScope())
				{
					using (var playerKey = new GUILayout.VerticalScope())
					{
						foreach (var data in QAnalysisData.Instance.AnalysisData)
						{
							DrawCell(data.Key);
							var lastRect = GUILayoutUtility.GetLastRect();
							Handles.DrawLine(new Vector3(0, lastRect.yMax), new Vector3(position.xMax, lastRect.yMax));
						//	Handles.DrawLine(new Vector3(rect.xMin - 2, rect.yMax + 2), new Vector3(rect.xMax, rect.yMax + 2));
						}
					}
					using (var playerDataScroll = new GUILayout.ScrollViewScope(viewPos))
					{
						using (var playerDataVer = new GUILayout.VerticalScope())
						{
							foreach (var playerData in QAnalysisData.Instance.AnalysisData)
							{
								using (var playerDataHor = new GUILayout.HorizontalScope())
								{
									foreach (var eventData in playerData.EventList)
									{
										DrawCell(eventData.eventKey);
									}
								}
							}
						}
						viewPos = playerDataScroll.scrollPosition;
					}
				}
			}
			if (IsLoading)
			{
				GUI.enabled = true;
				GUI.Label(new Rect(Vector2.zero,position.size), "加载中..",QGUITool.BackStyle);
			}
			
		}
		public void DrawCell(string value)
		{
			GUILayout.Label(value, QGUITool.CenterLable, GUILayout.Width(100), GUILayout.Height(50));
		
		}
		public bool DrawButton(string name)
		{
			return GUILayout.Button(name);
		}
	}
	public class QAnalysisData
	{
		public readonly static QAnalysisData Instance = Activator.CreateInstance<QAnalysisData>();
		public QList<string, QAnalysisEvent> EventList = new QList<string, QAnalysisEvent>();
		public QAutoList<string, QPlayerData> AnalysisData = new QAutoList<string, QPlayerData>();
		public QMailInfo LastMail=null;
		static QAnalysisData()
		{
			FileManager.Load("QTool/" + QAnalysis.StartKey, "{}").ParseQData(Instance);
		}
		public static async Task FreshData() 
		{
			await QMailTool.FreshEmails(QToolSetting.Instance.QAnalysisMail, (mailInfo) =>
			{ 
				if (mailInfo.Subject.StartsWith(QAnalysis.StartKey))
				{
					AddEvent(mailInfo.Body.ParseQData<List<QAnalysisEvent>>());
				}
				Instance.LastMail = mailInfo;
			}, Instance.LastMail);

			FileManager.Save("QTool/" + QAnalysis.StartKey, Instance.ToQData());
		}
		public static void AddEvent(List<QAnalysisEvent> newEventList)
		{
			foreach (var eventData in newEventList)
			{
				Instance.EventList.Add(eventData);
				Instance.AnalysisData[eventData.accountId].Add(eventData);
			}
		}

	}
		public class QPlayerData : IKey<string>
		{
			public QDictionary<string, object> Data = new QDictionary<string, object>();
			public string Key { get; set; }
			public List<QAnalysisEvent> EventList = new List<QAnalysisEvent>();
			public void Add(QAnalysisEvent eventData)
			{
				EventList.Add(eventData);
				Data[eventData.eventKey] = eventData.eventKey + ":" + eventData.evventValue;
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
