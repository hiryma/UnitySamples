using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JointController : MonoBehaviour
{
	[SerializeField] Transform target;
	[SerializeField] ConfigurableJoint joint;

	void Start()
	{
		initialRotation = joint.transform.localRotation;
		joint.connectedAnchor = joint.transform.localPosition;
		joint.anchor = Vector3.zero;
	}

	void FixedUpdate()
	{
		var gq = target.rotation;
		var cq = joint.transform.rotation;
		var targetRotation = Quaternion.Inverse(gq) * cq * initialRotation;

		var gp = target.position;
		var cp = joint.transform.position;
		var targetPosition = (Quaternion.Inverse(gq) * (gp - cp));

		joint.targetPosition = targetPosition;
	}

	Quaternion initialRotation;
}
