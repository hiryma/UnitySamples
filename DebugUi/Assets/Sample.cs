using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kayac;

public class Sample : MonoBehaviour
{
	[SerializeField]
	GameObject _gameObjectForDebugUiManager;
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
		_renderer = new DebugPrimitiveRenderer2D(
			_textShader,
			_texturedShader,
			_font,
			_camera,
			capacity: 8192);
		_debugUi = DebugUiManager.Create(
			_gameObjectForDebugUiManager,
			_renderer);
		_sampleWindow = new SampleWindow(_debugUi);
		_debugUi.Add(_sampleWindow);
	}

	void Update()
	{
		_debugUi.ManualUpdate(Time.deltaTime);
	}

	void LateUpdate()
	{
		_renderer.LateUpdate();
	}
}
