using QTool.Serialize;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace QTool.Binary
{

  
    public class QBinaryReader:BinaryReader
    {
        public static Func<QBinaryReader, Type,object, object> customReadType;
        public T ReadObject<T>(T obj = default)
        {
            return QSerialize.Deserialize<T>(ReadBytes(), obj);
        }
        public object ReadObjectType(Type type ,object obj = default,bool checkCustom=true)
        {
            if (customReadType == null|| !checkCustom )
            {
                return QSerialize.DeserializeType(ReadBytes(), type, obj);
            }
            else
            {
                return customReadType(this, type, obj);
            }
        }
        public QBinaryReader(byte[] bytes):base(new MemoryStream(bytes))
        {
        }
        public MemoryStream memory => BaseStream as MemoryStream;
      
        public override byte[] ReadBytes(int count=-1)
        {
            if (count < 0)
            {
                count= base.ReadInt32();
            }
            return base.ReadBytes(count);
        }
        public byte[] ReadByteLengthBytes()
        {
            var count = base.ReadByte();
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
        public static Action<QBinaryWriter,object, Type> customWriteType;
        public QBinaryWriter() : base(new MemoryStream())
        {
        }
        public void WriteObject<T>(T obj)
        {
            this.Write(QSerialize.Serialize( obj));
        }

        public void WriteObjectType(object obj,Type type,bool checkCustom=true)
        {
            if ( customWriteType == null||!checkCustom)
            {
                this.Write(QSerialize.SerializeType(obj, type));
            }
            else
            {
                customWriteType(this,obj, type);
            }
        }
        public byte[] ToArray()
        {
            return (BaseStream as MemoryStream).ToArray();
        }
        public void WriteByteLengthBytes(byte[] buffer, bool writeLength)
        {
            base.Write((byte)buffer.Length);
            base.Write(buffer);
        }
        public  void Write(byte[] buffer,bool writeLength)
        {
            if (writeLength)
            {
                base.Write(buffer.Length);
            }
            base.Write(buffer);
        }
        public override void Write(byte[] buffer)
        {
            if (buffer != null)
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


        public static byte[] GetBytes(this Vector3 value)
        {
            var bytes = new byte[4 * 3];
            Array.Copy(value.x.GetBytes(), 0, bytes, 0, 4);
            Array.Copy(value.y.GetBytes(), 0, bytes, 4*1, 4);
            Array.Copy(value.z.GetBytes(), 0, bytes, 4*2, 4);
            return bytes;
        }
        public static Vector3 GetVector3(this byte[] value, int start = 0)
        {
            return new Vector3(value.GetSingle(start+0), value.GetSingle(start+4 * 1), value.GetSingle(start+4 * 2));
        }

        public static byte[] GetBytes(this Quaternion value)
        {
            var bytes = new byte[4 * 4];
            Array.Copy(value.x.GetBytes(), 0, bytes, 0, 4);
            Array.Copy(value.y.GetBytes(), 0, bytes, 4*1, 4);
            Array.Copy(value.z.GetBytes(), 0, bytes, 4*2, 4);
            Array.Copy(value.w.GetBytes(), 0, bytes, 4*3, 4);
            return bytes;
        }
        public static Quaternion GetQuaternion(this byte[] value, int start = 0)
        {
            return new Quaternion(value.GetSingle(start+0), value.GetSingle(start+4 * 1), value.GetSingle(start+4 * 2), value.GetSingle(start+4 * 3));
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
