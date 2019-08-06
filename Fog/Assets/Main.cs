using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class Main : MonoBehaviour, IDragHandler
{
	[SerializeField] Transform cameraRotation;
	[SerializeField] Camera camera3d;
	[SerializeField] RawImage rawImage;
	[SerializeField] Shader[] shaders;
	[SerializeField] Text fpsText;

	enum Mode
	{
		Exp,
		HeightUniform,
		HeightExp,
		None,
	}
	bool perVertex;
	Mode mode;
	Material[] materials;
	float xAngle = 40f;
	float yAngle = 0f;
	float cameraDistance = 10f;
	float density = 0.1f;
	float heightDensityAttenuation = 0.1f;
	Kayac.FrameTimeWatcher frameTimeWatcher;
	float resolutionRatio = 1f;

	void Start()
	{
		Application.targetFrameRate = 1000;
		frameTimeWatcher = new Kayac.FrameTimeWatcher();
		mode = Mode.None;
		materials = new Material[shaders.Length];
		for (int i = 0; i < shaders.Length; i++)
		{
			materials[i] = new Material(shaders[i]);
		}
		SetMaterial();
	}

	void Update()
	{
		frameTimeWatcher.Update();
		fpsText.text = "FPS: " + frameTimeWatcher.fps;
	}

	void SetMaterial()
	{
		var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
		var roots = scene.GetRootGameObjects();
		var materialIndex = (int)mode * 2;
		if (mode != Mode.None)
		{
			if (perVertex)
			{
				materialIndex++;
			}
		}
		foreach (var root in roots)
		{
			var components = root.GetComponentsInChildren<MeshRenderer>();
			foreach (var component in components)
			{
				component.sharedMaterial = materials[materialIndex];
			}
		}
	}

	void OnGUI()
	{
		var changed = false;
		var names = System.Enum.GetNames(typeof(Mode));
		var newMode = GUI.SelectionGrid(new Rect(0, 0, 160f, 128f), (int)mode, names, 1);

		GUI.Label(new Rect(256f, 0f, 128f, 32f), "Density: " + density.ToString("F3"));
		var log = Mathf.Log10(density);
		var newLog = GUI.HorizontalSlider(new Rect(378f, 0f, 128f, 32f), log, -3f, 1f);
		if (newLog != log)
		{
			changed = true;
			log = newLog;
			density = Mathf.Pow(10f, log);
		}

		GUI.Label(new Rect(256f, 32f, 128f, 32f), "Height Attn: " + heightDensityAttenuation.ToString("F3"));
		log = Mathf.Log10(heightDensityAttenuation);
		newLog = GUI.HorizontalSlider(new Rect(378f, 32f, 128f, 32f), log, -3f, 1f);
		if (newLog != log)
		{
			changed = true;
			log = newLog;
			heightDensityAttenuation = Mathf.Pow(10f, log);
		}

		GUI.Label(new Rect(256f, 64f, 128f, 32f), "Cam Distance: " + cameraDistance.ToString("F1"));
		log = Mathf.Log10(cameraDistance);
		newLog = GUI.HorizontalSlider(new Rect(378, 64f, 128f, 32f), log, 0f, 3f);
		if (newLog != log)
		{
			changed = true;
			log = newLog;
			cameraDistance = Mathf.Pow(10f, log);
			camera3d.transform.localPosition = new Vector3(0f, 0f, -cameraDistance);
		}

		GUI.Label(new Rect(256f, 96f, 128f, 32f), "Reso Ratio: " + resolutionRatio.ToString("F3"));
		log = Mathf.Log10(resolutionRatio);
		newLog = GUI.HorizontalSlider(new Rect(378, 96f, 128f, 32f), log, -3f, 0f);
		if (newLog != log)
		{
			log = newLog;
			resolutionRatio = Mathf.Pow(10f, log);
			var sqrtRatio = Mathf.Sqrt(resolutionRatio);
			camera3d.rect = new Rect(0f, 0f, sqrtRatio, sqrtRatio);
			rawImage.uvRect = new Rect(0f, 0f, sqrtRatio, sqrtRatio);
		}

		var newPerVertex = GUI.Toggle(new Rect(0f, 128f, 128f, 32f), perVertex, "perVertex");

		if ((newMode != (int)mode) || (newPerVertex != perVertex))
		{
			changed = true;
			mode = (Mode)newMode;
			perVertex = newPerVertex;;
			SetMaterial();
		}

		if (changed)
		{
			foreach (var material in materials)
			{
				material.SetFloat("_FogDensity", density);
				material.SetFloat("_FogDensityAttenuation", heightDensityAttenuation);
			}
		}
	}

	public void OnDrag(PointerEventData data)
	{
		var scale = 1f / Mathf.Min(Screen.width, Screen.height);
		xAngle -= data.delta.y * Time.deltaTime * scale * 2000f;
		yAngle += data.delta.x * Time.deltaTime * scale * 2000f;
		cameraRotation.transform.localRotation = Quaternion.Euler(xAngle, yAngle, 0f);
	}
}
