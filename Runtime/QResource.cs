using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.Serialize;
using System.Threading.Tasks;
#if Addressables
using UnityEngine.AddressableAssets;
#endif
namespace QTool.Resource
{

    public abstract class ResourceList<TLabel,TObj> where TObj:UnityEngine.Object where TLabel:ResourceList<TLabel,TObj>
    {
        public static QDcitionary<string, TObj> objDic = new QDcitionary<string, TObj>();
        public static string Label
        {
            get
            {
                return typeof(TLabel).Name;
            }
        }
        static bool _loadOver=false;
        public static bool LoadOver() {
#if Addressables
            if (!_loadOver)
            {
                LoadAsync();
            }
#else
            _loadOver=true;
#endif
            return _loadOver;
        }
        private static Action OnLoadOver;
        public static void LoadOverRun(Action action)
        {
            if (LoadOver())
            {
                action?.Invoke();
            }
            else
            {
                OnLoadOver += action;
            }
        }
        public static bool ContainsKey(string key)
        {
            return objDic.ContainsKey(key);
        }
        public static bool Contains(string key)
        {
            return ContainsKey(key);
        }
        public static TObj Get(string key)
        {
            if (ContainsKey(key))
            {
                return objDic[key];
            }
            else
            {
#if Addressables
                 if(LoadOver())
                {
                    Debug.LogError(Label + "标签中不存在资源[" + key + "]");
                }
                else
                {
                    Debug.LogError(Label + "找不到资源[" + key + "]");
                }
                return null;
#else
                var obj = Resources.Load<TObj>(Label + '/' + key);
                if (obj == null)
                {
                    Debug.LogError("不找不到资源" + Label + '/' + key);
                }
                return obj;
#endif
            }
        }
        #if Addressables
        public static async void GetAsync(string key,Action<TObj> loadOver)
        {
            if (objDic.ContainsKey(key))
            {
                loadOver?.Invoke(Get(key));
            }
            else
            {
                var load = Addressables.LoadAssetAsync<TObj>(key);
                load.Completed += (result) =>
                {
                    if (result.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                    {
                        Set(key, result.Result);
                        loadOver?.Invoke(result.Result);
                    }
                    else
                    {
                        loadOver?.Invoke(null);
                    }
                };
                await load.Task;
            }
        }
    #endif

        public static void Set(string key,TObj obj)
        {
            objDic[key] = obj;
        }
#if Addressables
        static Task loaderTask;
        public static async void LoadAsync()
        {
            if (_loadOver|| loaderTask!=null) return;
            var load = Addressables.LoadAssetsAsync<TObj>(Label, null);
            load.Completed += (loader) =>
            {
                if (loader.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    foreach (var result in loader.Result)
                    {
                        if (result == null) continue;
                        Set(result.name, result);
                        if (result is GameObject)
                        {
                            var qid = (result as GameObject).GetComponentInChildren<QId>();
                            if (qid != null)
                            {
                                Set(qid.PrefabId, result);
                            }
                        }
                    }
                    Debug.Log("[" + Label + "]加载完成总数" + objDic.Count);
                    _loadOver = true;
                    OnLoadOver?.Invoke();
                    OnLoadOver = null;
                }
                else
                {
                    if (loader.OperationException != null)
                    {
                        Debug.LogError("加载资源表[" + Label + "]出错" + loader.OperationException);
                    }
                }
            };
            loaderTask = load.Task;
            await load.Task;
            
        }
#endif
    }
    public abstract class PrefabResourceList<TLabel>: ResourceList<TLabel,GameObject> where TLabel:PrefabResourceList<TLabel>
    {
        static Dictionary<string, ObjectPool<GameObject>> PoolDic = new Dictionary<string, ObjectPool<GameObject>>();
        static ObjectPool<GameObject> GetPool(string key)
        {
            var poolkey = key + "_ObjPool";
            if (!PoolDic.ContainsKey(poolkey))
            {
                var prefab = Get(key) as GameObject;
                if (prefab == null)
                {
                    new Exception(Label + "找不到预制体资源" + key);
                    PoolDic.Add(poolkey, null);
                }
                else
                {
                    var pool = PoolManager.GetPool(poolkey, prefab);
                    PoolDic.Add(poolkey, pool);
                }
            }
            return PoolDic[poolkey];
        }
        public static GameObject GetInstance(string key,Transform parent = null)
        {
            var obj = GetPool(key)?.Get();
            if (obj == null)
            {
                return null;
            }
            if (parent != null)
            {
                obj.transform.SetParent(parent);
            }
            if(obj.transform is RectTransform)
            {
                var prefab = Get(key);
                (obj.transform as RectTransform).anchoredPosition = (prefab.transform as RectTransform).anchoredPosition;
            }
            obj.name = key;
            return obj;
        }
        public static GameObject GetInstance(string key, Vector3 position,Quaternion rotation,Transform parent = null)
        {
            var obj = GetInstance(key, parent);
            obj.transform.position = position;
            obj.transform.localRotation = rotation;
            return obj;
        }
        public static void Push(string key,GameObject obj)
        {
            var pool = GetPool(key);
            if (pool != null)
            {
                obj = pool.Push(obj);
            }
            if (obj != null)
            {
                GameObject.Destroy(obj);
                Debug.LogError("强制删除[" + key + "]:" + obj.name);
            }
        }
        public static void Push(GameObject obj)
        {
            Push(obj.name, obj);
        }
        public static CT GetInstance<CT>(string key, Transform parent = null) where CT : Component
        {
            var obj = GetInstance(key, parent);
            if (obj == null)
            {
                return null;
            }
            return obj.GetComponent<CT>();
        }
        public static CT GetInstance<CT>(string key, Vector3 pos, Quaternion rotation, Transform parent = null) where CT : Component
        {
            var obj = GetInstance(key, pos, rotation, parent);
            if (obj == null)
            {
                return null;
            }
            return obj.GetComponent<CT>();
        }
    }
}


