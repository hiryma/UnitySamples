using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sample : MonoBehaviour
{
	public RectTransform _rootTransform;
	public DragDetector _dragDetector;

	void Start()
	{
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
