using QTool.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
namespace QTool
{
	public class QTaskCancelException : Exception
	{
		public override string ToString()
		{
			return "QTask 异步逻辑被取消运行";
		}
	}
	public static class QTask
	{
		static Dictionary<string, Task> OnlyOneRun = new Dictionary<string, Task>();
		/// <summary>
		/// 保证只有一个相同Key的Task正在运行
		/// </summary>
		public static async Task RunOnlyOne(string onlyOneKey, Func<Task> taskFunc)
		{
			if (!OnlyOneRun.ContainsKey(onlyOneKey))
			{
				lock (OnlyOneRun)
				{
					OnlyOneRun.Add(onlyOneKey,taskFunc());
				}
				await OnlyOneRun[onlyOneKey];
				lock (OnlyOneRun)
				{
					OnlyOneRun.RemoveKey(onlyOneKey);
				}
			}
			else
			{
				await OnlyOneRun[onlyOneKey];
			}
		}
		public static async Task Run(this Task task,Func<Task>  nextAction)
		{
			await task;
			await nextAction();
		}
		public static async Task Run(this Task task, Action nextAction)
		{
			await task;
			nextAction();
		}
		public static int RunningFlag { get; private set; } = QId.GetNewId().GetHashCode();
		public static void StopAllWait()
		{
			RunningFlag = QId.GetNewId().GetHashCode();
		}
		public static async Task Wait(float second, bool ignoreTimeScale = false)
		{
			if (Application.isPlaying)
			{
				var startTime = (ignoreTimeScale ? Time.unscaledTime : Time.time);
				await Wait(() => startTime + second <= (ignoreTimeScale ? Time.unscaledTime : Time.time));
			}
			else
			{
				var startTime = DateTime.Now;
				await Wait(() =>(DateTime.Now-startTime).TotalSeconds>second );
			}
		}
		public static async Task Wait(Func<bool> flagFunc)
		{
			var flag = RunningFlag;
			var playingFlag = Application.isPlaying;
			if (flagFunc == null) return;
			while (!flagFunc.Invoke())
			{
				await Task.Delay(100);
				if (!playingFlag.Equals(Application.isPlaying) || !RunningFlag.Equals(flag))
				{
					throw new QTaskCancelException();
				}
			}
		}
		public static async Task<bool> IsCancel(this Task task)
		{
			Exception exception=null;
			try
			{
				await task;
			}
			catch (Exception e)
			{
				exception = e;
			}
			if (exception != null)
			{
				if(exception is QTaskCancelException)
				{
					return true;
				}
				else
				{
					Debug.LogError(exception);
				}
			}
			return false;
		}

		public static async Task TaskRunCoroutine(this IEnumerator enumerator)
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current is WaitForSeconds waitForSeconds)
				{
					var m_Seconds = (float)waitForSeconds.GetValue("m_Seconds");
					if (!await Wait(m_Seconds).IsCancel())
					{
						return;
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




		//public struct WaitTimeAwaiter : IAwaiter
		//{
		//	readonly float wait;
		//	Action action;
		//	public WaitTimeAwaiter(float wait)
		//	{
		//		this.wait = wait;
		//		action = null;
		//	}
		//	public async void Start()
		//	{
		//		await QTask.Wait(wait);
		//		IsCompleted = true;
		//		action?.Invoke();
		//		action = null;
		//	}

		//	public bool IsCompleted
		//	{
		//		get; set;
		//	}

		//	public void GetResult()
		//	{
		//	}


		//	public void OnCompleted(Action continuation)
		//	{
		//		Debug.LogError(continuation);
		//		action += continuation;
		//	}

		//}
		public static ResourceRequestAwaiter GetAwaiter(this ResourceRequest resourceRequest)
		{
			return new ResourceRequestAwaiter(resourceRequest);
		}
		public struct ResourceRequestAwaiter : IAwaiter<UnityEngine.Object>
		{
			ResourceRequest resourceRequest;
			public ResourceRequestAwaiter(ResourceRequest resourceRequest)
			{
				this.resourceRequest = resourceRequest;
			}
			public bool IsCompleted => resourceRequest.isDone;

			public UnityEngine.Object GetResult()
			{
				return resourceRequest?.asset;
			}

			public void OnCompleted(Action continuation)
			{
				resourceRequest.completed += (resourceRequest) =>
				{
					continuation?.Invoke();
				};
			}
		}
	}
	
	public interface IAwaiter : INotifyCompletion
	{

		public bool IsCompleted { get; }

		public void GetResult();
	}
	public interface IAwaiter<T>: INotifyCompletion
	{

		public bool IsCompleted { get; }

		public T GetResult();
	}
}
