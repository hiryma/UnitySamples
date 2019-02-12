using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sample : MonoBehaviour
{
	public RectTransform _rootTransform;
	public DragDetector _dragDetector;

	void Start()
	{
for (int i = 0; i <= 255; i++)
{
	// [0,255] -> [0,3]
	// i * 3 / 255 + 255/510
	// ( i * 6 + 255 )/510
	var r = (i * 6) + 255;
//	r = Mathf.Clamp(r, 0, 31);
Debug.LogWarning(i + " " + r + " " + r/510);
}
		_dragDetector.onDrag = (delta) =>
		{
			var pos = _rootTransform.anchoredPosition;
			pos += delta;
			_rootTransform.anchoredPosition = pos;
		};
	}

	void OnGUI()
	{
		var scale = _rootTransform.localScale;
		if (GUILayout.RepeatButton("ZoomIn"))
		{
			scale *= 1.01f;
		}
		if (GUILayout.RepeatButton("ZoomOut"))
		{
			scale *= 0.99f;
		}
		_rootTransform.localScale = scale;
	}
}
