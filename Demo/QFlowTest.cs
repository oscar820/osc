using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool;
using QTool.Command;
using QTool.Flow;
public class QFlowTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    [ContextMenu("Test")]
    public void Test()
    {
        QCommand.FreshCommands(typeof(QStateTestFunc));
        var graph = new QFlowGraph();
        var a= graph.Add(nameof(QStateTestFunc.DebugValue));
        var wait = graph.Add(nameof(QStateTestFunc.Wait));
        a["value"].SetValue("QState≤‚ ‘");
        wait["time"].SetValue(3);
        a.Connect(wait);
        wait.Connect(a);
        StartCoroutine(graph.Run());
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
public static class QStateTestFunc
{
    public static IEnumerator Wait(float time)
    {
        yield return new WaitForSeconds(time);
    }
    public static void DebugValue(string value="test1")
    {
        Debug.LogError(value);
    }
}
