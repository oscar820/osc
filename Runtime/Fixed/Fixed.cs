using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.QFixed
{
    [System.Serializable]
    public struct Fixed
    {
        public const int FixScale = 1000;
        public readonly static Fixed Zero = new Fixed(0);
        public readonly static Fixed One = new Fixed(1);
        public readonly static Fixed Half = new Fixed(0.5f);
        public readonly static Fixed MaxValue =Get(long.MaxValue-1);
        public readonly static Fixed MinValue =Get(long.MinValue+2);
        public readonly static Fixed PositiveInfinity = Get(long.MaxValue);
        public readonly static Fixed NegativeInfinity = Get(long.MinValue+1);
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
            return rawValue.GetHashCode();
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
            if (b == Zero)
            {
                new Exception("´íÎóFixed(" + a + ")²»ÄÜ³ý0");
            }
            return new Fixed(a.rawValue * FixScale / b.rawValue);
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
    }
    [System.Serializable]
    public struct Fixed2
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
                if (n == Fixed.Zero) return zero;
                return new Fixed2(x / n, y / n);
            }
        }
        public Fixed SqrMagnitude => (x * x) + (y * y);
        public Fixed Magnitude
        {
            get
            {
                if (x == Fixed.Zero && y == Fixed.Zero)
                {
                    return Fixed.Zero;
                }
                return Fixed.Sqrt(SqrMagnitude);
            }
        }
        public Fixed Dot(Fixed2 b)
        {
            return Dot(this, b);
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
            return base.GetHashCode();
        }
    }
    [System.Serializable]
    public struct Fixed3
    {
        public static readonly Fixed3 left = new Fixed3(-1, 0,0);
        public static readonly Fixed3 right = new Fixed3(1, 0,0);
        public static readonly Fixed3 up = new Fixed3(0, 1,0);
        public static readonly Fixed3 down = new Fixed3(0, -1,0);
        public static readonly Fixed3 zero = new Fixed3(0, 0,0 );
        public static readonly Fixed3 forward = new Fixed3(0,0, 1);
        public static readonly Fixed3 back = new Fixed3(0, 0,- 1);
        public Fixed x;
        public Fixed y;
        public Fixed z;
    

       
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
                if (n == Fixed.Zero) return zero;
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
                if (x == Fixed.Zero && y == Fixed.Zero && z == Fixed.Zero)
                {
                    return Fixed.Zero;
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
        public static Fixed3 operator *(Fixed3 a, Fixed b)
        {
            return new Fixed3(a.x * b, a.y * b, a.z * b);
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
        public static Fixed3 Transform(Fixed3 position, FixedMatrix3x3 matrix)
        {
            Fixed3 result;
            Fixed3.Transform(ref position, ref matrix, out result);
            return result;
        }

        public static void Transform(ref Fixed3 position, ref FixedMatrix3x3 matrix, out Fixed3 result)
        {
            Fixed num0 = ((position.x * matrix.M11) + (position.y * matrix.M21)) + (position.z * matrix.M31);
            Fixed num1 = ((position.x * matrix.M12) + (position.y * matrix.M22)) + (position.z * matrix.M32);
            Fixed num2 = ((position.x * matrix.M13) + (position.y * matrix.M23)) + (position.z * matrix.M33);
            result.x = num0;
            result.y = num1;
            result.z = num2;
        }
        public override bool Equals(object obj)
        {
            return obj is Fixed3 && (Fixed3)obj == this;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override string ToString()
        {
            return "{" + x.ToString() + "," + y.ToString() + "," + z.ToString() + "}";
        }
    }


    [Serializable]
    public struct Fixed4
    {
        public Fixed x;
        public Fixed y;
        public Fixed z;
        public Fixed w;

        public static readonly Fixed4 Zero = new Fixed4(0);
        public static readonly Fixed4 One = new Fixed4(1);
        public static readonly Fixed4 MinValue = new Fixed4(Fixed.MinValue);
        public static readonly Fixed4 MaxValue = new Fixed4(Fixed.MaxValue);

        public static Fixed4 Abs(Fixed4 other)
        {
            return new Fixed4(Fixed.Abs(other.x), Fixed.Abs(other.y), Fixed.Abs(other.z), Fixed.Abs(other.z));
        }

     
        public Fixed SqrMagnitude
        {
            get
            {
                return x * x + y * y + z * z + w * w;
            }
        }
        public Fixed Magnitude
        {
            get
            {
                return Fixed.Sqrt(SqrMagnitude);
            }
        }

        public static Fixed4 ClampMagnitude(Fixed4 vector, Fixed maxLength)
        {
            return vector.Normalized * maxLength;
        }

        public Fixed4 Normalized
        {
            get
            {
                Fixed4 result = new Fixed4(this.x, this.y, this.z, this.w);
                result.Normalize();
                return result;
            }
        }
        public Fixed4(Fixed x, Fixed y, Fixed z, Fixed w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public void Scale(Fixed4 other)
        {
            this.x = x * other.x;
            this.y = y * other.y;
            this.z = z * other.z;
            this.w = w * other.w;
        }

        public void Set(Fixed x, Fixed y, Fixed z, Fixed w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
        public Fixed4(Fixed value)
        {
            this.x = value;
            this.y = value;
            this.z = value;
            this.w = value;
        }

        public static Fixed4 Lerp(Fixed4 from, Fixed4 to, Fixed percent)
        {
            return from + (to - from) * percent;
        }

        public override string ToString()
        {
            return string.Format("({0:f1}, {1:f1}, {2:f1}, {3:f1})", x, y, z, w);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Fixed4)) return false;
            Fixed4 other = (Fixed4)obj;

            return (((x == other.x) && (y == other.y)) && (z == other.z) && (w == other.w));
        }

        public static Fixed4 Scale(Fixed4 vecA, Fixed4 vecB)
        {
            Fixed4 result;
            result.x = vecA.x * vecB.x;
            result.y = vecA.y * vecB.y;
            result.z = vecA.z * vecB.z;
            result.w = vecA.w * vecB.w;

            return result;
        }

        public static bool operator ==(Fixed4 value1, Fixed4 value2)
        {
            return (((value1.x == value2.x) && (value1.y == value2.y)) && (value1.z == value2.z) && (value1.w == value2.w));
        }
        public static bool operator !=(Fixed4 value1, Fixed4 value2)
        {
            if ((value1.x == value2.x) && (value1.y == value2.y) && (value1.z == value2.z))
            {
                return (value1.w != value2.w);
            }
            return true;
        }


        public static Fixed4 Min(Fixed4 value1, Fixed4 value2)
        {
            Fixed4 result;
            Fixed4.Min(ref value1, ref value2, out result);
            return result;
        }

        public static void Min(ref Fixed4 value1, ref Fixed4 value2, out Fixed4 result)
        {
            result.x = (value1.x < value2.x) ? value1.x : value2.x;
            result.y = (value1.y < value2.y) ? value1.y : value2.y;
            result.z = (value1.z < value2.z) ? value1.z : value2.z;
            result.w = (value1.w < value2.w) ? value1.w : value2.w;
        }

      
        public static Fixed4 Max(Fixed4 value1, Fixed4 value2)
        {
            Fixed4 result;
            Fixed4.Max(ref value1, ref value2, out result);
            return result;
        }

        public static Fixed Distance(Fixed4 v1, Fixed4 v2)
        {
            return Fixed.Sqrt((v1.x - v2.x) * (v1.x - v2.x) + (v1.y - v2.y) * (v1.y - v2.y) + (v1.z - v2.z) * (v1.z - v2.z) + (v1.w - v2.w) * (v1.w - v2.w));
        }

      
        public static void Max(ref Fixed4 value1, ref Fixed4 value2, out Fixed4 result)
        {
            result.x = (value1.x > value2.x) ? value1.x : value2.x;
            result.y = (value1.y > value2.y) ? value1.y : value2.y;
            result.z = (value1.z > value2.z) ? value1.z : value2.z;
            result.w = (value1.w > value2.w) ? value1.w : value2.w;
        }
      
        public void MakeZero()
        {
            x = Fixed.Zero;
            y = Fixed.Zero;
            z = Fixed.Zero;
            w = Fixed.Zero;
        }
        public bool IsZero()
        {
            return (this.SqrMagnitude == Fixed.Zero);
        }


    
        public static Fixed4 Transform(Fixed4 position, FixedMatrix4x4 matrix)
        {
            Fixed4 result;
            Fixed4.Transform(ref position, ref matrix, out result);
            return result;
        }

        public static Fixed4 Transform(Fixed3 position, FixedMatrix4x4 matrix)
        {
            Fixed4 result;
            Fixed4.Transform(ref position, ref matrix, out result);
            return result;
        }
        public static void Transform(ref Fixed3 vector, ref FixedMatrix4x4 matrix, out Fixed4 result)
        {
            result.x = vector.x * matrix.M11 + vector.y * matrix.M12 + vector.z * matrix.M13 + matrix.M14;
            result.y = vector.x * matrix.M21 + vector.y * matrix.M22 + vector.z * matrix.M23 + matrix.M24;
            result.z = vector.x * matrix.M31 + vector.y * matrix.M32 + vector.z * matrix.M33 + matrix.M34;
            result.w = vector.x * matrix.M41 + vector.y * matrix.M42 + vector.z * matrix.M43 + matrix.M44;
        }

        public static void Transform(ref Fixed4 vector, ref FixedMatrix4x4 matrix, out Fixed4 result)
        {
            result.x = vector.x * matrix.M11 + vector.y * matrix.M12 + vector.z * matrix.M13 + vector.w * matrix.M14;
            result.y = vector.x * matrix.M21 + vector.y * matrix.M22 + vector.z * matrix.M23 + vector.w * matrix.M24;
            result.z = vector.x * matrix.M31 + vector.y * matrix.M32 + vector.z * matrix.M33 + vector.w * matrix.M34;
            result.w = vector.x * matrix.M41 + vector.y * matrix.M42 + vector.z * matrix.M43 + vector.w * matrix.M44;
        }
     
        public static Fixed Dot(Fixed4 vector1, Fixed4 vector2)
        {
            return Fixed4.Dot(ref vector1, ref vector2);
        }


        public static Fixed Dot(ref Fixed4 vector1, ref Fixed4 vector2)
        {
            return ((vector1.x * vector2.x) + (vector1.y * vector2.y)) + (vector1.z * vector2.z) + (vector1.w * vector2.w);
        }
    
        public static Fixed4 Add(Fixed4 value1, Fixed4 value2)
        {
            Fixed4 result;
            Fixed4.Add(ref value1, ref value2, out result);
            return result;
        }

     
        public static void Add(ref Fixed4 value1, ref Fixed4 value2, out Fixed4 result)
        {
            result.x = value1.x + value2.x;
            result.y = value1.y + value2.y;
            result.z = value1.z + value2.z;
            result.w = value1.w + value2.w;
        }
    
        public static Fixed4 Divide(Fixed4 value1, Fixed scaleFactor)
        {
            Fixed4 result;
            Fixed4.Divide(ref value1, scaleFactor, out result);
            return result;
        }

       
        public static void Divide(ref Fixed4 value1, Fixed scaleFactor, out Fixed4 result)
        {
            result.x = value1.x / scaleFactor;
            result.y = value1.y / scaleFactor;
            result.z = value1.z / scaleFactor;
            result.w = value1.w / scaleFactor;
        }

    
        public static Fixed4 Subtract(Fixed4 value1, Fixed4 value2)
        {
            Fixed4 result;
            Fixed4.Subtract(ref value1, ref value2, out result);
            return result;
        }

        public static void Subtract(ref Fixed4 value1, ref Fixed4 value2, out Fixed4 result)
        {
            result.x = value1.x - value2.x;
            result.y = value1.y - value2.y;
            result.z = value1.z - value2.z;
            result.w = value1.w - value2.w;
        }
      
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
       
        public void Negate()
        {
            this.x = -this.x;
            this.y = -this.y;
            this.z = -this.z;
            this.w = -this.w;
        }

 
        public static Fixed4 Negate(Fixed4 value)
        {
            Fixed4 result;
            Fixed4.Negate(ref value, out result);
            return result;
        }

    
        public static void Negate(ref Fixed4 value, out Fixed4 result)
        {
            result.x = -value.x;
            result.y = -value.y;
            result.z = -value.z;
            result.w = -value.w;
        }
      
        public static Fixed4 Normalize(Fixed4 value)
        {
            Fixed4 result;
            Fixed4.Normalize(ref value, out result);
            return result;
        }

    
        public void Normalize()
        {
            Fixed num2 = ((this.x * this.x) + (this.y * this.y)) + (this.z * this.z) + (this.w * this.w);
            Fixed num = Fixed.One / Fixed.Sqrt(num2);
            this.x *= num;
            this.y *= num;
            this.z *= num;
            this.w *= num;
        }

       
        public static void Normalize(ref Fixed4 value, out Fixed4 result)
        {
            Fixed num2 = ((value.x * value.x) + (value.y * value.y)) + (value.z * value.z) + (value.w * value.w);
            Fixed num = Fixed.One / Fixed.Sqrt(num2);
            result.x = value.x * num;
            result.y = value.y * num;
            result.z = value.z * num;
            result.w = value.w * num;
        }
   
        public static void Swap(ref Fixed4 vector1, ref Fixed4 vector2)
        {
            Fixed temp;

            temp = vector1.x;
            vector1.x = vector2.x;
            vector2.x = temp;

            temp = vector1.y;
            vector1.y = vector2.y;
            vector2.y = temp;

            temp = vector1.z;
            vector1.z = vector2.z;
            vector2.z = temp;

            temp = vector1.w;
            vector1.w = vector2.w;
            vector2.w = temp;
        }
      
        public static Fixed4 Multiply(Fixed4 value1, Fixed scaleFactor)
        {
            Fixed4 result;
            Fixed4.Multiply(ref value1, scaleFactor, out result);
            return result;
        }

        public static void Multiply(ref Fixed4 value1, Fixed scaleFactor, out Fixed4 result)
        {
            result.x = value1.x * scaleFactor;
            result.y = value1.y * scaleFactor;
            result.z = value1.z * scaleFactor;
            result.w = value1.w * scaleFactor;
        }
      
        public static Fixed operator *(Fixed4 value1, Fixed4 value2)
        {
            return Fixed4.Dot(ref value1, ref value2);
        }
     
        public static Fixed4 operator *(Fixed4 value1, Fixed value2)
        {
            Fixed4 result;
            Fixed4.Multiply(ref value1, value2, out result);
            return result;
        }
        public static Fixed4 operator *(Fixed value1, Fixed4 value2)
        {
            Fixed4 result;
            Fixed4.Multiply(ref value2, value1, out result);
            return result;
        }
        
        public static Fixed4 operator -(Fixed4 value1, Fixed4 value2)
        {
            Fixed4 result; Fixed4.Subtract(ref value1, ref value2, out result);
            return result;
        }
   
        public static Fixed4 operator +(Fixed4 value1, Fixed4 value2)
        {
            Fixed4 result; Fixed4.Add(ref value1, ref value2, out result);
            return result;
        }
    
        public static Fixed4 operator /(Fixed4 value1, Fixed value2)
        {
            Fixed4 result;
            Fixed4.Divide(ref value1, value2, out result);
            return result;
        }

        public Fixed2 ToFixed2()
        {
            return new Fixed2(this.x, this.y);
        }

        public Fixed3 ToFixed3()
        {
            return new Fixed3(this.x, this.y, this.z);
        }
    }
}