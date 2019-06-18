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
	float radius = 0.2f;
	[SerializeField]
	float countPerMeter = 1f;
	[SerializeField]
	float beamSpeed = 1f;
	[SerializeField]
	float beamHoming = 1f;
	[SerializeField]
	float attenuation = 1f;
	[SerializeField]
	float randomWalk = 1f;

	DebugPrimitiveRenderer3D renderer3d;

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
			float homing)
		{
			this.position = position;
			this.velocity = velocity;
			this.speed = speed;
			this.homing = homing;
			time = 0f;
		}

		public void Update(float deltaTime, Vector3 target)
		{
			var toTarget = (target - position).normalized;
			var v = velocity.normalized;
			var c = toTarget - v;
			velocity += c * (deltaTime * homing);
			velocity.Normalize();
			velocity *= speed;
			position += velocity * deltaTime;
			time += deltaTime;
		}
		public Vector3 position;
		public Vector3 velocity;
		public float time;
		public float homing;
		public float speed;
	}

	struct Particle
	{
		public void Emit(Vector3 position)
		{
			this.position = position;
			velocity = Vector3.zero;
			time = 0f;
		}
		public void Update(
			float deltaTime,
			Vector3 force,
			float damping)
		{
			velocity -= velocity * damping * deltaTime;
			velocity += force * deltaTime;
			position += velocity * deltaTime;
			time += deltaTime;
		}
		public Vector3 position;
		public Vector3 velocity;
		public float time;
	}
	Particle[] particles;
	int nextParticleIndex;
	Beam[] beams;
	int nextBeamIndex;
	bool fired;

	void Start()
	{
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
			1f);
		nextBeamIndex++;
		if (nextBeamIndex >= beams.Length)
		{
			nextBeamIndex = 0;
		}
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			Fire();
		}

		for (int i = 0; i < beams.Length; i++)
		{
			if (beams[i].time >= 0)
			{
				var prev = beams[i].position;
				beams[i].Update(Time.deltaTime, target.position);
				var cur = beams[i].position;

				float l = (cur - prev).magnitude;
				float c = l * countPerMeter;
				int ci = (int)c;
				float frac = c - (float)ci;
				if (UnityEngine.Random.value < frac)
				{
					ci++;
				}
				for (int j = 0; j < ci; j++)
				{
					var pos = Vector3.Lerp(prev, cur, UnityEngine.Random.value);
					particles[nextParticleIndex].Emit(pos);
					nextParticleIndex++;
					if (nextParticleIndex >= particles.Length)
					{
						nextParticleIndex = 0;
					}
				}
			}
		}
		renderer3d.color = new Color32(192, 224, 255, 255);
		for (int i = 0; i < particles.Length; i++)
		{
			if (particles[i].time >= 0f)
			{
				particles[i].Update(
					Time.deltaTime,
					new Vector3(
						UnityEngine.Random.Range(-1f, 1f),
						UnityEngine.Random.Range(-1f, 1f),
						UnityEngine.Random.Range(-1f, 1f)) * randomWalk,
					0.1f);
				float r = radius * Mathf.Exp(-particles[i].time * attenuation);
				renderer3d.AddBillboard(
					particles[i].position,
					r,
					r,
					texture);
			}
		}
		renderer3d.UpdateMesh();
	}
}
