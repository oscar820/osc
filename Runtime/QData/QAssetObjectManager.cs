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
		[SerializeField]
        internal List<QAssetObjectReference> objList = new List<QAssetObjectReference>();

		public QDictionary<string, Object> ObjectCache { get; private set; } = new QDictionary<string, Object>();
		public QDictionary<Object, string> IdCache { get; private set; } = new QDictionary<Object, string>();
		private void OnEnable()
		{
			foreach (var or in objList)
			{
				if (or.obj == null) continue;
				ObjectCache[or.Key] = or.obj;
				IdCache[or.obj] = or.id;
			}
		}
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
					var objRef = QAssetObjectManager.Instance.objList.Get(obj, (item) => item.obj);
                    if (objRef == null)
                    {
						var id = UnityEditor.AssetDatabase.GetAssetPath(obj);
                        objRef = new QAssetObjectReference
                        {
                            Key = id,
                            obj = obj,
                        };
                        QAssetObjectManager.Instance.objList.Add(objRef);
						QAssetObjectManager.Instance.ObjectCache[objRef.id] = objRef.obj;
						QAssetObjectManager.Instance.IdCache[objRef.obj] = objRef.id;
						if (!Application.isPlaying)
						{ 
							UnityEditor.EditorUtility.SetDirty(QAssetObjectManager.Instance);
							UnityEditor.AssetDatabase.SaveAssets();
						}
						QDebug.Log("???????????? " + obj + " ??????Id???" + id);
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
                            QIdInitSetting.Instance.qIdInitList.AddCheckExist(qId);
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
			else if (QAssetObjectManager.Instance.ObjectCache.ContainsKey(id))
            {
                return QAssetObjectManager.Instance.ObjectCache[id];
            }

#if UNITY_EDITOR
			else if (id.StartsWith("Assets")&&!Application.isPlaying)
			{
				var obj = UnityEditor.AssetDatabase.LoadAssetAtPath(id, type);
				if (obj != null)
				{
					GetId(obj);
					return obj;
				}
			}
#endif
			Debug.LogError("?????????[" + id + "]??????");
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
