using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
namespace QTool.TileMap
{
    
    public abstract class TileBase:MonoBehaviour
    {
        private QTileMap map;
        public QTileMap Map
        {
            get
            {
                return map ?? (map = GetComponentInParent<QTileMap>());
            }
        }
    }
    public class QMergeTile : MergeTile<QMergeTile>
    {
        public GameObject View;

        public Transform Master
        {
            get
            {
                if (posList.Count == 0)
                {
                    return null;
                }
                return posList[0].Value;
            }
        }
        public bool isMaster
        {
            get
            {
                return transform == Master;
            }
        }
        public override void UnMerge()
        {
            if (posList == null || posList.Count == 0) return;
            View.gameObject.SetActive(true);
            View.transform.localPosition = Vector3.zero;
            View.transform.localScale = Vector3.one;
            posList = null;

        }
        public override bool Merge(PosList objects)
        {
            var value = base.Merge(objects);
            if (value)
            {
                if (isMaster)
                {
                    View.transform.position = bounds.center;
                    var boxSize = bounds.size;
                    View.transform.localScale = new Vector3(Mathf.Abs(boxSize.x / Map.tilePrefabSize.x) + 1, 1, Mathf.Abs(boxSize.z / Map.tilePrefabSize.y) + 1);
                  
                    View.SetActive(true);
                }
                else
                {
                    View.SetActive(false);
                }
            }
            return value;
        }
    }
}