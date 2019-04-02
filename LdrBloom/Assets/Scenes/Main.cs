using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
	[SerializeField]
	Kayac.LightPostProcessor _postProcess;
	[SerializeField]
	GameObject _spherePrefab;
	[SerializeField]
	GameObject _cylinderPrefab;
	[SerializeField]
	Transform _objectsRoot;
	[SerializeField]
	Shader _shader;
	[SerializeField]
	UnityEngine.UI.Slider _speedSlider;
	[SerializeField]
	UnityEngine.UI.Slider _scaleSlider;
	[SerializeField]
	FillRenderer _fillRenderer;
	[SerializeField]
	UnityEngine.UI.Text _text;
	[SerializeField]
	UnityEngine.UI.Toggle _benchmarkToggle;
	[SerializeField]
	UnityEngine.UI.Toggle _enableToggle;
	[SerializeField]
	UnityEngine.UI.Toggle _enableStdToggle;
	[SerializeField]
	UnityEngine.UI.Toggle _24fpsToggle;
	[SerializeField]
	UnityEngine.UI.Toggle _logToggle;
	[SerializeField]
	UnityEngine.UI.Text _logText;
	[SerializeField]
	UnityEngine.Rendering.PostProcessing.PostProcessLayer _stdPostProcessLayer;

	const int SphereCount = 100;
	const int CylinderCount = 100;
	const float WorldSize = 10f;

	GameObject[] _objects;
	Vector3[] _velocities;
	Vector3[] _angularVelocities;
	Material _material;

	float[] _times;
	int _timeIndex;
	float _count;
	float _countVelocity;
	Kayac.DebugSlack _slack;
	Kayac.MemoryLogHandler _log;

	void Start()
	{
		_logText.text = "Start Called.";
		_log = new Kayac.MemoryLogHandler(1000);
		_logText.text = "Log Initialized.";

#if !UNITY_WEBGL //WebGLではSlack初期化しない。なので叩くと死ぬ。
		StartCoroutine(CoSetupSlack());
#endif

		_benchmarkToggle.onValueChanged.AddListener(toggle =>
		{
			if (_benchmarkToggle.isOn)
			{
				_count = 0f; //1から始めることでだいぶ収束が速くなる。TODO: 速度も適切な値がありそう。
			}
			else
			{
				_count = 0f;
			}
			_countVelocity = 0f;
		});

		_speedSlider.value = 0.5f;
		_objects = new GameObject[SphereCount + CylinderCount];
		_velocities = new Vector3[_objects.Length];
		_angularVelocities = new Vector3[_objects.Length];
		_material = new Material(_shader);
		for (int i = 0; i < SphereCount; i++)
		{
			_objects[i] = Instantiate(_spherePrefab, _objectsRoot, false);
		}
		for (int i = 0; i < CylinderCount; i++)
		{
			_objects[SphereCount + i] = Instantiate(_cylinderPrefab, _objectsRoot, false);
		}
		var block = new MaterialPropertyBlock();
		var propertyId = Shader.PropertyToID("_Color");
		for (int i = 0; i < _objects.Length; i++)
		{
			var renderer = _objects[i].GetComponent<MeshRenderer>();
			renderer.sharedMaterial = _material;
#if true // 動かなくして中央に置きたい時もある
			block.SetColor(propertyId, new Color(
				Random.Range(-1f, 1f),
				Random.Range(-1f, 1f),
				Random.Range(-1f, 1f),
				1f));
			renderer.SetPropertyBlock(block);
			_objects[i].transform.localPosition = new Vector3(
				Random.Range(-WorldSize, WorldSize),
				Random.Range(-WorldSize, WorldSize),
				Random.Range(-WorldSize, WorldSize));
			_objects[i].transform.localRotation = Quaternion.Euler(
				Random.Range(-180f, 180f),
				Random.Range(-180f, 180f),
				Random.Range(-180f, 180f));
			_velocities[i] = new Vector3(
				Random.Range(-1f, 1f),
				Random.Range(-1f, 1f),
				Random.Range(-1f, 1f));
			_angularVelocities[i] = new Vector3(
				Random.Range(-10f, 10f),
				Random.Range(-10f, 10f),
				Random.Range(-10f, 10f));
#endif
		}
		// 以下ベンチマーク準備
		_times = new float[60];
		_fillRenderer.ManualStart();
		_logText.text = "Start Finished.";
	}

	IEnumerator CoSetupSlack()
	{
		var tokenFilePath = Application.streamingAssetsPath + "/slackToken.txt";
		if (tokenFilePath.IndexOf("file:///") < 0)
		{
			tokenFilePath = "file://" + tokenFilePath;
		}
		var req = UnityEngine.Networking.UnityWebRequest.Get(tokenFilePath);
		req.SendWebRequest();
		yield return req;
		if (req.error != null)
		{
			Debug.LogError(req.error);
		}
		else
		{
			var token = req.downloadHandler.text;
			_slack = new Kayac.DebugSlack(token, "unity-debug");
		}
	}

	public void OnClickSendRtButton()
	{
		StartCoroutine(CoSendRenderTargets());
	}

	IEnumerator CoSendRenderTargets()
	{
		yield return new WaitForEndOfFrame();
		yield return _slack.CoPostTextures(
			_postProcess.EnumerateRenderTexturesForDebug());
	}

	public void OnClickSaveRtButton()
	{
		StartCoroutine(CoSaveRenderTargets());
	}

	public void OnClickSendLogButton()
	{
		StartCoroutine(_slack.CoPostBinary(_log.GetBytes(), "log.txt"));
	}

	IEnumerator CoSaveRenderTargets()
	{
		yield return new WaitForEndOfFrame();
		foreach (var rt in _postProcess.EnumerateRenderTexturesForDebug())
		{
			Debug.Log("Saving: " + rt.name);
			var bytesList = Kayac.TextureUtil.ConvertAllLevelToFile(rt);
			for (int level = 0; level < bytesList.Length; level++)
			{
				var name = rt.name;
				if (bytesList.Length > 1)
				{
					name += "_" + level;
				}
				name += ".png";
				System.IO.File.WriteAllBytes(name, bytesList[level]);
			}
		}
		Debug.Log("Done");
	}

	void Update()
	{
if (Input.GetKeyDown(KeyCode.S))
{
	StartCoroutine(_slack.CoPostScreenshot(null, null, null, null, 0, false, Kayac.TextureUtil.FileType.Jpeg));
}

if (Input.GetKeyDown(KeyCode.F1))
{
	_postProcess.bloomCombineStartLevel -= 1;
	Debug.Log(_postProcess.bloomCombineStartLevel);
}
if (Input.GetKeyDown(KeyCode.F2))
{
	_postProcess.bloomCombineStartLevel += 1;
	Debug.Log(_postProcess.bloomCombineStartLevel);
}


		_postProcess.enabled = _enableToggle.isOn;
		_stdPostProcessLayer.enabled = _enableStdToggle.isOn;
		float speed = _speedSlider.value;
		speed = Mathf.Pow(10f, (speed - 0.5f) * 3);
		float dt = Time.deltaTime * speed;
		for (int i = 0; i < _objects.Length; i++)
		{
			Update(i, dt);
		}
		// 以下ベンチマーク
		_times[_timeIndex] = Time.realtimeSinceStartup;
		_timeIndex++;
		if (_timeIndex >= _times.Length)
		{
			_timeIndex = 0;
		}
		var latest = ((_timeIndex - 1) < 0) ? (_times.Length - 1) : (_timeIndex - 1);
		var avg = (_times[latest] - _times[_timeIndex]) / (_times.Length - 1);
		_text.text = "FrameTime: " + (avg * 1000f).ToString("N2") + "\nCount: " + _count.ToString("N2") + " " + _postProcess.bloomCombineStartLevel;

		// ベンチマーク中は物描かない
		_objectsRoot.gameObject.SetActive(!_benchmarkToggle.isOn);
		if (_benchmarkToggle.isOn)
		{
			var targetMs = _24fpsToggle.isOn ? (1000f / 24f) : (1000f / 40f);
			var accel = (((targetMs * 0.001f) - Time.unscaledDeltaTime) * (_count + 1f) * 0.1f) - (_countVelocity * 0.5f);
			_countVelocity += accel;
			_count += _countVelocity;
			_count = Mathf.Clamp(_count, 0f, 10000f);
			_fillRenderer.SetCount(_count);
		}
		else
		{
			_fillRenderer.SetCount(0);
		}
		_fillRenderer.ManualUpdate();
		if (_logToggle.isOn)
		{
			_logText.text = _log.Tail(10);
		}
		_logText.enabled = _logToggle.isOn;
	}

	void Update(int i, float dt)
	{
		var o = _objects[i];
		var pos = o.transform.localPosition;
		pos += _velocities[i] * dt;
		o.transform.localPosition = pos;
		_velocities[i].x = Bound(_velocities[i].x, pos.x);
		_velocities[i].y = Bound(_velocities[i].y, pos.y);
		_velocities[i].z = Bound(_velocities[i].z, pos.z);
		o.transform.localScale = new Vector3(_scaleSlider.value, _scaleSlider.value, _scaleSlider.value);

		var rot = o.transform.localRotation.eulerAngles;
		rot += _angularVelocities[i] * dt;
		o.transform.localRotation = Quaternion.Euler(rot);
	}

	float Bound(float velocity, float position)
	{
		if (position > WorldSize)
		{
			return -Mathf.Abs(velocity);
		}
		else if (position < -WorldSize)
		{
			return Mathf.Abs(velocity);
		}
		else
		{
			return velocity;
		}
	}
}
