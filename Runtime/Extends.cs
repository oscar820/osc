using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
# endif
namespace QTool
{
    public static partial class Tool
    {
        public static async void DelayInvoke(this float time,System.Action action)
        {
            await Task.Delay((int)(time * 1000));
            action?.Invoke();
        }
        public static Vector3 RayCastPlane(this Ray ray, Vector3 planeNormal, Vector3 planePoint)
        {
            float d = Vector3.Dot(planePoint - ray.origin, planeNormal) / Vector3.Dot(ray.direction, planeNormal);
            return d * ray.direction + ray.origin;
        }
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
                if (mesh is MeshRenderer ||mesh is SpriteRenderer ||mesh is SkinnedMeshRenderer)
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

#if UNITY_EDITOR
        static void StartUpdateEditorTime()
        {
            if (!updateEditorTime)
            {
                updateEditorTime = true;
                UnityEditor.EditorApplication.update += () =>
                {
                    editorDeltaTime = (float)(UnityEditor.EditorApplication.timeSinceStartup - lastTime);
                    lastTime = UnityEditor.EditorApplication.timeSinceStartup;
                };
            }
        }
       
        static bool updateEditorTime = false;
        static double lastTime;
#endif
        static float editorDeltaTime;
        public static float EditorDeltaTime
        {
            get
            {
#if UNITY_EDITOR
                StartUpdateEditorTime();
#endif
                return editorDeltaTime;
            }
        }

        public static void SetDirty(this Object obj)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(obj);
            }
# endif
        }
        public static void Record(this Object obj)
        {
#if UNITY_EDITOR
            Undo.RecordObject(obj, "RecordObj"+obj.GetHashCode());
#endif
        }

        public static T CheckInstantiate<T>(this Object recordObj,T prefab, Transform parent)where T: Object
        {

#if UNITY_EDITOR
            var obj = PrefabUtility.InstantiatePrefab(prefab, parent) as T;
#else
            var obj = GameObject.Instantiate(prefab, parent);
#endif
            return obj ;
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
                GameObject.DestroyImmediate(obj);
            }
#else
              GameObject.Destroy(obj);
#endif
        }
    }
    public static class RectTransformExtend
    {
        public static Vector2 UpRightRectOffset(this RectTransform rectTransform)
        {
            return new Vector2(rectTransform.Width() * (1 - rectTransform.pivot.x), rectTransform.Height() * (1 - rectTransform.pivot.y));
        }
        public static Vector2 DownLeftRectOffset(this RectTransform rectTransform)
        {
            return new Vector2(rectTransform.Width() * (rectTransform.pivot.x), rectTransform.Height() * (rectTransform.pivot.y));
        }

        public static float Height(this RectTransform rectTransform)
        {
            return rectTransform.rect.size.y;
        }
        public static float Width(this RectTransform rectTransform)
        {
            return rectTransform.rect.size.x;
        }
        public static Vector2 Size(this RectTransform rectTransform)
        {
            return rectTransform.rect.size;
        }

        public static RectTransform RectTransform(this Transform transform)
        {
            return transform as RectTransform;
        }
        public static Vector2 UpRight(this RectTransform rectTransform)
        {
            return new Vector2(rectTransform.position.x, rectTransform.position.y) + rectTransform.UpRightRectOffset();
        }
        public static Vector2 DownLeft(this RectTransform rectTransform)
        {
            return new Vector2(rectTransform.position.x, rectTransform.position.y) - rectTransform.DownLeftRectOffset();
        }

    }
}
