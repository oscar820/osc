using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Resource;
namespace QTool
{
  
    public abstract class InstanceBehaviour<T,ResourceType> : InstanceBehaviour<T> where ResourceType : PrefabResourceList<ResourceType> where T : InstanceBehaviour<T,ResourceType>
    {
        public new static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                    if (_instance == null)
                    {
                        PrefabResourceList<ResourceType>.LoadOverRun(() =>
                        {
                            if (_instance == null)
                            {
                                _instance = GetNewInstance();
                            }
                        });
                    }
                }
                return _instance;
            }
        }
        public static T GetNewInstance()
        {
           return  PrefabResourceList<ResourceType>.Get(typeof( ResourceType).Name).GetComponent<T>();
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
                    _instance = FindObjectOfType<T>();
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
    public abstract class InstanceObject<T> where T : InstanceObject<T>,new()
    {
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new T();
                    _instance.InstanceInit();
                }
                return _instance;
            }
        }
        protected static T _instance;
        protected abstract void InstanceInit();
    }
}