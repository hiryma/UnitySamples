using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
	[SerializeField] Transform animated;
	[SerializeField] Transform physical;

	void Start()
	{

	}

	void FixedUpdate()
	{
		MoveRandom(animated);	
	}

	void MoveRandom(Transform node)
	{
		node.localPosition = new Vector3(0f, 3f + Mathf.Sin(Time.time) * 1f);
		node.localRotation = Quaternion.Euler(
			Mathf.Sin(Time.time) * 10f, 
			Mathf.Sin(Time.time) * 20f, 
			Mathf.Sin(Time.time) * 30f);
		for (var i = 0; i < node.childCount; i++)
		{
			MoveRandom(node.GetChild(i));
		}
	}
}
