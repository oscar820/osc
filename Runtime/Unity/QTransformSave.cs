using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Binary;
using QTool.QFixed;

namespace QTool
{
    [RequireComponent(typeof( QId))]
    public class QTransformSave : MonoBehaviour,IQSerialize
    {
        public void Read(QBinaryReader reader)
        {
            transform.position = reader.ReadObject<Fixed3>().ToVector3();

        }

        public void Write(QBinaryWriter writer)
        {
            writer.WriteObject(transform.position.ToFixed3());
        }
    }
}