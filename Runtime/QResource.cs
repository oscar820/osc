using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.Serialize;
using System.Threading.Tasks;


namespace QTool.Resource
{
#if Addressables
    using UnityEngine.AddressableAssets;
#if UNITY_EDITOR
    using UnityEditor.AddressableAssets.Settings;
    using UnityEditor.AddressableAssets;
    using UnityEditor;
    using System.IO;

    public static  class AddressableTool
    {
        public static QDictionary<string, List<AddressableAssetEntry>> labelDic = new QDictionary<string, List<AddressableAssetEntry>>();
        public static QDictionary<string, AddressableAssetGroup> groupDic = new QDictionary<string, AddressableAssetGroup>();
        public static QDictionary<string, AddressableAssetEntry> entryDic = new QDictionary<string, AddressableAssetEntry>();
        public static void SetAddresableGroup(string assetPath, string groupName, string key = "")
        {
            var group = GetGroup(groupName);
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = entryDic.ContainsKey(guid) ? entryDic[guid] : AssetSetting.FindAssetEntry(guid);
            if (entry == null)
            {
                entry = AssetSetting.CreateOrMoveEntry(guid, group);
            }
            else if (entry.parentGroup != group)
            {
                AssetSetting.MoveEntry(entry, group);
            }
            else
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(key))
            {
                entry.address = Path.GetFileNameWithoutExtension(assetPath);
            }
            else
            {
                entry.address = key;
            }
            if (!entry.labels.Contains(groupName))
            {
             //   entry.labels.Clear();
                entry.SetLabel(groupName, true, true);
            }
        }
       

        public static AddressableAssetSettings AssetSetting
        {
            get
            {
                return AddressableAssetSettingsDefaultObject.Settings;
            }
        }
        public static List<AddressableAssetEntry> GetLabelList(string label)
        {
            if (labelDic[label] == null)
            {
                labelDic[label] = new List<AddressableAssetEntry>();
            }
            else
            {
                labelDic[label].Clear();
            }
            foreach (var group in AssetSetting.groups)
            {
                foreach (var item in group.entries)
                {
                    if (item.labels.Contains(label))
                    {
                        labelDic[label].Add(item);
                    }
                }
            }
            return labelDic[label];
      
        }
        public static AddressableAssetGroup GetGroup(string groupName)
        {
            var group = groupDic[groupName];
            if (group == null)
            {
                group = AssetSetting.FindGroup(groupName);
                if (group == null)
                {
                    group = AssetSetting.CreateGroup(groupName, false, false, false, new List<AddressableAssetGroupSchema>
                    {AssetSetting.DefaultGroup.Schemas[0],AssetSetting.DefaultGroup.Schemas[1] }, typeof(System.Data.SchemaType));
                }
                else
                {
                    foreach (var e in group.entries)
                    {
                        entryDic[e.guid] = e;
                    }
                }
            }
            return group;
        }
    }
#endif

#endif
    public abstract class ResourceList<TLabel,TObj> where TObj:UnityEngine.Object where TLabel:ResourceList<TLabel,TObj>
    {
        public static QDictionary<string, TObj> objDic = new QDictionary<string, TObj>();
        public static string Label
        {
            get
            {
                return typeof(TLabel).Name;
            }
        }
        public static void Clear()
        {
            _loadOver = false;
#if Addressables
            loaderTask = null;
#endif
            objDic.Clear();
        }
        static bool _loadOver=false;
        public static bool LoadOver() {

            if (!_loadOver)
            {
                LoadAll();
            }
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
        public static TObj GetResource(string key)
        {
            LoadOver();
            if (ContainsKey(key))
            {
                return objDic[key];
            }
            else
            {
                Debug.LogError(Label + "找不到资源[" + key + "]");
                return null;
            }
        }
        protected static void LoadAll()
        {
            if (_loadOver) return;
#if Addressables
            LoadAsync();
#else
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                (Application.dataPath + "\\Resources\\" + Label).ForeachDirectoryFiles((path) =>
                {
                    Set( UnityEditor.AssetDatabase.LoadAssetAtPath<TObj>(Label));
                });
            }
#endif
            foreach (var obj in Resources.LoadAll<TObj>(Label))
            {   
                Set(obj);
            }
            _loadOver = true;
#endif
        }
#if Addressables
        public static async Task GetAsync(string key,Action<TObj> loadOver)
        {
            if (objDic.ContainsKey(key))
            {
                loadOver?.Invoke(GetResource(key));
            }
            else
            {
                var loader = Addressables.LoadAssetAsync<TObj>(key);
                loader.Completed += (result) =>
                {
                    if (result.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                    {
                        Set(result.Result,key);
                        loadOver?.Invoke(result.Result);
                    }
                    else
                    {
                        loadOver?.Invoke(null);
                    }
                };
                if (loader.OperationException != null)
                {
                    Debug.LogError("异步加载"+Label+ "资源[" + key + "]出错"+loader.OperationException);
                };
                await loader.Task;
            }
        }
#endif

        public static void Set(TObj obj, string key="")
        {
            if (obj == null) return;
            if (string.IsNullOrEmpty(key))
            {
                key = obj.name;
            }
            objDic[key] = obj;
            ToolDebug.Log("资源缓存[" + key + "]:" + obj);
            if (obj is GameObject)
            {
                var qid = (obj as GameObject).GetComponentInChildren<QId>();
                if (qid != null&&qid.PrefabId!=key)
                {
                    Set( obj, qid.PrefabId);
                }
            }
        }
#if Addressables
        static Task loaderTask;
        public static async Task LoadAsync()
        {
            if (_loadOver || loaderTask != null) return;
            if (Application.isPlaying)
            {
                var load = Addressables.LoadAssetsAsync<TObj>(Label, null);
                load.Completed += (loader) =>
                {
                    if (loader.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                    {
                        foreach (var result in loader.Result)
                        {
                            Set(result);
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
            else
            {
#if UNITY_EDITOR
                var list = AddressableTool.GetLabelList(Label);
                foreach (var entry in list)
                {
                    Set(entry.TargetAsset as TObj,entry.address);
                }
                _loadOver = true;
                OnLoadOver?.Invoke();
                OnLoadOver = null;
#endif
            }
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
                var prefab = GetResource(key) as GameObject;
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
                var prefab = GetResource(key);
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


