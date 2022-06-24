using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Asset;
using UnityEngine.UI;
using QTool;
public class ResourceTest : QPrefabLoader<ResourceTest>
{

}
public class AddressableTest : MonoBehaviour
{
    public Text text;
    // Start is called before the first frame update
    [ContextMenu("加载Test1")]
    async void LoadTest1()
    {
        //   Debug.LogError( await ResourceTest.GetAsync("test1"));
         var obj=await ResourceTest.LoadAsync("Test1");
        text.text = "加载完成:" + obj;
    }
    [ContextMenu("加载全部")]
    async void LoadAll()
    {
     //   Debug.LogError( await ResourceTest.GetAsync("test1"));
        await ResourceTest.LoadAllAsync();
        //text.text = "加载完成:" + ResourceTest.objDic.Count + ResourceTest.objDic.ToOneString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
