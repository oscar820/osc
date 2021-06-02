using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Asset;
using UnityEngine.UI;
using QTool;
public class ResourceTest : PrefabAssetList<ResourceTest>
{

}
public class AddressableTest : MonoBehaviour
{
    public Text text;
    // Start is called before the first frame update
    [ContextMenu("����Test1")]
    async void LoadTest1()
    {
        //   Debug.LogError( await ResourceTest.GetAsync("test1"));
         var obj=await ResourceTest.GetAsync("Test1");
        text.text = "�������:" + obj;
    }
    [ContextMenu("����ȫ��")]
    async void LoadAll()
    {
     //   Debug.LogError( await ResourceTest.GetAsync("test1"));
        await ResourceTest.LoadAllAsync();
        text.text = "�������:" + ResourceTest.objDic.Count + ResourceTest.objDic.ToOneString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
