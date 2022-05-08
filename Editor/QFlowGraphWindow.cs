using QTool.Inspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
namespace QTool.FlowGraph
{

    public class QFlowGraphWindow : EditorWindow
    {
        const float dotSize = 16;
        static Texture2D DotTex => _dotTex ??= Resources.Load<Texture2D>("NodeEditorDot");
        static Texture2D _dotTex;
        static GUIStyle IutputPortStyle => _iutputPortStyle ??= new GUIStyle(EditorStyles.label);
        static GUIStyle _iutputPortStyle;
        static GUIStyle OutputPortStyle=> _outputPortStyle??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
        static GUIStyle _outputPortStyle;
        static Texture2D BackTex => _backTex ??= Resources.Load<Texture2D>("NodeEditorBackground");
        static Texture2D _backTex = null;

        public QFlowGraphAsset GraphAsset = null;
        [OnOpenAsset(0)]
        public static bool OnOpen(int instanceID, int line)
        {

            var asset = EditorUtility.InstanceIDToObject(instanceID) as QFlowGraphAsset;
            if (asset != null)
            {
                var window = GetWindow<QFlowGraphWindow>();
                window.minSize = new Vector2(500, 400);
                window.titleContent = new GUIContent(asset.name + " - 流图");
                window.GraphAsset = asset;
                window.ViewRange = new Rect(Vector2.zero, window.position.size);
                return true;
            }

            return false;
        }
        [MenuItem("Assets/Create/QTool/QFlowGraph", priority = 0)]
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

        private Rect ViewRange;
        void CreateMenu(QFlowPort fromPort)
        {

            GenericMenu menu = new GenericMenu();
            foreach (var info in QTool.Command.QCommand.KeyDictionary)
            {
                if (fromPort.CanConnect(info, out var portKey))
                {
                    menu.AddItem(new GUIContent(info.fullName), false, () =>
                    {
                        var node = GraphAsset.Graph.Add(info.Key, info.name);
                        node.rect = new Rect(mousePos, new Vector2(300, 80));
                        fromPort.Connect(node.Ports[portKey]);
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
                       var state = GraphAsset.Graph.Add(kv.Key, kv.name);
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
                if (curPort != null)
                {
                    menu.AddItem(new GUIContent("清空" + curPort.name + "端口连接"), false, curPort.ClearAllConnect);
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
                            QToolManager.Instance.StartCoroutine(GraphAsset.Graph.RunCoroutine(curNode.Key));
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
                GraphAsset.Graph.Parse(nodeList, mousePos);
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
            ForeachSelectNodes((node) => GraphAsset.Graph.Remove(node));
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
                if (curPort == null)
                {
                    nearPort = null;
                    var minDis = float.MaxValue;
                    foreach (var port in curNode.Ports)
                    {
                        var dis = Vector2.Distance(port.rect.position, mousePos);
                        if (connectStartPort.CanConnect(port))
                        {
                            if (dis < minDis)
                            {
                                nearPort = port;
                                minDis = dis;
                            }
                        }
                    }
                }
                else
                {
                    nearPort = curPort;
                }
            }
            else
            {
                nearPort = null;
            }

        }
        protected void UpdateCurrentData()
        {
            curNode = null;
            foreach (var state in GraphAsset.Graph.NodeList)
            {
                if (state.rect.Contains(mousePos))
                {
                    curNode = state;
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
        QFlowNode curNode;
        QFlowPort curPort;
        QFlowPort nearPort;
        private void OnGUI()
        {
            ViewRange.size = position.size;
            mousePos = Event.current.mousePosition + ViewRange.position;
            DrawBackground();
            if (GraphAsset == null||GraphAsset.Graph==null)
            {
                if (GUILayout.Button("创建新的QFlowGraph"))
                {
                    CreateNewFile();
                }
                return;
            }
            Controls();
            BeginWindows();
            for (int i = 0; i < GraphAsset.Graph.NodeList.Count; i++)
            {
                var node = GraphAsset.Graph.NodeList[i];
                if (node == null)
                {
                    Debug.LogError(i + "/" + GraphAsset.Graph.NodeList.Count);
                    continue;
                }
                if (ViewRange.Overlaps(node.rect))
                {
                    node.rect.position -= ViewRange.position;
                    var lastColor = GUI.backgroundColor;
                    GUI.backgroundColor = node.commandKey.ToColor(SelectNodes.Contains(node) ? 0.8f : 0.4f);
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
                        GUI.color = Color.black;
                        var box = SelectBox;
                        box.position -= ViewRange.position;
                        GUI.Box(box, "");
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
                            if (curPort != null)
                            {
                                if (curPort.isOutput)
                                {
                                    StartConnect(curPort);
                                }
                                else
                                {
                                    var fromPort = curPort.ConnectPort;
                                    if (fromPort != null)
                                    {
                                        fromPort.DisConnect(curPort);
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
                                }
                                break;
                            case EditorState.None:
                            case EditorState.MoveOffset:
                                if (Event.current.delta.magnitude < 100)
                                {
                                    ViewRange.position -= Event.current.delta;
                                    ControlState = EditorState.MoveOffset;
                                }
                                break;
                            case EditorState.ConnectPort:
                                UpdateNearPort();
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
                                        if (SelectBox.Overlaps(rect))
                                        {
                                            SelectNodes.Add(node);
                                        }
                                    }
                                }
                                break;
                            case EditorState.ConnectPort:
                                {
                                    StopConnect(nearPort);
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
                            case KeyCode.C:
                                if (Event.current.control)
                                {
                                    Copy();
                                }
                                break;
                            case KeyCode.V:
                                if (Event.current.control)
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
            var xTex = position.width / BackTex.width;
            var yTex = position.height / BackTex.height;
            var xStart = Fix(-ViewRange.x,-BackTex.width, 0, BackTex.width);
            var yStart = Fix(-ViewRange.y,-BackTex.height, 0, BackTex.height);
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
            var node = GraphAsset.Graph.NodeList[id];
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
        void DrawCurve(Vector2 start, Vector2 end,Color color)
        {
            if (!ViewRange.Contains(start) &&!ViewRange.Contains(end)) return;
            start -= ViewRange.position;
            end -= ViewRange.position;
            if (Vector3.Distance(start, end) < 0.1f)
            {
                return;
            }
            float size = Mathf.Abs(start.x - end.x) / 2;
            Handles.DrawBezier(start, end , start + Vector2.right * size, end + Vector2.left * size, color, null, 3f);
        }
        public void DrawCurve()
        {
            if (connectStartPort!=null)
            { 
                var color = GetTypeColor(connectStartPort.valueType);
                DrawCurve(connectStartPort.rect.center, mousePos, color);
                DrawDot(mousePos - ViewRange.position, dotSize*0.8f, color);
                if (nearPort != null)
                {
                    DrawDot(nearPort.rect.center - ViewRange.position, dotSize * 0.4f, color);
                }
            }
            foreach (var name in GraphAsset.Graph.NodeList)
            {
                foreach (var port in name.Ports)
                {
                    if (port.isOutput )
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
        void DrawPort(QFlowPort port)
        {
            var typeColor = GetTypeColor(port.valueType);

            Rect lastRect = default;
            if (port.Key == QFlowKey.NextPort|| port.Key == QFlowKey.FromPort)
            {
                lastRect = new Rect(50, 5, windowRect.width - 100, 20);
            }
            else
            {
                if (!port.isOutput && port.ConnectList.Count == 0 && port.valueType != null)
                {
                    if (typeof(UnityEngine.Object).IsAssignableFrom(port.valueType))
                    {
                        port.stringValue = QObjectReferenceDrawer.Draw(port.name, port.stringValue, port.valueType);
                    }
                    else
                    {
                        port.Value = port.Value.Draw(port.name, port.valueType);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(port.name, port.isOutput? OutputPortStyle: IutputPortStyle);
                }
                lastRect = GUILayoutUtility.GetLastRect();
            }
            Rect dotRect = default;
            if (port.isOutput)
            {
                var center = new Vector2(lastRect.xMax, lastRect.y) + Vector2.one * dotSize / 2;
                dotRect = DrawDot(center, dotSize, Color.black);
                DrawDot(center, dotSize * (port.ConnectPort == null ? 0.9f : 0.7f), typeColor);
            }
            else
            {
                var center = lastRect.position + new Vector2(-dotSize, dotSize) / 2;
                var canConnect = connectStartPort != null && connectStartPort.CanConnect(port);
                dotRect= DrawDot(center, dotSize * (canConnect ? 1 : 0.8f), typeColor);
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

        void StartConnect(QFlowPort startPort)
        {
            ControlState = EditorState.ConnectPort;
            connectStartPort = startPort;
        }
        void StopConnect(QFlowPort endPort)
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
        QFlowPort connectStartPort;
        QDictionary<string, Color> KeyColor = new QDictionary<string, Color>();
   
        public Color GetTypeColor(Type type,float s=0.4f,float v=0.9f)
        {
            if (type == null) return Color.HSVToRGB(0.6f, s, v);
            return type.Name.ToColor(s,v);
        }
    
      
    }

}