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
    public class QIdObject
    {
        public string id;
        Object _obj;

        public Object GetObject()
        {
            if (_obj == null)
            {
                _obj = GetObject(id,typeof(Object));
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
			return GetObject(id, typeof(T)) as T;
        }
	
      
        public static string GetId(Object obj)
        {
            if (obj != null)
            {
                if (obj is GameObject gObj)
                {
                    obj = gObj;
                }
                else if (obj is Component monoObj)
                {
                    obj = monoObj.gameObject;
                }
#if UNITY_EDITOR
                if (UnityEditor.EditorUtility.IsPersistent(obj))
                {
					QAssetObjectManager.Instance.objList.RemoveNull();
					var objRef = QAssetObjectManager.Instance.objList.Get(obj, (item) => item.obj);
                    if (objRef == null)
                    {
						var id = UnityEditor.AssetDatabase.GetAssetPath(obj);
						//if (QAssetObjectManager.Instance.objList.ContainsKey(id))
						//{
						//	id = UnityEditor.AssetDatabase.GetAssetPath(obj)+"_"+ QId.GetNewId();
						//}
                        objRef = new QAssetObjectReference
                        {
                            Key = id,
                            obj = obj,
                        };
                        QAssetObjectManager.Instance.objList.Add(objRef);
						if (!Application.isPlaying)
						{ 
							UnityEditor.EditorUtility.SetDirty(QAssetObjectManager.Instance);
							UnityEditor.AssetDatabase.SaveAssets();
						}
						QDebug.Log("生成对象 " + obj + " 引用Id：" + id);
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
                        if (!Application.isPlaying&& !gameObj.activeSelf)
                        {
                            QIdInitManager.Instance.qIdInitList.AddCheckExist(qId);
                        }
                        return qId.InstanceId;
                    }
                }
            }
            return "";
        }
        public static Object GetObject(string id,System.Type type)
        {
			if (typeof(Component).IsAssignableFrom(type))
			{
				return Get<GameObject>(id)?.GetComponent(type);
			}
			if (string.IsNullOrWhiteSpace(id)) return null;
            if (QId.InstanceIdList.ContainsKey(id) && QId.InstanceIdList[id] != null)
            {
                return QId.InstanceIdList[id].gameObject;
			}
			else if (QAssetObjectManager.Instance.objList.ContainsKey(id))
            {
                return QAssetObjectManager.Instance.objList.Get(id).obj;
            }

#if UNITY_EDITOR
			else if (id.StartsWith("Assets"))
			{
				var obj = UnityEditor.AssetDatabase.LoadAssetAtPath(id, type);
				if (obj != null)
				{
					GetId(obj);
					return obj;
				}
			}
#endif
			return null;
        }

    }
    public class QIdObject<T> : QIdObject where T:Object
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
