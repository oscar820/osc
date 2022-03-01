using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace QTool
{
    public abstract class OnPostRenderBase : MonoBehaviour
    {
        private void OnEnable()
        {
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }
        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        }
        public void OnEndCameraRendering(ScriptableRenderContext context,Camera camera)
        {
            OnPostRender();
        }

        protected abstract void OnPostRender();
        public struct VertexInfo
        {
            public Vector3 position;
            public Vector3 uv;
            public float x => position.x;
            public float y => position.y;
            public float z => position.z;
        }
        /// <summary>
        /// Ë³Ê±ÕëÈýµã
        /// </summary>
        public static void DrawTriangle(VertexInfo a, VertexInfo b, VertexInfo c, Material mat, int pass = 0)
        {
            if (!mat)
            {
                Debug.LogError("Please Assign a material on the inspector");
                return;
            }
            GL.PushMatrix();
            mat.SetPass(pass);
            GL.LoadOrtho();
            GL.Begin(GL.TRIANGLES);
            GL.Vertex3(a.x, a.y, a.z);
            GL.TexCoord(a.uv);
            GL.Vertex3(b.x, b.y, b.z);
            GL.TexCoord(a.uv);
            GL.Vertex3(c.x, c.y, c.z);
            GL.TexCoord(a.uv);
            GL.End();
            GL.PopMatrix();
        }
    }

}
