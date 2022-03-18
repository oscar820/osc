using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
    public class QSprieTrigger : MonoBehaviour
    {
        public List<Sprite> spriteList = new List<Sprite>();
        public void Trigger(string name)
        {
            var sprite= spriteList.Get(name,(o) => o.name);
            if (sprite != null)
            {
                OnSpriteChange.Invoke(sprite);
            }
        }
        public SpriteEvent OnSpriteChange;
    }

}
