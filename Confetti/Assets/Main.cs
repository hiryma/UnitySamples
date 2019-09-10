using UnityEngine;
using Kayac;

public class Main : MonoBehaviour
{
	[SerializeField] Camera camera;
	[SerializeField] Transform emitRoot;
	[SerializeField] float pieceWidth = 0.3f;
	[SerializeField] float pieceLength = 0.6f;
	[SerializeField] float normalBendRatio = 0.5f;
	[SerializeField] int step = 1;
	[SerializeField] ParticleMesh particleMesh;

	Piece[] pieces;
	float pieceCountLog = 0f;
	float gravityLog = 1f;
	float windLog = -2f;
	float resistanceLog = 1f;
	int capacity = 10000;

	void Start()
	{
		pieces = new Piece[capacity];
		Emit();
	}

	void Emit()
	{
		int pieceCount = (int)Mathf.Pow(10f, pieceCountLog);
		Vector3 emitCenter = emitRoot.position;
		for (int i = 0; i < pieceCount; i++)
		{
			Quaternion q;
			q.x = UnityEngine.Random.Range(-1f, 1f);
			q.y = UnityEngine.Random.Range(-1f, 1f);
			q.z = UnityEngine.Random.Range(-1f, 1f);
			q.w = UnityEngine.Random.Range(-1f, 1f);
			q.Normalize();
			Vector3 position;
			float theta, phi;
			Math.GetSphericalDistribution(out phi, out theta);
			Math.ToCartesian(out position, phi, theta);
			Math.Add(ref position, ref emitCenter);
			pieces[i].Init(
				position,
				q,
				new Vector2(UnityEngine.Random.value, 0f),
				pieceLength * 0.5f);
		}
	}

	void Update()
	{
		Vector3 wind;
		wind.x = -Mathf.Pow(10f, windLog);
		wind.y = wind.z = 0f;
		var gravity = new Vector3(0f, -Mathf.Pow(10f, gravityLog), 0f);
		float resistance = Mathf.Pow(10f, resistanceLog);
		int pieceCount = (int)Mathf.Pow(10f, pieceCountLog);
		float dt = Time.deltaTime / (float)step;
		float halfZSize = pieceLength * 0.5f;
		float halfXSize = pieceWidth * 0.5f;
		for (int stepIndex = 0; stepIndex < step; stepIndex++)
		{
			for (int i = 0; i < pieceCount; i++)
			{
				pieces[i].Update(dt, ref wind, ref gravity, resistance, halfZSize, halfXSize, normalBendRatio);
			}
		}
		for (int i = 0; i < pieceCount; i++)
		{
			pieces[i].Draw(particleMesh, halfZSize, halfXSize);
		}
		particleMesh.UpdateMesh();

Vector3 g = Vector3.zero;
for (int i = 0; i < pieceCount; i++)
{
	g += pieces[i].position;
}
g /= (float)pieceCount;
var c = new Vector3(g.x, g.y, g.z - 10f);
camera.transform.localPosition += (c - camera.transform.localPosition) * dt;
;
camera.transform.LookAt(g);
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
			-3f,
			2f);

		GUI.Label(new Rect(0f, 120f, 120f, 30f), "NormalBend: " + normalBendRatio.ToString("F2"));
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
