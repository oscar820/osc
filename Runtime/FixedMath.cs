using System.Collections;
using System.Collections.Generic;
using System;

namespace QTool
{
   
    [System.Serializable]
    public struct Fixed
    {
        public const int FixScale = 10000;
        public readonly static Fixed zero = new Fixed(0);
        public const long MaxValue = long.MaxValue / FixScale;
        float ToFloat()
        {
            return LongValue / FixScale;
        }
        public long LongValue { get; set; }
        public Fixed(int x)
        {
            LongValue = x * FixScale;
        }
        public Fixed(float x)
        {
            LongValue = (long)Math.Round(x * FixScale);
        }
        private Fixed(long value)
        {
            this.LongValue = value;
        }
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return LongValue == ((Fixed)obj).LongValue;
        }
        public override int GetHashCode()
        {
            return LongValue.GetHashCode();
        }
        public static Fixed Max(Fixed a,Fixed b)
        {
            return new Fixed(Math.Max(a.LongValue, b.LongValue));
        }
        public static Fixed Min(Fixed a, Fixed b)
        {
            return new Fixed(Math.Min(a.LongValue, b.LongValue));
        }
        public override string ToString()
        {
            return ToFloat().ToString();
        }
        public static explicit operator Fixed(float x)
        {
            return new Fixed(x);
        }
        public static explicit operator Fixed(int x)
        {
            return new Fixed(x);
        }
        public static explicit operator float(Fixed x)
        {
            return x.ToFloat();
        }
        public static Fixed operator +(Fixed a, Fixed b)
        {
            return new Fixed(a.LongValue + b.LongValue);
        }
        public static Fixed operator -(Fixed a)
        {
            return new Fixed(-a.LongValue);
        }
        public static Fixed operator -(Fixed a, Fixed b)
        {
            return new Fixed(a.LongValue - b.LongValue);
        }
        public static Fixed operator *(Fixed a, Fixed b)
        {
            return new Fixed(a.LongValue * b.LongValue / FixScale);
        }
        public static Fixed operator /(Fixed a, Fixed b)
        {
            if (b == zero)
            {
                new Exception("´íÎóFixed("+a+")²»ÄÜ³ý0");
            }
            return new Fixed(a.LongValue * FixScale / b.LongValue);
        }
       
        public static bool operator >(Fixed p1, Fixed p2)
        {
            return (p1.LongValue > p2.LongValue) ? true : false;
        }
        public static bool operator <(Fixed p1, Fixed p2)
        {
            return (p1.LongValue < p2.LongValue) ? true : false;
        }
        public static bool operator <=(Fixed p1, Fixed p2)
        {
            return (p1.LongValue <= p2.LongValue) ? true : false;
        }
        public static bool operator >=(Fixed p1, Fixed p2)
        {
            return (p1.LongValue >= p2.LongValue) ? true : false;
        }
        public static bool operator ==(Fixed a, Fixed b)
        {
            return a.LongValue == b.LongValue;
        }
        public static bool operator !=(Fixed a, Fixed b)
        {
            return a.LongValue != b.LongValue;
        }
        public static Fixed Abs( Fixed x)
        {
            return new Fixed(Math.Abs(x.LongValue));
        }
        public static Fixed Sqrt( Fixed x)
        {
            return new Fixed((long)Math.Sqrt(x.LongValue * Fixed.FixScale));
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
        public Fixed x { private set; get; }
        public Fixed y { private set; get; }

        public Fixed2(float x, float y)
        {
            this.x = new Fixed(x);
            this.y = new Fixed(y);
        }
        public Fixed2(Fixed x, Fixed y)
        {
            this.x = x;
            this.y = y;
        }
        public static bool DistanceLess(Fixed2 a, Fixed2 b, Fixed len)
        {
            var xLen = a.x - b.x;
            var yLen = a.y - b.y;
            return (xLen * xLen + yLen * yLen) < len * len;
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
                if (x == Fixed.zero && y == Fixed.zero)
                {
                    return new Fixed2();
                }
                Fixed n = Magnitude;
                if (n == Fixed.zero) return new Fixed2();
                return new Fixed2(x / n, y / n);
            }
        }
        public Fixed Magnitude
        {
            get
            {
                if (x == Fixed.zero && y ==Fixed.zero)
                {
                    return Fixed.zero;
                }
                return Fixed.Sqrt(((x * x) + (y * y)));
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
        //public static Fixed3 operator *(Fixed2 a, Fixed2 b)
        //{
        //    return new Fixed3(new Fixed(), new Fixed(), a.x * b.y - a.y * b.x);
        //}
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
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
    public struct Fixed3
    {
        public static readonly Fixed3 left = new Fixed3(-1, 0);
        public static readonly Fixed3 right = new Fixed3(1, 0);
        public static readonly Fixed3 up = new Fixed3(0, 1);
        public static readonly Fixed3 down = new Fixed3(0, -1);
        public static readonly Fixed3 zero = new Fixed3(0, 0);
        public Fixed x
        {
            get;
            private set;
        }
        public Fixed y
        {
            get;
            private set;
        }
        public Fixed z
        {
            get;
            private set;
        }

        public Fixed3(int x = 0, int y = 0, int z = 0)
        {
            this.x = new Fixed(x);
            this.y = new Fixed(y);
            this.z = new Fixed(z);

        }
        public Fixed3(float x, float y, float z)
        {
            this.x = new Fixed(x);
            this.y = new Fixed(y);
            this.z = new Fixed(z);
        }
        public Fixed3(Fixed x, Fixed y, Fixed z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        //public static V3 GetV3(Ratio x, Ratio y)
        //{
        //    return new V3(x, y);
        //}
        public static Fixed3 operator +(Fixed3 a, Fixed3 b)
        {
            return new Fixed3(a.x + b.x, a.y + b.y, a.z + b.z);
        }
        public static Fixed3 operator -(Fixed3 a, Fixed3 b)
        {
            return new Fixed3(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        
        public static Fixed Dot(Fixed3 a, Fixed3 b)
        {
            return a.x * b.x + b.y * a.y;
        }

        public static Fixed3 operator -(Fixed3 a)
        {
            return new Fixed3(-a.x, -a.y, -a.z);
        }
        public static Fixed2 operator *(Fixed3 a, Fixed2 b)
        {
            return new Fixed2(-a.z * b.y, a.z * b.x);
        }
        public override string ToString()
        {
            return "{" + x.ToString() + "," + y.ToString() +","+z.ToString()+ "}";
        }
    }
}
