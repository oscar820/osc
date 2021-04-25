using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool.Resource;
using UnityEngine.UI;
using QTool;
public class ResourceTest : PrefabResourceList<ResourceTest>
{

}
public class AddressableTest : MonoBehaviour
{
    public Text text;
    // Start is called before the first frame update
    void Start()
    {
        Debug.LogError( ResourceTest.Get("test1"));
        ResourceTest.LoadOverRun(() =>
        {
            text.text = "º”‘ÿÕÍ≥…:" + ResourceTest.objDic.Count + ResourceTest.objDic.ToOneString();
        });
        StartCoroutine(ResourceTest.AsyncLoadLabel());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
