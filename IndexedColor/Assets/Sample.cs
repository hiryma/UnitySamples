using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sample : MonoBehaviour
{
	[SerializeField]
	Transform _scaler;

	Vector3 _prevMousePos;

	void OnGUI()
	{
		if (GUILayout.RepeatButton("zoomIn"))
		{
			float s = _scaler.localScale.x;
			s *= 1.02f;
			_scaler.localScale = new Vector3(s, s, 1f);
		}
		if (GUILayout.RepeatButton("zoomOut"))
		{
			float s = _scaler.localScale.x;
			s *= 0.98f;
			_scaler.localScale = new Vector3(s, s, 1f);
		}
		var mousePos = Input.mousePosition;
		if (Input.GetMouseButton(0))
		{
			var dx = mousePos.x - _prevMousePos.x;
			var dy = mousePos.y - _prevMousePos.y;
			var pos = _scaler.localPosition;
			pos.x += dx * 1f;
			pos.y += dy * 1f;
			_scaler.localPosition = pos;
		}
		_prevMousePos = mousePos;
	}
}
