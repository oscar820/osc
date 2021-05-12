using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
# endif
namespace QTool
{
    public static partial class Tool
    {
        public static Bounds GetBounds(this GameObject obj)
        {
            return obj.transform.GetBounds();
        }
        public static Bounds GetBounds(this Component com)
        {
            var bounds = new Bounds(com.transform.position, Vector3.zero);
            Renderer[] meshs = com.GetComponentsInChildren<Renderer>();
            foreach (var mesh in meshs)
            {
                if (mesh)
                {
                    if (bounds.extents == Vector3.zero)
                    {
                        bounds = mesh.bounds;
                    }
                    else
                    {
                        bounds.Encapsulate(mesh.bounds);
                    }
                }
            }
            return bounds;
        }
        public static void SetDirty(this Object obj)
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(obj);
# endif
        }
        public static T CheckInstantiate<T>(this Object recordObj,T prefab, Transform parent)where T: Object
        {

#if UNITY_EDITOR
            Undo.RecordObject(recordObj, "CheckInstantiate");
            var obj = PrefabUtility.InstantiatePrefab(prefab, parent) as T;
            Undo.RegisterCreatedObjectUndo(obj, "CheckInstantiate");
#else
            var obj = GameObject.Instantiate(prefab, parent);
#endif
            return obj;
        }
        public static GameObject GetPrefab(this GameObject obj)
        {
#if UNITY_EDITOR
            return PrefabUtility.GetCorrespondingObjectFromSource(obj);
#else
            return null;
#endif
        }
        public static void CheckDestory(this Object recordObj, Object obj)
        {

#if UNITY_EDITOR
            if (obj != null)
            {
               Undo.DestroyObjectImmediate(obj);
            }
            Undo.RecordObject(recordObj, "CheckDestory");
#else
              GameObject.Destroy(obj);
#endif
        }
    }
}
