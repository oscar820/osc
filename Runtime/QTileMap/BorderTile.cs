using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using QTool.Inspector;
using QTool;

namespace QTool.TileMap
{
    [System.Flags]
    public enum BorderType 
    {
        空地=1<<1,
        相同地板=1<<2,
        不同地板 =1<<3,
        任意地板= 相同地板| 不同地板,
    }
 
    public interface IMergeBorder
    {
        void CheckConnect();
    }
    public class BorderTile : MonoBehaviour
    {
        public BorderTileSetting setting;
        [ReadOnly]
        public List<GameObject> borderView=new List<GameObject>();
     
        public void MergeBoder()
        {
            foreach (var border in borderView)
            {

                var merges = border.GetComponentsInChildren<IMergeBorder>();

                foreach (var merge in merges)
                {
                    if (merge != null)
                    {
                        merge.CheckConnect();
                    }
                }

            }
        }
        public void ClearBorderCheck()
        {
            foreach (var border in borderView)
            {

                var merges = border.GetComponentsInChildren<IMergeBorder>();

                foreach (var merge in merges)
                {
                    if (merge != null)
                    {
                        (merge as MonoBehaviour).gameObject.SetActive(false);
                    }
                }

            }
        }
        
        List<int> borderList = new List<int>();
        List<IMergeBorder> mergeBoderList = new List<IMergeBorder>();
        public void CheckBorder(BorderSetting setting, BorderType[] borderInfo,bool mirror=false)
        {
           // Debug.LogError("CheckBorder");
            borderList.Clear();
            mergeBoderList.Clear();
            var index = setting.Compare(borderInfo);
            while (index >= 0&& !borderList.Contains(index%8))
            {

                borderList.Add(index%8);
                if (index % 2 == (mirror?1: 0))
                {
                    //    Debug.LogError("border " + index);
                    if (setting.borderPrefab != null)
                    {
                        //     Debug.LogError("border " + setting.borderPrefab.name);
              //          Debug.LogError("index " + index + " : " + setting.borderPrefab.name);
                        var view =this.CheckInstantiate(setting.borderPrefab, transform);
                        view.transform.rotation = Quaternion.Euler(0, (mirror?-1:1)* 90 * ((index- (mirror ? 3 : 0)) / 2), 0);
                        var mergeBorders= view.GetComponentsInChildren<IMergeBorder>();
                        if (mergeBorders != null)
                        {
                            mergeBoderList.AddRange(mergeBorders);
                        }
                        borderView.Add(view);
                    }
                }

                index = setting.Compare(borderInfo, index + 1);
            }
        }
        private void OnDestroy()
        {
        }
        public void UpdateBorder(BorderType[] borderInfo)
        {
            for (int i = transform.childCount-1;  i >0; i--)
            {
                var child= transform.GetChild(i).gameObject;
                this.CheckDestory(child);
            }
            borderView.Clear();
            BorderType[] reverseborderInfo = null;
            foreach (var setting in setting.broderSetting)
            {
                CheckBorder(setting, borderInfo);
                if (setting.mirror)
                {
                    if (reverseborderInfo == null)
                    {
                        reverseborderInfo= borderInfo.Reverse().ToArray();
                    }
                    CheckBorder(setting, reverseborderInfo,true);
                }
            }
        }
    }
}