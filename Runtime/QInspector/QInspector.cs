using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace QTool
{
    /// <summary>
    /// 更改显示的名字
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter|AttributeTargets.Property)]
    public class ViewNameAttribute : PropertyAttribute
    {
        public string name;
        public string control = "";
        public ViewNameAttribute()
        {
            order = 10;
        }
        public ViewNameAttribute(string name, string showControl = "") 
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
    public class ReadOnlyAttribute : Attribute
    {
        public ReadOnlyAttribute() 
        {
        }
    }
  

}
namespace QTool.Inspector
{
    /// <summary>
    /// 数值更改时调用changeCallBack函数
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ChangeCallAttribute : PropertyAttribute
    {
        public bool change;
        public string changeCallBack;
        public ChangeCallAttribute(string changeCallBack)
        {
            this.changeCallBack = changeCallBack;
        }
    }
  

   

    /// <summary>
    /// 将字符传显示为枚举下拉款通过GetKeyListFunc获取所有可选择的字符串
    /// </summary>
    [AttributeUsage(AttributeTargets.Field|AttributeTargets.Property|AttributeTargets.Parameter)]
    public class ViewEnumAttribute : PropertyAttribute
	{
        public string GetKeyListFunc;
        public bool CanWriteString = false;
        public ViewEnumAttribute(string GetKeyListFunc)
        {
			this.GetKeyListFunc = GetKeyListFunc;
        }

    }

    /// <summary>
    /// 将数组显示为toolbar工具栏通过indexName来设置int值；
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ToolbarListAttribute : QHeightAttribute
    {
        public string listMember;
        public int pageSize= 0;
        //  public bool ResourceImage = false;
        public ToolbarListAttribute( string listMember, float height = 30, string showControl = "") : base("", height, showControl)
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
    public class ScriptToggleAttribute : QHeightAttribute
    {
        public string scriptList="";
        public ScriptToggleAttribute( string scriptList, float height = 30, string showControl = "") : base("", height, showControl)
        {
            this.scriptList = scriptList;
        }
    }
    /// <summary>
    /// 当在scene视窗鼠标事件调用 传入3个参数 (Vector3 点击位置,Collider 点击碰撞物体,bool 是否按下shift键)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SceneInputEventAttribute : Attribute
    {
        public EventType eventType = EventType.MouseDown;
        public KeyCode keyCode = KeyCode.None;
        public SceneInputEventAttribute(EventType eventType)
        {
            this.eventType = eventType;
        }
        public SceneInputEventAttribute(EventType eventType, KeyCode keyCode)
        {
            this.eventType = eventType;
            this.keyCode = keyCode;
        }
    }
    /// <summary>
    /// inspector初始化时调用
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class EidtorInitInvokeAttribute : Attribute
    {
        public EidtorInitInvokeAttribute()
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
    public class EditorModeAttribute : Attribute
    {
        public EditorModeState state;
        public EditorModeAttribute(EditorModeState state)
        {
            this.state = state;
        }

    }
    [AttributeUsage(AttributeTargets.Method)]
    public class ViewButtonAttribute : QHeightAttribute
    {
        public ViewButtonAttribute(string name, float height = 30, string showControl = "") : base(name, height, showControl)
        {
            // order = 0;
        }
    }
    /// <summary>
    /// 选取对象按钮显示 会调用函数CallFunc传入GameObject
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class SelectObjectButtonAttribute : ViewButtonAttribute
    {
        public SelectObjectButtonAttribute(string name, float height = 30, string showControl = "") : base(name,height, showControl)
        {
        }
    }
    /// <summary>
    /// 显示按钮 调用函数CallFunc 无参数
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class ViewToggleAttribute : QHeightAttribute
    {
  
        public ViewToggleAttribute(string name, float height = 30, string showControl="") :base(name, height, showControl)
        {
            
        }

    }
    public abstract class QHeightAttribute : ViewNameAttribute
    {
        public float height = 30;
        public QHeightAttribute(string name, float height = 30, string showControl=""):base(name,showControl)
        {
            this.height = height;
        }
    }
 
}
