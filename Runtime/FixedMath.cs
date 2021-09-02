using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
namespace QTool.QFixed
{
    /// <summary>
    /// 定点数数学类
    /// </summary>
    public static class FixedMath
    {
    
        public static Fixed ToFixed(this int x)
        {
            return new Fixed(x);
        }
        public static Fixed ToFixed(this float x)
        {
            return new Fixed(x);
        }
        public static Fixed2 ToFixed2(this UnityEngine.Vector2 v2)
        {
            return new Fixed2(v2.x,v2.y);
        }
        public static Fixed3 ToFixed3(this UnityEngine.Vector3 v3)
        {
            return new Fixed3(v3.x,v3.y,v3.z);
        }
        private static int tabCount = 18 * 4;
        /// <summary>
        /// sin值对应表
        /// </summary>
        private static readonly List<Fixed> sinTab = new List<Fixed>();
        public static readonly Fixed PI = new Fixed(3.14159265f);
        static FixedMath()
        {
            sinTab.Add(new Fixed(0f));//0
            sinTab.Add(new Fixed(0.08715f));
            sinTab.Add(new Fixed(0.17364f));
            sinTab.Add(new Fixed(0.25881f));
            sinTab.Add(new Fixed(0.34202f));//20
            sinTab.Add(new Fixed(0.42261f));
            sinTab.Add(new Fixed(0.5f));

            sinTab.Add(new Fixed(0.57357f));//35
            sinTab.Add(new Fixed(0.64278f));
            sinTab.Add(new Fixed(0.70710f));
            sinTab.Add(new Fixed(0.76604f));
            sinTab.Add(new Fixed(0.81915f));//55
            sinTab.Add(new Fixed(0.86602f));//60

            sinTab.Add(new Fixed(0.90630f));
            sinTab.Add(new Fixed(0.93969f));
            sinTab.Add(new Fixed(0.96592f));
            sinTab.Add(new Fixed(0.98480f));//80
            sinTab.Add(new Fixed(0.99619f));

            sinTab.Add(new Fixed(1f));
        }
        public static Fixed Lerp(Fixed a, Fixed b, Fixed t)
        {
            return a + (b - a) * t;
        }
        private static Fixed GetSinTab(Fixed r)
        {

            Fixed i = new Fixed(r.ToInt());
            if (i.ToInt() == sinTab.Count - 1)
            {
                return sinTab[(int)i.ToInt()];
            }
            else
            {
                return Lerp(sinTab[i.ToInt()], sinTab[i.ToInt() + 1], r - i);
            }
        }
        private static Fixed GetAsinTab(Fixed sin)
        {
            for (int i = sinTab.Count - 1; i >= 0; i--)
            {
                if (sin > sinTab[i])
                {
                    if (i == sinTab.Count - 1)
                    {
                        return new Fixed(i) / (tabCount / 4) * (PI / 2);
                    }
                    else
                    {
                        return Lerp(new Fixed(i), new Fixed(i + 1), (sin - sinTab[i]) / (sinTab[i + 1] - sinTab[i])) / (tabCount / 4) * (PI / 2);
                    }
                }
            }
            return new Fixed();
        }
        public static Fixed PiToAngel(Fixed pi)
        {
            return pi / PI * 180;
        }
        public static Fixed Asin(Fixed sin)
        {
            if (sin < -1 || sin > 1) { return new Fixed(); }
            if (sin >= 0)
            {
                return GetAsinTab(sin);
            }
            else
            {
                return -GetAsinTab(-sin);
            }
        }
        public static Fixed Sin(Fixed r)
        {

            Fixed result = new Fixed();
            r = (r * tabCount / 2 / PI);
            while (r < Fixed.zero)
            {
                r += tabCount;
            }
            while (r > tabCount)
            {
                r -= tabCount;
            }
            if (r >= 0 && r <= tabCount / 4)                // 0 ~ PI/2
            {
                result = GetSinTab(r);
            }
            else if (r > tabCount / 4 && r < tabCount / 2)       // PI/2 ~ PI
            {
                r -= new Fixed(tabCount / 4);
                result = GetSinTab(new Fixed(tabCount / 4) - r);
            }
            else if (r >= tabCount / 2 && r < 3 * tabCount / 4)    // PI ~ 3/4*PI
            {
                r -= new Fixed(tabCount / 2);
                result = -GetSinTab(r);
            }
            else if (r >= 3 * tabCount / 4 && r < tabCount)      // 3/4*PI ~ 2*PI
            {
                r = new Fixed(tabCount) - r;
                result = -GetSinTab(r);
            }
            return result;
        }
        public static Fixed Abs(Fixed ratio)
        {
            return Fixed.Abs(ratio);
        }
        public static Fixed Sqrt(Fixed r)
        {
            return Fixed.Sqrt(r);
        }

        public static Fixed Cos(Fixed r)
        {
            return Sin(r + PI / 2);
        }
        public static Fixed SinAngle(Fixed angle)
        {
            return Sin(angle / 180 * PI);
        }
        public static Fixed CosAngle(Fixed angle)
        {
            return Cos(angle / 180 * PI);
        }
    }
    [System.Serializable]
    public struct Fixed
    {
        public const int FixScale = 10000;
        public readonly static Fixed zero = new Fixed(0);
        public readonly static Fixed MaxValue =Fixed.Get( long.MaxValue);
        public readonly static Fixed MinValue = Fixed.Get( long.MinValue);
        public float ToFloat()
        {
            return longValue*1f / FixScale;
        }
        public long longValue;
        public int ToInt()
        {
            return (int)(longValue / FixScale);
        }
        public Fixed(int x=0)
        {
            longValue = x * FixScale;
        }
        public Fixed(float x)
        {
            longValue = (long)Math.Round(x * FixScale);
        }
        public static Fixed Get(long value)
        {
            return new Fixed(value);
        }
        private Fixed(long value)
        {
            this.longValue = value;
        }
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return longValue == ((Fixed)obj).longValue;
        }
        public override int GetHashCode()
        {
            return longValue.GetHashCode();
        }
        public static Fixed Max(Fixed a,Fixed b)
        {
            return new Fixed(Math.Max(a.longValue, b.longValue));
        }
        public static Fixed Min(Fixed a, Fixed b)
        {
            return new Fixed(Math.Min(a.longValue, b.longValue));
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
            return new Fixed(a.longValue + b.longValue);
        }
        public static Fixed operator +(Fixed a, int b)
        {
            return a + new Fixed(b);
        }
        public static Fixed operator -(Fixed a)
        {
            return new Fixed(-a.longValue);
        }
        public static Fixed operator -(Fixed a, Fixed b)
        {
            return new Fixed(a.longValue - b.longValue);
        }
        public static Fixed operator -(Fixed a, int b)
        {
            return a + b.ToFixed();
        }
        public static Fixed operator *(Fixed a, Fixed b)
        {
            return new Fixed(a.longValue * b.longValue / FixScale);
        }
        public static Fixed operator *(Fixed a, int b)
        {
            return new Fixed(a.longValue * b);
        }
        //public static Fixed operator *(int a, Fixed b)
        //{
        //    return b * a;
        //}
        public static Fixed operator /(Fixed a, Fixed b)
        {
            if (b == zero)
            {
                new Exception("错误Fixed("+a+")不能除0");
            }
            return new Fixed(a.longValue * FixScale / b.longValue);
        }
        public static Fixed operator /(Fixed a, int b)
        {
            return new Fixed(a.longValue / b);
        }
        public static bool operator >(Fixed p1, Fixed p2)
        {
            return (p1.longValue > p2.longValue) ? true : false;
        }
        public static bool operator >(Fixed p1, int p2)
        {
            return p1 > p2.ToFixed();
        }
     
        public static bool operator <(Fixed p1, Fixed p2)
        {
            return (p1.longValue < p2.longValue) ? true : false;
        }
        public static bool operator <(Fixed p1, int p2)
        {
            return p1 < p2.ToFixed();
        }
        public static bool operator <=(Fixed p1, Fixed p2)
        {
            return (p1.longValue <= p2.longValue) ? true : false;
        }
        public static bool operator >=(Fixed p1, int p2)
        {
            return p1 >= p2.ToFixed();
        }
        public static bool operator >=(Fixed p1, Fixed p2)
        {
            return (p1.longValue >= p2.longValue) ? true : false;
        }
        public static bool operator <=(Fixed p1, int p2)
        {
            return p1 <= p2.ToFixed();
        }
        public static bool operator ==(Fixed a, Fixed b)
        {
            return a.longValue == b.longValue;
        }
        public static bool operator ==(Fixed p1, int p2)
        {
            return p1 == p2.ToFixed();
        }
        public static bool operator !=(Fixed a, Fixed b)
        {
            return a.longValue != b.longValue;
        }
        public static bool operator !=(Fixed p1, int p2)
        {
            return p1 != p2.ToFixed();
        }
        public static Fixed Abs( Fixed x)
        {
            return new Fixed(Math.Abs(x.longValue));
        }
        public static Fixed Sqrt( Fixed x)
        {
            return new Fixed((long)Math.Sqrt(x.longValue * Fixed.FixScale));
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
                if (x == Fixed.zero && y ==Fixed.zero)
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
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
    [System.Serializable]
    public struct Fixed3
    {
        public static readonly Fixed3 left = new Fixed3(-1, 0);
        public static readonly Fixed3 right = new Fixed3(1, 0);
        public static readonly Fixed3 up = new Fixed3(0, 1);
        public static readonly Fixed3 down = new Fixed3(0, -1);
        public static readonly Fixed3 zero = new Fixed3(0, 0);
        public Fixed x;
        public Fixed y;
        public Fixed z;
        public UnityEngine.Vector3 ToVector3()
        {
            return new UnityEngine.Vector3(x.ToFloat(), y.ToFloat(),z.ToFloat());
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
        public Fixed3 Normalized
        {
            get
            {
                Fixed n = Magnitude;
                if (n == Fixed.zero) return zero;
                return new Fixed3(x / n, y / n,z/n);
            }
        }
        public Fixed SqrMagnitude =>((x* x) + (y* y) + (z* z));
        public Fixed Magnitude
        {
            get
            {
                if (x == Fixed.zero && y == Fixed.zero&&z==Fixed.zero)
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
        public static Fixed3 operator *(Fixed3 a, Fixed b)
        {
            return new Fixed3(a.x *b, a.y *b, a.z *b);
        }
        public static Fixed3 operator /(Fixed3 a, Fixed b)
        {
            return new Fixed3(a.x / b, a.y / b, a.z / b);
        }
        public static Fixed Dot(Fixed3 a, Fixed3 b)
        {
            return a.x * b.x + b.y * a.y+a.z*b.z;
        }
        public static Fixed3 Cross(Fixed3 a, Fixed3 b)
        {
            return new Fixed3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        }

        public static Fixed3 operator -(Fixed3 a)
        {
            return new Fixed3(-a.x, -a.y, -a.z);
        }
        public override string ToString()
        {
            return "{" + x.ToString() + "," + y.ToString() +","+z.ToString()+ "}";
        }
    }
}
