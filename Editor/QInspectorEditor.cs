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
    [CustomPropertyDrawer(typeof(InstanceReference))]
    public class Fix64Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var value = property.FindPropertyRelative("id");
            var objValue = property.FindPropertyRelative("_obj");
            if (QId.InstanceIdList.ContainsKey(value.stringValue) && objValue.objectReferenceValue==null)
            {
                objValue.objectReferenceValue = QId.InstanceIdList[value.stringValue].gameObject;
            }
            var left = position;
            left.width = position.width * 0.3f;
            var right = position;
            right.xMin = left.xMin+left.width;
            right.width = position.width * 0.7f;
            EditorGUI.LabelField(left, label.text + "  ["+ value.stringValue+"]");
            objValue.objectReferenceValue = EditorGUI.ObjectField(right, objValue.objectReferenceValue, typeof(GameObject), true) as GameObject;
            if (objValue.objectReferenceValue != null)
            {
                var id = (objValue.objectReferenceValue as GameObject).GetComponent<QId>();
                if (id == null)
                {
                    id = (objValue.objectReferenceValue as GameObject).AddComponent<QId>();
                    EditorUtility.SetDirty((objValue.objectReferenceValue as GameObject));
                }
                value.stringValue = id.InstanceId;
            }
            else
            {
                value.stringValue = "";
            }
          
          
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label);
        }
    }
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
            if (property.IsShow())
            {
                property.Draw(position, att.name);
            }
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.IsShow())
            {
                return property.GetHeight();
            }
            else
            {
                return 0;
            }
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
                GUI.color = property.boolValue ? Color.Lerp(Color.black, Color.grey, 0.3f) : Color.Lerp(Color.black, Color.grey, 0.7f);
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



    [CustomPropertyDrawer(typeof(ViewEnumAttribute))]
    public class ViewEnumAttributeDrawer : PropertyDrawBase<ViewEnumAttribute>
    {
        public List<string> enumList = null;
        public int selectIndex =0;
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
         
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!property.IsShow()) return;
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
                if (att.CanWriteString)
                {
                    property.stringValue = EditorGUI.TextField(position.HorizontalRect(0.4f, 0.7f), property.stringValue);
                }
                if (GUI.changed)
                {
                    UpdateList(property.stringValue);
                }
                if (selectIndex < 0)
                {
                    selectIndex = 0;
                    property.stringValue = selectValue;
                }
                
                var newIndex = EditorGUI.Popup(position.HorizontalRect(0.7f, 1), selectIndex, enumList.ToArray());
                if (newIndex !=selectIndex)
                {
                    selectIndex = newIndex;
                    property.stringValue = selectValue;
                }

            }

            // EditorGUI.BeginChangeCheck();
            // property.Draw(position, att.name );

            //EditorGUI.PropertyField(rect, property, new GUIContent(viewName), property.isExpanded);
            // EditorGUI.EndChangeCheck();
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.IsShow()) return 0;
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

    #endregion
    public static class QEditorTool
    {

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
            var method = objType.GetMethod(funcName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
            {
                method = objType.GetStaticMethod(funcName);
                if (method==null)
                {
                    Debug.LogWarning(obj + " 不存在函数 " + funcName + "()");
                    return null;
                }
            }
            return method?.Invoke(method.IsStatic?null:obj, paramsList);
        }
        public static Action DrawLayout(this SerializedProperty property)
        {
            if (!property.IsShow())
            {
                return null;
            }
            var changeCall = property.GetAttribute<ChangeCallAttribute>();
            if (changeCall != null)
            {
                EditorGUI.BeginChangeCheck(); ;
            }
            var readonlyAtt = property.GetAttribute<ReadOnlyAttribute>();
            if (readonlyAtt != null && readonlyAtt.Active(property.serializedObject.targetObject))
            {
                var last = GUI.enabled;
                GUI.enabled = false;
                EditorGUILayout.PropertyField(property, new GUIContent(property.ViewName()), true);
                GUI.enabled = last;
            }
            else
            {

                EditorGUILayout.PropertyField(property, new GUIContent(property.ViewName()), true);
            }
            if (changeCall != null)
            {
                if (EditorGUI.EndChangeCheck())
                {
                    return () =>
                    {
                        property.Call(changeCall.changeCallBack);
                    };
                }
            }
            return null;


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
        // static QDictionary<object, QDictionary<string, bool>> tempBoolList = new QDictionary<object, QDictionary<string, bool>>();
        public static bool GetBool(this object target, string key)
        {
            var info = GetMember(target, key);
            if (info == null)
            {
                return true;
            }
            else
            {
                return (bool)info.Get(target);
            }
        }
        public static QMemeberInfo GetMember(this object target, string key)
        {
            var memeberInfo = QInspectorType.Get(target.GetType()).Members[key];
            return memeberInfo;
        }
        public static bool Active(this ViewContorlAttribute att, object target)
        {
            if (string.IsNullOrWhiteSpace(att.control))
            {
                return true;
            }
            else
            {
                return (bool)target.GetBool(att.control);
            }
        }
        public static bool IsShow(this SerializedProperty property)
        {
            var att = property.GetAttribute<ViewNameAttribute>();
            if (att == null)
            {
                return true;
            }
            else
            {
                return att.Active(property.serializedObject.targetObject);
            }
        }
        public static void AddObject(this List<GUIContent> list, object obj)
        {
            if (obj != null)
            {
                if (obj is UnityEngine.GameObject)
                {
                    var uObj = obj as UnityEngine.GameObject;
                    var texture = AssetPreview.GetAssetPreview(uObj);
                    list.Add(new GUIContent(uObj.name, texture, uObj.name));
                }
                else
                {
                    list.Add(new GUIContent(obj.ToString()));
                }
            }
            else
            {
                list.Add(new GUIContent("空"));
            }
        }
    }
    public class QInspectorType : QTypeInfo<QInspectorType>
    {
        public QDictionary<EidtorInitInvokeAttribute, QFunctionInfo> initFunc = new QDictionary<EidtorInitInvokeAttribute, QFunctionInfo>();
        public QDictionary<SceneInputEventAttribute, QFunctionInfo> mouseEventFunc = new QDictionary<SceneInputEventAttribute, QFunctionInfo>();
        public QDictionary<ViewButtonAttribute, QFunctionInfo> buttonFunc = new QDictionary<ViewButtonAttribute, QFunctionInfo>();
        public ScriptToggleAttribute scriptToggle = null;
        public QDictionary<EditorModeAttribute, QFunctionInfo> editorMode = new QDictionary<EditorModeAttribute, QFunctionInfo>();
        protected override void Init(Type type)
        {
            MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            FunctionFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            base.Init(type);
            scriptToggle = type.GetCustomAttribute<ScriptToggleAttribute>();
            foreach (var funcInfo in Functions)
            {
                foreach (var att in funcInfo.MethodInfo.GetCustomAttributes<SceneInputEventAttribute>())
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
                foreach (var att in funcInfo.MethodInfo.GetCustomAttributes<EditorModeAttribute>())
                {
                    editorMode[att] = funcInfo;
                }
             
                
            }
        }
    }
    [CustomEditor(typeof(UnityEngine.Object), true, isFallback = true)]
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
            typeInfo = QInspectorType.Get(target.GetType());

            foreach (var kv in typeInfo.initFunc)
            {
                kv.Value.Invoke(target);
            }
            EditorApplication.playModeStateChanged += EditorModeChanged;
        }
        private void OnDestroy()
        {
            EditorApplication.playModeStateChanged -= EditorModeChanged;
        }
        void EditorModeChanged(PlayModeStateChange state)
        {
            foreach (var kv in typeInfo.editorMode)
            {
                if ((byte)kv.Key.state == (byte)state)
                {
                    kv.Value.Invoke(target);
                }
            }
        }
        RaycastHit hit;
        public void MouseCheck()
        {
            if (typeInfo.mouseEventFunc.Count <= 0) return;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            Event input = Event.current;
            if (!input.alt)
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
                        point = mouseRay.RayCastPlane(Vector3.up, Vector3.zero);
                    }

                    foreach (var kv in typeInfo.mouseEventFunc)
                    {
                        if (input.type == kv.Key.eventType)
                        {
                            if(input.isMouse)
                            {
                                if((bool) kv.Value.Invoke(target, point, hit, input.shift))
                                {
                                    input.Use();
                                }
                                
                            }
                            else if(input.keyCode == kv.Key.keyCode)
                            {
                                if ((bool)kv.Value.Invoke(target))
                                {
                                    input.Use();
                                }
                            }
                           
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
            GroupList.Clear();
            EditorGUI.BeginChangeCheck();
            DrawAllProperties(serializedObject);

            DrawButton();
            DrawGroup();
            DrawScriptToggleList();
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedProperties();
                ChangeCallBack?.Invoke();
            }

        }
        public void DrawGroup()
        {
            foreach (var kv in GroupList)
            {
                if (kv.Value.group is HorizontalGroupAttribute)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (kv.Value.group.Active(target))
                        {
                            kv.Value.func?.Invoke();
                        }
                    }
                }
            }
        }


        public void DrawButton()
        {
            foreach (var kv in typeInfo.buttonFunc)
            {
                CheckGroup(kv.Value.MethodInfo.GetCustomAttribute<GroupAttribute>(), () =>
                {
                    var att = kv.Key;
                    if (att.Active(target))
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
                });

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

        QDictionary<int, int> tempIndex = new QDictionary<int, int>(-1);
        #region 将数组数据显示成工具栏
        public bool DrawToolbar(SerializedProperty property)
        {
            var toolbar = property.GetAttribute<ToolbarListAttribute>();
            if (toolbar != null)
            {
                if (!toolbar.Active(target))
                {
                    return true;
                }
                var listMember = target.GetMember(toolbar.listMember);

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
                if (list.Count == 0)
                {
                    GUILayout.Toolbar(0, new string[] { toolbar.name + "为空" }, GUILayout.Height(toolbar.height));
                }
                else if (toolbar.pageSize <= 0)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        GuiList.AddObject(list[i]);
                    }
                    property.intValue = GUILayout.Toolbar(property.intValue, GuiList.ToArray(), GUILayout.Height(toolbar.height));
                }
                else
                {
                    var pageList = new List<GUIContent>();
                    for (int i = 0; i < list.Count / toolbar.pageSize + 1; i++)
                    {
                        pageList.AddObject(i);
                    }
                    if (tempIndex[list.GetHashCode()] < 0)
                    {
                        tempIndex[list.GetHashCode()] = property.intValue / toolbar.pageSize;
                    }
                    var start = tempIndex[list.GetHashCode()] * toolbar.pageSize;
                    for (int i = start; i < Mathf.Min(start + toolbar.pageSize, list.Count); i++)
                    {
                        GuiList.AddObject(list[i]);
                    }
                    if (list.Count > toolbar.pageSize)
                    {
                        tempIndex[list.GetHashCode()] = GUILayout.Toolbar(tempIndex[list.GetHashCode()], pageList.ToArray());
                    }
                    if (GuiList.Count == 0)
                    {
                        //  GUILayout.Toolbar(0, new string[] { toolbar.name+" 第" +tempIndex[list.GetHashCode()]+ "页为空" }, GUILayout.Height(toolbar.height));
                        tempIndex[list.GetHashCode()] = 0;
                    }
                    else
                    {
                        property.intValue = start + GUILayout.Toolbar(property.intValue - start, GuiList.ToArray(), GUILayout.Height(toolbar.height));
                    }

                }



                return true;

            }
            return false;
        }
        #endregion

        #region 将数组数据显示成可选项
        GameObject gameObject => (target as MonoBehaviour)?.gameObject;
        public bool HasCompoent(string name)
        {
            try
            {

                return gameObject?.GetComponent(QReflection.ParseType( name));
            }
            catch (System.Exception e)
            {

                Debug.LogError("判断脚本[" + name + "]出错：" + e);
                return false;
            }
        }
        public void SetCompoent(string name, bool value)
        {
            if (HasCompoent(name) != value)
            {
                if (value)
                {
                    gameObject?.AddComponent(QReflection.ParseType(name));
                    //  UnityEngineInternal.APIUpdaterRuntimeServices.AddComponent(gameObject, "Assets\Scripts\Scenes\FightScene\Tile\TileObject.cs (296,17)", nameDic[name]);
                }
                else
                {

                    DestroyImmediate(gameObject.GetComponent(QReflection.ParseType(name)));
                }
            }
        }
        public bool DrawScriptToggleList()
        {
            var att = typeInfo.scriptToggle;
            if (att != null)
            {
                if (!att.Active(target))
                {
                    return true;
                }
                var listFunc = target.GetMember(att.scriptList);

             //   var info = target.GetMember(property.name);

                var GuiList = new List<GUIContent>();

                if (listFunc != null)
                {
                    var list = (listFunc.Get(target) as IList);
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
                        var value = HasCompoent(list[i]?.ToString());
                        var style = EditorStyles.miniButton;
                        SetCompoent(list[i]?.ToString(), value.DrawToogleButton(GuiList[i], style));
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.Box("无法获取列表【" + att.scriptList + "】");
                }
                return true;

            }
            return false;
        }
        #endregion

        #region 对数组数据进行显示
        public void DrawArrayProperty(SerializedProperty property)
        {
           // if (DrawToggleList(property)) return;
            ChangeCallBack +=property.DrawLayout();
        }
        #endregion
        Action ChangeCallBack;
        public int pickId = -1;
        public void CheckGroup(GroupAttribute group, Action func)
        {
            if (group == null)
            {
                func();
            }
            else
            {
                if (GroupList[group.name] == null)
                {
                    GroupList[group.name] = new GroupInfo()
                    {
                        group = group
                    };
                }
                GroupList[group.name].func += func;
            }
        }
        public class GroupInfo
        {
            public GroupAttribute group;
            public Action func;
        }
        public QDictionary<string, GroupInfo> GroupList = new QDictionary<string, GroupInfo>();
        public void DrawProperty(SerializedProperty property)
        {
            CheckGroup(property.GetAttribute<GroupAttribute>(), () =>
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
                        ChangeCallBack +=property.DrawLayout();
                    }

                }
            });
        }
    }
}
