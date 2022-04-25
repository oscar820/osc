using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
    public class QToolManager:InstanceBehaviour<QToolManager>
    {
        public static GameObject InstanceObj => Instance.gameObject;
        public new static QToolManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GameObject.FindObjectOfType<QToolManager>();
                    if (_instance == null)
                    {
                        var obj= GameObject.Find(nameof(QToolManager));
                        if (obj == null)
                        {
                            obj = new GameObject(nameof(QToolManager));
                        }
                      
                        _instance = obj.AddComponent<QToolManager>();
                        _instance.SetDirty();
                    }
                }
                return _instance;
            }
        }
        protected override void Awake()
        {
            base.Awake();
            if (Application.isPlaying)
            {
                GameObject.DontDestroyOnLoad(gameObject);
            }
        }
    }
    [ExecuteInEditMode]
    public abstract class QManagerBase<T>:MonoBehaviour where T : QManagerBase<T>
    {
        private static T _instance;
        public static T Instance {
            get
            {
                if (_instance == null)
                {
                    _instance= QToolManager.InstanceObj.AddComponent<T>();
                    _instance.SetDirty();
                }
                return _instance;
            }
        }
        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
            }
        }
    }

   
}