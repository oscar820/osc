using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
namespace QTool
{
    public class QCapture : QToolManagerBase<QCapture>
    {
        RenderTexture renderTexture;
        IEnumerator CaptureIEnumerator()
        {
            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
            }
            Camera.main.targetTexture = renderTexture;
            yield return new WaitForEndOfFrame();
            Camera.main.targetTexture = null;
            captureOver = true;

        }
        bool captureOver=false;
        public async Task<Texture> Capture()
        {
            captureOver = false;
            StartCoroutine(CaptureIEnumerator());
            await QTask.Wait(() => captureOver);
            return renderTexture;
        }
    }
}

