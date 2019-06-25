using UnityEngine;

[System.Serializable]
class BeamParamters
{
	public float countPerMeter = 1f;
	public float speed = 1f;
	public float homing = 1f;
	public float damping = 0.1f;
	public float impact = 1f;
}

class Beam
{
	public Beam()
	{
		time = -float.MaxValue;
	}

	public void Emit(
		Vector3 position,
		Vector3 velocity,
		float speed,
		float homing,
		float damping)
	{
		this.position = position;
		this.velocity = velocity;
		this.speed = speed;
		this.homing = homing;
		this.damping = damping;
		// 終端速度がspeedになるaccelを求める
		// v = a / kだからa=v*k
		this.accel = speed * damping;
		time = 0f;
	}

	public void Update(float deltaTime, Vector3 target)
	{
		var toTarget = target - position;
		var v = velocity.normalized;
		var dot = Vector3.Dot(toTarget, v);
		var force = (toTarget - (v * dot)) / deltaTime;
		if (force.magnitude > homing)
		{
			force.Normalize();
			force *= homing;
		}
		force += v * accel;
		force -= velocity * damping;
		velocity += force * deltaTime;
		position += velocity * deltaTime;
		time += deltaTime;
	}
	public Vector3 position;
	public Vector3 velocity;
	public float time;
	float homing;
	float speed;
	float damping;
	float accel;
}
