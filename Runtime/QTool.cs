using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Serialization;
using System;
using System.Threading.Tasks;
using QTool.Reflection;
using System.Reflection;
using System.IO;
using System.Text;

namespace QTool
{


    public static partial class Tool
    {
        static QDictionary<string, Color> KeyColor = new QDictionary<string, Color>();
        public static Color ToColor(this string key, float s = 0.5f, float v = 1f)
        {
            if (string.IsNullOrWhiteSpace(key)) return Color.white;
            var colorKey = key + s + v;
            if (!KeyColor.ContainsKey(colorKey))
            {
                var colorValue = Mathf.Abs(key[0].GetHashCode() % 800) + Mathf.Abs(key.GetHashCode() % 200f);
                KeyColor[colorKey] = Color.HSVToRGB(colorValue / 1000, s, v);
            }
            return KeyColor[colorKey];
        }
        public static void RunTimeCheck(string name, System.Action action, Func<int> getLength = null, Func<string> getInfo = null)
        {
            var last = System.DateTime.Now;
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError("【" + name + "】运行出错:" + e);
                return;
            }
            var checkInfo = "【" + name + "】运行时间:" + (System.DateTime.Now - last).TotalMilliseconds;
            if (getLength != null)
            {
                checkInfo += " " + " 大小" + getLength().ToSizeString();
            }
            if (getInfo != null)
            {
                checkInfo += "\n" + getInfo();
            }
            Debug.LogError(checkInfo);
        }

        public static bool PercentRandom(float percent)
        {
            var value = UnityEngine.Random.Range(0, 100);
            return value <= percent;
        }
        public static T RandomGet<T>(this IList<T> list)
        {
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        public static IList<T> Random<T>(this IList<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var cur = list[i];
                list.Remove(cur);
                list.Insert(UnityEngine.Random.Range(0, i), cur);
            }
            return list;
        }
        public static async Task<bool> DelayGameTime(float second, bool ignoreTimeScale = false)
        {
            var startTime = (ignoreTimeScale ? Time.unscaledTime : Time.time);
            return await Tool.Wait(() => startTime + second <= (ignoreTimeScale ? Time.unscaledTime : Time.time));
        }
        public static async Task<bool> Wait(Func<bool> flagFunc)
        {
            if (flagFunc == null) return Application.isPlaying;
            while (!flagFunc.Invoke() && Application.isPlaying)
            {
                await Task.Delay(100);
            }
            return Application.isPlaying;
        }
        internal static void ForeachArray(this Array array, int deep, int[] indexArray, Action<int[]> Call, Action start = null, Action end = null, Action mid = null)
        {
            start?.Invoke();
            var length = array.GetLength(deep);
            for (int i = 0; i < length; i++)
            {
                indexArray[deep] = i;
                if (deep + 1 < indexArray.Length)
                {
                    ForeachArray(array, deep + 1, indexArray, Call, start, end, mid);
                }
                else
                {
                    Call?.Invoke(indexArray);
                }
                if (i < length - 1)
                {

                    mid?.Invoke();
                }

            }
            end?.Invoke();
        }
        public static string ToQData<T>(this T obj, bool hasName = true)
        {
            var type = typeof(T);
            return ToQDataType(obj, type, hasName);
        }
        public static string ToQDataType(this object obj, Type type, bool hasName = true)
        {
			var builder = StringBuilderPool.Get();
			WriteType(builder, obj, type, hasName);
			var str= builder.ToString();
			builder.Clear();
			StringBuilderPool.Push(builder);
			return str;
		}
		static ObjectPool<StringBuilder> StringBuilderPool = new ObjectPool<System.Text.StringBuilder>(nameof(StringBuilderPool), () => new System.Text.StringBuilder());
		internal static void WriteType(this StringBuilder writer, object obj, Type type, bool hasName)
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
										WriteCheckString( writer,runtimeType.FullName );
										writer.Append(':');
										WriteType(writer, obj, runtimeType, hasName);
									}
									else if (typeInfo.IsUnityObject)
									{
										writer.Append(QObjectReference.GetId(obj as UnityEngine.Object));
									}
									else if (typeInfo.IsIQData)
									{
										WriteCheckString(writer,(obj as IQData).ToQData());
									}
									else
									{
										if (hasName)
										{
											for (int i = 0; i < typeInfo.Members.Count; i++)
											{
												var memberInfo = typeInfo.Members[i];
												var member = memberInfo.Get(obj);
												WriteCheckString(writer, memberInfo.Name );
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
									writer.Append(']'); ;
									break;
								}
							default:
								break;
						}
						break;
					}
				case TypeCode.String:
					WriteCheckString(writer,obj?.ToString());
					break;
				case TypeCode.Boolean:
					writer.Append( obj.ToString().ToLower());
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
		static void WriteCheckString(this StringBuilder writer,string value)
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
							//int codepoint = Convert.ToInt32(c);
							//if ((codepoint >= 32) && (codepoint <= 126))
							//{
							//	writer.Write(c);
							//}
							//else
							//{
							//	writer.Write("\\u");
							//	writer.Write(codepoint.ToString("x4"));
							//}
							break;
					}
				}
				writer.Append("\"");
			}
		}
		static string ReadCheckString(this StringReader reader)
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
       
		public static object ParseQData(this string qdataStr, Type type, bool hasName = true, object target = null)
		{

			if (string.IsNullOrEmpty(qdataStr))
			{
				return type.IsValueType ? QReflection.CreateInstance(type, target) : null;
			}
			try
			{
				using (var reader=new StringReader(qdataStr))
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

		internal static object ReadType(this StringReader reader, Type type, bool hasName = true, object target = null)
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
											target = (QReflection.CreateInstance(type, target) as IQData).ParseQData(reader.ReadCheckString()); reader.NextIs('}');
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
										for (int i = 0; !reader.IsEnd()&&!reader.NextIs(']'); i++)
										{
											if (i < list.Count)
											{
												list[i]=reader.ReadType(typeInfo.ElementType, hasName, list[i]);
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
										for (int i = 0; !reader.IsEnd()&&!reader.NextIs(']'); i++)
										{
											list.Add(reader.ReadType(typeInfo.ElementType, hasName));
											var next =reader.NextIs(';') || reader.NextIs(',');
										}
									}
									else
									{
										throw new Exception("读取List出错[" + type + "][" + reader.ToString() + "]");
									}
									var array= QReflection.CreateInstance(type, null, list.Count) as Array;
									for (int i = 0; i < list.Count; i++)
									{
										array.SetValue(list[i],i);
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
		static string ReadValueString(this StringReader reader)
		{
			var builder = StringBuilderPool.Get();
			lock (BlockStack)
			{
				int index = -1;
				BlockStack.Clear();
				while (!reader.IsEnd())
				{
					var c = (char)reader.Peek();
					if (BlockStack.Count==0)
					{
						if (BlockEnd.IndexOf(c) >= 0)
						{
							break;
						}
						else if( (index=BlockStart.IndexOf(c))>=0)
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
		public static T ParseQData<T>(this string qdataStr, bool hasName = true, T target = default)
        {
            return (T)ParseQData(qdataStr, typeof(T), hasName, target);
        }
        public static bool NextIs(this StringReader reader, char value)
        {
            if (reader.Peek() == value)
            {
                reader.Read();
                return true;
            }
            return false;
        }
        public static void NextIgnore(this StringReader reader, char value)
        {
            if (reader.Peek() == value)
            {
                reader.Read();
            }
        }
        public static bool IsEnd(this StringReader reader)
        {
            return reader.Peek() < 0;
		}
	
		public static string ToQDataListElement(string value)
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
		public static string ParseQDataListElement(string value)
        {
            if (value.StartsWith("\"") && value.EndsWith("\"") && (value.Contains("\n")||true))
            {
                value = value.Substring(1, value.Length - 2);
                value = value.Replace("\"\"", "\"");
                return value;
            }
            return value;
        }
        public static string ReadElement(this StringReader reader ,out bool newLine )
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
                                newLine = false;
                                break;
                            }
                        }
                     
                    }
                    var value = writer.ToString();
                    return value;
                }
                else
                {
                    while (!reader.IsEnd()&&!reader.NextIs('\n'))
					{
						if (reader.NextIs('\t'))
						{
							newLine = false;
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
       
        static bool ReadSplit(this StringReader reader, out string start, out string end)
        {
            using (var writer = new StringWriter())
            {
                while (!reader.NextIs('=')&&!reader.NextIs(':') && !reader.IsEnd())
                {
                    var c = (char)reader.Read();
                    writer.Write(c);
                }
                start = writer.ToString();
                end = reader.ReadToEnd();
                return true;
            }
        }
       
        /// <summary>
        /// 获取异或校验值
        /// </summary>
        public static byte ToCheckFlag(this byte[] bytes, byte flag = 0)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                flag ^= bytes[i];
            }
            return flag;
        }
    }
    public class SecondsAverageList
    {
        public float Value
        {
            get; private set;
        }
        QDictionary<double, float> list = new QDictionary<double, float>();
        public float AllSum { private set; get; }
        
        double _lastSumTime=-1;
        float _secondeSum = 0;
        public double StartTime { private set; get; } = -1;
        public double EndTime { private set; get; }
        public float SecondeSum
        {
            get
            {
                if (EndTime == 0) return 0;
                if (EndTime == _lastSumTime)
                {
                    return _secondeSum;
                }
                _lastSumTime = EndTime;
                _secondeSum = 0f;
                foreach (var kv in list)
                {
                    _secondeSum += kv.Value;
                }
                return _secondeSum ;
            }
        }
        int maxCount = -1;
        static double CurTime
        {
            get
            {
                return (DateTime.Now - new DateTime()).TotalSeconds;
            }
        }
        public void Push(float value)
        {
            if (StartTime<0)
            {
                StartTime = CurTime;
            }
            AllSum += value;
            list.RemoveAll((kv) => (CurTime - kv.Key) > 1);
            list[CurTime] = value;
            EndTime = CurTime;
            Value = SecondeSum/list.Count;
        }
        public void Clear()
        {
            list.Clear();
            StartTime = -1;
            _lastSumTime = -1;
            _secondeSum = 0;
        }
        public override string ToString()
        {
            return "总记[" + AllSum + "]平均[" + Value + "/s]";
        }
        public  string ToString(Func<float,string> toString)
        {
            return "总记[" + toString(AllSum) + "]平均[" + toString(Value) + "/s]";
        }
    }


    public interface IQData
    {
        string ToQData();
        object ParseQData(string qdataStr);
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
                }else if (IsArray)
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
				return !IsQSValue(member.MemeberInfo) || member.Key == "Item" || member.Set == null || member.Get == null||(member.Type.IsArray&&member.Type.GetArrayRank()>1);
			});
			
		}

    }

}
