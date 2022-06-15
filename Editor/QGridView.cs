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
		public void DoLayout()
		{
			GridSize = GetSize();
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
							for (int x = 1; x < GridSize.x; x++)
							{
								DrawCell(x,0, CellSize);
							}
							GUILayout.FlexibleSpace();
						}

					}
					GUILayout.FlexibleSpace();
					GUILayout.Space(13);
				}
				using (new GUILayout.HorizontalScope())
				{

					using (new GUILayout.ScrollViewScope(new Vector2(0, ViewScrollPos.y), GUIStyle.none, GUIStyle.none, GUILayout.Width(GetWidth())))
					{
						using (new GUILayout.VerticalScope())
						{
							for (int y = 1; y < GridSize.y; y++)
							{
								DrawCell(0, y, CellSize);
							}
							GUILayout.Space(13);
						}
					}
					//if (Event.current.type == EventType.Repaint)
					//{
					//	viewRect = GUILayoutUtility.GetLastRect();
					//}
					GUILayout.Space(6);
					using (var dataView = new GUILayout.ScrollViewScope(ViewScrollPos))
					{
						using (new GUILayout.VerticalScope())
						{
							for (int y = 1; y < GridSize.y; y++)
							{
								using (new GUILayout.HorizontalScope())
								{
									for (int x = 1; x < GridSize.x; x++)
									{
										DrawCell(x, y, CellSize);
									}
									GUILayout.FlexibleSpace();
								}
							}
						}
						ViewScrollPos = dataView.scrollPosition;
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
