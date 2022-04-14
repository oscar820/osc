using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool;
using QTool.Command;
using QTool.StateMachine;
public class QStateTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    [ContextMenu("Test")]
    public void Test()
    {
        QCommand.FreshCommands(typeof(QStateTestFunc));
        var qsm = new QStateMachine();
        var a= qsm.Add(nameof(QStateTestFunc.DebugValue));
        var wait = qsm.Add(nameof(QStateTestFunc.Wait));
        a["value"].SetValue("QState≤‚ ‘");
        wait["time"].SetValue(3);
        a.Connect(wait);
        wait.Connect(a);
        StartCoroutine(qsm.Run());
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
