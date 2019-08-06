using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class Main : MonoBehaviour, IDragHandler, IPointerClickHandler
{
	[SerializeField] Transform cameraRotation;
	[SerializeField] Camera camera3d;
	[SerializeField] RawImage rawImage;
	[SerializeField] Shader[] shaders;
	[SerializeField] Text expButton;
	[SerializeField] Text heightUniformButton;
	[SerializeField] Text heightExpButton;
	[SerializeField] Text noneButton;
	[SerializeField] Text fpsText;
	[SerializeField] Text resolutionText;
	[SerializeField] Text densityText;
	[SerializeField] Text attenuationText;
	[SerializeField] Text cameraDistanceText;
	[SerializeField] Slider resolutionSlider;
	[SerializeField] Slider densitySlider;
	[SerializeField] Slider attenuationSlider;
	[SerializeField] Slider cameraDistanceSlider;
	[SerializeField] Toggle perVertexToggle;

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
	float cameraDistance;
	float density;
	float heightDensityAttenuation;
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
		noneButton.color = Color.green;
		SetMaterial();
	}

	void Update()
	{
		frameTimeWatcher.Update();
		fpsText.text = "FPS: " + frameTimeWatcher.fps.ToString("F1");

		float log, newLog;
		bool materialDirty = false;

		resolutionText.text = "Reso: " + resolutionRatio.ToString("F3");
		log = Mathf.Log10(resolutionRatio);
		newLog = resolutionSlider.value;
		if (newLog != log)
		{
			resolutionRatio = Mathf.Pow(10f, newLog);
			var sqrtRatio = Mathf.Sqrt(resolutionRatio);
			camera3d.rect = new Rect(0f, 0f, sqrtRatio, sqrtRatio);
			rawImage.uvRect = new Rect(0f, 0f, sqrtRatio, sqrtRatio);
		}

		densityText.text = "Density: " + density.ToString("F3");
		log = Mathf.Log10(density);
		newLog = densitySlider.value;
		if (newLog != log)
		{
			density = Mathf.Pow(10f, newLog);
			materialDirty = true;
		}

		attenuationText.text = "Attenuation: " + heightDensityAttenuation.ToString("F3");
		log = Mathf.Log10(heightDensityAttenuation);
		newLog = attenuationSlider.value;
		if (newLog != log)
		{
			heightDensityAttenuation = Mathf.Pow(10f, newLog);
			materialDirty = true;
		}

		cameraDistanceText.text = "Cam Distance: " + cameraDistance.ToString("F1");
		log = Mathf.Log10(cameraDistance);
		newLog = cameraDistanceSlider.value;
		if (newLog != log)
		{
			cameraDistance = Mathf.Pow(10f, newLog);
			camera3d.transform.localPosition = new Vector3(0f, 0f, -cameraDistance);
		}


		var newValue = perVertexToggle.isOn;
		if (newValue != perVertex)
		{
			perVertex = newValue;
			SetMaterial();
		}

		if (materialDirty)
		{
			foreach (var material in materials)
			{
				material.SetFloat("_FogDensity", density);
				material.SetFloat("_FogDensityAttenuation", heightDensityAttenuation);
			}
		}
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

	public void OnPointerClick(PointerEventData data)
	{
		Mode newMode = mode;
		if (data.rawPointerPress != null)
		{
			noneButton.color = Color.red;
			expButton.color = Color.red;
			heightExpButton.color = Color.red;
			heightUniformButton.color = Color.red;

			var name = data.rawPointerPress.name;
			if (name == "ExpButton")
			{
				newMode = Mode.Exp;
				expButton.color = Color.green;
			}
			else if (name == "HeightUniformButton")
			{
				newMode = Mode.HeightUniform;
				heightUniformButton.color = Color.green;
			}
			else if (name == "HeightExpButton")
			{
				newMode = Mode.HeightExp;
				heightExpButton.color = Color.green;
			}
			else if (name == "NoneButton")
			{
				newMode = Mode.None;
				noneButton.color = Color.green;
			}
			if (newMode != mode)
			{
				mode = newMode;
				SetMaterial();
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
