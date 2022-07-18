using QTool.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace QTool
{
	public static class QData
	{
	
		public static string ToQData<T>(this T obj, bool hasName = true)
		{
			var type = typeof(T);
			return ToQDataType(obj, type, hasName);
		}
		public static T ParseQData<T>(this string qdataStr, T target = default, bool hasName = true)
		{
			return (T)ParseQDataType(qdataStr, typeof(T), hasName, target);
		}

		public static string ToQDataType(this object obj, Type type, bool hasName = true)
		{
			using (var writer=new StringWriter())
			{
				WriteType(writer, obj, type, hasName);
				return writer.ToString();
			}
		}
		public static object ParseQDataType(this string qdataStr, Type type, bool hasName = true, object target = null)
		{

			if (string.IsNullOrEmpty(qdataStr))
			{
				return type.IsValueType ? QReflection.CreateInstance(type, target) : null;
			}
			try 
			{
				using (var reader = new StringReader(qdataStr))
				{
					return ReadType(reader, type, hasName, target);
				}
			}
			catch (Exception e)
			{
				Debug.LogError("解析类型【" + type + "】出错：" + "\n" + e+"\n"+ qdataStr);
				return type.IsValueType ? QReflection.CreateInstance(type, target) : null;
			}

		}

		public static void WriteQData<T>(this StringWriter writer, T obj, bool hasName = true)
		{
			WriteType(writer, obj, typeof(T), hasName);
		}
		static void WriteObject(this StringWriter writer, object obj, QSerializeType typeInfo, bool hasName = true)
		{

			if (obj == null) { writer.Write("null"); return; }
			writer.Write('{');
			if (typeInfo.IsIQData)
			{
				(obj as IQData).ToQData(writer);
			}
			else
			{
				if (hasName)
				{
					for (int i = 0; i < typeInfo.Members.Count; i++)
					{
						var memberInfo = typeInfo.Members[i];
						var member = memberInfo.Get(obj);
						WriteCheckString(writer, memberInfo.Key);
						writer.Write(':');
						WriteType(writer, member, memberInfo.Type, hasName);
						if (i < typeInfo.Members.Count - 1)
						{
							writer.Write(',');
						}
					}
				}
				else
				{
					for (int i = 0; i < typeInfo.Members.Count; i++)
					{
						var memberInfo = typeInfo.Members[i];
						var member = memberInfo.Get(obj);
						WriteType(writer, member, memberInfo.Type, hasName);
						if (i < typeInfo.Members.Count - 1)
						{
							writer.Write(',');
						}
					}
				}

			}
			writer.Write('}');


		}
		public static void WriteType(this StringWriter writer, object obj, Type type, bool hasName=true)
		{
			var typeCode = Type.GetTypeCode(type);
			switch (typeCode)
			{
				case TypeCode.Object:
					{
						var typeInfo = QSerializeType.Get(type);
						switch (typeInfo.objType)
						{
							case QObjectType.DynamicObject:
								{
									if(obj == null)
									{
										writer.Write("null");
									}
									else
									{
										writer.Write('{');
										var runtimeType = obj.GetType();
										var runtimeTypeInfo = QSerializeType.Get(runtimeType);
										switch (runtimeTypeInfo.objType)
										{
											case QObjectType.DynamicObject:
												{
													WriteCheckString(writer, runtimeType.FullName);
													writer.Write(':');
													WriteObject(writer, obj, runtimeTypeInfo, hasName);
												}
												break;
											case QObjectType.CantSerialize:
												break;
											default:
												{
													WriteCheckString(writer, runtimeType.FullName);
													writer.Write(':');
													WriteType(writer, obj, type, hasName);
												}
												break;
										}
										writer.Write('}');
									}
									
									
								}
								break;
							case QObjectType.UnityObject:
								{
									if (obj == null)
									{
										writer.Write("null");
									}
									else
									{
										writer.Write('{');
										writer.Write(QIdObject.GetId(obj as UnityEngine.Object));
										writer.Write('}');
									}
								
								}
								break;
							case QObjectType.Object:
								{
									WriteObject(writer, obj, typeInfo, hasName);
									break;
								}
							case QObjectType.List:
							case QObjectType.Array:
								{
									var list = obj as IList;
									if (list == null) break;
									writer.Write('[');
									for (int i = 0; i < list.Count; i++)
									{
										WriteType(writer, list[i], typeInfo.ElementType, hasName);
										if (i < list.Count - 1)
										{
											writer.Write(',');
										}
									}
									writer.Write(']');
									break;
								}
							case QObjectType.TimeSpan:
								{
									writer.Write(((TimeSpan)obj).Ticks);
								}break;
							default:
								break;
						}
						break;
					}
				case TypeCode.DateTime:
					WriteCheckString(writer,((DateTime)obj).ToQTimeString());
					break;
				case TypeCode.String:
					WriteCheckString(writer, obj?.ToString());
					break;
				case TypeCode.Boolean:
					writer.Write(obj.ToString().ToLower());
					break;
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
					if (type.IsEnum)
					{
						WriteCheckString(writer, obj.ToString());
					}
					else
					{
						writer.Write(obj.ToString());
					}
					break;
				default:
					writer.Write(obj.ToString());
					break;
			}
		}
		public static T ReadQData<T>(this StringReader reader, T target = default,bool hasName = true)
		{
			return (T)ReadType(reader, typeof(T), hasName, target);
		}
		static object ReadObject(this StringReader reader, QSerializeType typeInfo, bool hasName = true, object target = null)
		{
			if (reader.NextIs('{'))
			{
				if (reader.NextIs('}'))
				{
					return null;
				}
				if (typeInfo.IsIQData)
				{
					target = QReflection.CreateInstance(typeInfo.Type, target);
					(target as IQData).ParseQData(reader); reader.NextIs('}');
				}
				else
				{
					if (target == null)
					{
						target = QReflection.CreateInstance(typeInfo.Type, target);
					}
					if (hasName)
					{
						while (!reader.IsEnd())
						{
							var name = reader.ReadCheckString();
							var memeberInfo = typeInfo.Members[name];

							object result = null;

							if (!(reader.NextIs(':') || reader.NextIs('=')))
							{
								throw new Exception(name + " 后缺少分隔符 : 或 =\n" + reader.ReadLine());
							}
							if (memeberInfo != null)
							{
								try
								{
									result = reader.ReadType(memeberInfo.Type, hasName, memeberInfo.Get(target));
									memeberInfo.Set(target, result);

								}
								catch (Exception e)
								{
									Debug.LogError("读取成员【" + name + ":" + typeInfo.Type.Name + "." + memeberInfo.Key + "】出错" + memeberInfo.Type + ":" + result + ":" + memeberInfo.Get(target) + "\n" + e+"\n 剩余信息"+reader.ReadToEnd());
									throw e;
								}
							}
							else
							{
								Debug.LogWarning("不存在成员" + typeInfo.Key + "." + name + ":" + reader.ReadCheckString());
							}

							if (!(reader.NextIs(';') || reader.NextIs(',')))
							{
								if (reader.NextIs('}'))
								{
									break;
								}
							}
						}
					}
					else
					{
						foreach (var memeberInfo in typeInfo.Members)
						{
							memeberInfo.Set(target, reader.ReadType(memeberInfo.Type, hasName, memeberInfo.Get(target)));
							if (!(reader.NextIs(';') || reader.NextIs(',')))
							{
								if (reader.NextIs('}'))
								{
									break;
								}
							}
						}
					}

				}
			}
			else
			{
				if (reader.ReadValueString() == "null")
				{
					target = null;
				}
				else
				{
					target = null;
				}
			}
			return target;
		}
		public static object ReadType(this StringReader reader, Type type, bool hasName = true, object target = null)
		{
			var typeCode = Type.GetTypeCode(type);
			switch (typeCode)
			{
				case TypeCode.Object:
					{
						var typeInfo = QSerializeType.Get(type);
						switch (typeInfo.objType)
						{
							case QObjectType.DynamicObject:
								{
									if(reader.NextIs('{'))
									{
										if (reader.NextIs('}'))
										{
											return null;
										}
										var str = reader.ReadCheckString();
										var runtimeType = QReflection.ParseType(str);
										if (reader.NextIs(':') || reader.NextIs('='))
										{
											if (type == runtimeType)
											{
												target = ReadObject(reader, typeInfo, hasName, target);
											}
											else
											{
												target = ReadType(reader, runtimeType, hasName, target);
											}
										}
										while (!reader.IsEnd() && !reader.NextIs('}'))
										{
											reader.Read();
										}
									}
									else
									{
										if (reader.ReadValueString() == "null")
										{
											target = null;
										}
										else
										{
											target = null;
										}
									}
									return target;

								}
							case QObjectType.UnityObject:
								{
									reader.NextIs('{');
									var str = reader.ReadValueString();
									if (str == "null")
									{
										target = null;
									}
									else
									{
										target = QIdObject.GetObject(str, type);
									}
									reader.NextIs('}');
									return target;
								}
							case QObjectType.Object:
								{
									target=ReadObject(reader, typeInfo, hasName, target);
								}
								return target;

							case QObjectType.List:
								{
									var list = QReflection.CreateInstance(type, target) as IList;
									if (reader.NextIs('['))
									{
										var count = 0;
										for (var i = 0; !reader.IsEnd() && !reader.NextIs(']'); i++)
										{
											if (i < list.Count)
											{
												list[i] = reader.ReadType(typeInfo.ElementType, hasName, list[i]);
											}
											else
											{
												list.Add(reader.ReadType(typeInfo.ElementType, hasName));
											}
											count++;
											if (!( reader.NextIs(';') || reader.NextIs(','))){
												if (reader.NextIs(']'))
												{
													break; 
												}
												else
												{
													throw new Exception("格式出错 缺少;或,"); ;
												}
											}
										}
										for (int i = count; i < list.Count; i++)
										{
											list.RemoveAt(i);
										}
									}
									return list;
								}
							case QObjectType.Array:
								{
									List<object> list = new List<object>();
									if (reader.NextIs('['))
									{
										for (int i = 0; !reader.IsEnd() && !reader.NextIs(']'); i++)
										{
											list.Add(reader.ReadType(typeInfo.ElementType, hasName));
											if (!(reader.NextIs(';') || reader.NextIs(',')))
											{
												if (reader.NextIs(']'))
												{
													break;
												}
												else
												{
													throw new Exception("格式出错 缺少;或,"); ;
												}
											}
										}
									}
									var array = QReflection.CreateInstance(type, null, false,list.Count) as Array;
									for (int i = 0; i < list.Count; i++)
									{
										array.SetValue(list[i], i);
									}
									return array;
								}
							case QObjectType.TimeSpan:
								{
									return TimeSpan.FromTicks(reader.ReadQData<long>());
								}
							default:
								Debug.LogError("不支持类型[" + type + "]");
								return null;
						}
					}

				case TypeCode.Boolean:
					return bool.Parse(ReadValueString(reader));

				case TypeCode.Char:
					return char.Parse(ReadValueString(reader));
				case TypeCode.DateTime:
					return DateTime.Parse(ReadCheckString(reader));
				case TypeCode.DBNull:
					return null;
				case TypeCode.Decimal:
					return decimal.Parse(ReadValueString(reader));
				case TypeCode.Double:
					return double.Parse(ReadValueString(reader));
				case TypeCode.Empty:
					return null;

				case TypeCode.Single:
					return float.Parse(ReadValueString(reader));
				case TypeCode.String:
					return ReadCheckString(reader);
				case TypeCode.Int16:
					if (type.IsEnum)
					{
						return type.ParseEnum(ReadCheckString(reader,","));
					}
					return short.Parse(ReadValueString(reader));
				case TypeCode.Int32:
					if (type.IsEnum)
					{
						return type.ParseEnum(ReadCheckString(reader,","));
					}
					return int.Parse(ReadValueString(reader));
				case TypeCode.Int64:
					if (type.IsEnum)
					{
						return type.ParseEnum(ReadCheckString(reader,","));
					}
					return long.Parse(ReadValueString(reader));
				case TypeCode.SByte:
					if (type.IsEnum)
					{
						return type.ParseEnum(ReadCheckString(reader,","));
					}
					return sbyte.Parse(ReadValueString(reader));
				case TypeCode.Byte:
					if (type.IsEnum)
					{
						return type.ParseEnum(ReadCheckString(reader,","));
					}
					return byte.Parse(ReadValueString(reader));
				case TypeCode.UInt16:
					if (type.IsEnum)
					{
						return type.ParseEnum(ReadCheckString(reader,","));
					}
					return ushort.Parse(ReadValueString(reader));
				case TypeCode.UInt32:
					if (type.IsEnum)
					{
						return type.ParseEnum(ReadCheckString(reader,","));
					}
					return uint.Parse(ReadValueString(reader));
				case TypeCode.UInt64:
					if (type.IsEnum)
					{
						return type.ParseEnum(ReadCheckString(reader,","));
					}
					return ulong.Parse(ReadValueString(reader));
				default:
					Debug.LogError("不支持类型[" + typeCode + "]");
					return null;
			}
		}


		const string BlockStart = "<{[\"";
		const string BlockEnd = ">}]\",;=:";
		static Stack<char> BlockStack = new Stack<char>();
		public static string ReadValueString(this StringReader reader,string ignore="")
		{
			return Tool.BuildString((writer) =>
			{
				lock (BlockStack)
				{
					int index = -1;
					BlockStack.Clear();
					while (!reader.IsEnd())
					{
						var c = (char)reader.Peek();
						if (BlockStack.Count == 0)
						{
							if (ignore.IndexOf(c)<0&& BlockEnd.IndexOf(c) >= 0)
							{
								break;
							}
							else if ((index = BlockStart.IndexOf(c)) >= 0)
							{
								BlockStack.Push(BlockEnd[index]);
							}
						}
						else
						{
							if (BlockStack.Peek() == c)
							{
								BlockStack.Pop();
							}
							else if ((index = BlockStart.IndexOf(c)) >= 0)
							{
								BlockStack.Push(BlockEnd[index]);
							}
						}
						reader.Read();
						writer.Write(c);
					}
				}
			});
		}

		public static void WriteCheckString(this StringWriter writer, string value)
		{
			if (value == null)
			{
				writer.Write("\"\"");
				return;
			}
			using (StringReader reader = new StringReader(value))
			{
				writer.Write("\"");
				while (!reader.IsEnd())
				{

					var c = (char)reader.Read();
					switch (c)
					{
						case '"':
							writer.Write("\\\"");
							break;
						case '\\':
							writer.Write("\\\\");
							break;
						case '\b':
							writer.Write("\\b");
							break;
						case '\f':
							writer.Write("\\f");
							break;
						case '\n':
							writer.Write("\\n");
							break;
						case '\r':
							writer.Write("\\r");
							break;
						case '\t':
							writer.Write("\\t");
							break;
						default:
							writer.Write(c);
							break;
					}
				}
				writer.Write("\"");
			}
		}
		public static string ReadCheckString(this StringReader reader,string ignore="")
		{
			if (reader.NextIs('\"'))
			{
				using (var writer = new StringWriter())
				{
					while (!reader.IsEnd() && !reader.NextIs('\"'))
					{
						if (reader.NextIs('\\'))
						{
							var c = (char)reader.Read();
							switch (c)
							{
								case '"':
								case '\\':
								case '/':
									writer.Write(c);
									break;
								case 'b':
									writer.Write('\b');
									break;
								case 'f':
									writer.Write('\f');
									break;
								case 'n':
									writer.Write('\n');
									break;
								case 'r':
									writer.Write('\r');
									break;
								case 't':
									writer.Write('\t');
									break;
							}
						}
						else
						{
							writer.Write((char)reader.Read());
						}
					}
					return writer.ToString();
				}
			}
			else
			{
				return ReadValueString(reader, ignore);
			}
		}

		public static QDataList ToQDataList<T>(this IList<T> list,QDataList qdataList=null, Type type=null) 
		{
			if (type == null)
			{
				type = typeof(T);
				if (type == typeof(object))
				{
					throw new Exception(nameof(QDataList)+ "类型出错 " + type);
				}
			}
			if (qdataList == null)
			{
				qdataList = new QDataList();
			}
			else
			{
				qdataList.Clear();
			}
			
			var typeInfo = QSerializeType.Get(type);
			foreach (var member in typeInfo.Members)
			{
				qdataList.TitleRow.Add(member.ViewName);
				for (int i = 0; i < list.Count; i++)
				{
					qdataList[i + 1].SetValueType(member.ViewName, member.Get(list[i]), member.Type);
				}
			}
			return qdataList;
		}
		public static List<T> ParseQdataList<T>(this QDataList qdataList, List<T> list,Type type=null) where T:class
		{
			if (type == null)
			{
				type = typeof(T);
				if (type == typeof(object))
				{
					throw new Exception(nameof(QDataList) + "类型出错 " + type);
				}
			}
			var typeInfo = QSerializeType.Get(type);
			list.Clear();
			var titleRow = qdataList.TitleRow;
			var memeberList = new List<QMemeberInfo>();
			foreach (var title in titleRow)
			{
				var member = typeInfo.GetMemberInfo(title);
				if (member == null)
				{
					Debug.LogError("读取 " + type.Name + "出错 不存在属性 " + title);
				}
				memeberList.Add(member);
			}
			foreach (var row in qdataList)
			{
				if (row == titleRow) continue;
				var t = type.CreateInstance();
				for (int i = 0; i < titleRow.Count; i++)
				{
					var member = memeberList[i];
					if (member != null)
					{
						try
						{
							var value = row[i].ParseElement();
							var hasName= value!=null&& value.Contains( "\":");
							member.Set(t, value.ParseQDataType(member.Type, hasName));
						}
						catch (System.Exception e)
						{

							Debug.LogError("读取 " + type.Name + "出错 设置[" + row.Key + "]属性 " + member.Key + "(" + member.Type + ")异常：\n" + e);
						}

					}
				}
				list.Add(t as T);
			}
			QDebug.Log("读取 " + type.Name + " 完成：\n" + list.ToOneString() + "\n\nQDataList:\n" + qdataList);
			return list;
		}
	}
	public interface IQData
	{
		void ToQData(StringWriter writer);
		void ParseQData(StringReader reader);
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Interface)]
	public class QIgnoreAttribute : Attribute
	{

	}
	[AttributeUsage(AttributeTargets.Class ,AllowMultiple = false,Inherited = false)]
	public class QDynamicAttribute : Attribute
	{

	}

	public enum QObjectType
	{
		UnityObject,
		DynamicObject,
		Object,
		List,
		Array,
		TimeSpan,
		CantSerialize,
	}
	public class QSerializeType : QTypeInfo<QSerializeType>
	{
		public static QDictionary<Type, List<string>> TypeMembers = new QDictionary<Type, List<string>>()
		{
			new QKeyValue<Type, List<string>>
			{
				 Key=typeof(Rect),
				 Value=new List<string>
				 {
					 "position",
					 "size",
				 }
			}
		};
		static bool IsQSValue(MemberInfo info)
		{
			if (info.GetCustomAttribute<QIgnoreAttribute>() != null)
			{
				return false;
			}
			return true;
		}
		public QObjectType objType = QObjectType.Object;
		public bool IsIQSerialize { private set; get; }
		public bool IsIQData { private set; get; }
		//public bool IsUnityObject { private set; get; }
		protected override void Init(Type type)
		{

			Functions = null;
			base.Init(type);
			if (Code == TypeCode.Object)
			{
				objType = QObjectType.Object;
				if (typeof(Task).IsAssignableFrom(type))
				{
					objType = QObjectType.CantSerialize;
					return;
				}
				IsIQSerialize = typeof(Binary.IQSerialize).IsAssignableFrom(type);
				IsIQData = typeof(IQData).IsAssignableFrom(type);
				
				if (IsIQData)
				{
					objType = QObjectType.Object;
				}
				else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
				{
					objType = QObjectType.UnityObject;
				}
				else if( type==typeof(System.Object)||type.IsAbstract||type.IsInterface|| type.GetCustomAttribute<QDynamicAttribute>()!=null)
				{
					objType = QObjectType.DynamicObject;
				}
				else if (IsArray)
				{
					objType = QObjectType.Array;
				}
				else if (IsList)
				{
					objType = QObjectType.List;
				}
				else if (type == typeof(TimeSpan))
				{
					objType = QObjectType.TimeSpan;
				}
				else
				{
					if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
					{
						Debug.LogError("不支持序列化【" + type + "】Nullable类型");
					}
				}
			}

			Members.RemoveAll((member) =>
			{

				if (TypeMembers.ContainsKey(type))
				{
					return !TypeMembers[type].Contains(member.Key);
				}
				return !IsQSValue(member.MemeberInfo) || member.Key == "Item" || member.Set == null || member.Get == null || (member.Type.IsArray && member.Type.GetArrayRank() > 1);
			});

		}

	}
}

