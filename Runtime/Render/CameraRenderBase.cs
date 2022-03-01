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
    }

}
