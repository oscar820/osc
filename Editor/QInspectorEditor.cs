using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using QTool.Reflection;
namespace QTool.Inspector
{
    #region 自定义显示效果
    public class PropertyDrawBase<T> : PropertyDrawer where T : PropertyAttribute
    {
        public T att
        {
            get
            {
                return attribute as T;
            }
        }

    }
    public class DecoratorDrawBase<T> : DecoratorDrawer where T : PropertyAttribute
    {
        public T att
        {
            get
            {
                return attribute as T;
            }
        }

    }
    public class QStylesWindows : EditorWindow
    {

        private Vector2 scrollVector2 = Vector2.zero;
        private string search = "";

        [MenuItem("Window/GUIStyle查看器")]
        public static void InitWindow()
        {
            EditorWindow.GetWindow(typeof(QStylesWindows));
        }

        void OnGUI()
        {

            GUILayout.BeginHorizontal("HelpBox");
            GUILayout.Space(30);
            search = EditorGUILayout.TextField("", search, "SearchTextField", GUILayout.MaxWidth(position.x / 3));
            GUILayout.Label("", "SearchCancelButtonEmpty");
            GUILayout.EndHorizontal();
            scrollVector2 = GUILayout.BeginScrollView(scrollVector2);
            foreach (GUIStyle style in GUI.skin.customStyles)
            {
                if (style.name.ToLower().Contains(search.ToLower()))
                {
                    DrawStyleItem(style);
                }
            }
            GUILayout.EndScrollView();
        }

        void DrawStyleItem(GUIStyle style)
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Space(40);
            EditorGUILayout.SelectableLabel(style.name);
            GUILayout.Space(40);
            GUILayout.FlexibleSpace();
            EditorGUILayout.SelectableLabel(style.name, style);
            GUILayout.Space(40);
            EditorGUILayout.SelectableLabel("", style, GUILayout.Height(40), GUILayout.Width(40));
            GUILayout.Space(50);
            if (GUILayout.Button("复制GUIStyle名字"))
            {
                TextEditor textEditor = new TextEditor();
                textEditor.text = style.name;
                textEditor.OnFocus();
                textEditor.Copy();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }
    }
    [CustomPropertyDrawer(typeof(ViewNameAttribute))]
    public class ViewNameAttributeDrawer : PropertyDrawBase<ViewNameAttribute>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.Draw(position, att.name);
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.GetHeight();
        }
    }
    [CustomPropertyDrawer(typeof(ViewToggleAttribute))]
    public class ViewToggleAttributeDrawer : PropertyDrawBase<ViewToggleAttribute>
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Boolean)
            {
                position.height = att.height;
                var boxstyle = new GUIStyle();
                var back = new Texture2D(1, 1); ;
                var color = GUI.color;
                GUI.color = property.boolValue ? Color.grey : Color.white;
                boxstyle.normal.background = back;
                property.boolValue = EditorGUI.Toggle(position, property.boolValue, boxstyle);
                GUI.color = color;
                var style = EditorStyles.largeLabel;
                style.alignment = TextAnchor.MiddleCenter;
                EditorGUI.LabelField(position, att.name, style);

            }
            else
            {
                property.Draw(position, att.name);
            }

        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Boolean)
            {
                return att.height;
            }
            else
            {
                return property.GetHeight();
            }
        }
    }


    [CustomPropertyDrawer(typeof(ChangeCallAttribute))]
    public class ChangeCallAttributeDrawer : PropertyDrawBase<ChangeCallAttribute>
    {
        private bool Change = false;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (Change)
            {
                property.Call(att.changeCallBack);
                Change = false;
            }
            EditorGUI.BeginChangeCheck();
            property.Draw(position, att.name + "[回调]");
            EditorGUI.EndChangeCheck();
            if (GUI.changed)
            {
                Change = true;
            }
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.GetHeight();
        }
    }
    [CustomPropertyDrawer(typeof(ViewEnumAttribute))]
    public class ViewEnumAttributeDrawer : PropertyDrawBase<ViewEnumAttribute>
    {
        public List<string> enumList = null;
        public int selectIndex = 0;
        public string selectValue
        {
            get
            {
                return enumList[selectIndex];
            }
        }
        public void UpdateList(string input)
        {
            selectIndex = enumList.IndexOf(input);
            if (selectIndex < 0)
            {
                selectIndex = 0;
            }
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            if (property.propertyType == SerializedPropertyType.String)
            {
                if (enumList == null)
                {
                    var list = property.Call<IList<string>>(att.GetKeyListFunc);

                    enumList = new List<string>();
                    if (att.CanWriteString)
                    {
                        enumList.Add("【不存在】");
                    }

                    if (list != null)
                    {
                        enumList.AddRange(list);
                    }
                    UpdateList(property.stringValue);
                }
                EditorGUI.LabelField(position.HorizontalRect(0f, 0.3f), att.name);
                property.stringValue = EditorGUI.TextField(position.HorizontalRect(0.4f, 0.7f), property.stringValue);
                if (GUI.changed)
                {
                    UpdateList(property.stringValue);
                }
                selectIndex = EditorGUI.Popup(position.HorizontalRect(0.7f, 1), selectIndex, enumList.ToArray());
                if (GUI.changed)
                {
                    if (selectIndex != 0)
                    {
                        property.stringValue = selectValue;
                    }
                }

            }

            // EditorGUI.BeginChangeCheck();
            // property.Draw(position, att.name );

            //EditorGUI.PropertyField(rect, property, new GUIContent(viewName), property.isExpanded);
            // EditorGUI.EndChangeCheck();
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.GetHeight();
        }
    }
    [CustomPropertyDrawer(typeof(TitleAttribute))]
    public class TitleAttributeDrawer : DecoratorDrawBase<TitleAttribute>
    {
        public static GUIStyle style = new GUIStyle
        {

            fontSize = 10,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = new GUIStyleState
            {
                background = Texture2D.blackTexture,

            },

        };

        public override void OnGUI(Rect position)
        {
            var falg = GUI.enabled;
            GUI.enabled = true;
            var titleRect = position;
            titleRect.y += 10;
            titleRect.height = att.height;
            GUI.Label(titleRect, att.title, style);
            titleRect.y += 4;
            titleRect.height -= 4;
            GUI.Label(titleRect, "__________________________________", style);
            GUI.enabled = falg;
        }
        public override float GetHeight()
        {
            return att.height + 10;
        }
    }
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyAttributeDrawer : PropertyDrawBase<ReadOnlyAttribute>
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            property.Draw(position, property.ViewName() + "[只读]");
            GUI.enabled = true;
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.GetHeight();
        }
    }
    #endregion
    public static class QEditorTool
    {
        public static Vector3 LineCastPlane(Vector3 point, Vector3 direct, Vector3 planeNormal, Vector3 planePoint)
        {
            float d = Vector3.Dot(planePoint - point, planeNormal) / Vector3.Dot(direct.normalized, planeNormal);

            return d * direct.normalized + point;
        }
        public static Rect HorizontalRect(this Rect rect, float left, float right)
        {
            var leftOffset = left * rect.width;
            var width = (right - left) * rect.width;
            rect.x += leftOffset;
            rect.width = width;
            return rect;
        }
        public static bool HasAttribute<T>(this SerializedProperty prop)
        {
            object[] attributes = GetAttributes<T>(prop);
            if (attributes != null)
            {
                return attributes.Length > 0;
            }
            return false;
        }

        public static object[] GetAttributes<T>(this SerializedProperty prop)
        {
            object obj = prop.serializedObject.targetObject;
            if (obj == null)
                return new object[0];

            Type objType = obj.GetType();
            const BindingFlags bindingFlags = System.Reflection.BindingFlags.GetField
                                              | System.Reflection.BindingFlags.GetProperty
                                              | System.Reflection.BindingFlags.Instance
                                              | System.Reflection.BindingFlags.NonPublic
                                              | System.Reflection.BindingFlags.Public;
            FieldInfo field = objType.GetField(prop.name, bindingFlags);
            if (field != null)
            {
                return field.GetCustomAttributes(typeof(T), true);
            }
            return new object[0];
        }
        public static T GetAttribute<T>(this SerializedProperty prop) where T : Attribute
        {
            object[] attributes = GetAttributes<T>(prop);
            if (attributes.Length > 0)
            {
                return attributes[0] as T;
            }
            else
            {
                return null;
            }
        }

        public static string ViewName(this SerializedProperty property)
        {
            var att = property.GetAttribute<ViewNameAttribute>();
            if (att != null && att.name != "")
            {
                return att.name;
            }
            else
            {
                return property.displayName;
            }
        }
        public static T Call<T>(this SerializedProperty property, string funcName) where T : class
        {
            return Call(property, funcName) as T;
        }
        public static object Call(this SerializedProperty property, string funcName, object[] paramsList = null)
        {
            if (string.IsNullOrEmpty(funcName))
            {
                return null;
            }
            object obj = property.serializedObject.targetObject;
            Type objType = obj.GetType();
            var binding = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var method = objType.GetMethod(funcName, binding);
            if (method == null)
            {
                return null;
            }
            return method?.Invoke(obj, paramsList);
        }
        public static void DrawLayout(this SerializedProperty property)
        {

            if (property.HasAttribute<ReadOnlyAttribute>())
            {
                GUI.enabled = false;
                EditorGUILayout.PropertyField(property, new GUIContent(property.ViewName()), true);
                GUI.enabled = true;
            }
            else
            {

                EditorGUILayout.PropertyField(property, new GUIContent(property.ViewName()), true);
            }


        }
        public static bool DrawToogleButton(this bool value, GUIContent guiContent, GUIStyle backStyle)
        {
            var style = EditorStyles.largeLabel;
            style.alignment = TextAnchor.MiddleCenter;
            var returnValue = EditorGUILayout.Toggle(value, backStyle);
            var rect = GUILayoutUtility.GetLastRect();
            EditorGUI.LabelField(rect, guiContent, style);
            return returnValue;
        }
        public static bool Draw(this SerializedProperty property, Rect rect, string viewName)
        {
            return EditorGUI.PropertyField(rect, property, new GUIContent(viewName), property.isExpanded);
        }

        public static float GetHeight(this SerializedProperty property)
        {
            return EditorGUI.GetPropertyHeight(property, GUIContent.none, property.isExpanded);
        }

        public static Bounds GetBounds(this GameObject obj)
        {
            var bounds = new Bounds(obj.transform.position, Vector3.zero);
            Renderer[] meshs = obj.GetComponentsInChildren<Renderer>();
            foreach (var mesh in meshs)
            {
                if (mesh)
                {
                    if (bounds.extents == Vector3.zero)
                    {
                        bounds = mesh.bounds;
                    }
                    else
                    {
                        bounds.Encapsulate(mesh.bounds);
                    }
                }
            }
            return bounds;
        }
        public static QMemeberInfo GetMember(this object target,string key)
        {
            return QInspectorType.Get(target.GetType()).Members[key];
        }
        public static bool IsShow(this QEditorAttribute att, object target)
        {
            if (string.IsNullOrEmpty(att.showControl))
            {
                return true;
            }
            else
            {
                return (bool)target.GetMember(att.showControl).Get(target);
            }
        }
    }
    public class QInspectorType : QTypeInfo<QInspectorType>
    {
        public QDictionary<EidtorInitInvokeAttribute, QFunctionInfo> initFunc = new QDictionary<EidtorInitInvokeAttribute, QFunctionInfo>();
        public QDictionary<SceneMouseEventAttribute, QFunctionInfo> mouseEventFunc = new QDictionary<SceneMouseEventAttribute, QFunctionInfo>();
        public QDictionary<ViewButtonAttribute, QFunctionInfo> buttonFunc = new QDictionary<ViewButtonAttribute, QFunctionInfo>();

        protected override void Init(Type type)
        {
            MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic|BindingFlags.Static;
            FunctionFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic|BindingFlags.Static;
            base.Init(type);
            foreach (var funcInfo in Functions)
            {
                foreach (var att in funcInfo.MethodInfo.GetCustomAttributes<SceneMouseEventAttribute>())
                {
                    mouseEventFunc[att] = funcInfo;
                }
                foreach (var att in funcInfo.MethodInfo.GetCustomAttributes<ViewButtonAttribute>())
                {
                    buttonFunc[att] = funcInfo;
                }
                foreach (var att in funcInfo.MethodInfo.GetCustomAttributes<ContextMenu>())
                {
                    buttonFunc[new ViewButtonAttribute(att.menuItem)] = funcInfo;
                }
                foreach (var att in funcInfo.MethodInfo.GetCustomAttributes<EidtorInitInvokeAttribute>())
                {
                    initFunc[att] = funcInfo;
                }
            }
        }
    }
    [CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
    [CanEditMultipleObjects]
    public class QInspectorEditor : Editor
    {
        Vector2 previewDir;
        Quaternion previewRotation
        {
            get
            {
                return Quaternion.Euler(-previewDir.y, previewDir.x, 0);
            }
        }
        public QInspectorType typeInfo;
        private void OnEnable()
        {
            typeInfo= QInspectorType.Get(target.GetType());

            foreach (var kv in typeInfo.initFunc)
            {
                kv.Value.Invoke(target);
            }
        }
        RaycastHit hit;
        public void MouseCheck()
        {
            if (typeInfo.mouseEventFunc.Count <= 0) return;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            Event input = Event.current;
            if (input.isMouse && !input.alt)
            {

                if (input.button == 0)
                {
                    var mouseRay = HandleUtility.GUIPointToWorldRay(input.mousePosition);
                    Vector3 point = Vector3.zero;
                    if (Physics.Raycast(mouseRay, out hit))
                    {
                        point = hit.point;
                    }
                    else
                    {
                        point = QEditorTool.LineCastPlane(mouseRay.origin, mouseRay.direction, Vector3.up, Vector3.zero);
                    }

                    foreach (var kv in typeInfo.mouseEventFunc)
                    {
                        if (input.type == kv.Key.eventType)
                        {
                            kv.Value.Invoke(target, point, hit, input.shift);
                            input.Use();
                        }
                    }
                }
            }
        }
        private void OnSceneGUI()
        {
            MouseCheck();
        }
       
        public Dictionary<string, ReorderableList> listArray = new Dictionary<string, ReorderableList>();
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawAllProperties(serializedObject);
            DrawButton();
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedProperties();
            }
        }
     
        public void DrawButton()
        {
            foreach (var kv in typeInfo.buttonFunc)
            {
                var att = kv.Key;
                if (att.IsShow(target))
                {
                    if (att is SelectObjectButtonAttribute)
                    {
                        if (GUILayout.Button(att.name, GUILayout.Height(att.height)))
                        {
                            EditorGUIUtility.ShowObjectPicker<GameObject>(null, false, "", att.name.GetHashCode());

                        }
                        if (Event.current.commandName == "ObjectSelectorClosed")
                        {
                            if (EditorGUIUtility.GetObjectPickerControlID() == att.name.GetHashCode())
                            {
                                var obj = EditorGUIUtility.GetObjectPickerObject();
                                if (obj != null)
                                {
                                    kv.Value.Invoke(target, obj);
                                }
                                pickId = -1;


                            }
                        }
                    }
                    else
                    {
                        if (GUILayout.Button(att.name, GUILayout.Height(att.height)))
                        {
                            kv.Value.Invoke(target);
                        }
                    }

                }
            
            }
        }
        public void DrawAllProperties(SerializedObject serializedObject)
        {
            DrawPropertyIter(serializedObject.GetIterator());
        }
        public void DrawPropertyIter(SerializedProperty propertyIter)
        {
            if (propertyIter.NextVisible(true))
            {
                do
                {
                    DrawProperty(propertyIter.Copy());
                } while (propertyIter.NextVisible(false));
            }

        }
     

        #region 将数组数据显示成工具栏
        public bool DrawToolbar(SerializedProperty property)
        {
            var toolbar = property.GetAttribute<ToolbarListAttribute>();
            if (toolbar != null)
            {
                if (!toolbar.IsShow(target))
                {
                    return true;
                }
                var listMember = target.GetMember(toolbar.listMember);
                var info = target.GetType().GetField(property.name);

                var GuiList = new List<GUIContent>();
                IList list = null;
                try
                {
                    var obj = listMember.Get(target);
                    list = obj as IList;
                    if (list == null) throw new Exception();
                }
                catch (Exception)
                {
                    GUILayout.Box("无法列表从【" + toolbar.listMember + "】");
                    return false;
                }

                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] != null)
                    {
                        if (list[i] is UnityEngine.Object)
                        {
                            var uObj = list[i] as UnityEngine.Object;
                            var texture = AssetPreview.GetAssetPreview(uObj);
                            GuiList.Add(new GUIContent(texture, uObj.name));
                        }
                        else
                        {
                            GuiList.Add(new GUIContent(list[i].ToString()));
                        }
                    }
                    else
                    {
                        GuiList.Add(new GUIContent("空"));
                    }

                }
                property.intValue = GUILayout.Toolbar(property.intValue, GuiList.ToArray(), GUILayout.Height(toolbar.height));

                return true;

            }
            return false;
        }
        #endregion

        #region 将数组数据显示成可选项
        public bool DrawToggleList(SerializedProperty property)
        {
            var att = property.GetAttribute<ToggleListAttribute>();
            if (att != null)
            {
                if (!att.IsShow(target))
                {
                    return true;
                }
                var getFunc = target.GetType().GetMethod(att.valueGetFunc);
                var setFunc = target.GetType().GetMethod(att.valueSetFunc);

                var info = target.GetMember(property.name);

                var GuiList = new List<GUIContent>();
                var list = (info.Get(target) as IList);

                if (getFunc != null && setFunc != null)
                {
                    GUILayout.BeginHorizontal();
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i] != null)
                        {
                            if (list[i] is UnityEngine.Object)
                            {
                                var uObj = list[i] as UnityEngine.Object;
                                var texture = AssetPreview.GetAssetPreview(uObj);
                                GuiList.Add(new GUIContent(texture, uObj.name));
                            }
                            else
                            {
                                GuiList.Add(new GUIContent(list[i].ToString()));
                            }
                        }
                        else
                        {
                            GuiList.Add(new GUIContent("空"));
                        }
                        var value = (bool)getFunc.Invoke(target, new object[] { list[i] });
                        var style = EditorStyles.miniButton;
                        setFunc.Invoke(target, new object[] { list[i], value.DrawToogleButton(GuiList[i], style) });
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Box("无法获取函数【" + att.valueGetFunc + "】【" + att.valueSetFunc + "】");
                }
                return true;

            }
            return false;
        }
        #endregion

        #region 对数组数据进行显示
        public void DrawArrayProperty(SerializedProperty property)
        {
            if (DrawToggleList(property)) return;
            property.DrawLayout();
        }
        #endregion

        public int pickId = -1;
     

        public void DrawProperty(SerializedProperty property)
        {
            if (property.name.Equals("m_Script"))
            {
                GUI.enabled = false;
                EditorGUILayout.PropertyField(property, true);
                GUI.enabled = true;
            }
            else
            {
                if (property.isArray)
                {
                    DrawArrayProperty(property);
                }
                else
                {
                    if (DrawToolbar(property)) return;
                    property.DrawLayout();
                }

            }
        }
    }
}
