using System;
using System.Collections;
using System.IO;
using UnityEngine;
using QTool.Reflection;
namespace QTool.Binary
{

    public static class QSerialize
    {
        public static System.Byte[] Serialize<T>(T value)
        {
            return SerializeType(value, typeof(T));
        }
        public static T Deserialize<T>(byte[] bytes, T targetObj = default)
        {
            return (T)DeserializeType(bytes, typeof(T), targetObj);
        }
        public static QBinaryWriter SerializeType(this QBinaryWriter writer, object value, Type type)
        {
            try
            {
                TypeCode typeCode = Type.GetTypeCode(type);
                switch (typeCode)
                {
                    case TypeCode.Object:
                        {
                            var isNull = object.Equals(value, null);
                            writer.Write(isNull);
                            if (isNull) { return writer; }
                            QSerializeType typeInfo = QSerializeType.Get(type);
                            switch (typeInfo.objType)
                            {
								case QObjectType.DynamicObject:
									{
										var runtimeType = value.GetType();
										writer.Write(runtimeType.FullName);
										writer.SerializeType(value,runtimeType);
									}
									break;
								case QObjectType.UnityObject:
									{
										writer.Write(QIdObject.GetId(value as UnityEngine.Object));
									}
									break;
								case QObjectType.List:
								case QObjectType.Array:
									var list = value as IList;
                                    writer.Write(list.Count);
                                    foreach (var item in list)
                                    {
                                        writer.SerializeType(item, typeInfo.ElementType);
                                    }
                                    break;
                                case QObjectType.Object:

                                    if (typeInfo.IsIQSerialize)
                                    {
                                        (value as IQSerialize).Write(writer);
                                    }
                                    else
                                    {
                                        foreach (var item in typeInfo.Members)
                                        {
                                            writer.SerializeType(item.Get(value), item.Type);
                                        }
                                    }
                                    break;
                                default:
                                    throw new Exception("???????????????[" + type + "]??????");
                            }


                        }
                        break;
                    #region ????????????
                    case TypeCode.Boolean:
                        writer.Write((bool)value);
                        break;
                    case TypeCode.Byte:
                        writer.Write((byte)value);
                        break;
                    case TypeCode.Char:
                        writer.Write((char)value);
                        break;
                    case TypeCode.DateTime:
                        writer.Write(((DateTime)value).Ticks);
                        break;
                    case TypeCode.DBNull:
                        break;
                    case TypeCode.Decimal:
                    case TypeCode.Double:
                        writer.Write((double)value);
                        break;
                    case TypeCode.Empty:
                        break;
                    case TypeCode.Int16:
                        writer.Write((Int16)value);
                        break;
                    case TypeCode.Int32:
                        writer.Write((Int32)value);
                        break;
                    case TypeCode.Int64:
                        writer.Write((Int64)value);
                        break;

                    case TypeCode.SByte:
                        writer.Write((SByte)value);
                        break;
                    case TypeCode.Single:
                        writer.Write((Single)value);
                        break;
                    case TypeCode.String:
                        writer.Write((string)value);
                        break;
                    case TypeCode.UInt16:
                        writer.Write((UInt16)value);
                        break;
                    case TypeCode.UInt32:
                        writer.Write((UInt32)value);
                        break;
                    case TypeCode.UInt64:
                        writer.Write((UInt64)value);
                        break;
                    default:
                        Debug.LogError("?????????????????????" + typeCode + "???");
                        break;
                        #endregion
                }
            }
            catch (Exception e)
            {
                Debug.LogError("???????????????[" + type + "]:[" + value + "]?????????" + e);
            }

            return writer;
        }
        public static byte[] SerializeType(object value, Type type)
        {
            using (var writer = new QBinaryWriter())
            {
                return SerializeType(writer, value, type).ToArray();
            }
        }
        public static object DeserializeType(this QBinaryReader reader, Type type, object target = null)
        {
            TypeCode typeCode = Type.GetTypeCode(type);
            switch (typeCode)
            {
                case TypeCode.Object:
                    QSerializeType typeInfo = null;
                    if (reader.ReadBoolean())
                    {
                        return target;
                    }
                    var hasTarget = target != null;
                    typeInfo = QSerializeType.Get(type);
                    switch (typeInfo.objType)
                    {
						case QObjectType.DynamicObject:
							{
								var runtimeType = QReflection.ParseType(reader.ReadString());
								target = reader.ReadObjectType(runtimeType, target);
								return target;
							}
						case QObjectType.UnityObject:
							{
								target = QIdObject.GetObject(reader.ReadString(), type);
								return target;
							}
						case QObjectType.List:
						case QObjectType.Array:
							{
                                var count = reader.ReadInt32();
								var obj = QReflection.CreateInstance(type, typeInfo.IsArray? null:target,false, count);
								var list = obj as IList;
								for (int i = 0; i < count; i++)
								{
									if (i < list.Count)
									{
										list[i] = DeserializeType(reader, typeInfo.ElementType, list[i]);
									}
									else
									{
										list.Add(DeserializeType(reader, typeInfo.ElementType));
									}
								}
								return list;
                            }
                        case QObjectType.Object:
                            {
                                if (typeInfo.IsIQSerialize)
                                {
                                    var serObj = QReflection.CreateInstance(type, target) as IQSerialize;
                                    serObj.Read(reader);
                                    return serObj;
                                }
                                else
                                {
                                    var obj = QReflection.CreateInstance(type, target);
                                    if (hasTarget)
                                    {
                                        foreach (var memeberInfo in typeInfo.Members)
                                        {
                                            memeberInfo.Set.Invoke(obj, DeserializeType(reader, memeberInfo.Type, memeberInfo.Get(target) ));
										}
                                    }
                                    else
                                    {
                                        foreach (var memeberInfo in typeInfo.Members)
                                        {
                                            memeberInfo.Set.Invoke(obj, DeserializeType(reader, memeberInfo.Type));
                                        }
                                    }
                                    return obj;
                                }
                            }
                        default:
                            Debug.LogError("??????????????????[" + type + "]??????");
                            return null;
                    }
                #region ????????????

                case TypeCode.Boolean:
                    return reader.ReadBoolean();
                case TypeCode.Byte:
                    return reader.ReadByte();
                case TypeCode.Char:
                    return reader.ReadChar();
                case TypeCode.DateTime:
                    return new DateTime(reader.ReadInt64());
                case TypeCode.DBNull:
                    return null;
                case TypeCode.Decimal:
                    return (decimal)reader.ReadDouble();
                case TypeCode.Double:
                    return reader.ReadDouble();
                case TypeCode.Empty:
                    return null;
                case TypeCode.Int16:
                    return reader.ReadInt16();
                case TypeCode.Int32:
                    return reader.ReadInt32();
                case TypeCode.Int64:
                    return reader.ReadInt64();

                case TypeCode.SByte:
                    return reader.ReadSByte();
                case TypeCode.Single:
                    return reader.ReadSingle();
                case TypeCode.String:
                    return reader.ReadString();
                case TypeCode.UInt16:
                    return reader.ReadUInt16();
                case TypeCode.UInt32:
                    return reader.ReadUInt32();
                case TypeCode.UInt64:
                    return reader.ReadUInt64();
                default:
                    Debug.LogError("?????????????????????" + typeCode + "???");
                    return null;

                    #endregion
            }

        }
        public static object DeserializeType(byte[] bytes, Type type, object target = null)
        {
            using (QBinaryReader reader = new QBinaryReader(bytes))
            {
				return DeserializeType(reader, type, target);
            }
        }
    }
    public interface IQSerialize
    {
        void Write(QBinaryWriter writer);
        void Read(QBinaryReader reader);
    }

    public class QBinaryReader:BinaryReader
    {
        public T ReadObject<T>(T obj = default)
        {
            return (T)ReadObjectType(typeof(T), obj);
        }
        public object ReadObjectType(Type type, object obj = default)
        {
            return this.DeserializeType(type, obj);
        }
        public QBinaryReader(byte[] bytes):base(new MemoryStream(bytes))
        {
        }
        public MemoryStream memory => BaseStream as MemoryStream;
        public byte[] ReadByteLengthBytes()
        {
            var count = base.ReadByte();
            return base.ReadBytes(count);
        }
        public override byte[] ReadBytes(int count=-1)
        {
            if (count < 0)
            {
                count= base.ReadInt32();
            }
            return base.ReadBytes(count);
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            BaseStream?.Dispose();
           
        }
    }
    public class QBinaryWriter : BinaryWriter
    {
        public QBinaryWriter() : base(new MemoryStream())
        {
        }
        public void WriteObject<T>(T obj)
        {
            WriteObjectType(obj, typeof(T));
        }

        public void WriteObjectType(object obj,Type type)
        {
            this.SerializeType(obj, type);
        }
        public byte[] ToArray()
        {
            return (BaseStream as MemoryStream).ToArray();
        }
        public void WriteByteLengthBytes(byte[] buffer)
        {
            if (buffer == null)
            {
                base.Write((byte)0);
            }
            else
            {
                base.Write((byte)buffer.Length);
                base.Write(buffer);
            }

        }
        public override void Write(byte[] buffer)
        {
            if (buffer == null)
            {
                base.Write(0);
            }
            else
            {
                base.Write(buffer.Length);
                base.Write(buffer);
            }
           
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            BaseStream?.Dispose();
        }
    }
    public static class QBinaryExtends
    {
       
        public static byte[] GetBytes(this string value)
        {
            if (value == null)
            {
                return new byte[0];
            }
            return System.Text.Encoding.Unicode.GetBytes(value);
        }
        public static string GetString(this byte[] value,int start,int length)
        {
            if (value == null)
            {
                return "";
            }
            return System.Text.Encoding.Unicode.GetString(value,start,length);
        }
        public static string GetString(this byte[] value)
        {
            if (value == null)
            {
                return "";
            }
            return System.Text.Encoding.Unicode.GetString(value);
        }
        public static byte[] GetBytes(this Boolean value)
        {
            return BitConverter.GetBytes(value);
        }
        public static bool GetBoolean(this byte[] value, int start = 0)
        {
            return BitConverter.ToBoolean(value, start);
        }



        public static byte[] GetBytes(this char value)
        {
            return BitConverter.GetBytes(value);
        }
        public static char GetChar(this byte[] value, int start = 0)
        {
            return BitConverter.ToChar(value, start);
        }

     

        public static byte[] GetBytes(this Int16 value)
        {
            return BitConverter.GetBytes(value);
        }
        public static Int16 GetInt16(this byte[] value, int start = 0)
        {
            return BitConverter.ToInt16(value, start);
        }

        public static byte[] GetBytes(this UInt16 value)
        {
            return BitConverter.GetBytes(value);
        }
        public static UInt16 GetUInt16(this byte[] value, int start = 0)
        {
            return BitConverter.ToUInt16(value, start);
        }

        public static byte[] GetBytes(this int value)
        {
            return BitConverter.GetBytes(value);
        }
        public static int GetInt32(this byte[] value, int start = 0)
        {
            return BitConverter.ToInt32(value, start);
        }

        public static byte[] GetBytes(this UInt32 value)
        {
            return BitConverter.GetBytes(value);
        }
        public static UInt32 GetUInt32(this byte[] value, int start = 0)
        {
            return BitConverter.ToUInt32(value, start);
        }

        public static byte[] GetBytes(this long value)
        {
            return BitConverter.GetBytes(value);
        }
        public static long GetInt64(this byte[] value, int start = 0)
        {
            return BitConverter.ToInt64(value, start);
        }

        public static byte[] GetBytes(this UInt64 value)
        {
            return BitConverter.GetBytes(value);
        }
        public static UInt64 GetUInt64(this byte[] value, int start = 0)
        {
            return BitConverter.ToUInt64(value, start);
        }


        public static byte[] GetBytes(this float value)
        {
            return BitConverter.GetBytes(value);
        }
        public static float GetSingle(this byte[] value, int start = 0)
        {
            return BitConverter.ToSingle(value, start);
        }

        public static byte[] GetBytes(this double value)
        {
            return BitConverter.GetBytes(value);
        }
        public static Double GetDouble(this byte[] value, int start = 0)
        {
            return BitConverter.ToDouble(value, start);
        }
    
    }
}
