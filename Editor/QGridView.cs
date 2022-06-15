using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;

namespace QTool
{

	public class QGridView
	{
		public Func<int, int, Vector2, Rect> DrawCell;
		public Func<Vector2Int> GetSize;
		public Vector2Int GridSize { private set; get; }
		public Vector2 ViewDataSize { private set; get; }
		public Vector2 ViewSize { private set; get; }
		public Vector2 ViewScrollPos { private set; get; }
		public QGridView(Func<int, int,Vector2, Rect> DrawCell, Func<Vector2Int> GetSize)
		{
			this.DrawCell = DrawCell;
			this.GetSize = GetSize;
		}
	 	readonly static Vector2 CellSize = new Vector2(100,30);
		public float GetWidth(int x=0)
		{
			return CellSize.x;
		}
		public RectInt GetViewRange()
		{
			var range = new RectInt();
			var viewRect = new Rect(ViewScrollPos, ViewDataSize);
			range.x= (int)(viewRect.xMin / (CellSize.x))+1;
			range.y = (int)(viewRect.yMin / (CellSize.y))+1;
			range.width= Mathf.CeilToInt((viewRect.width /( CellSize.x)))+1; 
			range.height = Mathf.CeilToInt( (viewRect.height / (CellSize.y)))+1;
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
		RectInt ViewRange;
		public void DoLayout()
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
					var rect = DrawCell(0, 0, CellSize);
					Handles.DrawLine(new Vector3(0, rect.yMax), new Vector3(ViewSize.x, rect.yMax));
					Handles.DrawLine(new Vector3(rect.xMax, rect.yMin), new Vector3(rect.xMax, ViewSize.y));
					using (new GUILayout.ScrollViewScope(new Vector2(ViewScrollPos.x, 0), GUIStyle.none, GUIStyle.none, GUILayout.Height(CellSize.y)))
					{
						using (new GUILayout.HorizontalScope())
						{

							GUILayout.Space((ViewRange.x - 1) * (CellSize.x ));
							for (int x = ViewRange.x; x < ViewRange.xMax; x++)
							{
								DrawLine(DrawCell(x,0, CellSize));
							}
							if (ViewRange.xMax < GridSize.x)
							{
								GUILayout.Space((GridSize.x - ViewRange.xMax) * (CellSize.x ));
							}
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
							GUILayout.Space((ViewRange.y-1) * (CellSize.y));
							for (int y = ViewRange.y; y < ViewRange.yMax; y++)
							{
								DrawLine( DrawCell(0, y, CellSize));
							}
							if (ViewRange.yMax < GridSize.y)
							{
								GUILayout.Space((GridSize.y-ViewRange.yMax ) *( CellSize.y));
							}
							GUILayout.Space(13);
						}
					}
					GUILayout.Space(6);
					using (var dataView = new GUILayout.ScrollViewScope(ViewScrollPos))
					{
						using (new GUILayout.VerticalScope())
						{
							GUILayout.Space((ViewRange.y - 1) * (CellSize.y ));
							for (int y = ViewRange.y; y < ViewRange.yMax; y++)
							{
								using (new GUILayout.HorizontalScope())
								{

									GUILayout.Space((ViewRange.x - 1) * (CellSize.x ));
									for (int x = ViewRange.x; x < ViewRange.xMax; x++)
									{
										DrawLine(DrawCell(x, y, CellSize));
									}
									if (ViewRange.xMax < GridSize.x)
									{
										GUILayout.Space((GridSize.x - ViewRange.xMax) * (CellSize.x ));
									}
									GUILayout.FlexibleSpace();
								} 
							}
							if (ViewRange.yMax < GridSize.y)
							{
								GUILayout.Space((GridSize.y - ViewRange.yMax) * (CellSize.y ));
							}
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
		}
	}

}
