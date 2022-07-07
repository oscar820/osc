using QTool.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool.FlowGraph
{
	[QCommandType("基础")]
	public static class QFlowGraphNode
	{
		[ViewName("数值/获取变量")]
		[return: QOutputPort(true)]
		public static object GetValue(QFlowNode This, string key)
		{
			return This.Graph.Values[key];
		}
		[ViewName("数值/设置变量")]
		public static void SetValue(QFlowNode This, string key, object value)
		{
			This.Graph.Values[key] = value;
		}
		[QStartNode]
		[ViewName("起点/Start")]
		public static void Start()
		{
		}
		[QStartNode]
		[ViewName("起点/Event")]
		public static void Event([QNodeKeyName] string eventKey = "事件名")
		{
		}
		[ViewName("时间/延迟")]
		public static IEnumerator Deley(float time)
		{
			yield return new WaitForSeconds(time);
		}

		[ViewName("运算/加")]
		[return: QOutputPort(true)]
		public static object Add(object a, object b)
		{
			return QReflection.OperaterAdd(a, b);
		}
		[ViewName("运算/减")]
		[return: QOutputPort(true)]
		public static object Subtract(object a, object b)
		{
			return QReflection.OperaterSubtract(a, b);
		}
		[ViewName("运算/乘")]
		[return: QOutputPort(true)]
		public static object Multiply(object a, object b)
		{
			return QReflection.OperaterMultiply(a, b);
		}
		[ViewName("运算/除")]
		[return: QOutputPort(true)]
		public static object Divide(object a, object b)
		{
			return QReflection.OperaterDivide(a, b);
		}
		[ViewName("运算/大于")]
		[return: QOutputPort(true)]
		public static bool GreaterThan(object a, object b)
		{
			return QReflection.OperaterGreaterThan(a, b);
		}
		[ViewName("运算/大于等于")]
		[return: QOutputPort(true)]
		public static bool GreaterThanOrEqual(object a, object b)
		{
			return QReflection.OperaterGreaterThanOrEqual(a, b);
		}
		[ViewName("运算/小于")]
		[return: QOutputPort(true)]
		public static bool LessThan(object a, object b)
		{
			return QReflection.OperaterLessThan(a, b);
		}
		[ViewName("运算/小于等于")]
		[return: QOutputPort(true)]
		public static bool LessThanOrEqual(object a, object b)
		{
			return QReflection.OperaterLessThanOrEqual(a, b);
		}
		[ViewName("运算/等于")]
		[return: QOutputPort(true)]
		public static bool Equal(object a, object b)
		{
			return QReflection.OperaterEqual(a, b);
		}
		[ViewName("逻辑运算/与")]
		[return: QOutputPort(true)]
		public static bool And(bool a, bool b)
		{
			return a && b;
		}
		[ViewName("逻辑运算/或")]
		[return: QOutputPort(true)]
		public static bool Or(bool a, bool b)
		{
			return a || b;
		}
		[ViewName("逻辑运算/非")]
		[return: QOutputPort(true)]
		public static bool Not(bool a)
		{
			return !a;
		}

		[ViewName("分支/判断分支")]
		public static void BoolCheck(QFlowNode This, bool boolValue, [QOutputPort] QFlow True, [QOutputPort] QFlow False)
		{
			if (boolValue)
			{
				This.SetNetFlowPort(nameof(True));
			}
			else
			{
				This.SetNetFlowPort(nameof(False));
			}
		}
		[ViewName("分支/异步分支")]
		public static void AsyncBranch(QFlowNode This, [ QOutputPort]List<QFlow> branchs)
		{
			for (int i = 0; i < branchs.Count; i++)
			{
				if (i == 0)
				{
					This.SetNetFlowPort(nameof(branchs), i);
				}
				else
				{
					This.RunPort(nameof(branchs), i);
				}

			}
			
		}
		[ViewName("分支/全部完成")]
		
		public static IEnumerator AllOver(QFlowNode This,[QInputPort(true)]List<QFlow> branchs)
		{
			List<int> taskList = new List<int> { };
			for (int i = 0; i < branchs.Count; i++)
			{
				taskList.Add(i);
			}
			Debug.Log("全部完成节点开始：[" + taskList.ToOneString("|")+"]");
			This.TriggerPortList.Clear();
			while (taskList.Count > 0)
			{
				foreach (var port in This.TriggerPortList)
				{
					if (port.port == nameof(branchs))
					{
						taskList.Remove(port.index);
						Debug.Log("完成["+port.index+"]剩余[" + taskList.ToOneString("|") + "]");
					}
				}
				This.TriggerPortList.Clear();
				yield return null;
			}
			Debug.Log("全部完成");
		}
	}
}
