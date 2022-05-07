using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool;
using QTool.Command;
using QTool.Flow;
using QTool.Reflection;
public class QFlowTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    [ContextMenu("Test")]
    public void Test()
    {
        var c = QCommand.GetCommand(nameof(QStateTestFunc.OutTest));
        QCommand.FreshCommands(typeof(QStateTestFunc));
        var graph = new QFlowGraph();
        var a= graph.Add(nameof(QStateTestFunc.DebugValue));
        var wait = graph.Add(nameof(QStateTestFunc.Wait));
        a["value"].Value="QState≤‚ ‘";
        wait["time"].Value=3;
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
    [ViewName("Out≤‚ ‘")]
    public static void OutTest([ViewName(" ‰»ÎBool")] bool inBool, [ViewName(" ‰≥ˆBool")] out bool outBool, int inInt, out int outInt, float inFloat, out float outFloat)
    {
        outBool = inBool;
        outInt = inInt;
        outFloat = inFloat;
    }
}
