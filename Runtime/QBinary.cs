using QTool.Binary;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
namespace QTool.Binary
{

  
    public class QBinaryReader:BinaryReader
    {
      //  public static Func<QBinaryReader, Type,object, object> customReadType;
        public T ReadObject<T>(T obj = default)
        {
            return (T)ReadObjectType(typeof(T), obj);
        }
        public object ReadObjectType(Type type ,object obj = default)
        {
            //if (customReadType == null|| !checkCustom )
            //{
                return this.DeserializeType(type, obj);
            //}
            //else
            //{
            //    return customReadType(this, type, obj);
            //}
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
       // public static Action<QBinaryWriter,object, Type> customWriteType;
        public QBinaryWriter() : base(new MemoryStream())
        {
        }
        public void WriteObject<T>(T obj)
        {
            WriteObjectType(obj, typeof(T));
        }

        public void WriteObjectType(object obj,Type type)
        {
            //if ( customWriteType == null||!checkCustom)
            //{
            this.SerializeType(obj, type);
            //}
            //else
            //{
            //    customWriteType(this,obj, type);
            //}
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
