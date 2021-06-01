using QTool.Binary;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.Inspector;
namespace QTool
{
   
    public static class QIdExtends
    {
        public static QId GetQId(this MonoBehaviour mono)
        {
            if (mono == null)
            {
                Debug.LogError("游戏对象【" + mono.name + "】不存在QId脚本");
                return null;
            }
            return mono.gameObject.GetQId();
        }
        public static QId GetQId(this GameObject obj)
        {
            if (obj == null)
            {
                return null;
            }
            return obj.GetComponent<QId>();
        }
    }
    [DisallowMultipleComponent]
    public class QId : MonoBehaviour,IKey<string>
    {
#if UNITY_EDITOR
        private void OnValidate()
        {
            InitId();
        }
        private void SetPrefabId(string id)
        {
            if (id != PrefabId)
            {
                PrefabId = id;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
        private void SetInstanceId(string id)
        {
            if (id != InstanceId)
            {
                InstanceId = id;
                UnityEditor.EditorUtility.SetDirty(this);
                InstanceIdList[id] = this;
            }
        }
        public static QDictionary<string, object> InstanceIdList = new QDictionary<string, object>();
        private void InitId()
        {
            if (Application.IsPlaying(gameObject)) return;
            if (IsPrefabAssets)
            {
                SetPrefabId(UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(gameObject)));
              //  SetInstanceId("");
            }
            else if (IsPrefabInstance)
            {
                
                if (!string.IsNullOrWhiteSpace(InstanceId)&&InstanceIdList[InstanceId] == null)
                {
                    InstanceIdList[InstanceId] = this;
                }
                else 
                {
                    SetInstanceId(GetNewId());
                }
                var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
                if (prefab == null)
                {
                    Debug.LogError(gameObject + " 找不到预制体引用");
                }
                else
                {
                    SetPrefabId(UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(prefab)));
                }
            }
            isPrefabObject = HasPrefabId;
        }
        private bool HasPrefabId
        {
            get
            {
                isPrefabObject= IsPrefabInstance || IsPrefabAssets;
                return isPrefabObject;
            }
        }
        private bool IsPrefabInstance
        {
            get
            {
                return UnityEditor.PrefabUtility.IsAnyPrefabInstanceRoot(gameObject)&&!IsPrefabAssets||(Application.IsPlaying(gameObject)&& isPrefabObject);// || UnityEditor.PrefabUtility.ispreins(gameObject);
            }
        }
        private bool IsPrefabAssets
        {
            get
            {
                return UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject)|| (Application.IsPlaying(gameObject) && isPrefabObject);
            }
        }

        
#endif
        public static string GetNewId(string key = "")
        {
            return string.IsNullOrWhiteSpace(key) ? System.Guid.NewGuid().ToString("N") : System.Guid.Parse(key).ToString("N");
        }
        public string Key { get => InstanceId; set { } }
        [HideInInspector]
        public bool isPrefabObject;
        [ReadOnly]
        [ViewName("预制体Id", "HasPrefabId")]
        public string PrefabId;
        [ReadOnly]
        [ViewName("实例Id", "IsPrefabInstance")]
        public string InstanceId;
        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(InstanceId))
            {
                InstanceId = GetNewId();
            }
        }
  
    }
}