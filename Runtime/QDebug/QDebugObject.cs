using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QDebugObject : MonoBehaviour
{
	private void Awake()
	{
#if QDebug
		gameObject.SetActive(false);
#endif
	}
}
