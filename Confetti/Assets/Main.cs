using UnityEngine;
using Kayac;

public class Main : MonoBehaviour
{
	[SerializeField] MeshRenderer meshRenderer;
	[SerializeField] MeshFilter meshFilter;
	[SerializeField] Material material;
	[SerializeField] float pieceWidth = 0.3f;
	[SerializeField] float pieceLength = 0.6f;

	ParticleMesh particleMesh;
	Piece[] pieces;
	float pieceCountLog = 2f;
	float gravityLog = 1f;
	float windLog = 0f;
	float resistanceLog = -2f;
	Random32 random;
	int capacity = 8192;

	void Start()
	{
		random = new Random32(1);
		pieces = new Piece[capacity];
		particleMesh = new ParticleMesh(meshRenderer, meshFilter, capacity * 2);
		Emit();
	}

	void Emit()
	{
		int pieceCount = (int)Mathf.Pow(10f, pieceCountLog);
		for (int i = 0; i < pieceCount; i++)
		{
			int colorIndex = random.GetInt(0, 7);
			Vector3 position, axis, velocity;
			float theta, phi;
			Math.GetSphericalDistribution(out phi, out theta, ref random);
			Math.ToCartesian(out position, phi, theta);
			Math.GetSphericalDistribution(out phi, out theta, ref random);
			Math.ToCartesian(out axis, phi, theta);
			Math.GetHemisphericalCosPoweredDistribution(out phi, out theta, 8f, ref random);
			Math.ToCartesian(out velocity, phi, theta);
			// yとzを交換して上向きに
			float tmp = velocity.y;
			velocity.y = velocity.z;
			velocity.z = tmp;
			Math.Mul(ref velocity, 5f); // TODO: 調整必要なら調整可能にする
			pieces[i].Init(
				ref position,
				ref axis,
				ref velocity,
				pieceLength,
				colorIndex);
		}
	}

	void Update()
	{
		float dt = Time.deltaTime;
		Vector3 accel;
		accel.x = -Mathf.Pow(10f, windLog);
		accel.y = -Mathf.Pow(10f, gravityLog);
		accel.z = 0f;
		float resistance = Mathf.Pow(10f, resistanceLog);
		particleMesh.SetMaterial(material);
		particleMesh.color = new Color32(255, 255, 255, 255);
		var halfWidth = pieceWidth * 0.5f;
		int pieceCount = (int)Mathf.Pow(10f, pieceCountLog);
		for (int i = 0; i < pieceCount; i++)
		{
			pieces[i].Update(dt, ref accel, resistance);
			pieces[i].Draw(particleMesh, halfWidth);
		}
		particleMesh.Update();
	}

	void OnGUI()
	{
		int pieceCount = (int)Mathf.Pow(10f, pieceCountLog);
		GUI.Label(new Rect(0f, 0f, 120f, 30f), "Count: " + pieceCount);
		pieceCountLog = GUI.HorizontalSlider(
			new Rect(120f, 0f, 500f, 30f),
			pieceCountLog,
			0f,
			4f);

		float gravity = Mathf.Pow(10f, gravityLog);
		GUI.Label(new Rect(0f, 30f, 120f, 30f), "Gravity: " + gravity.ToString("F2"));
		gravityLog = GUI.HorizontalSlider(
			new Rect(120f, 30f, 500f, 30f),
			gravityLog,
			-2f,
			2f);

		float wind = Mathf.Pow(10f, windLog);
		GUI.Label(new Rect(0f, 60f, 120f, 30f), "Wind: " + wind.ToString("F2"));
		windLog = GUI.HorizontalSlider(
			new Rect(120f, 60f, 500f, 30f),
			windLog,
			-2f,
			2f);

		float resistance = Mathf.Pow(10f, resistanceLog);
		GUI.Label(new Rect(0f, 90f, 120f, 30f), "Resistance: " + resistance.ToString("F2"));
		resistanceLog = GUI.HorizontalSlider(
			new Rect(120f, 90f, 500f, 30f),
			resistanceLog,
			-4f,
			0f);

		if (GUI.Button(new Rect(668f, 0f, 100f, 100f), "Emit"))
		{
			Emit();
		}
	}
}
