using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TouchDetector : Graphic
	, IPointerClickHandler
	, IPointerDownHandler
	, IBeginDragHandler
	, IDragHandler
{
	Vector2 _currentPointer;
	float _canvasWidth;
	float _pixelToMillimeter;

	public Vector2 drag{ get; private set; }
	public Vector2 dragMilliMeter{ get; private set; }
	public bool pointerDown{ get; private set; }
	public bool clicked{ get; private set; }

	public void Initialize(float canvasWidth)
	{
		_canvasWidth = canvasWidth;
		var dpi = Screen.dpi;
		if (dpi == 0)
		{
			Debug.LogWarning("Screen.dpi seems to be invalid. " + dpi);
			dpi = 160f;
		}
		_pixelToMillimeter = 25.4f / dpi;
	}

	public void ClearInput()
	{
		this.pointerDown = false;
		this.clicked = false;
		this.drag = Vector2.zero;
		this.dragMilliMeter = Vector2.zero;
	}

	public void OnBeginDrag(PointerEventData data)
	{
		_currentPointer = data.position;
	}

	public void OnDrag(PointerEventData data)
	{
		var dPixels = (data.position - _currentPointer);
		float toVirtualResolution = _canvasWidth / (float)Screen.width;
		this.drag = dPixels * toVirtualResolution;
		this.dragMilliMeter = dPixels * _pixelToMillimeter;
		_currentPointer = data.position;
	}

	public void OnPointerClick(PointerEventData data)
	{
		this.clicked = true;
	}

	public void OnPointerDown(PointerEventData data)
	{
		this.pointerDown = true;
	}
}
