using QTool.Inspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
namespace QTool.FlowGraph
{
	
    public class QFlowGraphWindow : EditorWindow
    {
        const float dotSize = 16;
		static QFlowGraph Graph = null;
        [OnOpenAsset(0)]
        public static bool OnOpen(int instanceID, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceID) as QFlowGraphAsset;
            if (asset != null)
            {
				PlayerPrefs.SetString(nameof(QFlowGraphWindow) + "_" + nameof(Graph) + "Path", AssetDatabase.GetAssetPath(asset));
				Open(asset.Graph, asset.Save);
                return true;
			}
            return false;
        }
		

        public static QFlowGraphWindow Instance { get; private set; }
		public event Action OnSave;
		public static void AutoLoadPath()
		{
			if (Graph != null) return;
			Graph= AssetDatabase.LoadAssetAtPath<QFlowGraphAsset>(PlayerPrefs.GetString(nameof(QFlowGraphWindow) + "_" + nameof(Graph)+"Path"))?.Graph;
			if (Graph == null)
			{
				PlayerPrefs.SetString(nameof(QFlowGraphWindow) + "_" + nameof(Graph) + "Path", "");
			}
		}
        public static void Open(QFlowGraph graph,Action OnSave=null)
        {
            if (Instance == null)
            {
                Instance = GetWindow<QFlowGraphWindow>(); 
                Instance.minSize = new Vector2(400, 300);
            }
            Instance.titleContent = new GUIContent((graph?.Name== null?"":graph.Name + " - ") + nameof(QFlowGraph));
			Graph = graph;
            Instance.ViewRange = new Rect(Vector2.zero, Instance.position.size);
			Instance.Show();
			Instance.OnSave = OnSave;

		}
        [MenuItem("Assets/QTool/Create/QFlowGraph", priority = 0)]
        public static void CreateNewFile()
        {
            var selectPath = Application.dataPath;
            if(Selection.activeObject !=null)
            {
                selectPath= AssetDatabase.GetAssetPath(Selection.activeObject);
            }
            var path = EditorUtility.SaveFilePanel("保存QFG文件", selectPath, nameof(QFlowGraphAsset), "qfg");
			if (!string.IsNullOrWhiteSpace(path))
			{
				FileManager.Save(path, (new QFlowGraph()).ToQData());
				AssetDatabase.Refresh();
				var asset = AssetDatabase.LoadAssetAtPath<QFlowGraphAsset>(path.ToAssetPath());
				Open(asset.Graph, asset.Save);
			}
        }
        [MenuItem("QTool/窗口/流程图")]
        public static void OpenWindow()
        {
            Open(null); 
        }

        private void OnFocus()
        {
			AutoLoadPath();
		}
        private void OnLostFocus()
        { 
			if (Graph != null)
			{
				Graph.OnBeforeSerialize();
				OnSave?.Invoke();
			}
        }

        private Rect ViewRange;
        void CreateMenu(PortId fromPortId)
        {
            GenericMenu menu = new GenericMenu();
            var fromPort = Graph[fromPortId];
            foreach (var info in QTool.Command.QCommand.KeyDictionary)
            {
                if (fromPort.CanConnect(info, out var portKey))
                {
                    menu.AddItem(new GUIContent(info.fullName), false, () =>
                    {
                        var node =Graph.Add(info.Key);
                        node.rect = new Rect(mousePos, new Vector2(300, 80));
                        fromPort.Connect( node.Ports[portKey]);
                    });
                }
            }
            menu.ShowAsContext();
        }
        private void ShowMenu()
        {
            GenericMenu menu = new GenericMenu();
            if (curNode == null)
            {
                foreach (var kv in QTool.Command.QCommand.KeyDictionary)
                {
                    menu.AddItem(new GUIContent(kv.fullName), false, () =>
                   {
                       var state =Graph.Add(kv.Key);
                       state.rect = new Rect(mousePos, new Vector2(300, 80));
                   });
                }
                menu.AddSeparator("");
                if (!string.IsNullOrWhiteSpace(GUIUtility.systemCopyBuffer))
                {
                    menu.AddItem(new GUIContent("粘贴"), false, Parse);
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("粘贴"));
                }
            }
            else
            {
                if (curPortId != null)
                {

                    menu.AddItem(new GUIContent("清空" + curPortId + "端口连接"), false, ()=> Graph[ curPortId.Value].ClearAllConnect(curPortId.Value.index));
                }
                else
                {
                    menu.AddItem(new GUIContent("复制"), false, Copy);
                    if (!string.IsNullOrWhiteSpace(GUIUtility.systemCopyBuffer))
                    {
                        menu.AddItem(new GUIContent("粘贴"), false, Parse);
                    }
                    menu.AddItem(new GUIContent("删除"), false, DeleteSelectNodes);
                    menu.AddItem(new GUIContent("清空连接"), false, ClearAllConnect);
                    menu.AddSeparator("");
                    if (Application.isPlaying)
                    {
                        menu.AddItem(new GUIContent("运行节点"), false, () =>
                        {
                            QToolManager.Instance.StartCoroutine(Graph.RunIEnumerator(curNode.Key));
                        });
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent("运行节点"));
                    }
                }

            }
            menu.ShowAsContext();
        }
        void Copy()
        {
            if (SelectNodes.Count == 0)
            {
                SelectNodes.Add(curNode);
            }

            GUIUtility.systemCopyBuffer = SelectNodes.ToQData();
        }
        void Parse()
        {
            try
            {
                var nodeList = GUIUtility.systemCopyBuffer.ParseQData<List<QFlowNode>>();
				Graph.Parse(nodeList, mousePos);
            }
            catch (Exception e)
            {
                throw new Exception("粘贴出错", e);
            }
        }
        void ClearAllConnect()
        {
            ForeachSelectNodes((node) => node.ClearAllConnect());
        }
        void DeleteSelectNodes()
        {
            ForeachSelectNodes((node) => Graph.Remove(node));
			Repaint();
		}
        void ForeachSelectNodes(System.Action<QFlowNode> action)
        {
            if (SelectNodes.Count > 0)
            {
                foreach (var node in SelectNodes)
                {
                    action(node);
                }
            }
            else
            {
                action(curNode);
            }
        }
        public void UpdateNearPort()
        {
            UpdateCurrentData();
            if (curNode != null)
            {
                if (curPortId == null)
                {
                    if (curNode.Key == connectStartPort?.node) {
                        nearPortId = null;return;
                    }
                    nearPortId = null;
                    var minDis = float.MaxValue;
                    foreach (var port in curNode.Ports)
                    {
                        
                        if (Graph[connectStartPort].CanConnect(port))
                        {
                            var index = 0;
                            foreach (var c in port.ConnectInfolist)
                            {
                                var dis = Vector2.Distance(c.rect.position, mousePos);
                                if (dis < minDis)
                                {
                                    nearPortId = new PortId(port, index);
                                    minDis = dis;
                                }
                                index++;
                            }
                        }

                    }
                }
                else
                {
                    nearPortId = curPortId;
                }
            }
            else
            {
                nearPortId = null;
            }

        }
        protected void UpdateCurrentData()
        {
            curNode = null;
            foreach (var state in Graph.NodeList)
            {
                if (state.rect.Contains(mousePos))
                {
                    curNode = state;
                    break;
                }
            }
            curPortId = null;
            if (curNode != null)
            {
                foreach (var port in curNode.Ports)
                {
                    var index = 0;
                    foreach (var c in port.ConnectInfolist)
                    {
                        if (c.rect.Contains(mousePos))
                        {
                            curPortId = new PortId(port,index);
                            break;
                        }
                        index++;
                    }
                   
                }
            }

        }
        Vector2 mousePos;
        QFlowNode curNode;
        PortId? curPortId;
        PortId? nearPortId;
        private void OnGUI()
        {
            ViewRange.size = position.size;
            mousePos = Event.current.mousePosition + ViewRange.position;
            DrawBackground();
            if (Graph==null)
            {
                if (GUILayout.Button("创建新的QFlowGraph"))
                {
                    CreateNewFile();
                }
                return;
            }
            Controls();
            BeginWindows();
            for (int i = 0; i <Graph.NodeList.Count; i++)
            {
                var node = Graph.NodeList[i];
                if (node == null)
                {
                    Debug.LogError(i + "/" +Graph.NodeList.Count);
                    continue;
                }
                if (ViewRange.Overlaps(node.rect))
                {
                    node.rect.position -= ViewRange.position;
                    var lastColor = GUI.backgroundColor;
                    GUI.backgroundColor =Color.Lerp( node.commandKey.ToColor(SelectNodes.Contains(node) ? 0.8f : 0.4f),Color.green,node.IsRunning?0.8f:0f);
                    node.rect = GUI.Window(i, node.rect, DrawNode, node.ViewName);
                    GUI.backgroundColor = lastColor;
                    node.rect.position += ViewRange.position;
                }

            }
            EndWindows();
            DrawCurve();
            switch (ControlState)
            {
                case EditorState.BoxSelect:
                    {
						QGUITool.SetColor(Color.black);
                        var box = SelectBox;
                        box.position -= ViewRange.position;
                        GUI.Box(box, "");
						QGUITool.RevertColor();
					}
                    break;
                default:
                    break;
            }
           

        }
        enum EditorState
        {
            None,
            MoveOffset,
            BoxSelect,
            ConnectPort,
            MoveNode,
        }
        EditorState ControlState = EditorState.None;
        Vector2 StartPos = Vector2.zero;
        Rect SelectBox = new Rect();
        List<QFlowNode> SelectNodes = new List<QFlowNode>();
        void Controls()
        {
            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    {
                        UpdateCurrentData();
                        if(ControlState== EditorState.None&&Event.current.button == 0)
                        {
                            if (curPortId != null)
                            {
                                if (Graph[curPortId.Value].isOutput)
                                {
                                    StartConnect(curPortId.Value);
                                }
                                else
                                {
                                    var curPort = Graph[curPortId];
                                    var fromPort = Graph.GetConnectInfo(curPortId).ConnectPort();
                                    if (fromPort != null)
                                    {
                                        Graph[fromPort].DisConnect(curPortId);
                                        StartConnect(fromPort);
                                    }
                                }
                                Event.current.Use();
                            }
                            else 
                            {
                                if (curNode == null)
                                {
                                    StartPos = mousePos;
                                    SelectBox = new Rect(StartPos, Vector2.zero);
                                    ControlState = EditorState.BoxSelect;
                                }
                                else
                                {
                                    SelectNodes.Clear();
                                    ControlState = EditorState.MoveNode;
                                }
                            }
                        }
                        
                    }
                    break;
                case EventType.MouseDrag:
                    {
                        switch (ControlState)
                        {
                            case EditorState.BoxSelect:
                                {
                                    var endPos = mousePos;
                                    SelectBox = new Rect(Mathf.Min(StartPos.x, endPos.x), Mathf.Min(StartPos.y, endPos.y), Mathf.Abs(StartPos.x - endPos.x), Mathf.Abs(StartPos.y - endPos.y));
                                    Repaint();
                                }
                                break;
                            case EditorState.None:
                            case EditorState.MoveOffset:
                                if (Event.current.delta.magnitude < 100)
                                {
                                    ViewRange.position -= Event.current.delta;
                                    ControlState = EditorState.MoveOffset;
                                    Repaint();
                                }
                                break;
                            case EditorState.ConnectPort:
                                UpdateNearPort(); Repaint();
                                break;
                            default:
                                break;
                        }

                    }
                    break;
                case EventType.MouseUp:
                    {

                        UpdateCurrentData();
                        switch (ControlState)
                        {
                            case EditorState.BoxSelect:
                                {
                                    SelectNodes.Clear();
                                    foreach (var node in Graph.NodeList)
                                    {
                                        var rect = node.rect;
                                        if (SelectBox.Overlaps(rect))
                                        {
                                            SelectNodes.Add(node);
                                        }
                                    }
                                    Repaint();
                                }
                                break;
                            case EditorState.ConnectPort:
                                {
                                    StopConnect(nearPortId);
                                    Event.current.Use();
                                }
                                break;
                            case EditorState.None:
                                if (Event.current.button == 1)
                                {
                                    ShowMenu();
                                    Event.current.Use();
                                }
                                else
                                {
                                    SelectNodes.Clear();
                                }
                                break;
                            default:
                                break;
                        } 

                        ControlState = EditorState.None;
                    }
                    break;
                case EventType.KeyUp:
                    {
                        switch (Event.current.keyCode)
                        {
                            case KeyCode.Delete:
                                DeleteSelectNodes();
                                break;
                            case KeyCode.C:
                                if (Event.current.control)
                                {
                                    Copy();
                                }
                                break;
                            case KeyCode.V:
                                if (Event.current.control&&curNode==null) 
                                {
                                    Parse();
                                }
                                break;
                            default:
                                break;
                        }
                    }break;
                default: break;
            }
        }
        #region 图形绘制
        static float Fix(float pos, float min, float max, float fixStep)
        {
            while (pos > max)
            {
                pos -= fixStep;
            } while (pos < min)
            {
                pos += fixStep;
            }
            return pos;
        }
        void DrawBackground()
        {
            var xTex = position.width / QGUITool.NodeEditorBackTexture2D.width;
            var yTex = position.height / QGUITool.NodeEditorBackTexture2D.height;
            var xStart = Fix(-ViewRange.x,-QGUITool.NodeEditorBackTexture2D.width, 0, QGUITool.NodeEditorBackTexture2D.width);
            var yStart = Fix(-ViewRange.y,-QGUITool.NodeEditorBackTexture2D.height, 0, QGUITool.NodeEditorBackTexture2D.height);
            for (int x = 0; x <= xTex + 1; x++)
            {
                for (int y = 0; y <= yTex + 1; y++)
                {
                    GUI.DrawTexture(new Rect(xStart + QGUITool.NodeEditorBackTexture2D.width * x, yStart + QGUITool.NodeEditorBackTexture2D.height * y, QGUITool.NodeEditorBackTexture2D.width, QGUITool.NodeEditorBackTexture2D.height), QGUITool.NodeEditorBackTexture2D);
                }
            }
        }
        Rect windowRect;
        void DrawNode(int id)
        {
            var node = Graph.NodeList[id];
			if (node == null) return;
            windowRect = node.rect;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(dotSize);
            EditorGUILayout.BeginVertical();
            if (node.command != null)
            {
                foreach (var port in node.Ports)
                {
                    DrawPort(port);
                }
            }
            else
            {
                GUILayout.Label("找不到命令【" + node.commandKey + "】 ");
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(dotSize);
            EditorGUILayout.EndHorizontal();
            if (Event.current.type== EventType.Repaint)
            {
                node.rect.height = GUILayoutUtility.GetLastRect().height + 30;
            }
            GUI.DragWindow();

        }
        void DrawCurve(Vector2 start, Vector2 end,Color color,bool isFlow=false)
        {
            if (!ViewRange.Contains(start) &&!ViewRange.Contains(end)) return;
			if (isFlow)
			{
				DrawCurve(start + Vector2.up * 2, end + Vector2.up * 2, color);
				DrawCurve(start + Vector2.down * 2, end + Vector2.down *2, color);
			}
			start -= ViewRange.position;
			end -= ViewRange.position;
			if (Vector3.Distance(start, end) < 0.1f)
			{
				return;
			}
			float size = (end.x - start.x) / 2;
			var yMax = Mathf.Min(end.y, start.y) - 100;
			var t = Mathf.Clamp(start.x - end.x, 0, 100) / 100;
			var startOffset = Vector2.Lerp(start + Vector2.right * size, new Vector2(start.x + 100, yMax),t );
			var endOffset= Vector2.Lerp(end + Vector2.left * size, new Vector2(end.x - 100, yMax), t);
			Handles.DrawBezier(start, end, startOffset, endOffset, color, null, 3f);
		}
        public void DrawCurve()
        {
            if (connectStartPort!=null)
            {
                var connectInfo = Graph.GetConnectInfo(connectStartPort);
                var color = GetTypeColor(Graph[connectStartPort].ConnectType);
                DrawCurve(connectInfo.rect.center, mousePos, color, Graph[connectStartPort].ConnectType==typeof(QFlow));
                DrawDot(mousePos - ViewRange.position, dotSize*0.8f, color);
                if (nearPortId != null)
                {
                    DrawDot(Graph.GetConnectInfo(nearPortId).rect.center - ViewRange.position, dotSize * 0.4f, color);
                }
            }
            foreach (var name in Graph.NodeList)
            {
                foreach (var port in name.Ports)
                {
                    if (port.isOutput )
                    {
                        var color = GetTypeColor(port.ConnectType);
                        foreach (var c in port.ConnectInfolist)
                        {
                            foreach (var connect in c.ConnectList)
                            {
                                var next = Graph.GetConnectInfo(connect);
                                if (next != null)
                                {
                                    DrawCurve(c.rect.center, next.rect.center, color, port.ConnectType==typeof(QFlow));
                                }
                            }
                        }
                      
                    }
                }
            }
        }
  

        Rect DrawDot(Vector2 center,float size,Color color)
        {
            var rect = new Rect();
            rect.size = Vector3.one * size;
            rect.center = center;
			QGUITool.SetColor(color);
            GUI.DrawTexture(rect, QGUITool.DotTexture2D);
			QGUITool.RevertColor();
			return rect;
        }
       
        void DrawPort(QFlowPort port)
        {
            curDrawPort = port;
            Rect lastRect = default;
            if (port.Key == QFlowKey.NextPort|| port.Key == QFlowKey.FromPort)
            {
                lastRect = new Rect(50, 5, windowRect.width - 100, 20);
            }
            else
            {
                if (port.ShowValue)
                {
                    if (typeof(UnityEngine.Object).IsAssignableFrom(port.ValueType))
                    {
                        port.stringValue = QObjectReferenceDrawer.Draw(port.ViewName, port.stringValue, port.ValueType);
                    }
                    else
                    {
                        if(port.FlowPort == null)
                        {
                            port.Value = port.Value.Draw(port.ViewName, port.ValueType, (obj) => {  port.Value = obj; },port.parameterInfo);
                        }
                        else
                        {
                            port.Value = port.Value.Draw(port.ViewName, port.ValueType,(obj)=> { port.Value = obj; },port.parameterInfo,  DrawFlowListDot,port.IndexChange);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(port.ViewName, port.isOutput ? QGUITool.RightLabel : QGUITool.LeftLable);
                }
                lastRect = GUILayoutUtility.GetLastRect();
            }
            if (port.isFlowList) return;
            DrawPortDot(lastRect, port.ConnectInfo, port.isOutput, port.ConnectType);
        }
        QFlowPort curDrawPort;
        void DrawFlowListDot(int i)
        {
            if (curDrawPort == null) return;
            DrawPortDot(GUILayoutUtility.GetLastRect(), curDrawPort[i],curDrawPort.isOutput, QFlow.Type);
        } 
        public void DrawPortDot(Rect rect,ConnectInfo port,bool isOutput,Type connectType)
        {
            var typeColor = GetTypeColor(connectType);
            Rect dotRect = default;
            if (isOutput)
            {
                var center = new Vector2(rect.xMax, rect.y) + Vector2.one * dotSize / 2;
                dotRect = DrawDot(center, dotSize, Color.black);
                DrawDot(center, dotSize * (port.ConnectList.Count==0? 0.9f : 0.7f), typeColor);
            }
            else
            {
                var center = rect.position + new Vector2(-dotSize, dotSize) / 2;
                var canConnect = connectStartPort != null &&Graph[connectStartPort].CanConnect(connectType);
                dotRect = DrawDot(center, dotSize * (canConnect ? 1 : 0.8f), typeColor);
                DrawDot(center, dotSize * (canConnect ? 0.8f : 0.7f), Color.black);
                if (port.ConnectList.Count > 0)
                {
                    DrawDot(center, dotSize * 0.6f, typeColor);
                }
            }
            if (Event.current.type == EventType.Repaint)
            {
                port.rect = new Rect(dotRect.position + windowRect.position, dotRect.size);
            }
        }

        #endregion

        void StartConnect(PortId? startPort)
        {
            if (startPort == null) return;
            ControlState = EditorState.ConnectPort;
            connectStartPort = startPort;
        }
        void StopConnect(PortId? endPort)
        {
            ControlState = EditorState.None;
            if (endPort != null)
            {
               Graph[connectStartPort].Connect(endPort,connectStartPort.Value.index);
            }
            else
            {
                CreateMenu(connectStartPort.Value);
            }
            connectStartPort = null;
        }
        PortId? connectStartPort;
        QDictionary<string, Color> KeyColor = new QDictionary<string, Color>();
   
        public Color GetTypeColor(Type type,float s=0.4f,float v=0.9f)
        {
			if (type == null) return Color.black;
            if (type == QFlow.Type) return Color.HSVToRGB(0.6f, s, v);
            return type.Name.ToColor(s,v);
        }
    
      
    }
	[CustomPropertyDrawer(typeof(QFlowGraph))]
	public class QFlowGraphDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var leftRect = position;
			leftRect.width /= 2;
			EditorGUI.LabelField(leftRect, label.text);
			leftRect.x += leftRect.width;
			if (property.serializedObject.targetObject.IsPrefabInstance()&&string.IsNullOrWhiteSpace(UnityEditor.SceneManagement.StageUtility.GetCurrentStage().assetPath))
			{
				if (GUI.Button(leftRect, "进入预制体编辑"))
				{
					UnityEditor.AssetDatabase.OpenAsset(property.serializedObject.targetObject.GetPrefab());
				}
			}
			else
			{
				if (GUI.Button(leftRect, "编辑"))
				{
					var graph = property.GetObject() as QFlowGraph;
					var path = property.propertyPath;
					var targetObject = property.serializedObject.targetObject;
					QFlowGraphWindow.Open(graph, () => { graph.Name = path; targetObject.SetDirty(); });
				}
			}
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return base.GetPropertyHeight(property, label);
		}
	}
}
