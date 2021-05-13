using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
namespace QTool.TileMap
{
    public class QMergeTile : MonoBehaviour, IMergeTile
    {
        public GameObject View;
        [ReadOnly]
        public List<QMergeTile> objList = new List<QMergeTile>();

        public QMergeTile Master
        {
            get
            {
                if (objList.Count == 0)
                {
                    return null;
                }
                return objList[0];
            }
        }
        public bool isMaster
        {
            get
            {
                return this == Master;
            }
        }
        public void UnMerge()
        {
            if (isMaster)
            {
                foreach (var item in objList)
                {
                    item.View.gameObject.SetActive(true);
                    item.View.transform.localPosition = Vector3.zero;
                    item.View.transform.localScale = Vector3.one;
                }
                objList.Clear();
            }
            else
            {
                Master?.UnMerge();
            }

        }
        public void Merge(GameObject[] objects, Vector3 startPosition, Vector3 endPosition)
        {
            if (objects.Length > 1)
            {
                UnMerge();
                if (objects[0] == gameObject)
                {

                    for (int i = 0; i < objects.Length; i++)
                    {
                        objList.Add(objects[i].GetComponent<QMergeTile>());
                    }
                    var center = (startPosition + endPosition) / 2;
                    var boxSize = endPosition - startPosition;
                    var scale = new Vector3(Mathf.Abs(boxSize.x / transform.localScale.x) + 1, 1, Mathf.Abs(boxSize.z / transform.localScale.z) + 1);
                    var TileView = objList[0].View;
                    foreach (var tile in objList)
                    {
                        if (tile.View != TileView)
                        {
                            tile.View.SetActive(false);
                        }
                        tile.objList = objList;
                    }
                    TileView.transform.position = center;
                    TileView.transform.localScale = scale;
                }
            }
        }
    }
}