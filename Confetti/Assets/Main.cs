#define USE_2

using UnityEngine;
using Kayac;

public class Main : MonoBehaviour
{
	[SerializeField] Transform emitRoot;
	[SerializeField] MeshRenderer meshRenderer;
	[SerializeField] MeshFilter meshFilter;
	[SerializeField] Material material;
	[SerializeField] float pieceWidth = 0.3f;
	[SerializeField] float pieceLength = 0.6f;
	[SerializeField] float normalBendRatio = 0.5f;
	[SerializeField] int step = 1;

	ParticleMesh particleMesh;
	Piece[] pieces;
	float pieceCountLog = 0f;
	float gravityLog = 1f;
	float windLog = -2f;
	float resistanceLog = 1f;
	Random32 random;
	int capacity = 10000;

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
		Vector3 emitCenter = emitRoot.position;
		for (int i = 0; i < pieceCount; i++)
		{
			int colorIndex = random.GetInt(0, 7);
			Vector3 position, axis, velocity;
			float theta, phi;
			Math.GetSphericalDistribution(out phi, out theta, ref random);
			Math.ToCartesian(out position, phi, theta);
			Math.Add(ref position, ref emitCenter);
			Math.GetSphericalDistribution(out phi, out theta, ref random);
			Math.ToCartesian(out axis, phi, theta);
//axis.Set(1f, 0,0);
			Math.GetHemisphericalCosPoweredDistribution(out phi, out theta, 8f, ref random);
			Math.ToCartesian(out velocity, phi, theta);
			// yとzを交換して上向きに
			float tmp = velocity.y;
			velocity.y = velocity.z;
			velocity.z = tmp;
			Math.Mul(ref velocity, 10f); // TODO: 調整必要なら調整可能にする
			pieces[i].Init(
				ref position,
				ref axis,
				ref velocity,
				colorIndex);
		}
	}

	void Update()
	{
		Vector3 wind;
		wind.x = -Mathf.Pow(10f, windLog);
		wind.y = wind.z = 0f;
		var gravity = Mathf.Pow(10f, gravityLog);
		float resistance = Mathf.Pow(10f, resistanceLog);
		particleMesh.SetMaterial(material);
		particleMesh.color = new Color32(255, 255, 255, 255);
		int pieceCount = (int)Mathf.Pow(10f, pieceCountLog);
		float dt = Time.deltaTime / (float)step;
		float halfZSize = pieceLength * 0.5f;
		float halfXSize = pieceWidth * 0.5f;
		for (int stepIndex = 0; stepIndex < step; stepIndex++)
		{
			for (int i = 0; i < pieceCount; i++)
			{
				pieces[i].Update(dt, ref wind, gravity, resistance, halfZSize, halfXSize, normalBendRatio);
			}
		}
		for (int i = 0; i < pieceCount; i++)
		{
			pieces[i].Draw(particleMesh, halfZSize, halfXSize);
		}
		particleMesh.Update();
	}

	void OnGUI()
	{
		int pieceCount = (int)Mathf.Pow(10f, pieceCountLog);
		GUI.Label(new Rect(0f, 0f, 120f, 30f), "Count: " + pieceCount);
		pieceCountLog = GUI.HorizontalSlider(
			new Rect(120f, 0f, 200f, 30f),
			pieceCountLog,
			0f,
			4f);

		float gravity = Mathf.Pow(10f, gravityLog);
		GUI.Label(new Rect(0f, 30f, 120f, 30f), "Gravity: " + gravity.ToString("F2"));
		gravityLog = GUI.HorizontalSlider(
			new Rect(120f, 30f, 200f, 30f),
			gravityLog,
			-2f,
			2f);

		float wind = Mathf.Pow(10f, windLog);
		GUI.Label(new Rect(0f, 60f, 120f, 30f), "Wind: " + wind.ToString("F2"));
		windLog = GUI.HorizontalSlider(
			new Rect(120f, 60f, 200f, 30f),
			windLog,
			-2f,
			2f);

		float resistance = Mathf.Pow(10f, resistanceLog);
		GUI.Label(new Rect(0f, 90f, 120f, 30f), "Resistance: " + resistance.ToString("F2"));
		resistanceLog = GUI.HorizontalSlider(
			new Rect(120f, 90f, 200f, 30f),
			resistanceLog,
			-4f,
			1f);

		GUI.Label(new Rect(0f, 120f, 120f, 30f), "NormalBe d: " + normalBendRatio.ToString("F2"));
		normalBendRatio = GUI.HorizontalSlider(
			new Rect(120f, 120f, 200f, 30f),
			normalBendRatio,
			0f,
			1f);


		if (GUI.Button(new Rect(332f, 0f, 100f, 100f), "Emit"))
		{
			Emit();
		}
	}
}
