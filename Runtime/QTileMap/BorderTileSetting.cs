using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
namespace QTool.TileMap
{
    [CreateAssetMenu(fileName = "地板边界设置", menuName = "QTileMap地板边界设置")]
    [System.Serializable]
    public class BorderTileSetting : ScriptableObject
    {
        [Title("以正右方为正方向 顺时针检测 地板边缘 优先级从高到低")]
        public List<BorderSetting> broderSetting = new List<BorderSetting>();
    }
    [System.Serializable]
    public class BorderSetting
    {
        [ViewName("边界规则")]
        public BorderType[] borderType = new BorderType[8];
        [ViewName("边界预制体")]
        public GameObject borderPrefab;
        [ViewName("包含镜像逻辑")]
        public bool mirror = false;
        //public static bool TypeEqule(BorderType a, BorderType b)
        //{
        //    return (a & b) == b;
        //}
        public int Compare(BorderType[] borderInfo, int infoStart = 0)
        {

            int equalsCount = 0;
            var startIndex = -1;
            for (int i = infoStart; i < borderInfo.Length * 2 && equalsCount < borderType.Length; i++)
            {

                if (borderType[equalsCount].HasFlag( borderInfo[i % 8]))
                {
                    if (startIndex < 0)
                    {
                        startIndex = i;
                    }
                    equalsCount++;

                }
                else if (startIndex >= 0)
                {
                    i -= (equalsCount);
                    equalsCount = 0;
                    startIndex = -1;

                }

            }
            if (equalsCount == borderType.Length)
            {
                return startIndex;
            }
            else
            {
                return -1;
            }
        }

    }
}