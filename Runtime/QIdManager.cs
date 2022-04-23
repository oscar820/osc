using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
    public class QIdManager : QManagerBase<QIdManager>
    {
        public List<QId> qIdInitList = new List<QId>();
        protected override void Awake()
        {
            base.Awake();
            qIdInitList.RemoveAll((obj) => obj == null);
            foreach (var id in qIdInitList)
            {
                QId.InstanceIdList[id.InstanceId] = id;
            }
        }
    }
}