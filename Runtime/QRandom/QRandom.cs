using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QTool
{
	public class QRandomTable
	{
		public int Max { get; set; }
		public int[] Table { get; private set; }
		public int Range { get; private set; }
		public QRandomTable(int seed = 0,int max=255, int size = 1024)
		{

			Table = new int[size];
			Max = max;
			Range = size - 1;
			System.Random random = new System.Random(seed);
			for (int i = 0; i < Table.Length; i++)
			{
				Table[i] = random.Next();
			}
		}
		public int this[int i]
		{
			get
			{
				return Table[i & Range]&Max;
			}
		}

		public int this[int i, int j]
		{
			get
			{
				return Table[(j + Table[i & Range]) & Range]&Max;
			}
		}

		public int this[int i, int j, int k]
		{
			get
			{
				return Table[(k + Table[(j + Table[i & Range]) & Range]) & Range]&Max;
			}
		}
	}
	public static class QRandomTool
	{
		public static T RandomGet<T>(this IList<T> list)
		{
			return list[UnityEngine.Random.Range(0, list.Count)];
		}

		public static IList<T> Random<T>(this IList<T> list)
		{
			for (int i = 0; i < list.Count; i++)
			{
				var cur = list[i];
				list.Remove(cur);
				list.Insert(UnityEngine.Random.Range(0, i+1), cur);
			}
			return list;
		}
	}
}
