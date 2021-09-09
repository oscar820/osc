using System;
using UnityEngine;
using QTool.Binary;

namespace QTool.QFixed
{
    [System.Serializable]
    public struct Fixed:IQSerialize
    {
        public const long FixScale = 100000000;
        public readonly static Fixed zero = new Fixed(0);
        public readonly static Fixed one = new Fixed(1);
        public readonly static Fixed half = new Fixed(0.5f);
        public readonly static Fixed MaxValue =Get(long.MaxValue);
        public readonly static Fixed MinValue =Get(long.MinValue);
        public readonly static Fixed PositiveInfinity = Get(long.MaxValue);
        public readonly static Fixed NaN = Get(long.MinValue);
        public float ToFloat()
        {
            return rawValue * 1f / FixScale;
        }
        public long RawValue => rawValue;
        [SerializeField]
        private long rawValue;
        public int ToInt()
        {
            return (int)(rawValue / FixScale);
        }
        public Fixed(int x = 0)
        {
            rawValue = x * FixScale;
        }
        public Fixed(float x)
        {
            rawValue = (long)Math.Round(x * FixScale);
        }
        public Fixed(double x)
        {
            rawValue = (long)Math.Round(x * FixScale);
        }
        public static Fixed Get(long value)
        {
            return new Fixed(value);
        }
        private Fixed(long value)
        {
            this.rawValue = value;
        }
        public override bool Equals(object obj)
        {
            return obj == null&&rawValue == ((Fixed)obj).rawValue;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public static Fixed Max(Fixed a, Fixed b)
        {
            return new Fixed(Math.Max(a.rawValue, b.rawValue));
        }
        public static Fixed Min(Fixed a, Fixed b)
        {
            return new Fixed(Math.Min(a.rawValue, b.rawValue));
        }
        public override string ToString()
        {
            return ToFloat().ToString();
        }
        public static implicit operator Fixed(double value)
        {
            return new Fixed(value);
        }
        public static implicit operator Fixed(float value)
        {
            return new Fixed(value);
        }
        public static implicit operator Fixed(int value)
        {
            return new Fixed(value);
        }
        public static Fixed operator +(Fixed a, Fixed b)
        {
            return new Fixed(a.rawValue + b.rawValue);
        }
        public static Fixed operator -(Fixed a)
        {
            return new Fixed(-a.rawValue);
        }
        public static Fixed operator -(Fixed a, Fixed b)
        {
            return new Fixed(a.rawValue - b.rawValue);
        }
        public static Fixed operator *(Fixed a, Fixed b)
        {
            return new Fixed(a.rawValue * b.rawValue / FixScale);
        }
        public static Fixed operator /(Fixed a, Fixed b)
        {
            if (b==Fixed.zero)
            {
                throw new Exception("´íÎóFixed(" + a + ")²»ÄÜ³ý0");
            }
            return new Fixed((a.rawValue * FixScale )/ b.rawValue);
        }
        public static bool operator >(Fixed p1, Fixed p2)
        {
            return (p1.rawValue > p2.rawValue) ? true : false;
        }
        public static bool operator <(Fixed p1, Fixed p2)
        {
            return (p1.rawValue < p2.rawValue) ? true : false;
        }
        public static bool operator <=(Fixed p1, Fixed p2)
        {
            return (p1.rawValue <= p2.rawValue) ? true : false;
        }
        public static bool operator >=(Fixed p1, Fixed p2)
        {
            return (p1.rawValue >= p2.rawValue) ? true : false;
        }
        public static bool operator ==(Fixed a, Fixed b)
        {
            return a.rawValue == b.rawValue;
        }
        public static bool operator !=(Fixed a, Fixed b)
        {
            return a.rawValue != b.rawValue;
        }
        public static Fixed Abs(Fixed x)
        {
            return new Fixed(Math.Abs(x.rawValue));
        }
        public static Fixed Sqrt(Fixed x)
        {
            return new Fixed((long)Math.Sqrt(x.rawValue * Fixed.FixScale));
        }

        public void Write(QBinaryWriter write)
        {
            write.Write(rawValue);
        }

        public void Read(QBinaryReader read)
        {
            rawValue = read.ReadInt64();
        }
    }
    public class FixedWaitTime
    {
        public Fixed Time { get; protected set; }
        public Fixed CurTime { get; protected set; }

        public void Clear()
        {
            CurTime = 0;
        }
        public void Over()
        {
            CurTime = Time;
        }
        public void Reset(Fixed time, bool startOver = false)
        {
            this.Time = time;
            CurTime = 0;
            if (startOver) Over();
        }
        public FixedWaitTime(Fixed time, bool startOver = false)
        {
            Reset(time, startOver);
        }

        public bool Check(Fixed deltaTime, bool autoClear = true)
        {
            CurTime += deltaTime;
            var subTime = CurTime - Time;
            if (subTime >= 0)
            {
                if (autoClear) { CurTime = subTime; }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    [System.Serializable]
    public struct Fixed2:IQSerialize
    {
        public readonly static Fixed2 one = new Fixed2(1, 1);
        public readonly static Fixed2 left = new Fixed2(-1, 0);
        public readonly static Fixed2 right = new Fixed2(1, 0);
        public readonly static Fixed2 up = new Fixed2(0, 1);
        public readonly static Fixed2 down = new Fixed2(0, -1);
        public readonly static Fixed2 zero = new Fixed2(0, 0);
        public Fixed x;
        public Fixed y;
        public UnityEngine.Vector2 ToVector2()
        {
            return new UnityEngine.Vector2(x.ToFloat(), y.ToFloat());
        }
        public Fixed2(Fixed x, Fixed y)
        {
            this.x = x;
            this.y = y;
        }
        public static Fixed2 operator +(Fixed2 a, Fixed2 b)
        {
            return new Fixed2(a.x + b.x, a.y + b.y);
        }
        public static Fixed2 operator -(Fixed2 a, Fixed2 b)
        {
            return new Fixed2(a.x - b.x, a.y - b.y);
        }
        public static Fixed2 operator *(Fixed2 a, Fixed b)
        {
            return new Fixed2(a.x * b, a.y * b);
        }

        public Fixed2 Normalized
        {

            get
            {
                Fixed n = Magnitude;
                if (n == Fixed.zero) return zero;
                return new Fixed2(x / n, y / n);
            }
        }
        public Fixed SqrMagnitude => (x * x) + (y * y);
        public Fixed Magnitude
        {
            get
            {
                if (x == Fixed.zero && y == Fixed.zero)
                {
                    return Fixed.zero;
                }
                return Fixed.Sqrt(SqrMagnitude);
            }
        }
        public Fixed Dot(Fixed2 b)
        {
            return Dot(this, b);
        }
        public static explicit operator Fixed2(Vector2 value)
        {
            return new Fixed2(value.x, value.y);
        }
        public static implicit operator Vector2(Fixed2 value)
        {
            return new Vector2(value.x.ToFloat(), value.y.ToFloat());
        }
        public static Fixed Dot(Fixed2 a, Fixed2 b)
        {
            return a.x * b.x + b.y * a.y;
        }
        public static Fixed2 operator -(Fixed2 a)
        {
            return new Fixed2(-a.x, -a.y);
        }
        public static bool operator ==(Fixed2 a, Fixed2 b)
        {
            return a.x == b.x && a.y == b.y;
        }
        public static bool operator !=(Fixed2 a, Fixed2 b)
        {
            return a.x != b.x || a.y != b.y;
        }
        public override string ToString()
        {
            return "{" + x.ToString() + "," + y.ToString() + "}";
        }
        public override bool Equals(object obj)
        {
            return obj is Fixed2 && this == (Fixed2)obj;
        }
        public override int GetHashCode()
        {
            return x.GetHashCode() + y.GetHashCode();
        }

        public void Write(QBinaryWriter writer)
        {
            writer.WriteObject(x);
            writer.WriteObject(y);
        }

        public void Read(QBinaryReader reader)
        {
            x = reader.ReadObject(x);
            y = reader.ReadObject(x);
        }
    }
    [System.Serializable]
    public struct Fixed3:IQSerialize
    {
        public static readonly Fixed3 left = new Fixed3(-1, 0,0);
        public static readonly Fixed3 right = new Fixed3(1, 0,0);
        public static readonly Fixed3 up = new Fixed3(0, 1,0);
        public static readonly Fixed3 down = new Fixed3(0, -1,0);
        public static readonly Fixed3 zero = new Fixed3(0, 0,0 );
        public static readonly Fixed3 forward = new Fixed3(0,0, 1);
        public static readonly Fixed3 back = new Fixed3(0, 0,- 1);
        public static readonly Fixed3 one = new Fixed3(1, 1, 1);
        public Fixed x;
        public Fixed y;
        public Fixed z;


        public static explicit operator Vector3(Fixed3 value)
        {
            return new Vector3(value.x.ToFloat(), value.y.ToFloat(),value.z.ToFloat());
        }
        public Fixed3(Fixed x, Fixed y, Fixed z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public Fixed3 Normalized
        {
            get
            {
                Fixed n = Magnitude;
                if (n == Fixed.zero) return zero;
                return new Fixed3(x / n, y / n, z / n);
            }
        }
        public void Normalize()
        {
            var n = Normalized;
            x = Normalized.x;
            y = Normalized.y;
            z = Normalized.z;
        }
        public Fixed SqrMagnitude => ((x * x) + (y * y) + (z * z));
        public Fixed Magnitude
        {
            get
            {
                if (x == Fixed.zero && y == Fixed.zero && z == Fixed.zero)
                {
                    return Fixed.zero;
                }
                return Fixed.Sqrt(SqrMagnitude);
            }
        }
        public static Fixed3 operator +(Fixed3 a, Fixed3 b)
        {
            return new Fixed3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Fixed3 operator -(Fixed3 a, Fixed3 b)
        {
            return new Fixed3(a.x - b.x, a.y - b.y, a.z - b.z);
        }
        public static implicit operator Fixed3(Vector3 value)
        {
            return new Fixed3(value.x, value.y, value.z);
        }
        public static Fixed3 operator *(Fixed3 a, Fixed b)
        {
            return new Fixed3(a.x * b, a.y * b, a.z * b);
        }
        public static Fixed3 operator *(Fixed a, Fixed3 b)
        {
            return b * a;
        }
        public static Fixed3 operator /(Fixed3 a, Fixed b)
        {
            return new Fixed3(a.x / b, a.y / b, a.z / b);
        }
        public static Fixed Dot(Fixed3 a, Fixed3 b)
        {
            return a.x * b.x + b.y * a.y + a.z * b.z;
        }
        public static Fixed3 Cross(Fixed3 a, Fixed3 b)
        {
            return new Fixed3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        }

        public static Fixed3 operator -(Fixed3 a)
        {
            return new Fixed3(-a.x, -a.y, -a.z);
        }
        public static bool operator ==(Fixed3 a, Fixed3 b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z;
        }
        public static bool operator !=(Fixed3 a, Fixed3 b)
        {
            return a.x != b.x || a.y != b.y || a.z != b.z;
        }
     
        public override bool Equals(object obj)
        {
            return obj is Fixed3 && (Fixed3)obj == this;
        }
        public override int GetHashCode()
        {
            return x.GetHashCode() + y.GetHashCode() + z.GetHashCode();
        }
        public override string ToString()
        {
            return "{" + x.ToString() + "," + y.ToString() + "," + z.ToString() + "}";
        }

        public void Write(QBinaryWriter writer)
        {
            writer.WriteObject(x);
            writer.WriteObject(y);
            writer.WriteObject(z);
        }

        public void Read(QBinaryReader reader)
        {
            x = reader.ReadObject(x);
            y = reader.ReadObject(x);
            z = reader.ReadObject(z);
        }
    }


   
}