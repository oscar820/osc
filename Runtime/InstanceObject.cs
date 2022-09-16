using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Asset;
using System;

namespace QTool
{
	/// <summary>
	/// C#对象单例
	/// </summary>
    public abstract class InstanceObject<T> where T : InstanceObject<T>
    {
        public static readonly T Instance = Activator.CreateInstance<T>();
        protected InstanceObject()
        {

        }
    }
	/// <summary>
	/// SO文件单例 存储在Resouces文件夹下
	/// </summary>
    public abstract class InstanceScriptable<T> : ScriptableObject where T : InstanceScriptable<T>
    {
        protected static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
					var key = typeof(T).Name;

					 _instance = Resources.Load<T>(key);
				//	AddressableLoad(key);
#if UNITY_EDITOR
					if (_instance==null)
                    {
                        var obj = ScriptableObject.CreateInstance<T>();
                        _instance = obj;
						var path = ("Assets/Resources/" + key + ".asset");
						QFileManager.CheckPath(ref path);
						UnityEditor.AssetDatabase.CreateAsset(obj, path);
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
	/// <summary>
	/// 不会自动创建的单例
	/// </summary>
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
	/// <summary>
	/// 会自动创建对象的单例
	/// </summary>
	public abstract class InstanceManager<T> : MonoBehaviour where T : InstanceManager<T>
	{
		public static T Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = FindObjectOfType<T>(true);
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
		protected static T _instance;
		protected virtual void Awake()
		{
			_instance = this as T;
		}
	}
}
