using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.QFixed
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(FixedTransform))]
    public abstract class FixedBehaviour : MonoBehaviour
    {
        [HideInInspector]
        public FixedTransform fixedTransform;
        private void Reset()
        {
            fixedTransform = GetComponent<FixedTransform>();
        }
        private void OnValidate()
        {
            if (fixedTransform == null)
            {
                fixedTransform = GetComponent<FixedTransform>();
            }
        }
        protected virtual void Awake()
        {
            if (fixedTransform == null)
            {
                fixedTransform = GetComponent<FixedTransform>();
            }
        }
    }

}
