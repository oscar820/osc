using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Asset;
using System;

namespace QTool
{
  
    public abstract class InstanceBehaviour<T,ResourceLabel> : InstanceBehaviour<T> where ResourceLabel : PrefabAssetList<ResourceLabel> where T : InstanceBehaviour<T,ResourceLabel>
    {
        public  new static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                    if (_instance == null)
                    {
                        if (_instance == null)
                        {
                             GetNewInstance();
                        }
                    }
                }
                return _instance;
            }
        }
       
        public static async void GetNewInstance()
        {
            _instance=(await PrefabAssetList<ResourceLabel>.GetInstance(typeof(T).Name)).GetComponent<T>();
        }
    }
    public abstract class InstanceBehaviourAutoCreate<T> : MonoBehaviour where T : InstanceBehaviourAutoCreate<T>
    {
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                    if (_instance == null)
                    {
                        var obj = new GameObject(typeof(T).Name);
                        _instance = obj.AddComponent<T>();
                    }
                }
                return _instance;
            }
        }
        protected static T _instance;

        protected virtual void Awake()
        {
            //if (_instance != null)
            //{
            //    Debug.LogWarning("已存在 " + _instance+" 自动删除 "+this);
            //    Destroy(gameObject); ;
            //}
            //else
            //{
                _instance = this as T;
            //}
        }
    }
    public abstract class InstanceBehaviour<T> : MonoBehaviour where T : InstanceBehaviour<T>
    {
        public static T Instance
        {
            get
            {
                return _instance;
            }
        }
        protected static T _instance;
        protected virtual void Awake()
        {
            //if (_instance != null)
            //{
            //    Debug.LogWarning("已存在 " + _instance + " 自动删除 " + this);
            //    Destroy(gameObject);;
            //}
            //else
            //{
                _instance = this as T;
           // }
        }
    }
    public abstract class InstanceObject<T> where T : InstanceObject<T>
    {
        public static readonly T Instance = Activator.CreateInstance<T>();
        protected InstanceObject()
        {

        }
    }
}