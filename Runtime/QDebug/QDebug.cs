using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public static class QDebug
	{
		[System.Diagnostics.Conditional("QDebug")]
		public static void Log(object obj)
		{
			Debug.Log(obj);
		}
	}
}
