using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

namespace QTool{
    
    public class QDataTable: QAutoList<string, QDataLine>
    {
        public static QDataTable QToolSetting => GetData(nameof(QToolSetting)+".qdata");
        public static string StreamingPathRoot => Application.streamingAssetsPath +'\\'+ nameof(QDataTable)+'\\';
        static QDataTable()
        {
            Application.focusChanged += (focus) =>
            {
                if (!focus)
                {
                    QToolSetting.Save();
                }
            };
        }
        public static QDataTable GetData(string path)
        {
            if (!dataList.ContainsKey(path))
            {
                dataList[path] = new QDataTable(FileManager.Load(path));
                dataList[path].LoadPath = path;
            }
            return dataList[path];
        }

        static QDictionary<string, QDataTable> dataList = new QDictionary<string, QDataTable>();
        public override void OnCreate(QDataLine obj)
        {
            obj.OwnerData = this;
            base.OnCreate(obj);
        }
        public string LoadPath { get; private set; }
        public void Save(string path = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = LoadPath;
            }
            FileManager.Save(path, ToString());
        }
        public bool TryGetTitleIndex(string title,out int index)
        {
            index = -1;
            if (TitleLine != null)
            {
                index= TitleLine.IndexOf(title); 
            }
            return index >= 0;
        }
        public QDataLine TitleLine
        {
            get
            {
                if (Count > 0)
                {
                    return base[0];
                }
                return null;
            }
        }
        public new QDataLine this[int index]
        {
            get
            {
                if (index >= Count)
                {
                    var line=new QDataLine();
                    line.OwnerData = this;
                    base[index] = line;
                }
                return base[index];
            }
        }
       
        public QDataTable(string dataStr)
        {
            var lineStrs = dataStr.Split('\n');
            foreach (var lineStr in lineStrs)
            {
                Add(new QDataLine(lineStr, this));
            }
        }
        public override string ToString()
        {
            return this.ToOneString("\n");
        }

    }

    public class QDataLine:QList<string>,IKey<string>
    {
        public string Key { get => base[0]; set
            {

                base[0] = value;
            }
        }
        public T GetValue<T>()
        {
            Value.TryParse<T>(out var obj);
            return obj;
        }
        public void SetValue<T>(T value)
        {
            Value = value?.ToString();
        }
        public string Value
        {
            get
            {
                if (Count > 1)
                {
                    return base[1];
                }
                else
                {
                    return "";
                }
            }
            set
            {
                base[1] = value;
                Debug.LogError(base[0]+"."+ base[1] + "=>" + value);
            }
        }
        public string this[string title]
        {
            get
            {
                if(OwnerData.TryGetTitleIndex(title,out var index))
                {
                    return this[index];
                }
                else
                {
                    throw new System.Exception("不存在的列名[" + title + "]");
                }
            }
            set
            {
                if (OwnerData.TryGetTitleIndex(title, out var index))
                {
                    this[index] = value;
                }
                else
                {
                    throw new System.Exception("不存在的列名[" + title + "]");
                }

            }
        }
        public QDataLine()
        {
        }
        public QDataTable OwnerData { get; internal set; }
        public QDataLine(string lineStr,QDataTable ownerData)
        {
            var valueS = lineStr.Split('\t');
            for (int i = 0; i < valueS.Length; i++)
            {
                var value = valueS[i];
                this[i] = value;
            }
            OwnerData = ownerData;
        }
        public override string ToString()
        {
            return this.ToOneString("\t");
        }
    }
}
