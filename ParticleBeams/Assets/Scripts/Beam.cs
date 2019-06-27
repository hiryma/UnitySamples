using UnityEngine;

[System.Serializable]
class BeamParamters
{
	public float countPerMeter = 1f;
	public float speed = 1f;
	public float curvatureRadius = 1f;
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
		float curvatureRadius,
		float damping)
	{
		this.position = position;
		this.velocity = velocity;
		// 速さv、半径rで円を描く時、その向心力はv^2/r。これを計算しておく。
		this.maxCentripetalAccel = speed * speed / curvatureRadius;
		this.damping = damping;
		// 終端速度がspeedになるaccelを求める
		// v = a / kだからa=v*k
		this.propulsion = speed * damping;
		time = 0f;
	}

	public void Update(float deltaTime, Vector3 target)
	{
		var toTarget = target - position;
		var vn = velocity.normalized;
		var dot = Vector3.Dot(toTarget, vn);
		var centripetalAccel = toTarget - (vn * dot);
		var centripetalAccelMagnitude = centripetalAccel.magnitude;
		if (centripetalAccelMagnitude > 1f)
		{
			centripetalAccel /= centripetalAccelMagnitude;
		}
		var force = centripetalAccel * maxCentripetalAccel;
		force += vn * propulsion;
		force -= velocity * damping;
		velocity += force * deltaTime;
		position += velocity * deltaTime;
		time += deltaTime;
	}
	public Vector3 position;
	public Vector3 velocity;
	public float time;
	float maxCentripetalAccel;
	float damping;
	float propulsion; // 推進力
}
