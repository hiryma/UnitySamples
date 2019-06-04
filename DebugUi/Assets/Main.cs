using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kayac;

public class Main : MonoBehaviour
{
	[SerializeField]
	Shader _textShader;
	[SerializeField]
	Shader _texturedShader;
	[SerializeField]
	Font _font;
	[SerializeField]
	Camera _camera;

	DebugUiManager _debugUi;
	DebugPrimitiveRenderer2D _renderer;
	SampleWindow _sampleWindow;

	void Start()
	{
		_debugUi = DebugUiManager.Create(
			_camera,
			_textShader,
			_texturedShader,
			_font,
			referenceScreenWidth: 1136,
			referenceScreenHeight: 640,
			screenPlaneDistance: 100f,
			triangleCapacity: 8192);
		_sampleWindow = new SampleWindow(_debugUi);
		_debugUi.Add(_sampleWindow);
	}

	void Update()
	{
		_debugUi.ManualUpdate(Time.deltaTime);
	}
}
