﻿
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using QTool.Binary;
using System.Threading.Tasks;
using QTool.Asset;
namespace QTool
{
    public class PreviewData : IKey<string>
    {
        [XmlElement("关键名")]
        public string Key { get; set; }
        [XmlElement("存档时间")]
        public DateTime saveTime { get; set; }
    }
    public abstract class QSaveData<T> : QSaveData<T, PreviewData> where T : QSaveData<T>, new()
    {
        protected override PreviewData GetPreview()
        {
            return new PreviewData() { Key = Key };
        }
    }
    public abstract class QSaveData<T, PreviewT> : IKey<string> where T : QSaveData<T, PreviewT>, new() where PreviewT : PreviewData
    {
        #region 基础属性
        [XmlElement("关键名")]
        public string Key { get; set; }
        public override string ToString()
        {
            return "[" + Key + "]";
        }
        protected abstract PreviewT GetPreview();
        public void Save()
        {
            Save(Key,this as T);
        }
        public void Delete()
        {
            Delete(Key);
        }
        #endregion
        #region 数据表相关
        static QSaveData(){
            var list=FileManager.LoadXml<QList<string,PreviewT>>(PreviewPath);
            if (list != null)
            {
                PreviewList = list;
                Debug.LogError("加载【" + TypeName + "】数目" + list.Count);
            }
        }
        public static QList<string, PreviewT> PreviewList = new QList<string, PreviewT>();
        public static string TypeName => typeof(T).Name;
        public static int Count => PreviewList.Count;
        public static bool Contains(string key)
        {
            return PreviewList.ContainsKey(key);
        }
      

        public static string RootPath => ((Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer) ?
            Application.persistentDataPath : Application.streamingAssetsPath) + "\\QSaveData\\" + typeof(T).Name + "\\";
      
        public static string PreviewPath => RootPath + typeof(PreviewT).Name;
       
        public static T Load(string key)
        {
            return FileManager.LoadXml<T>(PreviewPath + key);
        }

        public static void Delete(string key )
        {
            System.IO.File.Delete(PreviewPath + key);
            PreviewList.Remove(key);
            FileManager.SaveXml(PreviewPath, PreviewList);
        }
        public static void Save(string key ,T data)
        {
            FileManager.SaveXml(PreviewPath + key, data);
            PreviewList[key] =data.GetPreview();
            PreviewList[key].saveTime = DateTime.Now;
            FileManager.SaveXml(PreviewPath, PreviewList);
            QToolDebug.Log(()=> "保存数据：【"+ PreviewPath + key + "】" );
        }
        public static void Clear()
        {
            foreach (var preview in PreviewList)
            {
                System.IO.File.Delete(PreviewPath + preview.Key);
            }
            PreviewList.Clear();
            FileManager.SaveXml(PreviewPath, PreviewList);
            QToolDebug.Log(() => "清空 【" + TypeName + "】");
        }

        #endregion
    }
}

