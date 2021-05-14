using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool;
namespace QTool.TileMap
{
    //[ExecuteInEditMode]
    public interface IMergeBorderView
    {
        void Connect(MergeBorder a, MergeBorder b);
    }
    public class MergeBorder : MonoBehaviour,IMergeBorder
    {
        public MergeBorder connect;
        public LayerMask checkLayer;
        public GameObject viewPrefab;
        public GameObject view;
        //private void OnDrawGizmos()
        //{
        //    if (connect == null)
        //    {
        //        Gizmos.color = Color.red;
        //        Gizmos.DrawSphere(transform.position, 0.1f);
        //        Gizmos.DrawRay(transform.position - transform.forward * 0.1f, transform.forward);
        //    }
        //    else
        //    {
        //        Gizmos.color = Color.green;
        //        Gizmos.DrawSphere(transform.position, 0.1f);

        //        Gizmos.DrawRay(transform.position - transform.forward * 0.1f, connect.transform.position - transform.position);

        //    }

        //}
        //private void Update()
        //{
        //    CheckConnect();


        //}
        [ContextMenu("CheckConnect")]
        public void CheckConnect()
        {
            var hits = Physics.RaycastAll(transform.position - transform.forward * 0.3f, transform.forward, 3, checkLayer);

            List<RaycastHit> hitList = new List<RaycastHit>();

            if (hits != null && hits.Length > 0)
            {
                hitList.AddRange(hits);
                hitList.Sort((a, b) =>
                {
                    return (int)(a.distance - b.distance);
                });
                foreach (var hit in hitList)
                {
                    if (hit.collider.gameObject == gameObject)
                    {
                        continue;
                    }
                    if (Mathf.Abs(Vector3.Angle(hit.collider.transform.forward, transform.forward) - 180) > 1f)
                    {
                        //   Debug.LogError(Vector3.Angle(hit.collider.transform.forward, transform.forward) - 180);
                        continue;
                    }

                    var other = hit.collider.GetComponent<MergeBorder>();
                    if (other != null)
                    {

                        Connect(other);
                        break;
                    }
                }
            }
        }
        public void Connect(MergeBorder other)
        {

            if (connect != null)
            {
                //  connect.connect = null;
                return;
            }
            if (other.connect != null)
            {
                //  other.connect.connect = null;
                return;
            }
            if (Vector3.Distance(other.transform.position, transform.position) > 0.01f)
            {



                connect = other;
                other.connect = this;
                if (view == null)
                {
                    view = this.CheckInstantiate(viewPrefab, transform);
                    view.transform.SetParent(transform.parent);
                    var Iview = view.GetComponent<IMergeBorderView>();
                    Iview.Connect(this, other);
                }
            }
        }
        private void OnDestroy()
        {
            if (connect != null)
            {

            }
        }
    }
}