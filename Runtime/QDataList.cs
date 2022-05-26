using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using QTool.Asset;
using QTool.Reflection;

namespace QTool{
	public class QDataList<T>  where T : QDataList<T>, IKey<string>
	{
		public static T Get(string key)
		{
			key = key.Trim();
			var value= list[key]; ;
			if (value == null)
			{
				Debug.LogError(typeof(T).Name + " 未找到[" + key + "]");
			}
			return value;
		}
		static QDataList(){ 
			var type = typeof(T);
			var typeInfo = QSerializeType.Get(type);
			var path = "QDataListAssets\\" + type.Name;
			var text= Resources.Load<TextAsset>(path);
			if (text != null&&!string.IsNullOrWhiteSpace(text.text))
			{
				list.Clear();
				var qdataList = new QDataList(text.text);
				var titleRow = qdataList.TitleRow;
				var memeberList = new List<QMemeberInfo>();
				foreach (var title in titleRow)
				{
					var member = typeInfo.Members[title];
					if (member == null)
					{
						member = typeInfo.Members.Get(title, (obj) => obj.MemeberInfo.ViewName());
					}
					if (member == null)
					{
						Debug.LogError("读取 "+type.Name+"出错 不存在属性 " + title);
					}
					memeberList.Add(member);
				}
				foreach (var row in qdataList)
				{
					if (row == titleRow) continue;
					var obj= type.CreateInstance();
					var t = (obj as T);
					for (int i = 0; i < titleRow.Count; i++)
					{
						var member = memeberList[i];
						if (member!=null)
						{
							try
							{
								member.Set(t, row[i].ParseQData(member.Type, false));
							}
							catch (System.Exception e)
							{

								Debug.LogError("读取 " + type.Name + "出错 设置["+row.Key+"]属性 "+member.Name+"("+member.Type+")异常：\n"+e);
							}
							
						}
					}
					t.Key = row.Key; 
					list.Add(t);
				}
				Debug.Log("读取 Resources\\" + path + "]完成：\n" + list.ToOneString());
			}
			else
			{
				Debug.LogError("读取 Resources\\" + path + "]出错");
			}
		}
		public static QList<string, T> list = new QList<string, T>();
    }
    public class QDataList: QAutoList<string, QDataRow>
    {
        public static QDataList QToolSetting => GetData(StreamingPathRoot+nameof(QToolSetting)+".txt",(data)=> { data[nameof(QToolDebug)].SetValue(false); });
        public static string StreamingPathRoot => Application.streamingAssetsPath +'\\'+ nameof(QDataList)+'\\';
        static QDataList()
        {
            Application.focusChanged += (focus) =>
            {
                if (!focus)
                {
                    QToolSetting.Save();
                }
            };
        }
        public static QDataList GetData(string path,System.Action<QDataList> autoCreate=null)
        {
            if (!dataList.ContainsKey(path))
            {
                if (FileManager.ExistsFile(path))
                {
                    try
                    {
                        dataList[path] = new QDataList(FileManager.Load(path));
                        dataList[path].LoadPath = path;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("读取QDataList[" + path + "]出错：\n" + e);
                    }
                }
                else
                {
                    if (autoCreate!=null)
                    {
                        var qdataList = new QDataList();
                        qdataList.LoadPath = path;
                        dataList[path] = qdataList;
                        autoCreate(qdataList);
                        qdataList.Save();
                        Debug.LogWarning("不存在QDataList自动创建[" + path + "]");
                    }
                }
            }
            return dataList[path];
        }

        static QDictionary<string, QDataList> dataList = new QDictionary<string, QDataList>();
        public override void OnCreate(QDataRow obj)
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
            index = TitleRow.IndexOf(title);
            return index >= 0;
        }
        public QDataRow TitleRow
        {
            get
            {
                return this[0];
            }
        }
        public void SetTitles(params string[] titles)
        {
            for (int i = 0; i < titles.Length; i++)
            {
                TitleRow[i] = titles[i];
            }
        }
        public new QDataRow this[int index]
        {
            get
            {
                if (index >= Count)
                {
                    var line=new QDataRow();
                    line.OwnerData = this;
                    base[index] = line;
                }
                return base[index];
            }
        }
       public void Parse(string dataStr)
        {
            Clear();
            using (var reader = new StringReader(dataStr))
            {
                int index = 0;
                var row = new QDataRow(this);
                while (!reader.IsEnd())
                {
                    var value = reader.ReadElement(out var newLine);
                    row[index] = value;
                    index++;
                    if (newLine)
                    {
                        if (row.Count > 0)
                        {
                            Add(row);
                        }
                        index = 0;
                        row = new QDataRow(this);
                    }
                   
                }
            }
        }
        public QDataList()
        {
        }
        public QDataList(string dataStr)
        {
            Parse(dataStr);
        }
        public override string ToString()
        {
            using (var writer =new StringWriter())
            {
                for (int i = 0; i < this.Count; i++)
                {
                    writer.Write(this[i].ToString());
                    if (i < Count - 1)
                    {
                        writer.Write('\n');
                    }
                }
                return writer.ToString();
            }
          
        }

    }

    public class QDataRow:QList<string>,IKey<string>
    {
        public string Key { get => base[0]; set
            {

                base[0] = value;
            }
        }
        public T GetValue<T>(int index=1)
        {
            return base[index].ParseQData<T>(false);
        }
        public void SetValue<T>(T value, int index=1)
        {
            base[index] = value.ToQData(false);
        }
        public T GetValue<T>(string title)
        {
            if (OwnerData.TryGetTitleIndex(title, out var index))
            {
                return GetValue<T>(index);
            }
            else
            {
                throw new System.Exception("不存在的列名[" + title + "]");
            }
        }
        public QDataRow SetValue<T>(string title,T value)
        {
            if (OwnerData.TryGetTitleIndex(title, out var index))
            {
                SetValue(value, index);
            }
            else
            {
                Debug.LogWarning("不存在的列名[" + title + "]自动创建");
                OwnerData[0].Add(title);
                SetValue(title, value);
            }
            return this;
        }
      
        public QDataRow()
        {
        }
        public QDataList OwnerData { get; internal set; }
        public QDataRow(QDataList ownerData)
        {
            OwnerData = ownerData;
        }
        public override string ToString()
        {
            using (var writer=new StringWriter())
            {
                for (int j = 0; j < Count; j++)
                {
                    var qdata = this[j];
                    writer.Write(qdata);
                    if (j < Count - 1)
                    {
                        writer.Write('\t');
                    }
                }
                return writer.ToString();
            }
        }
    }
}
