using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
namespace QTool
{
	public static class QDebug
	{
		[System.Diagnostics.Conditional("QDebug")]
		public static void Log(object obj)
		{
	
		}
		public static QDictionary<string, ProfilerMarker> ProfilerMarkerList = new QDictionary<string, ProfilerMarker>((key)=> new ProfilerMarker(key));
		[System.Diagnostics.Conditional("QDebug")]
		public static void StartProfiler(string key)
		{
			var profiler = ProfilerMarkerList[key];
			profiler.Begin();
		}
		[System.Diagnostics.Conditional("QDebug")]
		public static void StopProfiler(string key)
		{
			var profiler = ProfilerMarkerList[key];
			profiler.End();
		}
		[System.Diagnostics.Conditional("QDebug")]
		public static void ChangeProfilerCount(string key, int changeCount=0)
		{
#if Profiler
			var obj = ProfilerCount[key];
			obj.Value=changeCount;
#endif
		}
#if Profiler

		private static readonly ProfilerCategory filerCategory = ProfilerCategory.Scripts;
		private static QDictionary<string, ProfilerCounterValue<int>> ProfilerCount = new QDictionary<string, ProfilerCounterValue<int>>((key) => new ProfilerCounterValue<int>(filerCategory, key, ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame));
#endif
	}

	//class GameStats
	//{

	//	public static readonly ProfilerCounter<int> EnemyCount =
	//		new ProfilerCounter<int>(MyProfilerCategory, "Enemy Count", ProfilerMarkerDataUnit.Count);

	//	public static ProfilerCounterValue<int> BulletCount =
	//		new ProfilerCounterValue<int>(MyProfilerCategory, "Bullet Count",
	//			ProfilerMarkerDataUnit.Count, ProfilerCounterOptions.FlushOnEndOfFrame);
	//}

}
