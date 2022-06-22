using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QTool.Binary;
using QTool.Inspector;
using QTool.Asset;
namespace QTool
{
    public class QIdPrefabs : PrefabAssetList<QIdPrefabs>
    {

    }
    public static class QIdExtends
    {
        public static QId GetQId(this Component mono)
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
        public static byte[] SaveAllInstance<IDType>(this QList<string, IDType> InstanceIdList) where IDType : QId
        {
            using (QBinaryWriter writer = new QBinaryWriter())
            {
                var saveList = new List<QId>();
                foreach (var item in InstanceIdList)
                {
                    if (item.IsSceneInstance)
                    {
                        saveList.Add(item);
                    }
                }
                writer.Write((short)saveList.Count);

                foreach (var qId in saveList)
                {

                  
                    writer.Write(qId.InstanceId);
                    writer.Write(qId.PrefabId);
                }
                foreach (var qId in saveList)
                {
                    writer.WriteObject(qId);
                }

                var bytes = writer.ToArray();
                Debug.Log("保存数据 数目：" + saveList.Count+" 大小："+ bytes.Length.ToSizeString());
                return bytes;
            }
        }
        public static void LoadAllInstance<IDType>(this QList<string, IDType> InstanceIdList, byte[] bytes) where IDType:QId
        {
            LoadAllInstance(InstanceIdList,bytes,(prefab)=> {
                if (prefab != null)
                {
                    var obj = GameObject.Instantiate(prefab, null);
                    return obj.GetComponent<IDType>();
                }
                else
                {
                    return null;
                }
              
            },(qid)=> { 
                GameObject.Destroy(qid.gameObject);
            });
        }
        public static void LoadAllInstance<IDType>(this QList<string, IDType> InstanceIdList, byte[] bytes,System.Func<GameObject, IDType> createFunc,System.Action<IDType> destoryFunc ) where IDType : QId
        {
            using (QBinaryReader reader = new QBinaryReader(bytes))
            {
         
                var loadList = new List<IDType>();
                var shortCount = reader.ReadInt16();
                for (int i = 0; i < shortCount; i++)
                {
                    var key = reader.ReadString();
                    var prefabId= reader.ReadString();
               
                    if (InstanceIdList.ContainsKey(key)&&InstanceIdList[key]!=null)
                    {
                        var qid = InstanceIdList[key];
                        loadList.Add(qid);
                        loadList.Replace(i, loadList.IndexOf(qid));
                    }
                    else if(createFunc!=null)
                    {
                        var prefab = QIdPrefabs.ResourceGet(prefabId);
                        if (prefab != null)
                        {
                            var id = createFunc.Invoke(prefab);
                            if (id == null)
                            {
                                Debug.LogError("创建【" + prefab + "】失败 读取存档中断");
                                return;
                            }
                            id.InstanceId = key;
                            loadList.Add(id);
                            InstanceIdList[key] = id;
                            loadList.Replace(i, loadList.IndexOf(id));
                        }
                        else
                        {
                            Debug.LogError("不存在【" + prefabId + "】["+key+"]预制体 读取存档中断");
                            return;
                        }
                    }
                    else
                    {
                        Debug.LogError("不存在【" + key + "】读取存档中断");
                        return;
                    }
                }
                if (destoryFunc != null)
                {
                    foreach (var item in InstanceIdList)
                    {
                        if (!loadList.Contains(item)&&item.IsSceneInstance)
                        {
                            destoryFunc.Invoke(item);
                        }
                    }
                }
                foreach (var item in loadList)
                {
                    reader.ReadObject(item);
                }
                Debug.Log("读取数据完成数目：" + shortCount);
            }
        }
    } 

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
   
    
        [ContextMenu("更新ID")]
        private void InitId()
        {
            if (Application.IsPlaying(gameObject)) return;
            FreshInstanceId();
            if (gameObject.IsPrefabAsset())
            {
                SetPrefabId(UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(gameObject)));
           
            }
            else if (gameObject.IsPrefabInstance() || Application.IsPlaying(gameObject))
            {
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
     
     
        public static QList<string, QId> InstanceIdList = new QList<string, QId>();
    
        public static string GetNewId(string key = "")
        {
            return string.IsNullOrWhiteSpace(key) ? System.Guid.NewGuid().ToString("N") : System.Guid.Parse(key).ToString("N");
        }
        public bool HasPrefabId
        {
            get
            {
                return !string.IsNullOrWhiteSpace(PrefabId);

            }
        }
        public string Key { get => InstanceId; set { } }
        [ReadOnly]
        [ViewName("预制体Id", nameof(HasPrefabId) )]
        public string PrefabId;
        [ReadOnly]
        [ViewName("实例Id")]
        public string InstanceId;
        bool IsPlaying
        {
            get
            {
                return Application.isPlaying;
            }
        }
        public bool IsSceneInstance
        {
            get
            {
#if UNITY_EDITOR
                if (this == null)
                {
                    return false;
                }
                if (UnityEditor.EditorUtility.IsPersistent(gameObject))
                {
                    return false;
                }
#endif
                return true;
            }
        }
        public List<IQSerialize> qSerializes = new List<IQSerialize>();
        private void FreshInstanceId()
        {
            if (string.IsNullOrWhiteSpace(InstanceId))
            {
                SetInstanceId(GetNewId());
            }
            else if (InstanceIdList[InstanceId] == null)
            {
                InstanceIdList[InstanceId] = this;
            }
            else if (InstanceIdList[InstanceId] != this)
            {
                SetInstanceId(GetNewId());
            }
        }
        private void SetInstanceId(string id)
        {
            if (id != InstanceId)
            {
                InstanceId = id;
                InstanceIdList[id] = this;
            }
        }
        [ExecuteInEditMode]
        protected virtual void Awake()
        {
          
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                InitId();
            }
#endif
            FreshInstanceId();
            qSerializes.Clear();
            qSerializes.AddRange( GetComponents<IQSerialize>());
            qSerializes.Remove(this);
        }
        protected virtual void OnDestroy()
        {
            if (InstanceIdList.ContainsKey(InstanceId)){
                if (InstanceIdList[InstanceId] == this)
                {
                    InstanceIdList.Remove(InstanceId);
                }
            }
        }
        public virtual void Write(QBinaryWriter writer)
        {
            writer.WriteObject(transform.position);
            var byteLength = (byte)qSerializes.Count;
            writer.Write(byteLength);
            for (int i = 0; i < byteLength; i++)
            {
                writer.WriteObject(qSerializes[i]);
            }
        }
       
        public override string ToString()
        {
            return name + "(" + InstanceId + " prefab:" + PrefabId + ")["+ qSerializes .Count+ "]";
        }

        public virtual void Read(QBinaryReader reader)
        {
            transform.position = reader.ReadObject(transform.position);
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
                Debug.LogError("读取序列化数据失败脚本数不匹配"+qSerializes.Count+":"+byteLength);
            }
           
        }
    }
}
