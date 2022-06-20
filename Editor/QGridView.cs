using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using QTool.Inspector;
namespace QTool
{

	public class QGridView
	{
		public Func<Vector2Int> GetSize;
		public Vector2Int GridSize { private set; get; }
		public Vector2 ViewDataSize { private set; get; }
		public Vector2 ViewSize { private set; get; }
		public Vector2 ViewScrollPos { private set; get; }
		QList<float> CellWidth = new QList<float>();
		public QGridView(Func<int, int, string> GetStringValue,Func<Vector2Int> GetSize)
		{
			this.GetStringValue = GetStringValue;
			this.GetSize = GetSize;
		}
	 	readonly static Vector2 DefualtCellSize = new Vector2(100,30);
		public float GetWidth(int x=0)
		{
			return Mathf.Max(CellWidth[x],100);
		}
		public float GetHeight(int x = 0)
		{
			return 30;
		}
		public void Space(int start,int end,bool width = true)
		{
			Func<int, float> GetValue = null;
			if (width)
			{
				GetValue = GetWidth;
			}
			else
			{
				GetValue = GetHeight;
			}
			var sum = 0f;
			for (int i = start; i < end; i++)
			{
				sum += GetValue(i);
			}
			GUILayout.Space(sum);
		}
		public RectInt GetViewRange()
		{
			var range = new RectInt();
			var viewRect = new Rect(ViewScrollPos, ViewDataSize);
			var sum = 0f;
			for (range.x = 1; range.x < GridSize.x; range.x++)
			{
				sum += GetWidth(range.x);
				if (sum >= viewRect.xMin)
				{
					break;
				}
			}
			for (range.width =1; range.xMax < GridSize.x; range.width++)
			{
				sum += GetWidth(range.xMax);
				if (sum >= viewRect.xMax)
				{
					range.width++;
					break;
				}
			}

			sum = 0;
			for (range.y = 1; range.y < GridSize.y; range.y++)
			{
				sum += GetHeight(range.y);
				if (sum >= viewRect.yMin)
				{
					break;
				}
			}
			for (range.height = 1; range.yMax < GridSize.y; range.height++)
			{
				sum += GetHeight(range.yMax);
				if (sum >= viewRect.yMax)
				{
					range.height++;
					break;
				}
			}
			if (range.yMax > GridSize.y) 
			{
				range.height -= range.yMax - GridSize.y;
			}
			if (range.xMax > GridSize.x)
			{
				range.width -= range.xMax - GridSize.x;
			}
			return range;
		}
		void DrawLine(Rect lastRect)
		{
			var lastColor = Handles.color;
			Handles.color = Color.gray;
			Handles.DrawLine(new Vector3(lastRect.xMin, lastRect.yMax), new Vector3(lastRect.xMax, lastRect.yMax));
			Handles.DrawLine(new Vector3(lastRect.xMax, lastRect.yMin), new Vector3(lastRect.xMax, lastRect.yMax));
			Handles.color = lastColor;
		}
		public Func<int, int, string> GetStringValue=null;
		public Func<int, int, bool> EditCell = null;
		public Vector2Int editIndex = Vector2Int.one * -1;
		public Rect DrawCell(int x, int y)
		{
			var width = GUILayout.Width(GetWidth(x));
			var height = GUILayout.Height(GetHeight(y));
			GUILayout.Label(GetStringValue(x,y), QGUITool.CenterLable, width, height);
			var rect = GUILayoutUtility.GetLastRect();
			if(Event.current.type!= EventType.Layout)
			{
				rect.MouseMenuClick(null, () =>
				{
					if (EditCell != null)
					{
						editIndex = new Vector2Int
						{
							x=x,
							y=y
						};
					}
				});
			}
		
			return rect;
		}
		RectInt ViewRange;
		int DragXIndex = -1;
		float startPos=0;
		public void DoLayout(Action Repaint)
		{
			if(Event.current.type != EventType.Repaint)
			{
				GridSize = GetSize();

				ViewRange = GetViewRange();

			}
			using (new GUILayout.VerticalScope())
			{
				using (new GUILayout.HorizontalScope())
				{
					var rect = DrawCell(0, 0);
					Handles.DrawLine(new Vector3(0, rect.yMax), new Vector3(ViewSize.x, rect.yMax));
					Handles.DrawLine(new Vector3(rect.xMax, rect.yMin), new Vector3(rect.xMax, ViewSize.y));
					using (new GUILayout.ScrollViewScope(new Vector2(ViewScrollPos.x, 0), GUIStyle.none, GUIStyle.none, GUILayout.Height(GetHeight())))
					{
						using (new GUILayout.HorizontalScope())
						{
							Space(1, ViewRange.x);
							for (int x = ViewRange.x; x < ViewRange.xMax; x++)
							{
								var drawRect = DrawCell(x, 0);
								DrawLine(drawRect);
								var pos = drawRect.xMin;
								drawRect.x += drawRect.width - 5;
								drawRect.width = 10;
								if (drawRect.Contains(Event.current.mousePosition))
								{
									if (Event.current.type == EventType.MouseDown)
									{
										startPos = pos+rect.xMax-ViewScrollPos.x;
										DragXIndex = x;
									}
								}
								
							}
							Space(ViewRange.xMax, GridSize.x);
							GUILayout.FlexibleSpace();
						}

					}
					GUILayout.Space(13);
				}
				GUILayout.Space(5);
				using (new GUILayout.HorizontalScope())
				{
					using (new GUILayout.ScrollViewScope(new Vector2(0, ViewScrollPos.y), GUIStyle.none, GUIStyle.none, GUILayout.Width(GetWidth())))
					{
						using (new GUILayout.VerticalScope())
						{
							Space(1, ViewRange.y, false);
							for (int y = ViewRange.y; y < ViewRange.yMax; y++)
							{
								DrawLine( DrawCell(0, y));
							}
							Space(ViewRange.yMax, GridSize.y, false);
							GUILayout.Space(13);
						}
					}
					GUILayout.Space(6);
					using (var dataView = new GUILayout.ScrollViewScope(ViewScrollPos))
					{
						using (new GUILayout.VerticalScope())
						{
							Space(1, ViewRange.y, false);
							for (int y = ViewRange.y; y < ViewRange.yMax; y++)
							{
								using (new GUILayout.HorizontalScope())
								{
									Space(1, ViewRange.x);
									for (int x = ViewRange.x; x < ViewRange.xMax; x++)
									{
										DrawLine(DrawCell(x, y));
									}
									Space(ViewRange.xMax, GridSize.x);
									GUILayout.FlexibleSpace();
								} 
							}
							Space(ViewRange.yMax, GridSize.y, false);
						}
						ViewScrollPos = dataView.scrollPosition;
					}
					if (Event.current.type == EventType.Repaint)
					{
						ViewDataSize = GUILayoutUtility.GetLastRect().size;
					}
				}
			}
			if (Event.current.type == EventType.Repaint)
			{
				ViewSize = GUILayoutUtility.GetLastRect().size;
			}
			if (DragXIndex > 0)
			{

				CellWidth[DragXIndex] = Event.current.mousePosition.x - startPos;
				if (Event.current.type == EventType.MouseUp)
				{
					DragXIndex = -1;
				}
				Repaint();
			}
			if (editIndex.x >= 0 && editIndex.y >= 0)
			{
				if(EditCell(editIndex.x, editIndex.y))
				{
					Repaint();
				}
				editIndex = Vector2Int.one * -1;
			}
		}
	}
	public class QEidtCellWindow : EditorWindow
	{
		static QEidtCellWindow Instance { set; get; }
		public static object Show(string key,object value,Type type)
		{
			if (Instance == null)
			{
				Instance = GetWindow<QEidtCellWindow>();
				Instance.minSize = new Vector2(300, 100);
				Instance.maxSize = new Vector2(300, 100);
			}
			Instance.titleContent = new GUIContent( key);
			Instance.type = type;
			Instance.value = value;
			Instance.ShowModal();
			return Instance.value;
		}
		public Type type;
		public object value;
		public Vector2 scrollPos;
		private void OnGUI()
		{
			using (new GUILayout.ScrollViewScope(scrollPos))
			{
				value= value.Draw("", type);	
			}
		}
	}
}
