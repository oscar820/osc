using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using QTool.Reflection;
using System.Threading.Tasks;

namespace QTool.Inspector
{



    #region 自定义显示效果
    [CustomPropertyDrawer(typeof(QIdObject))]
    public class QObjectReferenceDrawer : PropertyDrawer
    {
        public static string Draw(string lable, string id,Type type,Rect? rect=null, params GUILayoutOption[] options)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var name = lable + "【" + (id == null ? "" : id.Substring(0, Mathf.Min(4, id.Length))) + "~】";
                var oldObj =QIdObject.GetObject(id,type);
                var newObj = oldObj;

                if (rect == null)
                {
                    newObj = EditorGUILayout.ObjectField(name, oldObj, type, true);
                }
                else
                {
                    newObj = EditorGUI.ObjectField(rect.Value, name, oldObj, type, true);
                }
                if (newObj != oldObj)
                {
                   id= QIdObject.GetId(newObj);
                }
            }
            return id;
        }
        public static QIdObject Draw(string lable, QIdObject ir, params GUILayoutOption[] options)
        {
            using ( new EditorGUILayout.HorizontalScope())
            {
                var newId= Draw(lable, ir.id,typeof(UnityEngine.Object),null, options);
                if (newId != ir.id)
                {
                    ir.id = newId;
                }
            }
            return ir;
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
			var id = property.FindPropertyRelative(nameof(QIdObject.id));
			id.stringValue= Draw(label.text, id.stringValue,typeof(UnityEngine.Object),position);
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
   
    [CustomPropertyDrawer(typeof(QToggleAttribute))]
    public class ViewToggleAttributeDrawer : PropertyDrawBase<QToggleAttribute>
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Boolean)
            {
                position.height = att.height;;
				QGUITool.SetColor(property.boolValue ? Color.Lerp(Color.black, Color.grey, 0.3f) : Color.Lerp(Color.black, Color.grey, 0.7f));
                property.boolValue = EditorGUI.Toggle(position, property.boolValue, QGUITool.BackStyle);
				QGUITool.RevertColor();
				var style = EditorStyles.largeLabel;
                style.alignment = TextAnchor.MiddleCenter;
                EditorGUI.LabelField(position, att.name, style);
            }
            else
            {
                property.Draw("",position);
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



    [CustomPropertyDrawer(typeof(QEnumAttribute))]
    public class QEnumAttributeDrawer : PropertyDrawBase<QEnumAttribute>
    {
		public List<string> enumList = new List<string>();
        public int selectIndex =0;
        public string SelectValue
        {
            get
            {
				if (selectIndex >= 0 && selectIndex < enumList.Count)
				{
					return enumList[selectIndex]=="null"?null:enumList[selectIndex];
				}
				else
				{
					return "";
				}
            }
        }
        public void UpdateList(string input)
        {
			if (input.IsNullOrEmpty())
			{
				selectIndex = enumList.Count - 1;
			}
			else
			{
				selectIndex = enumList.IndexOf(input);
			}
        }
		public static QDictionary<string, QEnumAttributeDrawer> DrawerDic = new QDictionary<string, QEnumAttributeDrawer>((key)=>new QEnumAttributeDrawer());
		public static object Draw(object obj,QEnumAttribute att)
		{
			var str = obj?.ToString();
			{
				var funcKey= obj?.GetType() +" "+ att.GetKeyListFunc;
				var drawer = DrawerDic[funcKey];
				using (new GUILayout.HorizontalScope())
				{
					if (drawer.enumList.Count <= 0)
					{
						var getObj = QReflection.InvokeStaticFunction(null, att.GetKeyListFunc);
						drawer.enumList.Clear();
						if (getObj != null)
						{
							if (getObj is IList<string> stringList)
							{
								drawer.enumList.AddRange(stringList);
							}
							else if (getObj is IList itemList)
							{
								foreach (var item in itemList)
								{
									if (item is IKey<string> key)
									{
										drawer.enumList.AddCheckExist(key.Key);
									}
									else if (item is UnityEngine.Object uObj)
									{
										drawer.enumList.AddCheckExist(uObj.name);
									}
									else
									{
										drawer.enumList.AddCheckExist(item?.ToString());
									}
								}
							}
							else
							{
								EditorGUILayout.LabelField("错误函数" + att.GetKeyListFunc);
							}
						}
						else
						{
							EditorGUILayout.LabelField("错误函数" + att.GetKeyListFunc);
						}
						drawer.enumList.AddCheckExist("null");
					}
					

					drawer.UpdateList(str);

					if (att.CanWriteString)
					{
						str = EditorGUILayout.TextField("", str);
					}
					if (GUI.changed)
					{
						drawer.UpdateList(str);
					}
					if (drawer.selectIndex < 0)
					{
						drawer.selectIndex = 0;
						str = drawer.SelectValue;
					}
					var newIndex = EditorGUILayout.Popup(drawer.selectIndex, drawer.enumList.ToArray());
					if (newIndex != drawer.selectIndex)
					{
						drawer.selectIndex = newIndex;
						if (drawer.selectIndex >= 0)
						{
							str = drawer.SelectValue;
						}
					}
				}
				return str;
			}
			
		}
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!property.IsShow()) return;
            if (property.propertyType == SerializedPropertyType.String)
            {
				object list = null;
				try
				{
					list = property.Call(att.GetKeyListFunc);
				}
				catch (Exception e)
				{
					Debug.LogError(e);
				}

				enumList = new List<string>();
				enumList.Add("null");

				if (list is IList<string> strList)
				{
					enumList.AddRange(strList);
				}
				else if( list is IList ItemList)
				{
					foreach (var item in ItemList)
					{
						if(item is IKey<string> key)
						{
							enumList.Add(key.Key);
						}
						else if (item is UnityEngine.Object uObj)
						{
							enumList.AddCheckExist(uObj.name);
						}
						else
						{
							enumList.AddCheckExist(item?.ToString());
						}
					}
				}
				UpdateList(property.stringValue);
				EditorGUI.LabelField(position.HorizontalRect(0f, 0.3f), property.QName());
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
                    property.stringValue = SelectValue;
                }
                
                var newIndex = EditorGUI.Popup(position.HorizontalRect(0.7f, 1), selectIndex, enumList.ToArray());
                if (newIndex !=selectIndex)
                {
                    selectIndex = newIndex;
                    property.stringValue = SelectValue;
                }

            }
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.IsShow()) return 0;
            return property.GetHeight();
        }
    }
   

    #endregion
    public static class QEditorTool
    {
		public static object GetPathObject(this object target, string path)
		{
			if (path.SplitTowString(".", out var start, out var end))
			{
				try
				{
					if (start == "Array" && end.StartsWith("data"))
					{
						var list = target as IList;
						if (list == null)
						{
							return null;
						}
						else
						{
							return list[int.Parse(end.GetBlockValue('[',']'))];
						}
					}
					else
					{

						return target.GetPathObject(start).GetPathObject(end);
					}
					
				}
				catch (Exception e)
				{
					throw new Exception("路径出错：" + path, e);
				}
			}
			else
			{
				var memebers = QInspectorType.Get(target.GetType()).Members;
				if (memebers.ContainsKey(path))
				{
					var Get = memebers[path].Get;
					return Get(target);
				}
				else
				{
					throw new Exception(" 找不到 key " + path);
				}
			}
		}
		public static bool GetPathBool(this object target, string key)
		{
			var info = target.GetPathObject(key);
			if (info == null)
			{
				return true;
			}
			else
			{
				return (bool)info;
			}
		}
		public static Rect HorizontalRect(this Rect rect, float left, float right)
        {
            var leftOffset = left * rect.width;
            var width = (right - left) * rect.width;
            rect.x += leftOffset;
            rect.width = width;
            return rect;
        }
        //public static bool HasAttribute<T>(this SerializedProperty prop, string parentKey)
        //{
        //    object[] attributes = GetAttributes<T>(prop, parentKey);
        //    if (attributes != null)
        //    {
        //        return attributes.Length > 0;
        //    }
        //    return false;
        //}

        public static object[] GetAttributes<T>(this SerializedProperty prop, string parentKey)
        {
            var type = string.IsNullOrWhiteSpace(parentKey) ? prop.serializedObject.targetObject?.GetType() : QReflection.ParseType(parentKey);
            var field = GetChildObject(type, prop.name);
            if (field != null)
            {
                return field.GetCustomAttributes(typeof(T), true);
            }
            return new object[0];
        }
        public static FieldInfo GetChildObject(Type type, string key)
        {
            if (type == null || string.IsNullOrWhiteSpace(key)) return null;
            const BindingFlags bindingFlags = System.Reflection.BindingFlags.GetField
                                              | System.Reflection.BindingFlags.GetProperty
                                              | System.Reflection.BindingFlags.Instance
                                              | System.Reflection.BindingFlags.NonPublic
                                              | System.Reflection.BindingFlags.Public;
            return type.GetField(key, bindingFlags);
        }
        public static T GetAttribute<T>(this SerializedProperty prop, string parentKey = "") where T : Attribute
        {
            object[] attributes = GetAttributes<T>(prop, parentKey);
            if (attributes.Length > 0)
            {
                return attributes[0] as T;
            }
            else
            {
                return null;
            }
        }

        public static string QName(this SerializedProperty property, string parentName = "")
        {
            var att = property.GetAttribute<QNameAttribute>(parentName);
            if (att != null && !string.IsNullOrWhiteSpace(att.name))
            {
                return att.name;
            }
            else
            {
                return property.displayName;
            }
        }

        //public static T Call<T>(this SerializedProperty property, string funcName) where T : class
        //{
        //    return Call(property, funcName) as T;
        //}
		public static object GetObject(this SerializedProperty property)
		{
			return property?.serializedObject.targetObject.GetPathObject(property.propertyPath);
		}
        public static object Call(this SerializedProperty property, string funcName, object[] paramsList = null)
        {
			return property.serializedObject.targetObject.InvokeFunction(funcName,paramsList);

		}
        public static Action DrawLayout(this SerializedProperty property)
        {
            if (!property.IsShow())
            {
                return null;
            }
            var changeCall = property.GetAttribute<QOnChangeAttribute>();
			var group = property.GetAttribute<QGroupAttribute>();
			if (changeCall != null)
            {
                EditorGUI.BeginChangeCheck(); ;
            }
            var readonlyAtt = property.GetAttribute<QReadOnlyAttribute>();
			if (group != null && group.start)
			{
				GUILayout.BeginVertical(QGUITool.BackStyle);
			}
            if (readonlyAtt != null)
            {
                var last = GUI.enabled;
                GUI.enabled = false;
                property.Draw();
                GUI.enabled = last;
            }
            else
            {
                property.Draw();
            }
			if (group != null &&! group.start)
			{
				GUILayout.EndVertical();
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
        public class QStylesWindows : EditorWindow
        {

            private Vector2 scrollVector2 = Vector2.zero;
            private string search = "";

            [MenuItem("QTool/工具/GUIStyle查看器")]
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
        public static QDictionary<string, bool> FoldoutDic = new QDictionary<string, bool>();
        public static QDictionary<string, Action<SerializedProperty, Func<float, float>>> DrawPropertyToFloat = new QDictionary<string, Action<SerializedProperty, Func<float, float>>>();
		public static QDictionary<Type, Func<object, string, object>> DrawOverride = new QDictionary<Type, Func<object, string, object>>();
        static Color BackColor = new Color(0, 0, 0, 0.6f);

		public static List<string> TypeMenuList = new List<string>() { typeof(UnityEngine.Object).FullName.Replace('.', '/') };
		public static List<Type> TypeList = new List<Type>() { typeof(UnityEngine.Object) };
        public static object Draw(this object obj,string name,Type type,Action<object> changeValue=null,ICustomAttributeProvider customAttribute=null, Action<int> DrawElementCall=null,Action<int,int> IndexChange=null,params GUILayoutOption[] layoutOption)
		{
			var hasName = !string.IsNullOrWhiteSpace(name);
			if (type == null)
			{
				EditorGUILayout.LabelField(name, layoutOption);
			}
			if (obj == null && type.IsValueType)
			{
				obj = type.CreateInstance();
			}
			if (DrawOverride.ContainsKey(type))
			{
				return DrawOverride[type].Invoke(obj, name);
			}

			var typeInfo = QSerializeType.Get(type);
			if (type!=typeof(object)&& !TypeList.Contains(type)&&!type.IsGenericType)
            {
                TypeList.Add(type);
                TypeMenuList.AddCheckExist(type.FullName.Replace('.', '/'));
            }
            switch (typeInfo.Code)
            {
                case TypeCode.Boolean:
                        obj= EditorGUILayout.Toggle(name, (bool)obj, layoutOption);break;
                case TypeCode.Char:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                    if (type.IsEnum)
                    {
						var flagsEnum = type.GetAttribute<System.FlagsAttribute>();
						if (flagsEnum != null)
						{
							obj = EditorGUILayout.EnumFlagsField(name, (Enum)obj, layoutOption);
						}
						else
						{
							obj = EditorGUILayout.EnumPopup(name, (Enum)obj, layoutOption);
						}
                    }
                    else
                    {
                            obj = EditorGUILayout.IntField(name, (int)obj, layoutOption);
                    }
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                        obj= EditorGUILayout.LongField(name, (long)obj, layoutOption);break;
                case TypeCode.Single:
                        obj= EditorGUILayout.FloatField(name, (float)obj, layoutOption);break;
                case TypeCode.Decimal:
                case TypeCode.Double:
                        obj= EditorGUILayout.DoubleField(name, (double)obj, layoutOption);break;
                case TypeCode.String:
					var enumView = customAttribute?.GetAttribute<QEnumAttribute>();
					if (enumView != null)
					{
						obj = QEnumAttributeDrawer.Draw(obj, enumView);break;
					}
					else if (string.IsNullOrWhiteSpace(name))
					{
						obj = EditorGUILayout.TextArea(obj?.ToString(), layoutOption);break;
					}
					else
					{
						obj = EditorGUILayout.TextField(name, obj?.ToString(), layoutOption); break;
					}
                case TypeCode.Object:
                    switch (typeInfo.objType)
                    {
						case QObjectType.DynamicObject:
							{
								using (new EditorGUILayout.HorizontalScope(layoutOption))
								{
									if (obj == null)
									{
										obj = "";
									}
									var objType = obj.GetType();
									var oldType = TypeList.IndexOf(objType);
									var newType = EditorGUILayout.Popup(oldType, TypeMenuList.ToArray(), GUILayout.Width(20), GUILayout.Height(20));
									if (newType != oldType)
									{
										objType = TypeList[newType];
										obj = objType.CreateInstance();
									}
									obj = Draw(obj, name, objType);
								}
							}break;
						case QObjectType.UnityObject:
							{
								obj = EditorGUILayout.ObjectField(name, (UnityEngine.Object)obj, type, true, layoutOption);
							}
							break;
                        case QObjectType.Object:
							{ 
                                if (obj == null)
                                {
                                    obj = type.CreateInstance();
                                }
                                if (typeof(QIdObject).IsAssignableFrom(type))
                                {
                                    obj = QObjectReferenceDrawer.Draw(name, (QIdObject)obj, layoutOption);
                                }
                                else
                                {

                                    var color = GUI.backgroundColor;
                                    GUI.backgroundColor = BackColor;
                                    using (new EditorGUILayout.VerticalScope(QGUITool.BackStyle, layoutOption))
                                    {
                                        GUI.backgroundColor = color;
										if (hasName)
										{
											FoldoutDic[name] = EditorGUILayout.Foldout(FoldoutDic[name], name);
										}
                                        if (!hasName||FoldoutDic[name])
                                        {
                                            using (new EditorGUILayout.HorizontalScope( layoutOption))
                                            {
												if (hasName)
												{
													EditorGUILayout.Space(10);
												}
                                                using (new EditorGUILayout.VerticalScope())
                                                {
													
                                                    foreach (var member in typeInfo.Members)
                                                    {
														try
														{
															if (member.Type.IsValueType)
															{
																member.Set(obj, member.Get(obj).Draw(member.QName, member.Type));
															}
															else
															{
																member.Set(obj, member.Get(obj).Draw(member.QName, member.Type, (value) => member.Set(obj, value)));
															}
														}
														catch (Exception e)
														{
															Debug.LogError("序列化【" + member.Key + "】出错\n"+e);
														}

                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }break;

                        case QObjectType.Array:
                        case QObjectType.List:
                            {
                                if(typeof(IList).IsAssignableFrom( type) )
                                {
                                    if (typeInfo.ArrayRank > 1)
                                    {
                                        break;
                                    }
                                    var list = obj as IList;
                                    if (list == null)
                                    {
                                        obj= typeInfo.ArrayRank==0?type.CreateInstance():type.CreateInstance(null,false,0);
                                        list = obj as IList;
                                    }
                                    var color = GUI.backgroundColor;
                                    GUI.backgroundColor = BackColor;
									
									using (new EditorGUILayout.VerticalScope(QGUITool.BackStyle, layoutOption))
                                    {
										
                                        GUI.backgroundColor = color;
                                        var canHideChild = DrawElementCall==null;
										if (hasName)
										{
											if (canHideChild)
											{
												FoldoutDic[name] = EditorGUILayout.Foldout(FoldoutDic[name], name);
											}
											else
											{
												EditorGUILayout.LabelField(name);
											}
										}
                                        if (!canHideChild|| !hasName  || FoldoutDic[name])
                                        {
                                            using (new EditorGUILayout.HorizontalScope())
                                            {
												if (hasName)
												{
													EditorGUILayout.Space(10);
												}
                                                using (new EditorGUILayout.VerticalScope())
                                                {
													for (int i = 0; i < list.Count; i++)
													{
														using (new EditorGUILayout.VerticalScope(QGUITool.BackStyle))
														{
															var key = name + "[" + i + "]";
															var element = list[i].Draw(key, typeInfo.ElementType,null,customAttribute);
															list[i] = element;
															DrawElementCall?.Invoke(i);
															using (new EditorGUILayout.HorizontalScope())
															{
																GUILayout.FlexibleSpace();
																QGUITool.SetColor(Color.blue.LerpTo(Color.white,0.5f));
																if (GUILayout.Button(new GUIContent("","新增当前数据"), GUILayout.Width(10), GUILayout.Height(10)))
																{
																	obj = list.CreateAt(typeInfo, i);
																	IndexChange?.Invoke(-1, i + 1);
																}
																QGUITool.RevertColor();
																QGUITool.SetColor(Color.red.LerpTo(Color.white, 0.5f));
																if (GUILayout.Button(new GUIContent("", "删除当前数据"), GUILayout.Width(10), GUILayout.Height(10)))
																{
																	obj = list.RemoveAt(typeInfo, i);
																	IndexChange?.Invoke(i, -1);
																}
																QGUITool.RevertColor();
															}
														}
														
													}
                                                }
                                            }
											if (list.Count == 0)
											{
												if (GUILayout.Button("添加新元素", GUILayout.Height(20)))
												{
													obj = list.CreateAt(typeInfo);
												}
											}

										}
                                    }
                                }
                            }
                            break;
                    
                        default:
                            break;
                    }
                    break;

				case TypeCode.DateTime:
					
				case TypeCode.Empty:
                case TypeCode.DBNull:
                default:;
						 EditorGUILayout.LabelField(name, obj?.ToString(), layoutOption);
					break;
			}
            if (changeValue!=null)
            {
                GUILayoutUtility.GetLastRect().MouseMenuClick((menu) =>
                {
                    menu.AddItem(new GUIContent("复制" + name), false, () => GUIUtility.systemCopyBuffer = obj.ToQDataType(type));
                    if (!string.IsNullOrWhiteSpace(GUIUtility.systemCopyBuffer))
                    {
                        menu.AddItem(new GUIContent("粘贴" + name), false, () => changeValue(GUIUtility.systemCopyBuffer.ParseQDataType(type, true, obj)));
                    }

                });
            }
         
            return obj;
        }
        public static void MouseMenuClick(this Rect rect, System.Action<GenericMenu> action,Action click=null)
        {
            if (EventType.MouseUp.Equals(Event.current.type))
            {
				if (rect.Contains(Event.current.mousePosition))
				{
					switch (Event.current.button)
					{
						case 0:
							{
								if (click != null)
								{
									click.Invoke();
									Event.current.Use();
								}
							}
							break;
						case 1:
							{
								if (action != null)
								{
									var rightMenu = new GenericMenu();
									action.Invoke(rightMenu);
									rightMenu.ShowAsContext();
									Event.current.Use();
								}
								
							}
							break;
						default:
							break;
					}
				}
			
            }
        }
        public static bool Draw(this SerializedProperty property,string parentKey="", Rect? rect=null)
        {
            var cur= property.Copy();
            if (DrawPropertyToFloat.ContainsKey(cur.type) && DrawPropertyToFloat[cur.type]!=null)
            {
                DrawPropertyToFloat[cur.type](cur, (value) => {
                    var range = cur.GetAttribute<RangeAttribute>();
                    if (range == null)
                    {
                        if (rect == null)
                        {
                            return EditorGUILayout.FloatField(cur.QName(parentKey), value);
                        }
                        else
                        {
                            return EditorGUI.FloatField(rect.Value, cur.QName(parentKey), value);
                        }
                    }
                    else
                    {
                        if (rect == null)
                        {
                            return EditorGUILayout.Slider(cur.QName(parentKey), value, range.min, range.max);
                        }
                        else
                        {
                            return EditorGUI.Slider(rect.Value, cur.QName(parentKey), value, range.min, range.max);
                        }
                    }
                });
                return true;
            }
            else
            {
                if (cur.hasVisibleChildren && !cur.isArray)
                {
                    if (rect != null)
                    {
                        rect = new Rect(rect.Value.position, new Vector2(rect.Value.width, cur.GetHeight(false)));
                    }
                    var expanded = rect == null ? EditorGUILayout.PropertyField(cur, new GUIContent(cur.QName(parentKey)), false)
                        : EditorGUI.PropertyField(rect.Value, cur, new GUIContent(cur.QName(parentKey)), false);
                    parentKey = property.type;
                    if (expanded)
                    {
                        using (rect == null ? new EditorGUILayout.HorizontalScope() : null)
                        {
                            if (rect == null)
                            {
                                EditorGUILayout.Space(20, false);
                            }
                            else
                            {
                                rect = new Rect(rect.Value.x + 20, rect.Value.y, rect.Value.width - 10, rect.Value.height);
                            }
                            using (rect == null ? new EditorGUILayout.VerticalScope() : null)
                            {
                                var end = cur.GetEndProperty();
                                cur.NextVisible(true);
                                do
                                {
                                    if (SerializedProperty.EqualContents(cur, end)) return expanded;
                                    if (rect != null)
                                    {
                                        rect = new Rect(new Vector2(rect.Value.x, rect.Value.yMax + 2), new Vector2(rect.Value.width, cur.GetHeight(false)));
                                    }
                                    cur.Draw(parentKey, rect);
                                } while (cur.NextVisible(false));
                            }

                        }
                    }
                    return expanded;
                }
                else
                {
                    if (rect == null)
                    {
                        return EditorGUILayout.PropertyField(cur, new GUIContent(cur.QName(parentKey)));
                    }
                    else
                    {
                        return EditorGUI.PropertyField(rect.Value, cur, new GUIContent(cur.QName(parentKey)));
                    }
                }
            }
         
        } 
        public static float GetHeight(this SerializedProperty property,bool containsChild=true)
        {
            return EditorGUI.GetPropertyHeight(property,containsChild&& property.isExpanded);
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
 
   
        public static bool Active(this QNameAttribute att, object target)
        {
            if (string.IsNullOrWhiteSpace(att.control))
            {
                return true;
            }
            else
            {
                return (bool)target.GetPathBool(att.control);
            }
        }
        public static bool IsShow(this SerializedProperty property)
        {
            var att = property.GetAttribute<QNameAttribute>();
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
        public QDictionary<QOnEidtorInitAttribute, QFunctionInfo> initFunc = new QDictionary<QOnEidtorInitAttribute, QFunctionInfo>();
        public QDictionary<QOnSceneInputAttribute, QFunctionInfo> mouseEventFunc = new QDictionary<QOnSceneInputAttribute, QFunctionInfo>();
        public QDictionary<QNameAttribute, QFunctionInfo> buttonFunc = new QDictionary<QNameAttribute, QFunctionInfo>();
        public QScriptToggleAttribute scriptToggle = null;
        public QDictionary<QOnEditorModeAttribute, QFunctionInfo> editorMode = new QDictionary<QOnEditorModeAttribute, QFunctionInfo>();
        protected override void Init(Type type)
        {
            MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            FunctionFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            base.Init(type);
            scriptToggle = type.GetCustomAttribute<QScriptToggleAttribute>();
            foreach (var funcInfo in Functions)
            {
                foreach (var att in funcInfo.MethodInfo.GetCustomAttributes<QOnSceneInputAttribute>())
                {
                    mouseEventFunc[att] = funcInfo;
                }
                foreach (var att in funcInfo.MethodInfo.GetCustomAttributes<QNameAttribute>())
                {
                    buttonFunc[att] = funcInfo;
                }
                foreach (var att in funcInfo.MethodInfo.GetCustomAttributes<ContextMenu>())
                {
                    buttonFunc[new QNameAttribute(att.menuItem)] = funcInfo;
                }
                foreach (var att in funcInfo.MethodInfo.GetCustomAttributes<QOnEidtorInitAttribute>())
                {
                    initFunc[att] = funcInfo;
                }
                foreach (var att in funcInfo.MethodInfo.GetCustomAttributes<QOnEditorModeAttribute>())
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
        protected virtual void OnEnable()
        {
            typeInfo = QInspectorType.Get(target.GetType());

            foreach (var kv in typeInfo.initFunc)
            {
                var result= kv.Value.Invoke(target);
				if(result is Task task)
				{
					task.GetAwaiter().OnCompleted(() =>
					{
						if (task.Exception != null)
						{
							Debug.LogError(task.Exception);
						}
					});
				}
            }
            EditorApplication.playModeStateChanged += EditorModeChanged;
        }
        protected virtual void OnDestroy()
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
            if (serializedObject == null||serializedObject.targetObject==null)
            {
                base.OnInspectorGUI();
                return;
            }
          //  GroupList.Clear();
            EditorGUI.BeginChangeCheck();
            DrawAllProperties(serializedObject);

            DrawButton();
          //  DrawGroup();
            DrawScriptToggleList();
            if (EditorGUI.EndChangeCheck())
            {
				target?.SetDirty();
                serializedObject.ApplyModifiedProperties();
                ChangeCallBack?.Invoke();
            }

        }
        //public void DrawGroup()
        //{
        //    foreach (var kv in GroupList)
        //    {
        //        if (kv.Value.group is HorizontalGroupAttribute)
        //        {
        //            using (new EditorGUILayout.HorizontalScope())
        //            {
        //                if (kv.Value.group.Active(target))
        //                {
        //                    kv.Value.func?.Invoke();
        //                }
        //            }
        //        }
        //    }
        //}


        public void DrawButton()
        {
            foreach (var kv in typeInfo.buttonFunc)
            {
                //CheckGroup(kv.Value.MethodInfo.GetCustomAttribute<GroupAttribute>(), () =>
                //{
                    var att = kv.Key;
                    if (att.Active(target))
                    {
                        if (att is QSelectObjectButtonAttribute)
                        {
                            if (GUILayout.Button(att.name))
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
                            if (GUILayout.Button(att.name))
                            {
                                kv.Value.Invoke(target);
                            }
                        }

                    }
                //});

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

        QDictionary<int, int> tempIndex = new QDictionary<int, int>((key)=>-1);
        #region 将数组数据显示成工具栏
        public bool DrawToolbar(SerializedProperty property)
        {
            var toolbar = property.GetAttribute<QToolbarAttribute>();
            if (toolbar != null)
            {
                if (!toolbar.Active(target))
                {
                    return true;
                }

                var GuiList = new List<GUIContent>();
                IList list = null;
                try
                {
                    var obj = target.GetPathObject(toolbar.listMember);
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
        public bool HasCompoent(Type type)
        {
			return gameObject?.GetComponent(type);
		}
        public void SetCompoent(Type type, bool value)
        {
            if (HasCompoent(type) != value)
            {
				if (value)
				{
					gameObject?.AddComponent(type);
				}
				else
				{

					DestroyImmediate(gameObject.GetComponent(type));
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

                var GuiList = new List<GUIContent>();
				var types= att.baseType.GetAllTypes();
				GUILayout.BeginHorizontal();
				for (int i = 0; i < types.Length; i++)
				{
					var type = types[i];
					GuiList.Add(new GUIContent(type.QName()));
					var value = HasCompoent(type);
					var style = EditorStyles.miniButton;
					SetCompoent(type, value.DrawToogleButton(GuiList[i], style));
				}
				GUILayout.EndHorizontal();
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
        //public void CheckGroup(GroupAttribute group, Action func)
        //{
        //    if (group == null)
        //    {
        //        func();
        //    }
        //    else
        //    {
        //        if (GroupList[group.name] == null)
        //        {
        //            GroupList[group.name] = new GroupInfo()
        //            {
        //                group = group
        //            };
        //        }
        //        GroupList[group.name].func += func;
        //    }
        //}
        //public class GroupInfo
        //{
        //    public GroupAttribute group;
        //    public Action func;
        //}
       // public QDictionary<string, GroupInfo> GroupList = new QDictionary<string, GroupInfo>();
        public void DrawProperty(SerializedProperty property)
        {
            //CheckGroup(property.GetAttribute<GroupAttribute>(), () =>
            //{
                if (property.name.Equals("m_Script"))
                {
                    GUI.enabled = false;
                    property.Draw();
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
           // });
        }
    }
}
