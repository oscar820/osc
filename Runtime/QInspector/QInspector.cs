using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace QTool
{
    /// <summary>
    /// 更改显示的名字
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Method|AttributeTargets.Interface | AttributeTargets.Parameter|AttributeTargets.Property)]
    public class QNameAttribute : PropertyAttribute
    {
        public string name;
        public string control = "";
        public QNameAttribute()
        {
            order = 10;
        }
        public QNameAttribute(string name, string showControl = "") 
        {
            order = 10;
            this.name = name;
            this.control = showControl;
        }
    }
    /// <summary>
    /// 使数据在inspector视窗不可更改
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class QReadOnlyAttribute : Attribute
    {
        public QReadOnlyAttribute() 
        {
        }
    }
	[AttributeUsage(AttributeTargets.Field)]
	public class QGroupAttribute : Attribute
	{
		public bool start = true;
		public QGroupAttribute(bool start)
		{
			this.start = start;
		}
	}
	
}
namespace QTool.Inspector
{
    /// <summary>
    /// 数值更改时调用changeCallBack函数
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class QOnChangeAttribute : PropertyAttribute
    {
        public bool change;
        public string changeCallBack;
        public QOnChangeAttribute(string changeCallBack)
        {
            this.changeCallBack = changeCallBack;
        }
    }
  

   

    /// <summary>
    /// 将字符传显示为枚举下拉款通过GetKeyListFunc获取所有可选择的字符串
    /// </summary>
    [AttributeUsage(AttributeTargets.Field|AttributeTargets.Property|AttributeTargets.Parameter)]
    public class QEnumAttribute : PropertyAttribute
	{
        public string GetKeyListFunc;
        public bool CanWriteString = false;
        public QEnumAttribute(string GetKeyListFunc)
        {
			this.GetKeyListFunc = GetKeyListFunc;
        }

    }

    /// <summary>
    /// 将数组显示为toolbar工具栏通过indexName来设置int值；
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class QToolbarAttribute : QHeightAttribute
    {
        public string listMember;
        public int pageSize= 0;
        //  public bool ResourceImage = false;
        public QToolbarAttribute( string listMember, float height = 30, string showControl = "") : base("", height, showControl)
        {
            this.listMember = listMember;
            if (name == default)
            {
                name = listMember;
            }
        }
    }

    /// <summary>
    /// 将脚本名List 显示未开关按钮 来添加删除脚本
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class QScriptToggleAttribute : QHeightAttribute
    {
        public Type baseType;
        public QScriptToggleAttribute(Type baseType, float height = 30, string showControl = "") : base("", height, showControl)
        {
            this.baseType = baseType;
        }
    }
    /// <summary>
    /// 当在scene视窗鼠标事件调用 传入3个参数 (Vector3 点击位置,Collider 点击碰撞物体,bool 是否按下shift键)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class QOnSceneInputAttribute : Attribute
    {
        public EventType eventType = EventType.MouseDown;
        public KeyCode keyCode = KeyCode.None;
        public QOnSceneInputAttribute(EventType eventType)
        {
            this.eventType = eventType;
        }
        public QOnSceneInputAttribute(EventType eventType, KeyCode keyCode)
        {
            this.eventType = eventType;
            this.keyCode = keyCode;
        }
    }
    /// <summary>
    /// inspector初始化时调用
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class QOnEidtorInitAttribute : Attribute
    {
        public QOnEidtorInitAttribute()
        {
        }

    } 
    public enum EditorModeState
    {
        //
        // 摘要:
        //     Occurs during the next update of the Editor application if it is in edit mode
        //     and was previously in play mode.
        EnteredEditMode = 0,
        //
        // 摘要:
        //     Occurs when exiting edit mode, before the Editor is in play mode.
        ExitingEditMode = 1,
        //
        // 摘要:
        //     Occurs during the next update of the Editor application if it is in play mode
        //     and was previously in edit mode.
        EnteredPlayMode = 2,
        //
        // 摘要:
        //     Occurs when exiting play mode, before the Editor is in edit mode.
        ExitingPlayMode = 3
    }
    [AttributeUsage(AttributeTargets.Method)]
    public class QOnEditorModeAttribute : Attribute
    {
        public EditorModeState state;
        public QOnEditorModeAttribute(EditorModeState state)
        {
            this.state = state;
        }

    }
  
    /// <summary>
    /// 选取对象按钮显示 会调用函数CallFunc传入GameObject
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class QSelectObjectButtonAttribute : QNameAttribute
    {
        public QSelectObjectButtonAttribute(string name,  string showControl = "") : base(name, showControl)
        {
        }
    }
    /// <summary>
    /// 显示按钮 调用函数CallFunc 无参数
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class QToggleAttribute : QHeightAttribute
    {
  
        public QToggleAttribute(string name, float height = 30, string showControl="") :base(name, height, showControl)
        {
            
        }

    }
    public abstract class QHeightAttribute : QNameAttribute
    {
        public float height = 30;
        public QHeightAttribute(string name, float height = 30, string showControl=""):base(name,showControl)
        {
            this.height = height;
        }
    }
 
}
