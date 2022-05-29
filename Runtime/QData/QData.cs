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
			var builder = StringBuilderPool.Get();
			WriteType(builder, obj, type, hasName);
			var str = builder.ToString();
			builder.Clear();
			StringBuilderPool.Push(builder);
			return str;
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
				Debug.LogError("解析类型【" + type + "】出错：" + qdataStr + "\n" + e);
				return type.IsValueType ? QReflection.CreateInstance(type, target) : null;
			}

		}

		static ObjectPool<StringBuilder> StringBuilderPool = new ObjectPool<System.Text.StringBuilder>(nameof(StringBuilderPool), () => new System.Text.StringBuilder());
		public static void Write<T>(this StringBuilder writer, T obj, bool hasName = true)
		{
			WriteType(writer, obj, typeof(T), hasName);
		}
		public static void WriteType(this StringBuilder writer, object obj, Type type, bool hasName=true)
		{
			var typeCode = Type.GetTypeCode(type);
			switch (typeCode)
			{
				case TypeCode.Object:
					{
						var typeInfo = QSerializeType.Get(type);
						switch (typeInfo.objType)
						{
							case QObjectType.Object:
								{
									if (obj == null) break;
									writer.Append('{');
									if (type == typeof(object))
									{
										var runtimeType = obj.GetType();
										WriteCheckString(writer, runtimeType.FullName);
										writer.Append(':');
										WriteType(writer, obj, runtimeType, hasName);
									}
									else if (typeInfo.IsUnityObject)
									{
										writer.Append(QObjectReference.GetId(obj as UnityEngine.Object));
									}
									else if (typeInfo.IsIQData)
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
												WriteCheckString(writer, memberInfo.Name);
												writer.Append(':');
												WriteType(writer, member, memberInfo.Type, hasName);
												if (i < typeInfo.Members.Count - 1)
												{
													writer.Append(',');
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
													writer.Append(',');
												}
											}
										}

									}
									writer.Append('}');

									break;
								}
							case QObjectType.List:
							case QObjectType.Array:
								{
									var list = obj as IList;
									if (list == null) break;
									writer.Append('[');
									for (int i = 0; i < list.Count; i++)
									{
										WriteType(writer, list[i], typeInfo.ElementType, hasName);
										if (i < list.Count - 1)
										{
											writer.Append(',');
										}
									}
									writer.Append(']');
									break;
								}
							default:
								break;
						}
						break;
					}
				case TypeCode.String:
					WriteCheckString(writer, obj?.ToString());
					break;
				case TypeCode.Boolean:
					writer.Append(obj.ToString().ToLower());
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
						writer.Append(obj.ToString());
					}
					break;
				default:
					writer.Append(obj.ToString());
					break;
			}
		}
		public static object Read<T>(this StringReader reader, T target = default,bool hasName = true)
		{
			return ReadType(reader, typeof(T), hasName, target);
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
							case QObjectType.Object:
								{
									if (reader.NextIs('{'))
									{
										if (reader.NextIs('}'))
										{
											return null;
										}
										if (type == typeof(object))
										{
											var runtimeType = QReflection.ParseType(reader.ReadCheckString());
											if (reader.NextIs(':') || reader.NextIs('='))
											{
												target = ReadType(reader, runtimeType, hasName);
											}
											reader.NextIs('}');
										}
										else if (typeInfo.IsUnityObject)
										{
											target = QObjectReference.GetObject(reader.ReadValueString(), type); reader.NextIs('}');
										}
										else if (typeInfo.IsIQData)
										{
											target = QReflection.CreateInstance(type, target) ;
											(target as IQData).ParseQData(reader); reader.NextIs('}');
										}
										else
										{
											if (target == null)
											{
												target = QReflection.CreateInstance(type, target);
											}
											if (hasName)
											{
												while (!reader.IsEnd())
												{
													var name = reader.ReadCheckString();
													var memeberInfo = typeInfo.Members[name];
													if (memeberInfo != null)
													{

														object result = null;
														try
														{
															if (reader.NextIs(':') || reader.NextIs('='))
															{
																result = reader.ReadType(memeberInfo.Type, hasName, memeberInfo.Get(target));
																memeberInfo.Set(target, result);
															}
															else
															{
																throw new Exception("缺少分隔符 : 或 =");
															}
															if (!(reader.NextIs(';') || reader.NextIs(',')))
															{
																if (reader.NextIs('}'))
																{
																	break;
																}
															}
														}
														catch (Exception e)
														{
															Debug.LogError("读取成员【" + type.Name + "." + memeberInfo.Name + "】出错" + memeberInfo.Type + ":" + result + ":" + memeberInfo.Get(target) + "\n" + e);
															throw e;
														}
													}
													else
													{
														Debug.LogWarning("不存在成员" + typeInfo.Key + "." + name);
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
										return null;
									}
								}
								return target;

							case QObjectType.List:
								{
									var list = QReflection.CreateInstance(type, target) as IList;
									if (reader.NextIs('['))
									{
										for (int i = 0; !reader.IsEnd() && !reader.NextIs(']'); i++)
										{
											if (i < list.Count)
											{
												list[i] = reader.ReadType(typeInfo.ElementType, hasName, list[i]);
											}
											else
											{
												list.Add(reader.ReadType(typeInfo.ElementType, hasName));
											}
											var next = reader.NextIs(';') || reader.NextIs(',');
										}
									}
									else
									{
										throw new Exception("读取List出错[" + type + "][" + reader.ToString() + "]");
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
											var next = reader.NextIs(';') || reader.NextIs(',');
										}
									}
									else
									{
										throw new Exception("读取Array出错[" + type + "][" + reader.ToString() + "]");
									}
									var array = QReflection.CreateInstance(type, null, list.Count) as Array;
									for (int i = 0; i < list.Count; i++)
									{
										array.SetValue(list[i], i);
									}
									return array;
								}
							default:
								return null;
						}
					}

				case TypeCode.Boolean:
					return bool.Parse(ReadValueString(reader));

				case TypeCode.Char:
					return char.Parse(ReadValueString(reader));
				case TypeCode.DateTime:
					return DateTime.Parse(ReadValueString(reader));
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
						return Enum.Parse(type, ReadCheckString(reader));
					}
					return short.Parse(ReadValueString(reader));
				case TypeCode.Int32:
					if (type.IsEnum)
					{
						return Enum.Parse(type, ReadCheckString(reader));
					}
					return int.Parse(ReadValueString(reader));
				case TypeCode.Int64:
					if (type.IsEnum)
					{
						return Enum.Parse(type, ReadCheckString(reader));
					}
					return long.Parse(ReadValueString(reader));
				case TypeCode.SByte:
					if (type.IsEnum)
					{
						return Enum.Parse(type, ReadCheckString(reader));
					}
					return sbyte.Parse(ReadValueString(reader));
				case TypeCode.Byte:
					if (type.IsEnum)
					{
						return Enum.Parse(type, ReadCheckString(reader));
					}
					return byte.Parse(ReadValueString(reader));
				case TypeCode.UInt16:
					if (type.IsEnum)
					{
						return Enum.Parse(type, ReadCheckString(reader));
					}
					return ushort.Parse(ReadValueString(reader));
				case TypeCode.UInt32:
					if (type.IsEnum)
					{
						return Enum.Parse(type, ReadCheckString(reader));
					}
					return uint.Parse(ReadValueString(reader));
				case TypeCode.UInt64:
					if (type.IsEnum)
					{
						return Enum.Parse(type, ReadCheckString(reader));
					}
					return ulong.Parse(ReadValueString(reader));
				default:
					Debug.LogError("不支持类型[" + typeCode + "]");
					return null;
			}


		}


		const string BlockStart = "{[\"";
		const string BlockEnd = "}]\",;=:";
		static Stack<char> BlockStack = new Stack<char>();
		public static string ReadValueString(this StringReader reader)
		{
			var builder = StringBuilderPool.Get();
			lock (BlockStack)
			{
				int index = -1;
				BlockStack.Clear();
				while (!reader.IsEnd())
				{
					var c = (char)reader.Peek();
					if (BlockStack.Count == 0)
					{
						if (BlockEnd.IndexOf(c) >= 0)
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
					}
					reader.Read();
					builder.Append(c);
				}
			}
			var value = builder.ToString();
			builder.Clear();
			StringBuilderPool.Push(builder);
			return value;
		}

		public static void WriteCheckString(this StringBuilder writer, string value)
		{
			if (value == null)
			{
				return;
			}
			using (StringReader reader = new StringReader(value))
			{
				writer.Append("\"");
				while (!reader.IsEnd())
				{

					var c = (char)reader.Read();
					switch (c)
					{
						case '"':
							writer.Append("\\\"");
							break;
						case '\\':
							writer.Append("\\\\");
							break;
						case '\b':
							writer.Append("\\b");
							break;
						case '\f':
							writer.Append("\\f");
							break;
						case '\n':
							writer.Append("\\n");
							break;
						case '\r':
							writer.Append("\\r");
							break;
						case '\t':
							writer.Append("\\t");
							break;
						default:
							writer.Append(c);
							break;
					}
				}
				writer.Append("\"");
			}
		}
		public static string ReadCheckString(this StringReader reader)
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
				return ReadValueString(reader);
			}
		}


	}
	public interface IQData
	{
		void ToQData(StringBuilder writer);
		void ParseQData(StringReader reader);
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Interface)]
	public class QIgnoreAttribute : Attribute
	{

	}


	public enum QObjectType
	{
		Object,
		List,
		Array,
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
		public bool IsUnityObject { private set; get; }
		protected override void Init(Type type)
		{

			Functions = null;
			base.Init(type);
			if (Code == TypeCode.Object)
			{
				if (typeof(Task).IsAssignableFrom(type))
				{
					objType = QObjectType.CantSerialize;
					return;
				}
				IsIQSerialize = typeof(Binary.IQSerialize).IsAssignableFrom(type);
				IsIQData = typeof(IQData).IsAssignableFrom(type);
				IsUnityObject = typeof(UnityEngine.Object).IsAssignableFrom(type);
				if (IsIQSerialize || IsIQData)
				{
					objType = QObjectType.Object;
				}
				else if (IsArray)
				{
					objType = QObjectType.Array;
				}
				else if (IsList)
				{
					objType = QObjectType.List;
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

