using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
    public static class QToolManager
    {
        private static GameObject _instanceObj;
        public static GameObject InstanceObj
        {
            get
            {
                if (_instanceObj == null)
                {
                    _instanceObj = new GameObject(nameof(QToolManager));
                }
                return _instanceObj;
            }
        }
    }
    [ExecuteInEditMode]
    public abstract class QToolManagerBase<T>:MonoBehaviour where T : QToolManagerBase<T>
    {
        private static T _instance;
        public static T Instance {
            get
            {
                if (_instance == null)
                {
                    _instance= QToolManager.InstanceObj.AddComponent<T>();
#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(_instance);
#endif
                }
                return _instance;
            }
        }
    }
    public class QIdManager : QToolManagerBase<QIdManager>
    {
        public List<QId> qIdInitList = new List<QId>();
        public void Awake()
        {
            if (Application.isPlaying)
            {
                GameObject.DontDestroyOnLoad(gameObject);
            }
            foreach (var id in qIdInitList)
            {
                QId.InstanceIdList[id.InstanceId] = id;
            }
        }
    }
}