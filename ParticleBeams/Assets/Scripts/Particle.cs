using UnityEngine;
using Kayac;

[System.Serializable]
struct ParticleParameters
{
	public float attenuation;
	public float randomWalk;
	public float damping;
	public float gravity;
	public float radius;
}


struct Particle
{
	public void Emit(
		Vector3 position,
		Vector3 velocity,
		Vector3 constantForce,
		float randomAccelStrengh,
		float damping,
		float attenuation,
		float radius)
	{
		this.position = position;
		this.velocity = velocity;
		this.constantAccel = constantForce;
		this.randomAccelStrengh = randomAccelStrengh;
		this.damping = damping;
		this.attenuation = attenuation;
		this.radius = radius;
		time = 0f;
	}
	public void Update(
		float deltaTime,
		ref Vector3 billboardUp,
		ref Vector3 billboardRight,
		ref Random32 random)
	{
		velocity *= 1f - (damping * deltaTime);
		float min = -0.5f * randomAccelStrengh;
		float max = -min;
		var a = new Vector3(
			random.GetFloat(min, max),
			random.GetFloat(min, max),
			random.GetFloat(min, max));
		Math.Add(ref a, ref constantAccel);
		Math.Madd(ref velocity, ref a, deltaTime);
		Math.Madd(ref position, ref velocity, deltaTime);
		radius *= 1f - (attenuation * deltaTime);
		time += deltaTime;

		// 頂点座標計算
		const float cos30 = 0.866025403784439f;
		float rCos30x2 = radius * cos30 * 2f;
		Vector3 tmpUp, tmpRight, upCenter;
		Math.SetMul(out tmpUp, ref billboardUp, radius);
		Math.SetMul(out tmpRight, ref billboardRight, rCos30x2);
		// 上辺はcenter.y - r*sin(30) = r/2
		// 下端はcenter.y + r*cos(30)
		Math.SetAdd(out upCenter, ref position, ref tmpUp);
		Math.SetMsub(out p0, ref position, ref tmpUp, 2f);
		Math.SetSub(out p1, ref upCenter, ref tmpRight);
		Math.SetAdd(out p2, ref upCenter, ref tmpRight);
	}
	public Vector3 position;
	public Vector3 velocity;
	public float time;
	public float radius;
	public Vector3 p0, p1, p2;
	Vector3 constantAccel;
	float randomAccelStrengh;
	float damping;
	float attenuation;
}
