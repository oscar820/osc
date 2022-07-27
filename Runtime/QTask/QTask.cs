using QTool.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
namespace QTool
{
	public static class QTask
	{
		public static Action StopAllWait;
		public static async Task<bool> Wait(float second, bool ignoreTimeScale = false)
		{
			var startTime = (ignoreTimeScale ? Time.unscaledTime : Time.time);
			return await Wait(() => startTime + second <= (ignoreTimeScale ? Time.unscaledTime : Time.time));
		}
		public static async Task<bool> Wait(Func<bool> flagFunc)
		{
			var WaitStop = false;
			if (flagFunc == null) return Application.isPlaying;
			Action OnWaitStop = () => { WaitStop = true; };
			StopAllWait += OnWaitStop;
			while (!flagFunc.Invoke())
			{
				await Task.Delay(100);
				if (!Application.isPlaying || WaitStop)
				{
					StopAllWait -= OnWaitStop;
					return false;
				}
			}
			StopAllWait -= OnWaitStop;
			return true;
		}

		public static async Task TaskRunCoroutine(this IEnumerator enumerator)
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current is WaitForSeconds waitForSeconds)
				{
					var m_Seconds = (float)waitForSeconds.GetValue("m_Seconds");
					if (Application.isPlaying)
					{
						if (!await QTask.Wait(m_Seconds))
						{
							return;
						}
					}
					else
					{
						await Task.Delay((int)(m_Seconds * 1000));
					}
				}
				else
				{
					Debug.LogError(enumerator.Current);
					typeof(WaitForSeconds).ForeachMemeber((file) =>
					{
						Debug.LogError(file.Name);
					}, (member) =>
					{
						Debug.LogError(member.Name);
					});
					await Task.Yield();
				}

			}

		}

	}


}
