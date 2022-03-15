using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace QTool
{
   
    public static class QEventManager
    {
        /// <summary>
        /// 当任意事件触发时调用
        /// </summary>
        public static event System.Action<string> OnEventTigger;
        /// <summary>
        /// 事件列表 对应事件触发时调用对应Action 使用方法： EventList["事件名"]+=Action;
        /// </summary>
        internal static QDictionary<string, System.Action> EventList = new QDictionary<string, System.Action>();
        /// <summary>
        /// 触发事件
        /// </summary>
        /// <param name="eventKey">事件名</param>
        public static void Trigger(string eventKey)
        {
            eventKey = eventKey.Trim();
            if (string.IsNullOrWhiteSpace(eventKey))
            {
                return;
            }
            OnEventTigger?.Invoke(eventKey);
            if (EventList.ContainsKey(eventKey))
            {
                EventList[eventKey]?.Invoke();
            }
        }
        public static void Trigger<T>(string eventKey,T value)
        {
            QEventManager<T>.Trigger(eventKey, value);
        }
        public static void Register(string eventKey, System.Action action)
        {
            EventList[eventKey] += action;
        }
        public static void UnRegister(string eventKey, System.Action action)
        {
            EventList[eventKey] -= action;
        }
        public static void Register<T>(string eventKey,System.Action<T> action)
        {
            QEventManager<T>.EventList[eventKey] += action;
        }
        public static void UnRegister<T>(string eventKey, System.Action<T> action)
        {
            QEventManager<T>.EventList[eventKey] -= action;
        }
    }
    public class QEventManager<T>
    {
        /// <summary>
        /// 当任意事件触发时调用
        /// </summary>
        public static event System.Action<string, T> OnEventTigger;
        /// <summary>
        /// 事件列表 对应事件触发时调用对应Action 使用方法： EventList["事件名"]+=Action;
        /// </summary>
        internal static QDictionary<string, System.Action<T>> EventList = new QDictionary<string, System.Action<T>>();
        public static void Trigger(string eventKey,T value)
        {
            eventKey = eventKey.Trim();
            if (string.IsNullOrWhiteSpace(eventKey))
            {
                return;
            }
            OnEventTigger?.Invoke(eventKey, value);
            if (EventList.ContainsKey(eventKey))
            {
                EventList[eventKey]?.Invoke(value);
            }
        }
    }
    [System.Serializable]
    public class QEventTrigger : MonoBehaviour
    {
        public List<ActionEventTrigger> actionEventList = new List<ActionEventTrigger>();
        public List<StringEventTrigger> stringEventList = new List<StringEventTrigger>();
        public List<BoolEventTrigger> boolEventList = new List<BoolEventTrigger>();
        public List<FloatEventTrigger> floatEventList = new List<FloatEventTrigger>();
        public void Invoke(string eventName, string value)
        {
            stringEventList.Get(eventName)?.eventAction?.Invoke(value);
        }
        public void Invoke(string eventName)
        {
            actionEventList.Get(eventName)?.eventAction?.Invoke();
        }
        public void Invoke(string eventName, bool value)
        {
            boolEventList.Get(eventName)?.eventAction?.Invoke((bool)value);
        }
        public new void Invoke(string eventName, float value)
        {
            floatEventList.Get(eventName)?.eventAction?.Invoke(value);
        }
    }
    public class EventTriggerBase<T> : IKey<string> where T : UnityEventBase
    {
        public string EventName;
        public string Key { get => EventName; set => EventName = value; }
        public T eventAction = default;
    }
    [System.Serializable]
    public class ActionEvent : UnityEvent
    {
    }
    [System.Serializable]
    public class BoolEvent : UnityEvent<bool>
    {
    }
    [System.Serializable]
    public class IntEvent : UnityEvent<int>
    {
    }
    [System.Serializable]
    public class FloatEvent : UnityEvent<float>
    {
    }
    [System.Serializable]
    public class StringEvent : UnityEvent<string>
    {
    }
    [System.Serializable]
    public class SpriteEvent : UnityEvent<Sprite>
    {
    }
    [System.Serializable]
    public class Vector3Event : UnityEvent<Vector3>
    {
    }

    [System.Serializable]
    public class FloatEventTrigger : EventTriggerBase<FloatEvent>
    {
    }
    [System.Serializable]
    public class BoolEventTrigger : EventTriggerBase<BoolEvent>
    {
    }
    [System.Serializable]
    public class ActionEventTrigger : EventTriggerBase<UnityEvent>
    {
    }
    [System.Serializable]
    public class StringEventTrigger : EventTriggerBase<StringEvent>
    {
    }


    public static class ValueEventTriggerExtends
    {

        public static QEventTrigger GetTrigger(this GameObject obj)
        {
            if (obj == null)
            {
                return null;
            }
            var tigger = obj.GetComponentInChildren<QEventTrigger>(true);
            return tigger;
        }

        public static QEventTrigger GetParentTrigger(this GameObject obj)
        {
            if (obj.transform.parent == null || obj == null)
            {
                return null;
            }
            var tigger = obj.transform.parent.GetComponentInParent<QEventTrigger>();
            return tigger;
        }
        public static void InvokeEvent(this GameObject obj, string eventName)
        {
            obj.GetTrigger()?.Invoke(eventName.Trim());
        }
        public static void InvokeEvent(this GameObject obj, string eventName, bool value)
        {
            obj.GetTrigger()?.Invoke(eventName.Trim(), value);
        }
        public static void InvokeEvent(this GameObject obj, string eventName, float value)
        {
            obj.GetTrigger()?.Invoke(eventName.Trim(), value);
        }
        public static void InvokeEvent(this GameObject obj, string eventName, string value)
        {
            obj.GetTrigger()?.Invoke(eventName.Trim(), value);
        }
        public static void InvokeParentEvent(this GameObject obj, string eventName)
        {
            obj.GetParentTrigger()?.Invoke(eventName.Trim());
        }
        public static void InvokeParentEvent(this GameObject obj, string eventName, bool value)
        {
            obj.GetParentTrigger()?.Invoke(eventName.Trim(), value);
        }
        public static void InvokeParentEvent(this GameObject obj, string eventName, float value)
        {
            obj.GetParentTrigger()?.Invoke(eventName.Trim(), value);
        }
        public static void InvokeParentEvent(this GameObject obj, string eventName, string value)
        {
            obj.GetParentTrigger()?.Invoke(eventName.Trim(), value);
        }
    }
}