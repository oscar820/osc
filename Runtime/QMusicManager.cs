using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
namespace QTool
{
    public class QMusicManager : QManagerBase<QMusicManager>
    {
        
        static AudioSource previewAudio;
        protected override void Awake()
        {
            base.Awake();
            previewAudio = gameObject.AddComponent<AudioSource>();
        }
        const float intervel = 0.1f;
        public static void ParseMusic(AudioClip clip)
        {
            if (PlayerPrefs.HasKey(clip.name))
            {
                AllData = FileManager.XmlDeserialize<float[][]>(PlayerPrefs.GetString(clip.name));
                if (AllData != null)
                {
                    Debug.LogError("∂¡»°°æ" + clip.name + "°ø");
                    return;
                }
                PlayerPrefs.DeleteKey(clip.name);
                ParseMusic(clip);
            }
            else
            {
                Instance.StartCoroutine(CorParseMusic(clip));
            }
        }
        static float curTime;
        static IEnumerator CorParseMusic(AudioClip clip)
        {
            previewAudio.clip = clip;
            previewAudio.Play();
            AllData = new float[(int)(clip.length / intervel)][];
            while (previewAudio.isPlaying)
            {
                AllData[(int)(previewAudio.time/intervel)] = GetData();
                curTime = previewAudio.time;
                yield return null;
            }
            PlayerPrefs.SetString(clip.name, FileManager.XmlSerialize(AllData));
            Debug.LogError("±£¥Ê °æ" + clip.name + "°ø");
        }
        static float[][] AllData;
        public static float[] GetParseData(float time)
        {
            return AllData.Get((int)(time/ intervel));
        }
        static float[] tempData = new float[2048];
        static float[] GetData(int samples = 2048)
        {

            previewAudio.GetSpectrumData(tempData, 0, FFTWindow.Rectangular);
            var datas = new float[2048];
            var max = 0f;
            for (int i = 0; i < tempData.Length; i++)
            {
                if (tempData[i] > tempData.Get(i - 1) && tempData[i] > tempData.Get(i + 1))
                {
                    datas[i] = tempData[i];
                    max = Mathf.Max(datas[i], max);
                }
                else
                {
                    datas[i] = 0;
                }
            }
            for (int i = 0; i < datas.Length; i++)
            {
                if (datas[i] < max * 0.4f)
                {
                    datas[i] = 0;
                }
            }
            return datas;
        }
    }
}