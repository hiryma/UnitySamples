using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomMover : MonoBehaviour
{
	[SerializeField] float slideSpeed = 1f;
	[SerializeField] float slideLength = 1f;
	[SerializeField] float rotSpeed = 1f;

	void Start()
	{
		initialPosition = transform.localPosition;
		initialRotation = transform.localRotation;
	}

	void Update()
	{
		var p = initialPosition;
		p += slideLength * Mathf.Sin(Time.time * slideSpeed) * Vector3.up;

		var q = initialRotation;
		q *= Quaternion.AngleAxis(rotSpeed * Time.time, new Vector3(1f, 1f, 1f));

		transform.localPosition = p;
		transform.localRotation = q;		
	}

	Vector3 initialPosition;
	Quaternion initialRotation;
}
