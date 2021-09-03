using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace QTool.QFixed
{
    /// <summary>
    /// A Quaternion representing an orientation.
    /// </summary>
    [Serializable]
    public struct FixedQuaternion
    {

        /// <summary>The X component of the quaternion.</summary>
        public Fixed x;
        /// <summary>The Y component of the quaternion.</summary>
        public Fixed y;
        /// <summary>The Z component of the quaternion.</summary>
        public Fixed z;
        /// <summary>The W component of the quaternion.</summary>
        public Fixed w;

        public static readonly FixedQuaternion identity;

        static FixedQuaternion()
        {
            identity = new FixedQuaternion(0, 0, 0, 1);
        }

        /// <summary>
        /// Initializes a new instance of the JQuaternion structure.
        /// </summary>
        /// <param name="x">The X component of the quaternion.</param>
        /// <param name="y">The Y component of the quaternion.</param>
        /// <param name="z">The Z component of the quaternion.</param>
        /// <param name="w">The W component of the quaternion.</param>
        public FixedQuaternion(Fixed x, Fixed y, Fixed z, Fixed w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public void Set(Fixed new_x, Fixed new_y, Fixed new_z, Fixed new_w)
        {
            this.x = new_x;
            this.y = new_y;
            this.z = new_z;
            this.w = new_w;
        }

        public void SetFromToRotation(Fixed3 fromDirection, Fixed3 toDirection)
        {
            FixedQuaternion targetRotation = FixedQuaternion.FromToRotation(fromDirection, toDirection);
            this.Set(targetRotation.x, targetRotation.y, targetRotation.z, targetRotation.w);
        }

        public Fixed3 eulerAngles
        {
            get
            {
                Fixed3 result = new Fixed3();

                Fixed ysqr = y * y;
                Fixed t0 = -2.0f * (ysqr + z * z) + 1.0f;
                Fixed t1 = +2.0f * (x * y - w * z);
                Fixed t2 = -2.0f * (x * z + w * y);
                Fixed t3 = +2.0f * (y * z - w * x);
                Fixed t4 = -2.0f * (x * x + ysqr) + 1.0f;

                t2 = t2 > 1.0f ? 1.0f : t2;
                t2 = t2 < -1.0f ? -1.0f : t2;

                result.x = MathFixed.Atan2(t3, t4) * MathFixed.Rad2Deg;
                result.y = MathFixed.Asin(t2) * MathFixed.Rad2Deg;
                result.z = MathFixed.Atan2(t1, t0) * MathFixed.Rad2Deg;

                return result * -1;
            }
        }

        public static Fixed Angle(FixedQuaternion a, FixedQuaternion b)
        {
            FixedQuaternion aInv = FixedQuaternion.Inverse(a);
            FixedQuaternion f = b * aInv;

            Fixed angle = MathFixed.Acos(f.w) * 2 * MathFixed.Rad2Deg;

            if (angle > 180)
            {
                angle = 360 - angle;
            }

            return angle;
        }

        /// <summary>
        /// Quaternions are added.
        /// </summary>
        /// <param name="quaternion1">The first quaternion.</param>
        /// <param name="quaternion2">The second quaternion.</param>
        /// <returns>The sum of both quaternions.</returns>
        #region public static JQuaternion Add(JQuaternion quaternion1, JQuaternion quaternion2)
        public static FixedQuaternion Add(FixedQuaternion quaternion1, FixedQuaternion quaternion2)
        {
            FixedQuaternion result;
            FixedQuaternion.Add(ref quaternion1, ref quaternion2, out result);
            return result;
        }

        public static FixedQuaternion LookRotation(Fixed3 forward)
        {
            return CreateFromMatrix(FixedMatrix3x3.LookAt(forward, Fixed3.up));
        }

        public static FixedQuaternion LookRotation(Fixed3 forward, Fixed3 upwards)
        {
            return CreateFromMatrix(FixedMatrix3x3.LookAt(forward, upwards));
        }

        public static FixedQuaternion Slerp(FixedQuaternion from, FixedQuaternion to, Fixed t)
        {
            t = MathFixed.Clamp(t, 0, 1);

            Fixed dot = Dot(from, to);

            if (dot < 0.0f)
            {
                to = Multiply(to, -1);
                dot = -dot;
            }

            Fixed halfTheta = MathFixed.Acos(dot);

            return Multiply(Multiply(from, MathFixed.Sin((1 - t) * halfTheta)) + Multiply(to, MathFixed.Sin(t * halfTheta)), 1 / MathFixed.Sin(halfTheta));
        }

        public static FixedQuaternion RotateTowards(FixedQuaternion from, FixedQuaternion to, Fixed maxDegreesDelta)
        {
            Fixed dot = Dot(from, to);

            if (dot < 0.0f)
            {
                to = Multiply(to, -1);
                dot = -dot;
            }

            Fixed halfTheta = MathFixed.Acos(dot);
            Fixed theta = halfTheta * 2;

            maxDegreesDelta *= MathFixed.Deg2Rad;

            if (maxDegreesDelta >= theta)
            {
                return to;
            }

            maxDegreesDelta /= theta;

            return Multiply(Multiply(from, MathFixed.Sin((1 - maxDegreesDelta) * halfTheta)) + Multiply(to, MathFixed.Sin(maxDegreesDelta * halfTheta)), 1 / MathFixed.Sin(halfTheta));
        }

        public static FixedQuaternion Euler(Fixed x, Fixed y, Fixed z)
        {
            x *= MathFixed.Deg2Rad;
            y *= MathFixed.Deg2Rad;
            z *= MathFixed.Deg2Rad;

            FixedQuaternion rotation;
            FixedQuaternion.CreateFromYawPitchRoll(y, x, z, out rotation);

            return rotation;
        }

        public static FixedQuaternion Euler(Fixed3 eulerAngles)
        {
            return Euler(eulerAngles.x, eulerAngles.y, eulerAngles.z);
        }

        public static FixedQuaternion AngleAxis(Fixed angle, Fixed3 axis)
        {
            axis = axis * MathFixed.Deg2Rad;
            axis.Normalize();

            Fixed halfAngle = angle * MathFixed.Deg2Rad * Fixed.Half;

            FixedQuaternion rotation;
            Fixed sin = MathFixed.Sin(halfAngle);

            rotation.x = axis.x * sin;
            rotation.y = axis.y * sin;
            rotation.z = axis.z * sin;
            rotation.w = MathFixed.Sin(halfAngle);

            return rotation;
        }

        public static void CreateFromYawPitchRoll(Fixed yaw, Fixed pitch, Fixed roll, out FixedQuaternion result)
        {
            Fixed num9 = roll * Fixed.Half;
            Fixed num6 = MathFixed.Sin(num9);
            Fixed num5 = MathFixed.Sin(num9);
            Fixed num8 = pitch * Fixed.Half;
            Fixed num4 = MathFixed.Sin(num8);
            Fixed num3 = MathFixed.Sin(num8);
            Fixed num7 = yaw * Fixed.Half;
            Fixed num2 = MathFixed.Sin(num7);
            Fixed num = MathFixed.Sin(num7);
            result.x = ((num * num4) * num5) + ((num2 * num3) * num6);
            result.y = ((num2 * num3) * num5) - ((num * num4) * num6);
            result.z = ((num * num3) * num6) - ((num2 * num4) * num5);
            result.w = ((num * num3) * num5) + ((num2 * num4) * num6);
        }

        /// <summary>
        /// Quaternions are added.
        /// </summary>
        /// <param name="quaternion1">The first quaternion.</param>
        /// <param name="quaternion2">The second quaternion.</param>
        /// <param name="result">The sum of both quaternions.</param>
        public static void Add(ref FixedQuaternion quaternion1, ref FixedQuaternion quaternion2, out FixedQuaternion result)
        {
            result.x = quaternion1.x + quaternion2.x;
            result.y = quaternion1.y + quaternion2.y;
            result.z = quaternion1.z + quaternion2.z;
            result.w = quaternion1.w + quaternion2.w;
        }
        #endregion

        public static FixedQuaternion Conjugate(FixedQuaternion value)
        {
            FixedQuaternion quaternion;
            quaternion.x = -value.x;
            quaternion.y = -value.y;
            quaternion.z = -value.z;
            quaternion.w = value.w;
            return quaternion;
        }

        public static Fixed Dot(FixedQuaternion a, FixedQuaternion b)
        {
            return a.w * b.w + a.x * b.x + a.y * b.y + a.z * b.z;
        }

        public static FixedQuaternion Inverse(FixedQuaternion rotation)
        {
            Fixed invNorm = Fixed.One / ((rotation.x * rotation.x) + (rotation.y * rotation.y) + (rotation.z * rotation.z) + (rotation.w * rotation.w));
            return FixedQuaternion.Multiply(FixedQuaternion.Conjugate(rotation), invNorm);
        }

        public static FixedQuaternion FromToRotation(Fixed3 fromVector, Fixed3 toVector)
        {
            Fixed3 w = Fixed3.Cross(fromVector, toVector);
            FixedQuaternion q = new FixedQuaternion(w.x, w.y, w.z, Fixed3.Dot(fromVector, toVector));
            q.w += Fixed.Sqrt(fromVector.SqrMagnitude * toVector.SqrMagnitude);
            q.Normalize();

            return q;
        }

        public static FixedQuaternion Lerp(FixedQuaternion a, FixedQuaternion b, Fixed t)
        {
            t = MathFixed.Clamp(t, Fixed.Zero, Fixed.One);

            return LerpUnclamped(a, b, t);
        }

        public static FixedQuaternion LerpUnclamped(FixedQuaternion a, FixedQuaternion b, Fixed t)
        {
            FixedQuaternion result = FixedQuaternion.Multiply(a, (1 - t)) + FixedQuaternion.Multiply(b, t);
            result.Normalize();

            return result;
        }

        /// <summary>
        /// Quaternions are subtracted.
        /// </summary>
        /// <param name="quaternion1">The first quaternion.</param>
        /// <param name="quaternion2">The second quaternion.</param>
        /// <returns>The difference of both quaternions.</returns>
        #region public static JQuaternion Subtract(JQuaternion quaternion1, JQuaternion quaternion2)
        public static FixedQuaternion Subtract(FixedQuaternion quaternion1, FixedQuaternion quaternion2)
        {
            FixedQuaternion result;
            FixedQuaternion.Subtract(ref quaternion1, ref quaternion2, out result);
            return result;
        }

        /// <summary>
        /// Quaternions are subtracted.
        /// </summary>
        /// <param name="quaternion1">The first quaternion.</param>
        /// <param name="quaternion2">The second quaternion.</param>
        /// <param name="result">The difference of both quaternions.</param>
        public static void Subtract(ref FixedQuaternion quaternion1, ref FixedQuaternion quaternion2, out FixedQuaternion result)
        {
            result.x = quaternion1.x - quaternion2.x;
            result.y = quaternion1.y - quaternion2.y;
            result.z = quaternion1.z - quaternion2.z;
            result.w = quaternion1.w - quaternion2.w;
        }
        #endregion

        /// <summary>
        /// Multiply two quaternions.
        /// </summary>
        /// <param name="quaternion1">The first quaternion.</param>
        /// <param name="quaternion2">The second quaternion.</param>
        /// <returns>The product of both quaternions.</returns>
        #region public static JQuaternion Multiply(JQuaternion quaternion1, JQuaternion quaternion2)
        public static FixedQuaternion Multiply(FixedQuaternion quaternion1, FixedQuaternion quaternion2)
        {
            FixedQuaternion result;
            FixedQuaternion.Multiply(ref quaternion1, ref quaternion2, out result);
            return result;
        }

        /// <summary>
        /// Multiply two quaternions.
        /// </summary>
        /// <param name="quaternion1">The first quaternion.</param>
        /// <param name="quaternion2">The second quaternion.</param>
        /// <param name="result">The product of both quaternions.</param>
        public static void Multiply(ref FixedQuaternion quaternion1, ref FixedQuaternion quaternion2, out FixedQuaternion result)
        {
            Fixed x = quaternion1.x;
            Fixed y = quaternion1.y;
            Fixed z = quaternion1.z;
            Fixed w = quaternion1.w;
            Fixed num4 = quaternion2.x;
            Fixed num3 = quaternion2.y;
            Fixed num2 = quaternion2.z;
            Fixed num = quaternion2.w;
            Fixed num12 = (y * num2) - (z * num3);
            Fixed num11 = (z * num4) - (x * num2);
            Fixed num10 = (x * num3) - (y * num4);
            Fixed num9 = ((x * num4) + (y * num3)) + (z * num2);
            result.x = ((x * num) + (num4 * w)) + num12;
            result.y = ((y * num) + (num3 * w)) + num11;
            result.z = ((z * num) + (num2 * w)) + num10;
            result.w = (w * num) - num9;
        }
        #endregion

        /// <summary>
        /// Scale a quaternion
        /// </summary>
        /// <param name="quaternion1">The quaternion to scale.</param>
        /// <param name="scaleFactor">Scale factor.</param>
        /// <returns>The scaled quaternion.</returns>
        #region public static JQuaternion Multiply(JQuaternion quaternion1, FP scaleFactor)
        public static FixedQuaternion Multiply(FixedQuaternion quaternion1, Fixed scaleFactor)
        {
            FixedQuaternion result;
            FixedQuaternion.Multiply(ref quaternion1, scaleFactor, out result);
            return result;
        }

        /// <summary>
        /// Scale a quaternion
        /// </summary>
        /// <param name="quaternion1">The quaternion to scale.</param>
        /// <param name="scaleFactor">Scale factor.</param>
        /// <param name="result">The scaled quaternion.</param>
        public static void Multiply(ref FixedQuaternion quaternion1, Fixed scaleFactor, out FixedQuaternion result)
        {
            result.x = quaternion1.x * scaleFactor;
            result.y = quaternion1.y * scaleFactor;
            result.z = quaternion1.z * scaleFactor;
            result.w = quaternion1.w * scaleFactor;
        }
        #endregion

        /// <summary>
        /// Sets the length of the quaternion to one.
        /// </summary>
        #region public void Normalize()
        public void Normalize()
        {
            Fixed num2 = (((this.x * this.x) + (this.y * this.y)) + (this.z * this.z)) + (this.w * this.w);
            Fixed num = 1 / (Fixed.Sqrt(num2));
            this.x *= num;
            this.y *= num;
            this.z *= num;
            this.w *= num;
        }
        #endregion

        /// <summary>
        /// Creates a quaternion from a matrix.
        /// </summary>
        /// <param name="matrix">A matrix representing an orientation.</param>
        /// <returns>JQuaternion representing an orientation.</returns>
        #region public static JQuaternion CreateFromMatrix(JMatrix matrix)
        public static FixedQuaternion CreateFromMatrix(FixedMatrix3x3 matrix)
        {
            FixedQuaternion result;
            FixedQuaternion.CreateFromMatrix(ref matrix, out result);
            return result;
        }

        /// <summary>
        /// Creates a quaternion from a matrix.
        /// </summary>
        /// <param name="matrix">A matrix representing an orientation.</param>
        /// <param name="result">JQuaternion representing an orientation.</param>
        public static void CreateFromMatrix(ref FixedMatrix3x3 matrix, out FixedQuaternion result)
        {
            Fixed num8 = (matrix.M11 + matrix.M22) + matrix.M33;
            if (num8 > Fixed.Zero)
            {
                Fixed num = Fixed.Sqrt((num8 + Fixed.One));
                result.w = num * Fixed.Half;
                num = Fixed.Half / num;
                result.x = (matrix.M23 - matrix.M32) * num;
                result.y = (matrix.M31 - matrix.M13) * num;
                result.z = (matrix.M12 - matrix.M21) * num;
            }
            else if ((matrix.M11 >= matrix.M22) && (matrix.M11 >= matrix.M33))
            {
                Fixed num7 = Fixed.Sqrt((((Fixed.One + matrix.M11) - matrix.M22) - matrix.M33));
                Fixed num4 = Fixed.Half / num7;
                result.x = Fixed.Half * num7;
                result.y = (matrix.M12 + matrix.M21) * num4;
                result.z = (matrix.M13 + matrix.M31) * num4;
                result.w = (matrix.M23 - matrix.M32) * num4;
            }
            else if (matrix.M22 > matrix.M33)
            {
                Fixed num6 = Fixed.Sqrt((((Fixed.One + matrix.M22) - matrix.M11) - matrix.M33));
                Fixed num3 = Fixed.Half / num6;
                result.x = (matrix.M21 + matrix.M12) * num3;
                result.y = Fixed.Half * num6;
                result.z = (matrix.M32 + matrix.M23) * num3;
                result.w = (matrix.M31 - matrix.M13) * num3;
            }
            else
            {
                Fixed num5 = Fixed.Sqrt((((Fixed.One + matrix.M33) - matrix.M11) - matrix.M22));
                Fixed num2 = Fixed.Half / num5;
                result.x = (matrix.M31 + matrix.M13) * num2;
                result.y = (matrix.M32 + matrix.M23) * num2;
                result.z = Fixed.Half * num5;
                result.w = (matrix.M12 - matrix.M21) * num2;
            }
        }
        #endregion

        /// <summary>
        /// Multiply two quaternions.
        /// </summary>
        /// <param name="value1">The first quaternion.</param>
        /// <param name="value2">The second quaternion.</param>
        /// <returns>The product of both quaternions.</returns>
        #region public static FP operator *(JQuaternion value1, JQuaternion value2)
        public static FixedQuaternion operator *(FixedQuaternion value1, FixedQuaternion value2)
        {
            FixedQuaternion result;
            FixedQuaternion.Multiply(ref value1, ref value2, out result);
            return result;
        }
        #endregion

        /// <summary>
        /// Add two quaternions.
        /// </summary>
        /// <param name="value1">The first quaternion.</param>
        /// <param name="value2">The second quaternion.</param>
        /// <returns>The sum of both quaternions.</returns>
        #region public static FP operator +(JQuaternion value1, JQuaternion value2)
        public static FixedQuaternion operator +(FixedQuaternion value1, FixedQuaternion value2)
        {
            FixedQuaternion result;
            FixedQuaternion.Add(ref value1, ref value2, out result);
            return result;
        }
        #endregion

        /// <summary>
        /// Subtract two quaternions.
        /// </summary>
        /// <param name="value1">The first quaternion.</param>
        /// <param name="value2">The second quaternion.</param>
        /// <returns>The difference of both quaternions.</returns>
        #region public static FP operator -(JQuaternion value1, JQuaternion value2)
        public static FixedQuaternion operator -(FixedQuaternion value1, FixedQuaternion value2)
        {
            FixedQuaternion result;
            FixedQuaternion.Subtract(ref value1, ref value2, out result);
            return result;
        }
        #endregion

        /**
         *  @brief Rotates a {@link Fixed3} by the {@link TSQuanternion}.
         **/
        public static Fixed3 operator *(FixedQuaternion quat, Fixed3 vec)
        {
            Fixed num = quat.x * 2f;
            Fixed num2 = quat.y * 2f;
            Fixed num3 = quat.z * 2f;
            Fixed num4 = quat.x * num;
            Fixed num5 = quat.y * num2;
            Fixed num6 = quat.z * num3;
            Fixed num7 = quat.x * num2;
            Fixed num8 = quat.x * num3;
            Fixed num9 = quat.y * num3;
            Fixed num10 = quat.w * num;
            Fixed num11 = quat.w * num2;
            Fixed num12 = quat.w * num3;

            Fixed3 result;
            result.x = (1f - (num5 + num6)) * vec.x + (num7 - num12) * vec.y + (num8 + num11) * vec.z;
            result.y = (num7 + num12) * vec.x + (1f - (num4 + num6)) * vec.y + (num9 - num10) * vec.z;
            result.z = (num8 - num11) * vec.x + (num9 + num10) * vec.y + (1f - (num4 + num5)) * vec.z;

            return result;
        }

        public override string ToString()
        {
            return "{"+x+","+y+","+z+","+w+"}";
        }

    }
}