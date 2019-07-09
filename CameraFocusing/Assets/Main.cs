using UnityEngine;

public class Main : MonoBehaviour
{
	[SerializeField] Transform spherePrefab;
	[SerializeField] Camera mainCamera;

	CameraController cameraController;
	Transform[] spheres;
	Vector3[] velocities;
	Vector3[] positions;

	void Start()
	{
		spheres = new Transform[16];
		velocities = new Vector3[spheres.Length];
		positions = new Vector3[spheres.Length];
		for (int i = 0; i < spheres.Length; i++)
		{
			spheres[i] = Instantiate(spherePrefab, gameObject.transform, false);
		}
		InitSpheres();
		cameraController = new CameraController(mainCamera);
		cameraController.Stiffness = 8f;
	}

	// Update is called once per frame
	void Update()
	{
		for (int i = 0; i < spheres.Length; i++)
		{
			velocities[i] *= 0.99f;
			velocities[i] += new Vector3(
				Random.Range(-50f, 50f),
				0f,
				Random.Range(-50f, 50f)) * Time.deltaTime;
			velocities[i] -= positions[i] * 0.5f * Time.deltaTime;
			positions[i] += velocities[i] * Time.deltaTime;
			spheres[i].localPosition = positions[i];
		}
		cameraController.FitByMove(positions);
		cameraController.ManualUpdate(Time.deltaTime);
	}

	void InitSpheres()
	{
		for (int i = 0; i < spheres.Length; i++)
		{
			positions[i] = new Vector3(
				Random.Range(-5f, 5f),
				0f,
				Random.Range(-5f, 5f));
			velocities[i] = new Vector3(
				Random.Range(-5f, 5f),
				0f,
				Random.Range(-5f, 5f));
		}
	}

	void OnGUI()
	{
		GUILayout.Label("Camera: " + mainCamera.transform.position);
		if (GUILayout.Button("Reset"))
		{
			InitSpheres();
		}
	}
}
