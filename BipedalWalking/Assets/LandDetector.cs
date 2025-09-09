using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandDetector : MonoBehaviour
{
	[SerializeField] Man owner;
	[SerializeField] Rigidbody body;

	void OnCollisionStay(Collision collision)
	{
		owner.OnLandDetectorCollisionStay(false, collision, body);
	}

	void OnCollisionEnter(Collision collision)
	{
		owner.OnLandDetectorCollisionStay(true, collision, body);
	}
}
