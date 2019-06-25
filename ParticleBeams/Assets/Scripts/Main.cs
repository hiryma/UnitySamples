using UnityEngine;
using Kayac;

public class Main : MonoBehaviour
{
	[SerializeField] ParticleRenderer particleRendererPrefab;
	[SerializeField] Shader textShader;
	[SerializeField] Shader texturedShader;
	[SerializeField] Font font;
	[SerializeField] Camera mainCamera;
	[SerializeField] Transform gunPoint;
	[SerializeField] Transform target;
	[SerializeField] Texture2D texture;
	[SerializeField] BeamParamters beamParameters;
	[SerializeField] SparkParameters sparkParameters;
	[SerializeField] float targetDamping = 0.1f;
	[SerializeField] float targetStiffness = 1f;
	[SerializeField] float cameraPositionParameter = 0.2f;
	[SerializeField] float cameraMargin = 0.1f;
	[SerializeField] float cameraStiffness = 2f;
	[SerializeField] ParticleParameters beamParticleParameters;
	[SerializeField] ParticleParameters sparkParticleParameters;
	[SerializeField] bool threadEnabled;


	[System.Serializable]
	class SparkParameters
	{
		public int particleCount = 100;
		public float velocityMinRatio = 0.5f;
		public float velocityMaxRatio = 1f;
		public float sharpness = 4f;
	}

	const int particleCapacity = 100000;
	Vector3 targetVelocity;
	Vector3 targetOrigin;
	CameraController cameraController;
	Random32 random;
	Particle[] particles;
	int nextParticleIndex;
	Beam[] beams;
	int nextBeamIndex;
	DebugUiManager debugUi;
	ThreadPool threadPool;
	Job[] jobs;
	ParticleRenderer[] particleRenderers;

	class Job : ThreadPool.IJob
	{
		Particle[] particles;
		int begin;
		int count;
		float deltaTime;
		Vector3 billboardUp;
		Vector3 billboardRight;
		Random32 random;
		ParticleRenderer renderer;

		public Job(Particle[] particles, int jobIndex, ParticleRenderer renderer)
		{
			this.particles = particles;
			this.renderer = renderer;
			this.random = new Random32(jobIndex);
		}
		public void Set(
			int begin,
			int count,
			float deltaTime,
			ref Vector3 billboardUp,
			ref Vector3 billboardRight)
		{
			this.begin = begin;
			this.count = count;
			this.deltaTime = deltaTime;
			this.billboardUp = billboardUp;
			this.billboardRight = billboardRight;
		}
		public void Execute()
		{
			const float cos30 = 0.866025403784439f;
			const float leftU = 0.5f - cos30;
			const float rightU = 0.5f + cos30;
			var uv0 = new Vector2(0.5f, -0.5f);
			var uv1 = new Vector2(leftU, 1f);
			var uv2 = new Vector2(rightU, 1f);
#if true
			var mesh = renderer.Mesh;
			mesh.color = new Color32(64, 224, 255, 255);
			for (int i = 0; i < count; i++)
			{
				int idx = i + begin;
				particles[idx].Update(
					deltaTime,
					ref billboardUp,
					ref billboardRight,
					ref random);
 				mesh.AddTriangle(
					 ref particles[idx].p0,
					 ref particles[idx].p1,
					 ref particles[idx].p2,
					 ref uv0,
					 ref uv1,
					 ref uv2);
			}
#else
			var renderer3d = renderer.Renderer;
			renderer3d.color = new Color32(64, 224, 255, 255);
			for (int i = 0; i < count; i++)
			{
				int idx = i + begin;
				particles[idx].Update(
					deltaTime,
					ref billboardUp,
					ref billboardRight,
					ref random);
 				renderer3d.AddTexturedTriangleFast(
					 ref particles[idx].p0,
					 ref particles[idx].p1,
					 ref particles[idx].p2,
					 ref uv0,
					 ref uv1,
					 ref uv2);
			}
#endif
		}
	}

	void Start()
	{
		debugUi = DebugUiManager.Create(
			mainCamera,
			textShader,
			texturedShader,
			font,
			768,
			432,
			1f,
			100);
		debugUi.Add(new FrameTimeGauge(100f, 20f, null), 0f, 0f, DebugUi.AlignX.Right);

		var toggle = new DebugUiToggle("Thread");
		toggle.onChange = on =>
		{
			threadEnabled = on;
		};
		debugUi.Add(toggle, 0f, 20f, DebugUi.AlignX.Right);

		var button = new DebugUiButton("Fire");
		button.onClick = () =>
		{
			Fire();
		};
		debugUi.Add(button, 0f, -50f, DebugUi.AlignX.Right, DebugUi.AlignY.Bottom);

		button = new DebugUiButton("Fire16");
		button.onClick = () =>
		{
			Fire(16);
		};
		debugUi.Add(button, 0f, 0f, DebugUi.AlignX.Right, DebugUi.AlignY.Bottom);

		random = new Random32(0);
		cameraController = new CameraController(mainCamera);
		targetOrigin = target.transform.position;
		beams = new Beam[64];
		for (int i = 0; i < beams.Length; i++)
		{
			beams[i] = new Beam();
		}
		particles = new Particle[particleCapacity];
		for (int i = 0; i < particles.Length; i++)
		{
			particles[i].position = new Vector3(1000f, 1000f, -1000f);
			particles[i].time = -float.MaxValue;
		}

		var threadCount = SystemInfo.processorCount;
		var jobCount = threadCount * 3;
		threadPool = new ThreadPool(threadCount, jobCount);
		jobs = new Job[jobCount];
		particleRenderers = new ParticleRenderer[jobCount];
		int rendererCapacity = (particleCapacity + jobCount - 1) / jobCount;
		for (int i = 0; i < jobCount; i++)
		{
			particleRenderers[i] = Instantiate(particleRendererPrefab, gameObject.transform, false);
			particleRenderers[i].Initialize(mainCamera, rendererCapacity);
			jobs[i] = new Job(particles, i, particleRenderers[i]);
		}

		Update(); // 初回
		LateUpdate();
		cameraController.Converge();
	}

	void Fire()
	{
		var rotation = Quaternion.Euler(
			0f,
			0f,
			random.GetFloat(-180f, 180f));
		Matrix4x4 m = Matrix4x4.TRS(
			Vector3.zero,
			rotation,
			Vector3.one);
		m = gunPoint.localToWorldMatrix * m;
		var baseV = new Vector3(0f, 0.86f, 0.5f) * beamParameters.speed;

		var v = m.MultiplyVector(baseV);
		Fire(v);
	}

	void Fire(int count)
	{
		float rotUnit = 360f / count;
		float rot = random.GetFloat(-180f, 180f);
		for (int i = 0; i < count; i++)
		{
			var rotation = Quaternion.Euler(
				0f,
				0f,
				rot);
			rot += rotUnit;
			Matrix4x4 m = Matrix4x4.TRS(
				Vector3.zero,
				rotation,
				Vector3.one);
			m = gunPoint.localToWorldMatrix * m;
			var baseV = new Vector3(0f, 1f, 0f) * beamParameters.speed;
			var v = m.MultiplyVector(baseV);
			Fire(v);
		}
	}

	void Fire(Vector3 v)
	{
		var beam = beams[nextBeamIndex];
		beam.Emit(
			gunPoint.position,
			v,
			beamParameters.speed,
			beamParameters.homing,
			beamParameters.damping);
		nextBeamIndex++;
		if (nextBeamIndex >= beams.Length)
		{
			nextBeamIndex = 0;
		}

	}

	void Update()
	{
		float dt = Time.deltaTime;
		debugUi.ManualUpdate(dt);
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

		gunPoint.LookAt(p);

		for (int i = 0; i < beams.Length; i++)
		{
			if (beams[i].time >= 0)
			{
				UpdateBeam(beams[i]);
			}
		}
		UnityEngine.Profiling.Profiler.BeginSample("Main.Update.UpdateParticles");

		var transform = mainCamera.gameObject.transform;
		var forwardVector = transform.forward;
		var upVector = transform.up;
		// TODO: 右ベクタ、上ベクタは前計算可能
		// 右ベクタ、上ベクタを生成 axisX = cross(axisY, axisZ)
		Vector3 right = Vector3.Cross(upVector, forwardVector);
		right.Normalize();
		// ビルボード空間の上ベクタを計算
		Vector3 up = Vector3.Cross(forwardVector, right);

		int rest = particles.Length;
		int unit = rest / jobs.Length;
		if ((rest - (unit * jobs.Length)) > 0)
		{
			unit += 1;
		}
		int begin = 0;
		for (int i = 0; i < jobs.Length; i++)
		{
			int count = (rest >= unit) ? unit : rest;
			jobs[i].Set(begin, count, dt, ref up, ref right);
//			particleRenderers[i].Renderer.BeginAddTexturedTriangle(texture);
			particleRenderers[i].Mesh.SetTexture(texture);
			if (threadEnabled)
			{
				threadPool.AddJob(jobs[i]);
			}
			else
			{
				jobs[i].Execute();
			}
			rest -= count;
			begin += count;
		}
		UnityEngine.Profiling.Profiler.EndSample();

		var tp = target.transform.position;
		var gp = gunPoint.position;
		cameraController.FitByMove2PointVertical(
			gp,
			tp,
			Vector3.up,
			cameraPositionParameter,
			cameraMargin);
		cameraController.Stiffness = cameraStiffness;
		cameraController.ManualUpdate(dt);
	}

	void LateUpdate()
	{
		if (threadEnabled)
		{
			threadPool.Wait(); // 完全終了待ち
		}
		UnityEngine.Profiling.Profiler.BeginSample("Main.LateUpdate.UpdateMesh");
		for (int i = 0; i < particleRenderers.Length; i++)
		{
//			particleRenderers[i].Renderer.UpdateMesh();
			particleRenderers[i].Mesh.Update();
		}
		UnityEngine.Profiling.Profiler.EndSample();
	}

	void UpdateBeam(Beam beam)
	{
		var prev = beam.position;
		beam.Update(Time.deltaTime, target.position);
		var cur = beam.position;

		float l = (cur - prev).magnitude;
		float c = l * beamParameters.countPerMeter;
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
			targetVelocity += beam.velocity * beamParameters.impact;
			beam.time = -float.MaxValue; // ビーム消滅
			Spark(beam.position, beam.velocity, diff.normalized, sparkParameters.sharpness, sparkParameters.particleCount);
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
			Math.GetHemisphericalCosPoweredDistribution(out xAngle, out yAngle, sharpness, ref random);
			Vector3 v;
			float sinX = Mathf.Sin(xAngle);
			v.x = sinX * Mathf.Cos(yAngle);
			v.y = sinX * Mathf.Sin(yAngle);
			v.z = Mathf.Cos(xAngle);
			var q = Quaternion.FromToRotation(new Vector3(0f, 0f, 1f), v);
			v = q * reflection;
			v *= random.GetFloat(sparkParameters.velocityMinRatio, sparkParameters.velocityMaxRatio);
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
}
