using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QTool
{
    using Pos = Vector2Int;
    [System.Flags]
    public enum Dir
    {
        None = 0,
        Up = 1 << 1,
        Down = 1 << 2,
        Left = 1 << 3,
        Right = 1 << 4,
        LeftRight = Left | Right,
        UpDown = Up | Down,
        All = Up | Down | Left | Right,
    }
    
    public static class PosExtends
    {
        public static readonly Pos undefined = Pos.one * int.MaxValue;
        public static Pos Undefined(this Pos pos)
        {
            return undefined;
        }
        public static bool IsUndefined(this Pos pos)
        {
            return pos == undefined;
        }
        public static Vector2 ToVector2(this Pos pos)
        {
            return new Vector2(pos.x, pos.y);
        }
        public static Pos ToPos(this Dir dir)
        {
            switch (dir)
            {
                case Dir.Right:
                    return Pos.right;
                case Dir.Up:
                    return Pos.up;
                case Dir.Left:
                    return Pos.left;
                case Dir.Down:
                    return Pos.down;
                default:
                    return Pos.zero;
            }
        }
        public static Dir ToDir(this Pos pos)
        {
           var dir=  pos.ToVector2().normalized;
            return dir.ToDir();
        }
        public static Dir ToDir(this Vector2 pos)
        {
            var dir = pos.normalized;
            if (dir.x >= 0.5f)
            {
                return Dir.Right;
            }
            else if(dir.x<=-0.5f)
            {
                return Dir.Left;
            }
            else if (dir.y >= 0.5f)
            {
                return Dir.Up;
            }
            else if (dir.y <=- 0.5f)
            {
                return Dir.Down;
            }
            else
            {
                return Dir.None;
            }
        }
        /// <summary>
        /// 以右侧为正方向旋转位置信息
        /// </summary>
        /// <param name="pos">位置</param>
        /// <param name="dir">更改方向</param>
        /// <returns>新的位置</returns>
        public static Pos Rotate(Pos pos, Dir dir)
        {
            var newPos = pos;
            switch (dir)
            {
                case Dir.Right:
                    break;
                case Dir.Up:
                    newPos = new Pos(-pos.y, pos.x);
                    break;
                case Dir.Left:
                    newPos = new Pos(-pos.x, -pos.y);
                    break;
                case Dir.Down:
                    newPos = new Pos(pos.y, -pos.x);
                    break;
                default:
                    break;
            }
            return newPos;
        }
    }
}