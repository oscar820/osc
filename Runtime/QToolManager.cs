using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
    public class QToolManager:InstanceManager<QToolManager>
    {
		protected override void Awake()
		{
			base.Awake();
			DontDestroyOnLoad(gameObject);
		}
		public event Action OnUpdate=null;
		private void Update()
		{
			Debug.LogError("update");
			OnUpdate?.Invoke();
		}
	}
    public abstract class QToolManagerBase<T>:MonoBehaviour where T : QToolManagerBase<T>
    {
        private static T _instance;
        public static T Instance {
            get
            {
                if (_instance == null)
                {
                    _instance = QToolManager.Instance.transform.GetComponent<T>();
                    if (_instance == null)
                    {
                        _instance = QToolManager.Instance.gameObject.AddComponent<T>();
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
