using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.Serialize;
using System.Threading.Tasks;


namespace QTool.Asset
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
    public abstract class AssetList<TLabel,TObj> where TObj:UnityEngine.Object where TLabel:AssetList<TLabel,TObj>
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
            ToolDebug.Log("清空ResourceList<" + Label+">");
        }
       
        public static bool ContainsKey(string key)
        {
            return objDic.ContainsKey(key);
        }
        public static void Set(TObj obj, string key = "", bool checkQid = true)
        {
            if (obj == null) return;
            var setKey = key;
            if (string.IsNullOrEmpty(setKey))
            {
                setKey = obj.name;
            }
            objDic[setKey] = obj;
            ToolDebug.Log("资源缓存[" + setKey + "]:" + obj);
            if (checkQid)
            {
                if (obj is GameObject)
                {
                    var qid = (obj as GameObject).GetComponentInChildren<QId>();
                    if (qid != null && !string.IsNullOrWhiteSpace(qid.PrefabId) && qid.PrefabId != key)
                    {
                        Set(obj, qid.PrefabId, false);
                    }
                }
            }
        }
        public static TObj Get(string key)
        {
            if (!_loadOver)
            {
                LoadAllAsync();
            }
            if (objDic.ContainsKey(key))
            {
                return objDic[key];
            }
            else if(_loadOver)
            {
                Debug.LogError("不存在资源" + Label + '\\' + key);
            }
            else
            {
                Debug.LogError("未初始化" + Label);
            }
            return null;
        }
        public static async Task<TObj> GetAsync(string key)
        {
            if (objDic.ContainsKey(key))
            {
                return objDic[key];
            }
            else
            {
#if Addressables
                return await AddressableGetAsync(key);
#else
                return ResourceGet(key);
#endif
            }
        }

        static bool _loadOver = false;
        public static async Task LoadAllAsync()
        {
            if (_loadOver) return;
#if Addressables
            await AddressableLoadAll();
#else
            ResourceLoadAll();
#endif
        }


 
#if Addressables
        #region Addressable加载

        static async Task<TObj> AddressableGetAsync(string key)
        {
            if (objDic.ContainsKey(key))
            {
                return objDic[key];
            }
            else
            {
                if (Application.isPlaying)
                {
                    var loader = Addressables.LoadAssetAsync<TObj>(key);
                    var obj = await loader.Task;
                    if(loader.Status== UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                    {

                        Set(obj, key);
                    }
                    else
                    {
                        if (loader.OperationException != null)
                        {
                            Debug.LogError("异步加载" + Label + "资源[" + key + "]出错" + loader.OperationException);
                        }
                    }
                    return obj;

                }
                else
                {
                    await AddressableLoadAll();
                    if (objDic.ContainsKey(key))
                    {
                        return objDic[key];
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
        static Task loaderTask;
        public static async Task AddressableLoadAll()
        {
            if (_loadOver || loaderTask != null) return;
            if (Application.isPlaying)
            {
                var loader = Addressables.LoadAssetsAsync<TObj>(Label, null);
                loaderTask = loader.Task;
                var obj = await loader.Task;
                if(loader.Status== UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {

                    foreach (var result in loader.Result)
                    {
                        Set(result);
                    }
                    ToolDebug.Log("[" + Label + "]加载完成总数" + objDic.Count);
                    _loadOver = true; 
                }
                else
                {
                    if (loader.OperationException != null)
                    {
                        Debug.LogError("加载资源表[" + Label + "]出错" + loader.OperationException);
                    }
                }
             
            }
            else
            {
#if UNITY_EDITOR
                var list = AddressableTool.GetLabelList(Label);
                foreach (var entry in list)
                {
                    Set(entry.TargetAsset as TObj, entry.address);
                }
                _loadOver = true;
#endif
            }
        }


        #endregion
#else
        #region Resource加载

        static TObj ResourceGet(string key)
        {
            ResourceLoadAll();
            if (objDic.ContainsKey(key)) {
                return objDic[key];
            }
            else
            {
                Debug.LogError("不存在资源 Resources\\" + Label + '\\' + key);
                return null;
            }
        }
        static void ResourceLoadAll()
        {
            if (_loadOver)
            {
                return;
            }
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                Application.dataPath.ForeachDirectory((rootPath) =>
                {
                    if (rootPath.EndsWith("\\Resources"))
                    {
                        if (System.IO.Directory.Exists(rootPath + "\\" + Label))
                        {
                            (rootPath + "\\" + Label).ForeachDirectoryFiles((loadPath) =>
                            {
                                Set(UnityEditor.AssetDatabase.LoadAssetAtPath<TObj>(Label));
                            });
                        }
                    }
                });
#endif
            }
            else
            {
                foreach (var obj in Resources.LoadAll<TObj>(Label))
                {
                    Set(obj);
                }
            }
            _loadOver = true;
        }
        #endregion
#endif
    }
    public abstract class PrefabAssetList<TLabel>: AssetList<TLabel,GameObject> where TLabel:PrefabAssetList<TLabel>
    {
        static Dictionary<string, ObjectPool<GameObject>> PoolDic = new Dictionary<string, ObjectPool<GameObject>>();
        static async Task<ObjectPool<GameObject>> GetPool(string key)
        {
            var poolkey = key + "_ObjPool";
            if (!PoolDic.ContainsKey(poolkey))
            {
                var prefab =await GetAsync(key) as GameObject;
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
        public static async Task<GameObject> GetInstance(string key,Transform parent = null)
        {
            var pool =await GetPool(key);
            var obj= pool.Get();
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
                var prefab =await GetAsync(key);
                (obj.transform as RectTransform).anchoredPosition = (prefab.transform as RectTransform).anchoredPosition;
            }
            obj.name = key;
            return obj;
        }
        public static async Task<GameObject> GetInstance(string key, Vector3 position,Quaternion rotation,Transform parent = null)
        {
            var obj =await GetInstance(key, parent);
            obj.transform.position = position;
            obj.transform.localRotation = rotation;
            return obj;
        }
        public static async void Push(string key,GameObject obj)
        {
            (await GetPool(key))?.Push(obj);
        }
        public static void Push(GameObject obj)
        {
            Push(obj.name, obj);
        }
        public static void Push(List<GameObject> objList)
        {
            foreach (var obj in objList)
            {
                Push(obj);
            }
            objList.Clear();
        }
        public async static Task<CT> GetInstance<CT>(string key, Transform parent = null) where CT : Component
        {
            var obj =await GetInstance(key, parent);
            if (obj == null)
            {
                return null;
            }
            return obj.GetComponent<CT>();
        }
        public async static Task<CT> GetInstance<CT>(string key, Vector3 pos, Quaternion rotation, Transform parent = null) where CT : Component
        {
            var obj =await GetInstance(key, pos, rotation, parent);
            if (obj == null)
            {
                return null;
            }
            return obj.GetComponent<CT>();
        }
    }
}


