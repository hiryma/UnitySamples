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
	SampleWindow _sampleWindow;
	DebugUiMenu _menu;

	void Start()
	{
		_debugUi = DebugUiManager.Create(
			_camera,
			_textShader,
			_texturedShader,
			_font,
			referenceScreenWidth: 1024,
			referenceScreenHeight: 576,
			screenPlaneDistance: 100f,
			triangleCapacity: 8192);
		_sampleWindow = new SampleWindow(_debugUi);
		_debugUi.Add(_sampleWindow, 0, 0, DebugUi.AlignX.Right, DebugUi.AlignY.Bottom);

		_menu = new DebugUiMenu(100, 40);
		var subA = new DebugUiSubMenu("SubA", 100, 40, DebugUi.Direction.Down);
		subA.AddItem("A1", () => Debug.Log("A1"));
		subA.AddItem("A2", () => Debug.Log("A2"));
		var subB = new DebugUiSubMenu("SubA", 100, 40, DebugUi.Direction.Down);
		subB.AddItem("B1", () => Debug.Log("B1"));
		subB.AddItem("B2", () => Debug.Log("B2"));
		subA.AddSubMenu(subB, DebugUi.Direction.Right);
		_menu.AddSubMenu(subA, DebugUi.Direction.Down);
		_menu.AddItem("1", () => Debug.Log("1"));
		_menu.AddItem("2", () => Debug.Log("2"));
		_debugUi.Add(_menu, 0, 0);
	}

	void Update()
	{
		_debugUi.ManualUpdate(Time.deltaTime);
	}
}
