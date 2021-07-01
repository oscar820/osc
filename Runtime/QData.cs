
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using QTool.Binary;
using System.Threading.Tasks;
using QTool.Asset;
namespace QTool.Data
{
    public class DataAsset:AssetList<DataAsset,TextAsset>
    {

    }
    public abstract class QData<T>: IKey<string> where T :QData<T>, new()
    {
        public override string ToString()
        {
            return "[" + Key + "]";
        }

        #region 基础属性

        [XmlElement("关键名")]
        public string Key { get; set; }

        #endregion
        #region 数据表相关
        public static QList<string, T> list = new QList<string, T>();
        public static string TableType
        {
            get
            {
                return typeof(T).Name;
            }
        }
        public static string TableName
        {
            get
            {
                return "表【"+ TableType + "】";
            }
        }
        public static int Count
        {
            get
            {
                return list.Count;
            }
        }
        public static bool Contains(string key)
        {
            return list.ContainsKey(key);
        }
        public static bool Contains(string prefix, string key)
        {
            return Contains(prefix + "." + key);
        }
        public static bool Contains(System.Enum enumKey)
        {
            return Contains(enumKey.ToString());
        }

        public static void Clear()
        {
            _loadOverFile.Clear();
            list.Clear();
            ToolDebug.Log("清空" + TableName);
        }
        public static void Set(T newData)
        {
            if (newData == null)
            {
                new Exception(TableName + "不能添加空对象");
            }
            list.Set(newData.Key, newData);
        }
        public static void Set(string prefix,T newData)
        {
            Set(newData);
            newData = GetNew(newData.Key);
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                if (!newData.Key.StartsWith(prefix + "."))
                {
                    newData.Key = prefix + "." + newData.Key;
                }
            }
            Set(newData);
        }
        public static void Set(string prefix, ICollection<T> newDatas)
        {
            foreach (var data in newDatas)
            {
                Set(prefix, data);
            }
        }
        public static T Get(string key)
        {
            key = key.Trim();
            var obj =list.Get(key);
            if (obj == null)
            {
                new Exception(TableName + "未包含[" + key + "]");
            }
            return obj;
        }
        public static T GetNew(string key)
        {
            var data = Get(key);
            return FileManager.Copy(data);
        }
        public static T Get(string prefix, string key)
        {
            return Get(prefix + "." + key);
        }
     
        public static T Get(System.Enum enumKey)
        {
            return Get(enumKey.ToString());
        }
        #endregion

        #region 数据读取相关

        static string GetName(string key = "")
        {
            return TableType + (string.IsNullOrWhiteSpace(key) ? "" : '\\' + key);
        }
        static string GetSubPath(string key = "")
        {
            return "DataAsset\\" + GetName(key) + ".xml";
        }

        public static string GetPlayerDataPath(string key = "")
        {
            var usePersistentPath = (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer);
            return (usePersistentPath ? Application.persistentDataPath : Application.streamingAssetsPath) + '\\' + GetSubPath(key);
        }
        public static string GetStaticTablePath(string key = "")
        {
            return Application.dataPath + '\\' + GetSubPath(key);
        }

        public static void DeletePlayerData(string key="")
        {
            var path = GetPlayerDataPath(key);
            System.IO.File.Delete(path);
           Debug.LogError("删除" + path);
        }
        public static void Load(string key = "")
        {
            LoadPath(GetStaticTablePath(key),key);
        }
        public static void LoadPlayerData(string key = "")
        {
            LoadPath(GetPlayerDataPath(key), key);
        }
        public static void SaveDefaultStaticTable(T DefaultData=null,string key="")
        {
            QList<string, T> list = new QList<string, T>();
            list.Add(DefaultData==null?new T():DefaultData);
            var path = GetStaticTablePath(key);
            var xmlStr = FileManager.Serialize(list);
            FileManager.Save(path, xmlStr);
            Debug.LogError(TableName + "保存示例静态表成功：" + path);
        }
        public static void SavePlayerData(string key = "")
        {
            var saveList = new List<T>();
            saveList.AddRange(list);
            if (!string.IsNullOrEmpty(key))
            {
                saveList.RemoveAll((obj) =>
                {
                    return !obj.Key.StartsWith(key + ".");
                });
            }
            var xmlStr = FileManager.Serialize(saveList);
            var path = GetPlayerDataPath(key);
            FileManager.Save(path, xmlStr);
            ToolDebug.Log(TableName + "保存数据：" + Count + " 大小：" + (xmlStr.Length * 8).ComputeScale());
        }
        static void LoadPath(string path,string key)
        {
            if (LoadOver(key))
            {
                return;
            }
            try
            {
                var data = FileManager.Load(path);
                if (data != null)
                {
                    var loadList = FileManager.Deserialize<QList<string, T>>(data);
                    foreach (var item in loadList)
                    {
                        Set(key, item);
                    }
                    ToolDebug.Log(TableName + "加载数据：" + loadList.Count + " 大小：" + (data.Length * 8).ComputeScale());
                    _loadOverFile.Add(GetName(key));
                }
            }
            catch (Exception e)
            {
                Debug.LogError(TableName + "加载出错"+path+"  异常信息："+e);
            }
          
        }
        static List<string> _loadOverFile = new List<string>();
        static bool LoadOver(string key = "")
        {
             return _loadOverFile.Contains(GetName(key));
        }
      

       //// static Dictionary<string, System.Action> LoadOverCallBack = new Dictionary<string, Action>();
       // static void InvokeLoadOver(string key)
       // {

       //     var loadKey = GetName(key);
       //     if (LoadOverCallBack.ContainsKey(loadKey))
       //     {
       //         LoadOverCallBack[loadKey]?.Invoke();
       //         LoadOverCallBack[loadKey] = null;
       //     }
       // }
        //public static void LoadOverRun(System.Action action, string key = "")
        //{
           
        //    if (LoadOver(key))
        //    {
        //        action?.Invoke();
        //    }
        //    else
        //    {
        //        var laodOverkey = GetName(key);
        //        if (LoadOverCallBack.ContainsKey(laodOverkey))
        //        {
        //            LoadOverCallBack[laodOverkey] += action;
        //        }
        //        else
        //        {
        //            LoadOverCallBack.Add(laodOverkey, action);
        //        }
        //    }
           
        //}
        static QDictionary<string, Task> loaderTasks = new QDictionary<string, Task>();
        public static async Task LoadAsync(string key = "")
        {
            if (LoadOver(key) )
            {
                return;
            }
            if(loaderTasks[key] != null)
            {
                await loaderTasks[key];
                return;
            }
            var task= DataAsset.GetAsync(GetName(key));
            loaderTasks[key] = task;
            var asset = await task;
            loaderTasks[key] = null;
            if (asset != null)
            {
                var newList = FileManager.Deserialize<QList<string, T>>(asset.text);
                Set(key, newList);
                ToolDebug.Log(TableName + "加载数据：" + newList.ToOneString());
            }
            else
            {
                Debug.LogError("加载文件[" + GetName(key) + "]出错");
            }
            _loadOverFile.Add(GetName(key));
        }
        #endregion
    }
}

