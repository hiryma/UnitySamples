using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
	[SerializeField] Rigidbody rBody;
	[SerializeField] ArticulationBody aBody;
	[SerializeField] Vector3 torque;
	[SerializeField] bool relative;

	void Start()
	{
		rBody.maxAngularVelocity = 100f;
		aBody.maxAngularVelocity = 100f;
	}

	void FixedUpdate()
	{
		var dt = Time.fixedDeltaTime;
		if (relative)
		{
			rBody.AddRelativeTorque(torque);
			aBody.AddRelativeTorque(torque);
		}
		else
		{
			rBody.AddTorque(torque);
			aBody.AddTorque(torque);
		}
	}
}
