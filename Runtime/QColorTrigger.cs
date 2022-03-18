using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
    [System.Serializable]
    public class QColorSetting
    {
        public string key;
        public Color color;
    }
    public class QColorTrigger : MonoBehaviour
    {
        public List<QColorSetting> colorList = new List<QColorSetting>();
        public void Trigger(string name)
        {
            var colorSetting= colorList.Get(name,(o) => o.key);
            if (colorSetting != null)
            {
                OnColorChange.Invoke(colorSetting.color);
            }
        }
        public ColorEvent OnColorChange;
    }

}
