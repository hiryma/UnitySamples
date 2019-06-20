using UnityEngine;
using Kayac;

public class Main : MonoBehaviour
{
	[SerializeField]
	Shader textShader;
	[SerializeField]
	Shader texturedShader;
	[SerializeField]
	Font font;
	[SerializeField]
	Camera mainCamera;
	[SerializeField]
	MeshRenderer meshRenderer;
	[SerializeField]
	MeshFilter meshFilter;
	[SerializeField]
	Transform gunPoint;
	[SerializeField]
	Transform target;
	[SerializeField]
	Texture2D texture;
	[SerializeField]
	float countPerMeter = 1f;
	[SerializeField]
	float beamSpeed = 1f;
	[SerializeField]
	float beamHoming = 1f;
	[SerializeField]
	float beamDamping = 0.1f;
	[SerializeField]
	int sparkCount = 100;
	[SerializeField]
	float sparkVelocityMinRatio = 0.5f;
	[SerializeField]
	float sparkVelocityMaxRatio = 1f;
	[SerializeField]
	float sparkSharpness = 4f;
	[SerializeField]
	float targetDamping = 0.1f;
	[SerializeField]
	float beamImpact = 1f;
	[SerializeField]
	float targetStiffness = 1f;
	[SerializeField]
	float cameraPositionParamter = 0.2f;
	[SerializeField]
	float cameraMargin = 0.1f;
	[SerializeField]
	float cameraStiffness = 2f;
	[SerializeField]
	ParticleParameters beamParticleParameters;
	[SerializeField]
	ParticleParameters sparkParticleParameters;

	[System.Serializable]
	struct ParticleParameters
	{
		public float attenuation;
		public float randomWalk;
		public float damping;
		public float gravity;
		public float radius;
	}

	DebugPrimitiveRenderer3D renderer3d;
	Vector3 targetVelocity;
	Vector3 targetOrigin;
	CameraController cameraController;

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
		public void Update(float deltaTime)
		{
			velocity -= velocity * damping * deltaTime;
			var a = new Vector3(
				UnityEngine.Random.Range(-0.5f, 0.5f),
				UnityEngine.Random.Range(-0.5f, 0.5f),
				UnityEngine.Random.Range(-0.5f, 0.5f)) * randomAccelStrengh;
			velocity += (constantAccel + a) * deltaTime;
			position += velocity * deltaTime;
			radius *= 1f - (attenuation * deltaTime);
			time += deltaTime;
		}
		public Vector3 position;
		public Vector3 velocity;
		public float time;
		public float radius;
		Vector3 constantAccel;
		float randomAccelStrengh;
		float damping;
		float attenuation;
	}
	Particle[] particles;
	int nextParticleIndex;
	Beam[] beams;
	int nextBeamIndex;

	void Start()
	{
		cameraController = new CameraController(mainCamera);
		targetOrigin = target.transform.position;
		renderer3d = new DebugPrimitiveRenderer3D(
			textShader,
			texturedShader,
			font,
			mainCamera,
			meshRenderer,
			meshFilter,
			20000);
		beams = new Beam[8];
		for (int i = 0; i < beams.Length; i++)
		{
			beams[i] = new Beam();
		}
		particles = new Particle[10000];
		for (int i = 0; i < particles.Length; i++)
		{
			particles[i].position = new Vector3(1000f, 1000f, -1000f);
			particles[i].time = -float.MaxValue;
		}
		Update(); // 初回
		cameraController.Converge();
	}

	void Fire()
	{
		var beam = beams[nextBeamIndex];
		Matrix4x4 m = Matrix4x4.TRS(
			Vector3.zero,
			Quaternion.Euler(
				UnityEngine.Random.Range(0f, 30f),
				UnityEngine.Random.Range(-90f, 90f),
				0f),
			Vector3.one);
		var v = m.MultiplyVector(new Vector3(0f, 0f, beamSpeed));
		beam.Emit(
			gunPoint.position,
			v,
			beamSpeed,
			beamHoming,
			beamDamping);
		nextBeamIndex++;
		if (nextBeamIndex >= beams.Length)
		{
			nextBeamIndex = 0;
		}
	}

	void Update()
	{
		float dt = Time.deltaTime;
		if (Input.GetKeyDown(KeyCode.Space))
		{
			Fire();
		}

		// テキトーに的を動かす
		var p = target.transform.localPosition;
		var dp = p - targetOrigin;
		targetVelocity *= 1f - targetDamping;
		targetVelocity -= dp * targetStiffness * dt;
		p += targetVelocity * dt;
		target.transform.localPosition = p;

		for (int i = 0; i < beams.Length; i++)
		{
			if (beams[i].time >= 0)
			{
				UpdateBeam(beams[i]);
			}
		}
		renderer3d.color = new Color32(64, 224, 255, 255);
		for (int i = 0; i < particles.Length; i++)
		{
			if (particles[i].time >= 0f)
			{
				particles[i].Update(dt);
				float r = particles[i].radius;
				renderer3d.AddBillboard(
					particles[i].position,
					r,
					r,
					texture);
			}
		}
		renderer3d.UpdateMesh();
		var tp = target.transform.position;
		var gp = gunPoint.position;
		cameraController.FitByMove2PointVertical(
			gp,
			tp,
			Vector3.up,
			cameraPositionParamter,
			cameraMargin);
		cameraController.Stiffness = cameraStiffness;
		cameraController.ManualUpdate(dt);
	}

	void UpdateBeam(Beam beam)
	{
		var prev = beam.position;
		beam.Update(Time.deltaTime, target.position);
		var cur = beam.position;

		float l = (cur - prev).magnitude;
		float c = l * countPerMeter;
		int ci = (int)c;
		float frac = c - (float)ci;
		if (UnityEngine.Random.value < frac)
		{
			ci++;
		}
		EmitBeamParticles(beam, ci, prev, cur);
		// ヒット判定
		var diff = beam.position - target.position;
		if (diff.magnitude < 3f)
		{
			targetVelocity += beam.velocity * beamImpact;
			beam.time = -float.MaxValue; // ビーム消滅
			Spark(beam.position, beam.velocity, diff.normalized, sparkSharpness, sparkCount);
		}
	}

	void EmitBeamParticles(Beam beam, int count, Vector3 prevPos, Vector3 curPos)
	{
		for (int i = 0; i < count; i++)
		{
			var pos = Vector3.Lerp(prevPos, curPos, UnityEngine.Random.value);
			particles[nextParticleIndex].Emit(
				pos,
				Vector3.zero,
				Vector3.zero,
				beamParticleParameters.randomWalk,
				beamParticleParameters.damping,
				beamParticleParameters.attenuation,
				beamParticleParameters.radius);
			nextParticleIndex++;
			if (nextParticleIndex >= particles.Length)
			{
				nextParticleIndex = 0;
			}
		}
	}

	void Spark(
		Vector3 position,
		Vector3 beamVelocity,
		Vector3 normal,
		float sharpness,
		int count)
	{
		var dot = Vector3.Dot(beamVelocity, normal);
		var reflection = beamVelocity - (normal * (dot * 2f));
		for (int i = 0; i < count; i++)
		{
			float xAngle, yAngle;
			GetHemisphericalCosPoweredRandom(out xAngle, out yAngle, sharpness);
			Vector3 v;
			float sinX = Mathf.Sin(xAngle);
			v.x = sinX * Mathf.Cos(yAngle);
			v.y = sinX * Mathf.Sin(yAngle);
			v.z = Mathf.Cos(xAngle);
			var q = Quaternion.FromToRotation(new Vector3(0f, 0f, 1f), v);
			v = q * reflection;
			v *= UnityEngine.Random.Range(sparkVelocityMinRatio, sparkVelocityMaxRatio);
			particles[nextParticleIndex].Emit(
				position,
				v,
				new Vector3(0f, -sparkParticleParameters.gravity, 0f),
				sparkParticleParameters.randomWalk,
				sparkParticleParameters.damping,
				sparkParticleParameters.attenuation,
				sparkParticleParameters.radius);
			nextParticleIndex++;
			if (nextParticleIndex >= particles.Length)
			{
				nextParticleIndex = 0;
			}
		}
	}

	void GetSphericalRandom(
		out float xAngle,
		out float yAngle)
	{
		yAngle = UnityEngine.Random.Range(-Mathf.PI, Mathf.PI);
		var r = UnityEngine.Random.value;
		xAngle = Mathf.Asin((2f * r) - 1f);
	}

	void GetHemisphericalRandom(
		out float xAngle,
		out float yAngle)
	{
		yAngle = UnityEngine.Random.Range(-Mathf.PI, Mathf.PI);
		var r = UnityEngine.Random.value;
		xAngle = Mathf.Acos(r);
	}

	void GetHemisphericalCosRandom(
		out float xAngle,
		out float yAngle)
	{
		yAngle = UnityEngine.Random.Range(-Mathf.PI, Mathf.PI);
		var r = UnityEngine.Random.value;
		xAngle = Mathf.Asin(Mathf.Sqrt(r));
	}

	void GetHemisphericalCosPoweredRandom(
		out float xAngle,
		out float yAngle,
		float power)
	{
		yAngle = UnityEngine.Random.Range(-Mathf.PI, Mathf.PI);
		var r = UnityEngine.Random.value;
		var powered = Mathf.Pow(r, 1f / (power + 1f));
		xAngle = Mathf.Acos(powered);
	}
}
