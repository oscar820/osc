using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
    public class QIdInitManager : QManagerBase<QIdInitManager>
    {
        public List<QId> qIdInitList = new List<QId>();
        [ExecuteInEditMode]
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