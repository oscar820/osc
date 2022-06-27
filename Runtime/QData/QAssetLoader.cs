using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace QTool.Asset
{
	#region AddressableTool

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
                if (entry == null)
                {
                    Debug.LogError("生成资源【" + key + "】出错：" + assetPath);
                    return;
                }
            }
            else if (entry.parentGroup != group)
            {
                AssetSetting.MoveEntry(entry, group);
            }
            if (string.IsNullOrWhiteSpace(key))
            {
                entry.address = Path.GetFileNameWithoutExtension(assetPath);
            }
            else if (entry.address != key)
            {
                entry.address = key;
            }
            if (!entry.labels.Contains(groupName))
            {
             //   entry.labels.Clear();
                entry.SetLabel(groupName, true, true);
            }
            EditorUtility.SetDirty(AssetSetting);
            EditorUtility.SetDirty(group);
        }
        public static AddressableAssetSettings AssetSetting
        {
            get
            {
                return AddressableAssetSettingsDefaultObject.Settings;
            }
        }
		public static List<T> GetLabelList<T>(string label) where T : UnityEngine.Object
		{
            if (labelDic[label] == null)
            {
                labelDic[label] = new List<AddressableAssetEntry>();
            }
            if (AssetSetting != null)
            {

                labelDic[label].Clear();
                foreach (var group in AssetSetting.groups)
                {
                    if (group == null) continue;
                    foreach (var item in group.entries)
                    {
						if(item.TargetAsset is T)
						{
							if (item.labels.Contains(label))
							{
								labelDic[label].Add(item);
							}
						}
                    }
                }
            }
			List<T> objList = new List<T>();
			foreach (var entry in labelDic[label])
			{
				objList.Add(entry.TargetAsset as T);
			}
            return objList;
      
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

	#endregion
	public abstract class QAssetLoader<TPath, TObj> where TObj : UnityEngine.Object
	{
		public static string DirectoryPath
		{
			get
			{
				return typeof(TPath).Name;
			}
		}
		public static string ResourcesPathStart
		{
			get
			{
				return  DirectoryPath + '/';
			}
		}
#if Addressables
		public static string AddressablePathStart
		{
			get
			{
				return   DirectoryPath + '/';
			}
		}
#endif
		public static async Task<IList<TObj>> LoadAllAsync()
		{
			List<TObj> objList = new List<TObj>();
			objList.AddRange(Resources.LoadAll<TObj>(DirectoryPath));
			#region Addressables
#if Addressables
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				try
				{
					objList.AddRange(AddressableTool.GetLabelList<TObj>(DirectoryPath.Replace('\\', '/')));
				}
				catch (Exception e)
				{
					Debug.LogError("加载资源表[" + DirectoryPath.Replace('\\', '/') + "]出错\n" + e);
				}
			}
			else
#endif
			{
				try
				{
					var loader = Addressables.LoadAssetsAsync<TObj>(DirectoryPath, null); ;
					if (loader.OperationException != null)
					{
						throw loader.OperationException;
					}
					var loaderTask = loader.Task;
					var list = await loaderTask;
					if (loaderTask.Exception != null)
					{
						throw loaderTask.Exception;
					}
					objList.AddRange( list);
				}
				catch (Exception e)
				{
					Debug.LogWarning("加载资源表[" + DirectoryPath + "]出错\n" + e);
				}
			}
#endif
			#endregion
			Debug.Log("加载 [" + DirectoryPath + "][" + typeof(TObj) + "] 资源：\n" + objList.ToOneString());
			return objList;
		}
		public static async Task<TObj> LoadAsync(string key)
		{
			var obj = Resources.Load<TObj>(ResourcesPathStart + key.Replace('\\', '/'));
			#region Addressables

#if Addressables
			if (obj == null)
			{
				key = key.Replace('\\', '/');
#if UNITY_EDITOR
				if (!Application.isPlaying)
				{
					obj = AssetDatabase.LoadAssetAtPath<TObj>("AddressableResources/" + AddressablePathStart + key);
				}
				else

#endif
				{
					var loader = Addressables.LoadAssetAsync<TObj>(AddressablePathStart + key);
					obj = await loader.Task;
					if (loader.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
					{
						if (loader.OperationException != null)
						{
							Debug.LogError("异步加载" + AddressablePathStart + key + "出错" + loader.OperationException);
						}
					}
				}
			}
#endif
			#endregion
			if (obj == null)
			{
				Debug.LogError("加载" + key + "出错 结果为空" );
			}
			return obj;
		}
#if Addressables
		public static void ReleaseAddressables<T>(params T[] obj) where T : UnityEngine.Object
		{
			Addressables.Release(obj);
		}
#endif
		
		public static void ReleaseResources<T>(params T[] objs) where T : UnityEngine.Object
		{
			foreach (var obj in objs)
			{
				if (obj == null) continue; ;
				if (obj is GameObject)
				{
					UnityEngine.Object.Destroy(obj);
				}
				else
				{
					Resources.UnloadAsset(obj);
				}
			}
			
		}
	}
    public abstract class QPrefabLoader<TPath>: QAssetLoader<TPath,GameObject> where TPath:QPrefabLoader<TPath>
    {
        static async Task<ObjectPool<GameObject>> GetPool(string key)
        {
            var poolkey =DirectoryPath+"_" + key + "_AssetList";
			var prefab = await LoadAsync(key);
			if (prefab != null)
			{
				return QPoolManager.GetPool(poolkey, prefab);

			}
			else
			{
				return null;
			}
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
            if (key.Contains(" "))
            {
                key = key.Substring(0, key.IndexOf(" "));
            }
			var pool = await GetPool(key);
			if (pool == null)
			{
				GameObject.Destroy(obj);
			}
			else
			{
				pool.Push(obj);
			}
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
        public static async Task<GameObject> GetInstance(string key, Transform parent = null)
        {
            var pool = await GetPool(key);
            if (pool == null)
            {
                Debug.LogError("无法实例化预制体[" + key + "]");
                return null;
            }
            var obj = pool.Get();
            if (obj == null)
            {
                return null;
            }
            if (parent != null)
            {
                obj.transform.SetParent(parent,false);
            }
            if (obj.transform is RectTransform)
            {
                var prefab = await LoadAsync(key);
                (obj.transform as RectTransform).anchoredPosition = (prefab.transform as RectTransform).anchoredPosition;
            }
            obj.name = key;
            return obj;
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


