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
	[SerializeField] Text strengthText;
	[SerializeField] Text attenuationText;
	[SerializeField] Text cameraDistanceText;
	[SerializeField] Slider resolutionSlider;
	[SerializeField] Slider strengthSlider;
	[SerializeField] Slider attenuationSlider;
	[SerializeField] Slider cameraDistanceSlider;
	[SerializeField] Toggle perVertexToggle;
	[SerializeField] Toggle imageShowToggle;

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
	float strength;
	float attenuation;
	float resolutionRatio;
	Kayac.FrameTimeWatcher frameTimeWatcher;

	void Start()
	{
		var vmin = Mathf.Min(Screen.width, Screen.height);
		var ratio = vmin / 4096f;
		resolutionSlider.value = Mathf.Log10(ratio * ratio);

		cameraDistance = Mathf.Pow(10f, cameraDistanceSlider.value);
		strength = Mathf.Pow(10f, strengthSlider.value);
		attenuation = Mathf.Pow(10f, attenuationSlider.value);
		resolutionRatio = Mathf.Pow(10f, resolutionSlider.value);
		ChangeResolution();

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

	void ChangeResolution()
	{
		var sqrtRatio = Mathf.Sqrt(resolutionRatio);
		camera3d.rect = new Rect(0f, 0f, sqrtRatio, sqrtRatio);
		var vmin = Mathf.Min(Screen.width, Screen.height);
		var ratio = Mathf.Min(vmin / 4096f, sqrtRatio);
		rawImage.uvRect = new Rect(0f, 0f, ratio, ratio);
	}

	void Update()
	{
		frameTimeWatcher.Update();
		fpsText.text = "Time: " + (frameTimeWatcher.averageFrameTime / 1024f).ToString("F1");

		float log, newLog;
		bool materialDirty = false;

		var sqrtRatio = Mathf.Sqrt(resolutionRatio);
		var edge = sqrtRatio * 4096f;
		resolutionText.text = "Reso: " + edge.ToString("F0") + " (" + resolutionRatio.ToString("F3") + ")";
		log = Mathf.Log10(resolutionRatio);
		newLog = resolutionSlider.value;
		if (newLog != log)
		{
			resolutionRatio = Mathf.Pow(10f, newLog);
			ChangeResolution();
		}

		strengthText.text = "Strength: " + strength.ToString("F1");
		log = Mathf.Log10(strength);
		newLog = strengthSlider.value;
		if (newLog != log)
		{
			strength = Mathf.Pow(10f, newLog);
			materialDirty = true;
		}

		attenuationText.text = "Attenuation: " + attenuation.ToString("F1");
		log = Mathf.Log10(attenuation);
		newLog = attenuationSlider.value;
		if (newLog != log)
		{
			attenuation = Mathf.Pow(10f, newLog);
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

		rawImage.enabled = imageShowToggle.isOn;

		if (materialDirty)
		{
			UpdateMaterialParams();
		}
	}

	void UpdateMaterialParams()
	{
		// 50%になる距離 → 1mあたりの量(フォグ密度、密度の減衰)
		var log05 = Mathf.Log(0.5f);
		var density = 1f - Mathf.Exp(log05 / strength);
		var densityAttenuation = 1f - Mathf.Exp(log05 / attenuation);
		foreach (var material in materials)
		{
			material.SetFloat("_FogDensity", density);
			material.SetFloat("_FogDensityAttenuation", densityAttenuation);
			material.SetFloat("_FogEndHeight", attenuation * 2f); // 仮にこれを入れておく
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
		UpdateMaterialParams();
	}

	public void OnPointerClick(PointerEventData data)
	{
		Mode newMode = mode;
		if (data.rawPointerPress != null)
		{
			var name = data.rawPointerPress.name;
			if (name == "ExpButton")
			{
				newMode = Mode.Exp;
				expButton.color = Color.green;
				noneButton.color = Color.red;
				heightExpButton.color = Color.red;
				heightUniformButton.color = Color.red;
			}
			else if (name == "HeightUniformButton")
			{
				newMode = Mode.HeightUniform;
				heightUniformButton.color = Color.green;
				noneButton.color = Color.red;
				expButton.color = Color.red;
				heightExpButton.color = Color.red;
			}
			else if (name == "HeightExpButton")
			{
				newMode = Mode.HeightExp;
				heightExpButton.color = Color.green;
				noneButton.color = Color.red;
				expButton.color = Color.red;
				heightUniformButton.color = Color.red;
			}
			else if (name == "NoneButton")
			{
				newMode = Mode.None;
				noneButton.color = Color.green;
				expButton.color = Color.red;
				heightExpButton.color = Color.red;
				heightUniformButton.color = Color.red;
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
