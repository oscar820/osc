using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace QTool.StateMachine
{
    public class QSMWindow : EditorWindow
    {
        static GUIStyle StateStyle=null;
       
        public QStateMachine qStateMachine = new QStateMachine();
        [MenuItem("Window/QStateMachine±à¼­Æ÷")]
        public static void ShowWindow()
        {
            var _window = GetWindow<QSMWindow>();
            _window.minSize = new Vector2(400, 400);
            _window.titleContent = new GUIContent("QStateMachine±à¼­Æ÷");

        }
        private Texture2D backTex = null;
        private Vector2 viewOffset;
        private void ShowMenu()
        {
            GenericMenu menu = new GenericMenu();
            if (selectState == null)
            {
                foreach (var kv in QTool.Command.QCommand.KeyDictionary)
                {
                    menu.AddItem(new GUIContent("´´½¨/" + kv.fullName), false, () =>
                    {
                        var state = qStateMachine.Add(kv.Key, kv.name);
                        state.windowRect = new Rect(mousePos.x, mousePos.y, 200, 80);
                        state.windowRect.position -= viewOffset;
                    });
                }
            }
            else
            {
                menu.AddItem(new GUIContent("É¾³ý"), false, () =>
                {
                    qStateMachine.Remove(selectState);
                });
            }
           
            menu.ShowAsContext();
        }
        protected QState GetMouseInNode()
        {
            foreach (var state in qStateMachine.StateList)
            {
                if (state.windowRect.Contains(mousePos-viewOffset))
                {
                    return state;
                }
            }
            return null;
        }
        Vector2 mousePos;
        QState selectState;
        private void OnGUI()
        {
            if (StateStyle == null)
            {
                StateStyle = new GUIStyle();
                StateStyle.border = new RectOffset(32, 32, 32, 32);
                StateStyle.padding = new RectOffset(16, 16, 4, 16);
            }
            mousePos = Event.current.mousePosition;
            selectState= GetMouseInNode();
            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    {
                        if (Event.current.button == 1)
                        {
                            ShowMenu();
                            Event.current.Use();
                        }
                    }
                    break;
                case EventType.MouseDrag:
                    {
                        if (selectState == null)
                        {
                            viewOffset += Event.current.delta;
                        }
                    }
                    break;
                default:
                    break;
            }
            if (backTex == null)
            {
                backTex = Resources.Load<Texture2D>("NodeEditorBackground");
            }
            var xTex = position.width / backTex.width;
            var yTex = position.height / backTex.height;
            var xStart = viewOffset.x.Fix(-backTex.width, 0, backTex.width);
            var yStart = viewOffset.y.Fix(-backTex.height, 0, backTex.height);
            for (int x = 0; x <= xTex + 1; x++)
            {
                for (int y = 0; y <= yTex + 1; y++)
                {
                    GUI.DrawTexture(new Rect(xStart + backTex.width * x, yStart + backTex.height * y, backTex.width, backTex.height), backTex);
                    Repaint();
                }
            }
            BeginWindows();
            for (int i = 0; i < qStateMachine.StateList.Count; i++)
            {
                var state = qStateMachine.StateList[i];
                state.windowRect.position += viewOffset;
                GUI.backgroundColor = Color.green;
                GUI.contentColor = Color.white;
                
                state.windowRect = GUI.Window(i, state.windowRect, DrawState, state.name);
                state.windowRect.position -= viewOffset;
            }
            EndWindows();
        }
        void DrawState(int id)
        {
            var state = qStateMachine.StateList[id];
            GUI.backgroundColor = Color.white;
            EditorGUILayout.BeginVertical();
            foreach (var port in state.Ports)
            {
                EditorGUILayout.LabelField(port.Key);
            }
            EditorGUILayout.EndVertical();
            if(Event.current.type== EventType.Repaint)
            {
                state.windowRect.height = Mathf.Max(state.windowRect.height, GUILayoutUtility.GetLastRect().height + 25);
            }
            GUI.DragWindow();

        }
    }
    public static class ViewTool
    {
        internal static float Fix(this float pos, float min, float max, float fixStep)
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
        internal static void DrawToChildCurve(this QState state)
        {
          
        }
    }
    public class QStateView
    {

    }
}