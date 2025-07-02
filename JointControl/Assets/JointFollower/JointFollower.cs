using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JointFollower : MonoBehaviour
{
	[SerializeField] Transform goalParent;
	[SerializeField] Transform goal;
	[SerializeField] ConfigurableJoint joint;
	[SerializeField] Rigidbody body;
	[SerializeField] float maxLinearVelocity = float.MaxValue;
	[SerializeField] float maxAngularVelocity = float.MaxValue;

	public void Init(
		Transform goalParent,
		Transform goal,
		ConfigurableJoint joint,
		Rigidbody body,
		float maxLinearVelocity,
		float maxAngularVelocity)
	{
		this.goalParent = goalParent;
		this.goal = goal;
		this.joint = joint;
		this.body = body;
		this.maxLinearVelocity = maxLinearVelocity;
		this.maxAngularVelocity = maxAngularVelocity;
	}

	void Start()
	{
		body.maxAngularVelocity = maxAngularVelocity;
		body.maxLinearVelocity = maxLinearVelocity;
		initialGoalLocalPosition = goalParent.InverseTransformPoint(goal.position);
//Debug.Log(body.gameObject.name + " " + goalParent.position + " " + goal.position + " " + initialGoalLocalPosition);
		joint.connectedAnchor = initialGoalLocalPosition;
		joint.anchor = Vector3.zero;

		var gq = goal.rotation;
		var parentGq = goalParent.rotation;
		var fromParentQ0 = Quaternion.Inverse(gq) * parentGq;
		toParentQ0 = Quaternion.Inverse(fromParentQ0);
	}

	void Update()
	{
		var goalWorldPosition = goal.position; // ゴール世界座標
		var anchorWorldPosition = goalParent.TransformPoint(joint.connectedAnchor);

		joint.targetPosition = Quaternion.Inverse(goal.transform.rotation) * (anchorWorldPosition - goalWorldPosition);

		var gq = goal.rotation;
		var parentGq = goalParent.rotation;
		var fromParentQ = Quaternion.Inverse(gq) * parentGq;

//Debug.Log(body.gameObject.name + " gq.up=" + (gq * Vector3.up) + " pGq.up=" + (parentGq * Vector3.up) + " fpQ.up=" + (fromParentQ * Vector3.up) + " rb.r=" + body.rotation);
		joint.targetRotation = fromParentQ * toParentQ0;
	}

	// non public ----
	Vector3 initialGoalLocalPosition;
	Quaternion toParentQ0;
}
