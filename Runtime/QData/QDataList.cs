using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace QTool
{
	
    public class QDataList: QList<string, QDataRow>
	{
		public static string ResourcesPathRoot => QFileManager.ResourcesRoot + nameof(QDataList) +"Asset"+ '/';
		public static string GetResourcesDataPath(string name,string childFile=null)
		{
			if (childFile.IsNullOrEmpty())
			{
				return ResourcesPathRoot + name + ".txt";
			}
			else
			{
				return ResourcesPathRoot + name +"/"+childFile+ ".txt";
			}
		}
		public static string GetAssetDataPath(string name)
		{
			return Application.dataPath+"/" + nameof(QDataList) + "Asset/" + name + ".txt";
		}
		public static QDataList GetResourcesData(string name, System.Func<QDataList> autoCreate = null)
		{
			return GetData(GetResourcesDataPath(name),autoCreate);
		}

		public static QDataList GetData(string path,System.Func<QDataList> autoCreate=null)
        {
			QDataList data = null;
			;
			try
			{
				data = new QDataList();
				data.LoadPath = path;
				QFileManager.LoadAll(path, (fileValue, loadPath) =>
				{
					data.Add(new QDataList(fileValue) { LoadPath = loadPath });
				}, "{}");
			}
			catch (System.Exception e)
			{
				Debug.LogError("读取QDataList[" + path + "]出错：\n" + e);

			}
			if (data == null)
			{
				if (autoCreate != null)
				{
					data = autoCreate();
					data.LoadPath = path;
					data.Save();
					Debug.LogWarning("不存在QDataList自动创建[" + path + "]:\n" + data);
				}
			}
			return data;

		}
	
     
        public string LoadPath { get; private set; }
        public void Save(string path = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = LoadPath;
			}
			QFileManager.Save(path, ToString(),true);
        }
        public int GetTitleIndex(string title)
        {
            var index = TitleRow.IndexOf(title);
			if (index >= 0)
			{
				return index;
			}
			else
			{
				TitleRow.Add(title);
				return TitleRow.IndexOf(title);
			}
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
                    var line=new QDataRow(this);
					var list = new List<float>();
                    base[index] = line;
                }
                return base[index];
            }
        }
       private void Parse(string dataStr)
        {
			Clear();
			using (var keyInfo = new StringWriter())
			{

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
								if (!string.IsNullOrEmpty(row.Key))
								{
									if (ContainsKey(row.Key))
									{
										Debug.LogWarning("加载覆盖 [" + row.Key + "] 来自文件 " + LoadPath + "\n旧数据: " + this[row.Key] + "\n新数据: " + row);
									}
									Add(row);
								}
								keyInfo.Write(row.Key);
								keyInfo.Write('\t');
							}
							valueIndex = 0;
							rowIndex++;
							row = new QDataRow(this);
						}

					}
				}
			}
        }
        public QDataList()
		{
			AutoCreate = () => new QDataRow(this);
		}
		public void Add(QDataList addList)
		{
			if (TitleRow.Count == 0)
			{
				TitleRow[0] = addList.TitleRow[0];
			}
			for (int i = 1; i < addList.Count; i++)
			{
				var row = addList[i];
				if (ContainsKey(row.Key))
				{
					Debug.LogWarning("加载覆盖 [" + row.Key + "] 来自文件 " + addList.LoadPath + "\n旧数据: " + this[row.Key] + "\n新数据: " + row);
				}
				var newRow = this[row.Key];
				for (int j = 1; j < addList.TitleRow.Count; j++)
				{
					var title = addList.TitleRow[j];
					newRow[title] = row[title];
				}

			}
		}
        public QDataList(string dataStr):this()
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
	public class QDataList<T> where T : QDataList<T>, IKey<string>, new()
	{
		public static bool ContainsKey(string key)
		{
			return list.ContainsKey(key);
		}
		public static T Get(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				Debug.LogError("key 为空");
				return null;
			}
			key = key.Trim();
			var value = list[key]; ;
			if (value == null)
			{
				Debug.LogError(typeof(T).Name + " 未找到[" + key + "]");
			}
			return value;
		}
		static QDataList()
		{

			var qdataList = QDataList.GetResourcesData(typeof(T).Name, () => new List<T> { new T { Key = "测试Key" }, }.ToQDataList());
			qdataList.ParseQdataList(list);
		}
		public static QList<string, T> list { get; private set; } = new QList<string, T>();
	}
	public class QDataRow:QList<string>,IKey<string>
    {
        public string Key { get => base[0]; set
            {

                base[0] = value;
            }
        }
		public string this[string title]
		{
			get => base[OwnerData.GetTitleIndex(title)];
			set => base[OwnerData.GetTitleIndex(title)] = value;
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
			return GetValue<T>(OwnerData.GetTitleIndex(title));
		}
		public bool HasValue(string title)
		{
			return OwnerData.TitleRow.IndexOf(title) >= 0;
		}
		public QDataRow SetValueType(string title, object value,Type type)
		{
			SetValueType(value, type, OwnerData.GetTitleIndex(title));
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
			if (!string.IsNullOrEmpty(value)&&value.StartsWith("\"") && value.EndsWith("\"") && (value.Contains(",")||value.Contains(".")||value.Contains("\n")||value.Contains("\"\"")))
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
