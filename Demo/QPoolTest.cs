using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QTool;
public class QPoolTest : MonoBehaviour
{
	public bool run = false;
	public GameObject prefab;
	public bool usePool;
	public int size = 10;
	List<GameObject> objList = new List<GameObject>();
	int count;
	
	private void Awake()
	{
		Application.targetFrameRate = 60;
		Pool= QPoolManager.GetPool("测试Pool", prefab); ;
	}
	public ObjectPool<GameObject> Pool;
	private void FixedUpdate()
	{
		if (!run) return;
		if (count >= 10)
		{
			foreach (var item in objList)
			{
				if (usePool)
				{
					Pool.Push(item);
				}
				else
				{
					Destroy(item);
				}
			}
			objList.Clear();
			for (int i = 0; i < size; i++)
			{
				if (usePool)
				{ 
					var obj =Pool.Get();
					obj.transform.position = Vector3.right * i;
					objList.Add(obj);
				}
				else
				{
					objList.Add(Instantiate(prefab, Vector3.right * i, Quaternion.identity));
				}
				
			}
			
			count = 0;
		}
		
		count++;
	}
}
