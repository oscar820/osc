using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace QTool
{
	public class QDataList<T>  where T : QDataList<T>, IKey<string>,new()
	{
		public static bool ContainsKey(string key)
		{
			return list.ContainsKey(key);
		}
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

			var qdataList=QDataList.GetResourcesData(typeof(T).Name, () => new List<T> { new T{Key="测试Key" }, }.ToQDataList());
			qdataList.ParseQdataList(list);
		}
		public static QList<string, T> list { get; private set; } = new QList<string, T>();
    }
    public class QDataList: QAutoList<string, QDataRow>
	{
		public static string ResourcesPathRoot => FileManager.ResourcesRoot + nameof(QDataList) +"Assets"+ '/';
		//public static string StreamingPathRoot => Application.streamingAssetsPath +'\\'+ nameof(QDataList)+'\\';
		public static string GetResourcesDataPath(string name)
		{
			return ResourcesPathRoot + name + ".txt";
		}
		public static string GetAssetDataPath(string name)
		{
			return Application.dataPath+"/" + nameof(QDataList) + "Assets/" + name + ".txt";
		}
		public static QDataList GetResourcesData(string name, System.Func<QDataList> autoCreate = null)
		{
			return GetData(GetResourcesDataPath(name),autoCreate);
		}
		public static QDataList GetData(string path,System.Func<QDataList> autoCreate=null)
        {
			return QDataListCache.Get(path, (key) =>
			{
				if (FileManager.Exists(path, true))
				{
					try
					{
						var data = new QDataList();
						data.LoadPath = path;
						FileManager.LoadAll(path, (fileValue,loadPath) =>
						{
							data.Parse(fileValue, loadPath);
						}, "{}");
						return data;
					}
					catch (System.Exception e)
					{
						Debug.LogError("读取QDataList[" + path + "]出错：\n" + e);
					}
				}
				else
				{
					if (autoCreate != null)
					{
						var qdataList = autoCreate();
						qdataList.LoadPath = path;
						qdataList.Save();
						Debug.LogWarning("不存在QDataList自动创建[" + path + "]:\n" + qdataList);
						return qdataList;
					}
				}
				return null;
			});

		}
		public static QKeyCache<string,QDataList, DateTime> QDataListCache = new QKeyCache<string,QDataList, DateTime>((key)=> {
			return FileManager.GetLastWriteTime(key);
		});
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
            FileManager.Save(path, ToString(),true);
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
       public void Parse(string dataStr,string addPath=null)
        {
			if (string.IsNullOrEmpty( addPath))
			{
				Clear();
			}
            using (var reader = new StringReader(dataStr))
            {
				int rowIndex = 0;
                int valueIndex = 0;
                var row = new QDataRow(this);
                while (!reader.IsEnd())
                {
                    var value = reader.ReadElement(out var newLine);
                    row[valueIndex] = value;
                    valueIndex++;
                    if (newLine)
                    {
                        if (row.Count > 0)
                        {
							if(!string.IsNullOrEmpty(addPath)&& rowIndex == 0)
							{
								for (int i = 0; i < row.Count; i++)
								{
									TitleRow[i] = row[i];
								}
							}
							else
							{
								if (ContainsKey(row.Key))
								{
									Debug.LogWarning("加载覆盖 [" + row.Key + "] 来自文件 "+ addPath+"\n旧数据: " + this[row.Key]+"\n新数据: "+row);
								}
								Add(row);
							}
							
                        }
                        valueIndex = 0;
						rowIndex++;
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
			if (typeof(T) == typeof(string))
			{
				return (T)(object)base[index] ;
			}
			else
			{
				return base[index].ParseQData<T>(default, false);
			}
        }
		public void SetValueType(object value,Type type,int index=1)
		{
			if (type == typeof(string))
			{
				base[index] = (string)value;
			}
			else
			{

				base[index] = value.ToQDataType(type, false);
			}
		}
        public void SetValue<T>(T value, int index=1)
        {
			SetValueType(value, typeof(T), index);
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
		public bool HasValue(string title)
		{
			if (OwnerData.TryGetTitleIndex(title, out var index))
			{
				return Count > index;
			}
			else
			{
				throw new System.Exception("不存在的列名[" + title + "]");
			}
		}
		public QDataRow SetValueType(string title, object value,Type type)
		{
			if (OwnerData.TryGetTitleIndex(title, out var index))
			{
				SetValueType(value,type, index);
			}
			else
			{
				Debug.LogWarning("不存在的列名[" + title + "]自动创建");
				OwnerData[0].Add(title);
				SetValueType(title, value, type);
			}
			return this;
		}
		public QDataRow SetValue<T>(string title,T value)
        {
			SetValueType(title, value, typeof(T));
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
                    writer.Write(qdata.ToElement());
                    if (j < Count - 1)
                    {
                        writer.Write('\t');
                    }
                }
                return writer.ToString();
            }
        }
    }
	public static class QDataListTool
	{
		public static string ToElement(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return "";
			}
			if (value.Contains("\t"))
			{
				value = value.Replace("\t", " ");
			}

			if (value.Contains("\n"))
			{
				if (value.Contains("\""))
				{
					value = value.Replace("\"", "\"\"");
				}
				value = "\"" + value + "\"";
			}
			return value;
		}
		public static string ParseElement(this string value)
		{
			if (!string.IsNullOrEmpty(value)&&value.StartsWith("\"") && value.EndsWith("\"") && (value.Contains("\n")||value.Contains("\"\"")))
			{
				value = value.Substring(1, value.Length - 2);
				value = value.Replace("\"\"", "\"");
				return value;
			}
			return value;
		}
		public static string ReadElement(this StringReader reader, out bool newLine)
		{
			newLine = true;
			using (var writer = new StringWriter())
			{
				if (reader.Peek() == '\"')
				{
					var checkExit = true;
					while (!reader.IsEnd())
					{
						var c = reader.Read();
						if (c != '\r')
						{
							writer.Write((char)c);
						}
						if (c == '\"')
						{
							checkExit = !checkExit;
						}
						if (checkExit)
						{
							reader.NextIgnore('\r');
							if (reader.NextIs('\n')) break;
							if (reader.NextIs('\t'))
							{
								if (!reader.IsEnd())
								{
									newLine = false;
								}
								break;
							}
						}

					}
					var value = ParseElement( writer.ToString());
					return value;
				}
				else
				{
					while (!reader.IsEnd() && !reader.NextIs('\n'))
					{
						if (reader.NextIs('\t'))
						{
							if(!reader.IsEnd())
							{
								newLine = false;
							}
							break;
						}
						var c = (char)reader.Read();
						if (c != '\r')
						{
							writer.Write(c);
						}
					}
					return writer.ToString();
				}

			}
		}

	}
}
