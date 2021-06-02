using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using QTool.Data;
[System.Serializable]
public class Data1 : QData<Data1>
{
    public string value = "123"; 
}
public class DataTest : MonoBehaviour
{
    public Data1 data;
    // Start is called before the first frame update
    void Start()
    {
       
    }
    [ContextMenu("�����ĵ�")]
    public  void Create()
    {
        Data1.SaveDefaultStaticTable(data);
    }
    [ContextMenu("����")]
   public async void Load()
    { 
        DataAsset.Clear(); 
        Data1.Clear();

        await Data1.LoadAsync();
        data = Data1.Get("t1");
    }
}
