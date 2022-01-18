using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Binary;

namespace QTool
{
    [RequireComponent(typeof( QId))]
    public class QTransformSave : MonoBehaviour,IQSerialize
    {
        public void Read(QBinaryReader reader)
        {
            transform.position = reader.ReadObject<Vector3>();
            transform.rotation = reader.ReadObject<Quaternion>();

        }

        public void Write(QBinaryWriter writer)
        {
            writer.WriteObject(transform.position);
            writer.WriteObject(transform.rotation);
        }
    }
}