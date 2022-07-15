using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
    public class QToolManager:InstanceManager<QToolManager>
    {
    }
    public abstract class QToolManagerBase<T>:MonoBehaviour where T : QToolManagerBase<T>
    {
        private static T _instance;
        public static T Instance {
            get
            {
                if (_instance == null)
                {
                    _instance = QToolManager.InstanceObj.GetComponent<T>();
                    if (_instance == null)
                    {
                        _instance = QToolManager.InstanceObj.AddComponent<T>();
                        _instance.SetDirty();
                    }
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
