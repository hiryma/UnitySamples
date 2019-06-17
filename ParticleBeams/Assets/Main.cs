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

	DebugPrimitiveRenderer3D renderer3d;

	class ParticleGroup
	{
		public ParticleGroup(
			Particle[] items,
			int itemsBegin,
			int itemCount,
			Color32 color,
			float damping,
			float gravity)
		{
			this.items = items;
			this.itemsBegin = itemsBegin;
			this.itemCount = itemCount;
			this.color = color;
			this.damping = damping;
			this.gravity = gravity;
		}

		public delegate void EmitFunc(ref Particle particle, int index);

		public void Emit(EmitFunc func, int count)
		{
			var index = nextIndex;
			for (int i = 0; i < count; i++)
			{
				func(ref items[itemsBegin + index], i);
				index++;
				if (index == itemCount)
				{
					index = 0;
				}
			}
			nextIndex = index;
		}
		public void Update(float deltaTime, DebugPrimitiveRenderer3D renderer)
		{
			renderer.color = color;
			for (int i = 0; i < itemCount; i++)
			{
				Vector3 force = new Vector3(
					UnityEngine.Random.Range(-100f, 100f),
					UnityEngine.Random.Range(-100f, 100f),
					UnityEngine.Random.Range(-10f, 10f));
				force.y -= gravity;

				items[itemsBegin + i].Update(deltaTime, force, damping);
//				renderer.AddParallelogram(
//					items[itemsBegin + i].position,
//					Vector3.right * 0.1f,
//					Vector3.up * 0.1f);
				renderer.AddBillboard(
					items[itemsBegin + i].position,
					0.1f,
					0.1f);
			}
		}
		Particle[] items;
		int itemsBegin;
		int itemCount;
		Color32 color;
		float damping;
		float gravity;
		int nextIndex;
	}
	struct Particle
	{
		public void Update(
			float deltaTime,
			Vector3 force,
			float damping)
		{
			velocity -= velocity * damping * deltaTime;
			velocity += force * deltaTime;
			position += velocity * deltaTime;
		}
		public Vector3 position;
		public Vector3 velocity;
	}
	Particle[] particles;
	ParticleGroup[] particleGroups;

    void Start()
    {
        renderer3d = new DebugPrimitiveRenderer3D(
			textShader,
			texturedShader,
			font,
			mainCamera,
			meshRenderer,
			meshFilter,
			16384);
		particles = new Particle[16000];
		particleGroups = new ParticleGroup[8];

		particleGroups[0] = new ParticleGroup(
			particles,
			0,
			16000,
			new Color32(128, 192, 224, 255),
			0.00f,
			1.00f);
    }

    void Update()
    {
		if (Input.anyKey)
		{
			Emit();
		}
		UpdateParticles(Time.deltaTime);
		renderer3d.UpdateMesh();
    }

	void UpdateParticles(float deltaTime)
	{
		for (int i = 0; i < particleGroups.Length; i++)
		{
			if (particleGroups[i] != null)
			{
				particleGroups[i].Update(deltaTime, renderer3d);
			}
		}
	}

	void Emit()
	{
		particleGroups[0].Emit(EmitFunc0, 500);
	}

	void EmitFunc0(ref Particle particle, int index)
	{
		particle.position = new Vector3(
			UnityEngine.Random.Range(-0.5f, 0.5f),
			UnityEngine.Random.Range(-0.5f, 0.5f),
			(float)index);
		particle.velocity = new Vector3(
			UnityEngine.Random.Range(-1f, 1f),
			UnityEngine.Random.Range(-1f, 1f) + 10f,
			100f + UnityEngine.Random.Range(-0.1f, 0.1f));
	}
}
