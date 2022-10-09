using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Inspector;
using QTool;

public class QMusicTest : MonoBehaviour
{
    public AudioSource audio;
    public LineRenderer[] lines;
    public GameObject cubePrefab;
    ObjectPool<GameObject> CubePool;
    [Range(1, 20)]
    public int Scale = 4;

    // Start is called before the first frame update
    [QButton("test")]
    void Start()
    {
        CubePool = new ObjectPool<GameObject>("QMusicPool", () => Instantiate(cubePrefab)); ;
        QMusicManager.ParseMusic(audio.clip);
        audio.Play();
        lines[0].gameObject.InvokeEvent("显示名", "QMusic");
        for (int i = 0; i < 6; i++)
        {
            lines[i + 1].gameObject.InvokeEvent("显示名", ((FFTWindow)i).ToString());
        }

    }

  
    float[] data = new float[2048];
    //float[] lastData = new float[512];
    // Update is called once per frame'
 
    void Update() 
    {
        FreshLine(lines[0], QMusicManager.GetParseData(audio.time));

        for (int lineIndex = 0; lineIndex <6; lineIndex++)
        {
            audio.GetSpectrumData(data, 0, (FFTWindow)lineIndex);
            FreshLine(lines[lineIndex + 1], data);
        }
    }
    public void FreshLine( LineRenderer line,float[] datas)
    {
        if (datas == null) return;
        line.positionCount = datas.Length / Scale ;
        for (int i = 0; i < line.positionCount; i++)
        {
            line.SetPosition(i, line.transform.position + new Vector3(Mathf.Lerp(-2.5f,2.5f,i*1f / line.positionCount), datas[i]) * 10);
        }
    }
}
