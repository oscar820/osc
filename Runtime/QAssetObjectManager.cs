using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
    [System.Serializable]
    public class QAssetObjectReference:IKey<string>
    {
        public string Key { get => id; set => id = value; }
        public string id;
        public Object obj;
    }
    public class QAssetObjectManager : InstanceScriptable<QAssetObjectManager>
    {
        public List<QAssetObjectReference> objList = new List<QAssetObjectReference>();
    }
  
    [System.Serializable]
    public class QObjectReference
    {
        public string id;
        Object _obj;

        public Object GetObject()
        {
            if (_obj == null)
            {
                _obj = GetObject(id);
            }
            return _obj;
        }
        public T Get<T>()where T : Object
        {
            if (_obj == null)
            {
                _obj = Get<T>(id);
            }
            return _obj as T;
        }
        public static T Get<T>(string id)where T:Object
        {
            if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T)))
            {
                return Get<GameObject>(id)?.GetComponent<T>();
            }
            else
            {
                return GetObject(id) as T;
            }
        }
      
        public static string GetId(Object obj)
        {
            if (obj != null)
            {
                if (obj is GameObject gObj)
                {
                    obj = gObj;
                }
                else if (obj is MonoBehaviour monoObj)
                {
                    obj = monoObj.gameObject;
                }
#if UNITY_EDITOR
                if (UnityEditor.EditorUtility.IsPersistent(obj))
                {
                    var objRef = QAssetObjectManager.Instance.objList.Get(obj, (objRef) => objRef.obj);
                    if (objRef == null)
                    {
                        objRef = new QAssetObjectReference
                        {
                            Key = QId.GetNewId(),
                            obj = obj,
                        };
                        QAssetObjectManager.Instance.objList.Add(objRef);
                        UnityEditor.EditorUtility.SetDirty(QAssetObjectManager.Instance);
                        UnityEditor.AssetDatabase.SaveAssets();
                    }
                    return objRef.Key;
                }
                else
#endif
                {
                    if (obj is GameObject gameObj)
                    {
                        var qId = gameObj.GetComponent<QId>();
                        if (qId == null)
                        {
                            qId = gameObj.AddComponent<QId>();
                            gameObj.SetDirty();
                          
                        }
                        if (!gameObj.activeSelf)
                        {
                            QIdInitManager.Instance.qIdInitList.AddCheckExist(qId);
                        }
                        return qId.InstanceId;
                    }
                }
            }
            return "";
        }
        public static Object GetObject(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            if (QId.InstanceIdList.ContainsKey(id) && QId.InstanceIdList[id] != null)
            {
                return QId.InstanceIdList[id].gameObject;
            }
            else if (QAssetObjectManager.Instance.objList.ContainsKey(id))
            {
                return QAssetObjectManager.Instance.objList.Get(id).obj;
            }
            return null;
        }

    }
    public class QObjectReference<T> : QObjectReference where T:Object
    {
        public T Object
        {
            get
            {
                return Get<T>();
            }
        }
    }
   
}