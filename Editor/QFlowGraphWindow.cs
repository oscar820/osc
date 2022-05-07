using QTool.Inspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
namespace QTool.Flow
{
    
    public class QFlowGraphWindow : EditorWindow
    {
        const float dotSize = 16;
        static Texture2D DotTex => _dotTex ??= Resources.Load<Texture2D>("NodeEditorDot");
        static Texture2D _dotTex;
        static GUIStyle OutputPortStyle => _outputPortStyle ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
        static GUIStyle _outputPortStyle;

        static Texture2D BackTex => _backTex ??= Resources.Load<Texture2D>("NodeEditorBackground");
        static Texture2D _backTex = null;

        public QFlowGraphAsset GraphAsset = null;
        [OnOpenAsset(0)]
        public static bool OnOpen(int instanceID, int line)
        {
           
            var asset= EditorUtility.InstanceIDToObject(instanceID) as QFlowGraphAsset;
            if (asset != null)
            {
                var window = GetWindow<QFlowGraphWindow>();
                window.minSize = new Vector2(500, 400);
                window.titleContent = new GUIContent(asset.name+" - 流图");
                window.GraphAsset = asset;
                window.viewOffset = Vector2.zero;
                return true;
            }
            return false;
        }
        [MenuItem("Assets/Create/QTool/QFlowGraph",priority = 0)]
        public static void CreateNewFile()
        {
            FileManager.Save(EditorUtility.SaveFilePanel("保存QFG文件", Application.dataPath, nameof(QFlowGraphAsset), "qfg"), (new QFlowGraph()).ToQData());
            AssetDatabase.Refresh();
        }
        private void OnFocus()
        {
        }
        private void OnLostFocus()
        {
            if (GraphAsset != null)
            {
                GraphAsset.Save();
            }
        }
       
        private Vector2 viewOffset;
        void CreateMenu(FlowPort fromPort)
        {
            
            GenericMenu menu = new GenericMenu();
            foreach (var info in QTool.Command.QCommand.KeyDictionary)
            {
                if (fromPort.CanConnect(info,out var portKey))
                {
                    menu.AddItem(new GUIContent(info.fullName), false, () =>
                    {
                        var state = GraphAsset.Graph.Add(info.Key, info.name);
                        state.rect = new Rect(mousePos.x, mousePos.y, 300, 80);
                        state.rect.position -= viewOffset;
                        fromPort.Connect(state[portKey]);
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
                    menu.AddItem(new GUIContent( kv.fullName), false, () =>
                    {
                        var state = GraphAsset.Graph.Add(kv.Key, kv.name);
                        state.rect = new Rect(mousePos.x, mousePos.y, 300, 80);
                        state.rect.position -= viewOffset;
                    });
                }
                menu.AddSeparator("1");
                menu.AddItem(new GUIContent("复制"), false, Copy);
                menu.AddItem(new GUIContent("粘贴"), false, Parse);
            }
            else
            {
                menu.AddItem(new GUIContent("删除"), false, DeleteSelectNodes);
                menu.AddItem(new GUIContent("清空连接"), false, DeleteSelectNodes);
               
                if (Application.isPlaying)
                {
                    menu.AddItem(new GUIContent("运行节点"), false, () =>
                    {
                        QToolManager.Instance.StartCoroutine(GraphAsset.Graph.Run(curNode.Key));
                    });
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
            foreach (var node in SelectNodes)
            {
                foreach (var port in node.Ports)
                {
                    port.ConnectList.RemoveAll((id) =>
                    {
                        var node = SelectNodes.Get(id.node);
                        if (node == null) return true;
                        if (node[id.port] == null) return true;
                        return false;
                    });
                }
            }
            GUIUtility.systemCopyBuffer = SelectNodes.ToQData();
        }
        void Parse()
        {
            try
            {
                var nodeList = GUIUtility.systemCopyBuffer.ParseQData<List<FlowNode>>();
                GraphAsset.Graph.AddRange(nodeList.ToArray());
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
            ForeachSelectNodes((node) =>GraphAsset.Graph.Remove(node));
        }
        void ForeachSelectNodes(System.Action<FlowNode> action)
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
        protected void UpdateCurrentData()
        {
            curNode = null;
            foreach (var state in GraphAsset.Graph.NodeList)
            {
                if (state.rect.Contains(mousePos-viewOffset))
                {
                    curNode= state;
                    break;
                }
            }
            curPort = null;
            if (curNode != null)
            {
                foreach (var port in curNode.Ports)
                {
                    if (port.rect.Contains(mousePos))
                    {
                        curPort = port;
                        break;
                    }
                }
            }

        }
        Vector2 mousePos;
        FlowNode curNode;
        FlowPort curPort;
        private void OnGUI()
        {
            mousePos = Event.current.mousePosition;
            DrawBackground();
            Controls();
            if (GraphAsset == null)
            {
                if (GUILayout.Button("创建新的QFlowGraph"))
                {
                    CreateNewFile();
                }
                return;
            }
            BeginWindows();
            for (int i = 0; i < GraphAsset.Graph.NodeList.Count; i++)
            {
                var node = GraphAsset.Graph.NodeList[i];
                if (node == null)
                {
                    Debug.LogError(i + "/" + GraphAsset.Graph.NodeList.Count);
                    continue;
                }
                node.rect.position += viewOffset;
                var lastColor = GUI.backgroundColor;
                GUI.backgroundColor =node.commandKey.ToColor(SelectNodes.Contains(node)?0.8f:0.4f);
                node.rect = GUI.Window(i, node.rect, DrawNode, node.name);
                GUI.backgroundColor = lastColor;
                node.rect.position -= viewOffset;
            }
            EndWindows();
            DrawCurve();
            if(ControlState == EditorState.BoxSelect)
            {
                GUI.color= Color.black;
                GUI.Box(SelectBox, "");
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
        List<FlowNode> SelectNodes = new List<FlowNode>();
        void Controls()
        {
            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    {
                        UpdateCurrentData();
                        if(ControlState== EditorState.None)
                        {
                            if (curPort != null)
                            {
                                if (curPort.portType == PortType.Input)
                                {
                                    var fromPort = curPort.ConnectPort;
                                    if (fromPort != null)
                                    {
                                        fromPort.DisConnect(curPort);
                                        StartConnect(fromPort);
                                    }
                                }
                                else
                                {
                                    StartConnect(curPort);
                                }
                                Event.current.Use();
                            }
                            else if (Event.current.button == 0)
                            {
                                if (curNode == null)
                                {
                                    StartPos = Event.current.mousePosition;
                                    SelectBox = new Rect(StartPos, Vector2.zero);
                                    ControlState = EditorState.BoxSelect;
                                }
                                else
                                {
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
                                    var endPos = Event.current.mousePosition;
                                    SelectBox = new Rect(Mathf.Min(StartPos.x, endPos.x), Mathf.Min(StartPos.y, endPos.y), Mathf.Abs(StartPos.x - endPos.x), Mathf.Abs(StartPos.y - endPos.y));
                                }
                                break;
                            case EditorState.None:
                            case EditorState.MoveOffset:
                                viewOffset += Event.current.delta;
                                ControlState = EditorState.MoveOffset;
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
                                    foreach (var node in GraphAsset.Graph.NodeList)
                                    {
                                        var rect = node.rect;
                                        rect.position += viewOffset;
                                        if (SelectBox.Overlaps(rect))
                                        {
                                            SelectNodes.Add(node);
                                        }
                                    }
                                }
                                break;
                            case EditorState.ConnectPort:
                                {
                                    StopConnect(curPort);
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
                            case KeyCode.Space:
                                ShowMenu();
                                break;
                            case KeyCode.Delete:
                                DeleteSelectNodes();
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
            var xTex = position.width / BackTex.width;
            var yTex = position.height / BackTex.height;
            var xStart = Fix(viewOffset.x,-BackTex.width, 0, BackTex.width);
            var yStart = Fix(viewOffset.y,-BackTex.height, 0, BackTex.height);
            for (int x = 0; x <= xTex + 1; x++)
            {
                for (int y = 0; y <= yTex + 1; y++)
                {
                    GUI.DrawTexture(new Rect(xStart + BackTex.width * x, yStart + BackTex.height * y, BackTex.width, BackTex.height), BackTex);
                    Repaint();
                }
            }
        }
        Rect windowRect;
        void DrawNode(int id)
        {
            var state = GraphAsset.Graph.NodeList[id];
            windowRect = state.rect;
            windowRect.position += viewOffset;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(dotSize);
            EditorGUILayout.BeginVertical();
            foreach (var port in state.Ports)
            {
                DrawPort(port);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(dotSize);
            EditorGUILayout.EndHorizontal();
            if (Event.current.type== EventType.Repaint)
            {
                state.rect.height = GUILayoutUtility.GetLastRect().height + 25;
            }
            GUI.DragWindow();

        }
        static void DrawCurve(Vector3 start, Vector3 end,Color color)
        {
            if (Vector3.Distance(start, end) < 0.1f)
            {
                return;
            }
            float size = Mathf.Abs(start.x - end.x) / 2;
            Handles.DrawBezier(start, end, start + Vector3.right * size, end + Vector3.left * size, color, null, 3f);
        }
        public void DrawCurve()
        {
            if (connectStartPort!=null)
            {
                var color = GetTypeColor(connectStartPort.valueType);
                DrawCurve(connectStartPort.rect.center, Event.current.mousePosition, color);
                DrawDot(Event.current.mousePosition, dotSize*0.8f, color);
            }
            foreach (var state in GraphAsset.Graph.NodeList)
            {
                foreach (var port in state.Ports)
                {
                    if (port.portType == PortType.Output)
                    {
                        var color = GetTypeColor(port.valueType);

                        foreach (var connect in port.ConnectList)
                        {
                            var next = GraphAsset.Graph[connect];
                            if (next != null)
                            {
                                DrawCurve(port.rect.center, next.rect.center, color);
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
            Color col = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, DotTex);
            GUI.color = col;
            return rect;
        }
        void DrawPort(FlowPort port)
        {
            var typeColor = GetTypeColor(port.valueType);
            if(port.portType == PortType.Input)
            {
                Rect lastRect = default;
                if (port.Key == QFlowKey.FromPort)
                {
                    lastRect = new Rect(50, 5, windowRect.width,20);
                }
                else
                {
                    if (port.ConnectList.Count== 0||typeof(UnityEngine.Object).IsAssignableFrom(port.valueType))
                    {
                        port.SetValue(port.GetValue().Draw(port.Key, port.valueType));
                    }
                    else
                    {
                        EditorGUILayout.LabelField(port.Key);
                    }
                    lastRect = GUILayoutUtility.GetLastRect();
                }
                var center = lastRect.position + new Vector2(-dotSize, dotSize) / 2;
                var canConnect = connectStartPort != null && connectStartPort.CanConnect(port);
                var dotRect = DrawDot(center, dotSize*(canConnect?1:0.8f), typeColor);
                if (Event.current.type == EventType.Repaint)
                {
                    port.rect = new Rect(dotRect.position + windowRect.position, dotRect.size);
                }
                DrawDot(center, dotSize *(canConnect? 0.8f:0.7f), Color.black);
                if (port.ConnectList.Count > 0)
                {
                    DrawDot(center, dotSize * 0.6f, typeColor);
                }
               
            }
            else
            {
                Rect lastRect = default;
                if (port.Key== QFlowKey.NextPort)
                {
                    lastRect = new Rect(0, 5, windowRect.width-50, 20);
                }
                else
                {
                    EditorGUILayout.LabelField(port.Key, OutputPortStyle);
                    lastRect = GUILayoutUtility.GetLastRect();
                }
                var center = new Vector2(lastRect.xMax, lastRect.y) + Vector2.one * dotSize / 2;
                var dotRect= DrawDot(center, dotSize, Color.black);
                DrawDot(center, dotSize*(port.ConnectPort==null? 0.9f:0.7f), typeColor);
                if (Event.current.type == EventType.Repaint)
                {
                    port.rect = new Rect(dotRect.position + windowRect.position, dotRect.size);
                }
                
            }
           
        }

        #endregion

        void StartConnect(FlowPort startPort)
        {
            ControlState = EditorState.ConnectPort;
            connectStartPort = startPort;
        }
        void StopConnect(FlowPort endPort)
        {
            ControlState = EditorState.None;
            if (endPort != null)
            {
               connectStartPort.Connect(endPort);
            }
            else
            {
                CreateMenu(connectStartPort);
            }
            connectStartPort = null;
        }
        FlowPort connectStartPort;
        QDictionary<string, Color> KeyColor = new QDictionary<string, Color>();
   
        public Color GetTypeColor(Type type,float s=0.4f,float v=0.9f)
        {
            if (type == null) return Color.HSVToRGB(0.6f, s, v);
            return type.Name.ToColor(s,v);
        }
    
      
    }

}