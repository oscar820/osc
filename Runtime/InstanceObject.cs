using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Asset;
using System;

namespace QTool
{
    public abstract class InstanceObject<T> where T : InstanceObject<T>
    {
        public static readonly T Instance = Activator.CreateInstance<T>();
        protected InstanceObject()
        {

        }
    }
    public abstract class InstanceScriptable<T> : ScriptableObject where T : InstanceScriptable<T>
    {
        protected static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                     _instance= Resources.Load<T>(typeof(T).Name);
#if UNITY_EDITOR
                    if (_instance==null)
                    {
                        var obj = ScriptableObject.CreateInstance<T>();
                        _instance = obj;
                        UnityEditor.AssetDatabase.CreateAsset(obj, ("Assets/Resources/" + typeof(T).Name + ".asset").CheckFolder());
						if (!Application.isPlaying)
						{
							UnityEditor.AssetDatabase.Refresh();
						}
                    }
#endif 
                }
                return _instance; 
            }
        }
        public virtual void Awake()
        {
            if (_instance != null) return;
            _instance = this as T;
            QDebug.Log("初始化单例【" + typeof(T).Name + "】");
        }
    }
    public abstract class InstanceBehaviour<T> : MonoBehaviour where T : InstanceBehaviour<T>
    {
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>(true);
                }
                return _instance;
            }
        }
        protected static T _instance;
        protected virtual void Awake()
        {
            _instance = this as T;
        }
    }

	public abstract class InstanceManager<T> : InstanceBehaviour<T> where T : InstanceManager<T>
	{
		public new static T Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = GameObject.FindObjectOfType<T>(true);
					if (_instance == null)
					{
						var obj = new GameObject(typeof(T).Name);
						_instance = obj.AddComponent<T>();
						_instance.SetDirty();
					}
				}
				return _instance;
			}
		}
	}
}
