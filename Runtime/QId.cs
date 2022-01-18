using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.Binary;
using QTool.Binary;
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
    [System.Serializable]
    public class InstanceReference
    {
        public string id;
        public GameObject _obj;
        public GameObject Obj
        {
            get
            {
                if (_obj == null)
                {
                    if (string.IsNullOrWhiteSpace(id)) return null;
                    if (QId.InstanceIdList.ContainsKey(id)&& QId.InstanceIdList[id]!=null)
                    {
                        _obj = QId.InstanceIdList[id].gameObject;
                    }
                    else
                    {
                        Debug.LogWarning("不存在物体 Id：[" + id + "]");
                    }
                 
                }
                return _obj;
            }
        }
    }
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class QId : MonoBehaviour,IKey<string>,IQSerialize
    {
#if UNITY_EDITOR

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                InitId();
            }
        }
        private void SetPrefabId(string id)
        {
            if (id != PrefabId)
            {
                PrefabId = id;
                //this.SetDirty();
            }
        }
        private void SetInstanceId(string id)
        {
            if (id != InstanceId)
            {
                InstanceId = id;
                //this.SetDirty();
                InstanceIdList[id] = this;
            }
        }
        [ContextMenu("更新ID")]
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
        }
      


#endif
        private bool IsPrefabInstance
        {
            get
            {
#if UNITY_EDITOR
                return (UnityEditor.PrefabUtility.IsAnyPrefabInstanceRoot(gameObject) && !IsPrefabAssets)|| Application.IsPlaying(gameObject);
                
#else
                return false;
#endif

            }
        }
        private bool HasPrefabId
        {
            get
            {
                return IsPrefabInstance || IsPrefabAssets;

            }
        }
        private bool IsPrefabAssets
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject);
#else
                return false;
#endif
            }
        }

        public static QDictionary<string, QId> InstanceIdList = new QDictionary<string, QId>();
        public static byte[] SaveAllInstance()
        {
            using (QBinaryWriter writer=new QBinaryWriter())
            {
                var shortCount = (short)InstanceIdList.Count;
             
                writer.Write(shortCount);
                for (int i = 0; i < shortCount; i++)
                {
                     var kv = InstanceIdList[i];
                     writer.Write(kv.Key);
                    writer.WriteObject(kv.Value);
                }
                return writer.ToArray();
            }
        }
        public static void LoadAllInstance(byte[] bytes)
        {
           using (QBinaryReader reader=new QBinaryReader(bytes))
            {
                var shortCount = reader.ReadInt16();
                for (int i = 0; i < shortCount; i++)
                {
                    var key = reader.ReadString();
                    if (InstanceIdList.ContainsKey(key))
                    {
                        reader.ReadObject(InstanceIdList[key]);
                    }
                    else
                    {
                        Debug.LogError("不存在【" + key + "】");
                    }
                }
            }
        }
        public static string GetNewId(string key = "")
        {
            return string.IsNullOrWhiteSpace(key) ? System.Guid.NewGuid().ToString("N") : System.Guid.Parse(key).ToString("N");
        }
        public string Key { get => InstanceId; set { } }
        [ReadOnly]
        [ViewName("预制体Id", "HasPrefabId")]
        public string PrefabId;
        [ReadOnly]
        [ViewName("实例Id", "IsPrefabInstance")]
        public string InstanceId;

        bool IsPlaying
        {
            get
            {
                return Application.isPlaying;
            }
        }
        public List<IQSerialize> qSerializes = new List<IQSerialize>();
        protected virtual void Awake()
        {
            if (string.IsNullOrWhiteSpace(InstanceId))
            {
                InstanceId = GetNewId();
            }
            InstanceIdList[InstanceId] = this;
            qSerializes.Clear();
            qSerializes.AddRange( GetComponents<IQSerialize>());
            qSerializes.Remove(this);
        }
        public void Write(QBinaryWriter writer)
        {
            var byteLength = (byte)qSerializes.Count;
            writer.Write(byteLength);
            for (int i = 0; i < byteLength; i++)
            {
                writer.WriteObject(qSerializes[i]);
            }
          
        }
        public override string ToString()
        {
            return name;
        }

        public void Read(QBinaryReader reader)
        {
            var byteLength = reader.ReadByte();
            if (qSerializes.Count == byteLength)
            {
                for (int i = 0; i < byteLength; i++)
                {
                 
                    reader.ReadObject(qSerializes[i]);
                }
            }
            else
            {
                Debug.LogError("读取序列化数据失败脚本数不匹配"+qSerializes.Count);
            }
           
        }
    }
}