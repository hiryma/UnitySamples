using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Main : MonoBehaviour
/*
	, IPointerDownHandler
	, IPointerEnterHandler
	, IPointerExitHandler
	, IPointerUpHandler
	, IPointerClickHandler
	, IEndDragHandler
*/
	, IBeginDragHandler
	, IDragHandler
{
	[SerializeField] Text text;
	[SerializeField] Button button;
	[SerializeField] Slider slider;
	[SerializeField] Toggle toggle;
	[SerializeField] Image image;
	[SerializeField] Button debugTapToggleButton;
	[SerializeField] Kayac.DebugTapper debugTapper;

	string[] log;
	int logPosition;
	Vector2 dragStartScreenPosition;
	Vector2 imageDragStartPosition;
	System.Text.StringBuilder stringBuilder;

	void Start()
	{
		log = new string[16];
		stringBuilder = new System.Text.StringBuilder();
		button.onClick.AddListener(() =>
		{
			Log("button clicked.");
		});
		slider.onValueChanged.AddListener(value =>
		{
			Log("slider changed. value = " + value);
		});
		toggle.onValueChanged.AddListener(value =>
		{
			Log("toggle changed. value = " + value);
		});
		debugTapToggleButton.onClick.AddListener(() =>
		{
			debugTapper.enabled = !debugTapper.enabled;
			Log("auto tap on/off");
		});
	}

	void Log(string message)
	{
		log[logPosition] = message;
		logPosition++;
		if (logPosition == log.Length)
		{
			logPosition = 0;
		}

		stringBuilder.Clear();
		int index = logPosition;
		for (int i = 0; i < log.Length; i++)
		{
			if (log[index] != null)
			{
				stringBuilder.AppendLine(log[index]);
			}
			index++;
			if (index >= log.Length)
			{
				index = 0;
			}
		}
		text.text = stringBuilder.ToString();
	}
/*
	public void OnPointerDown(PointerEventData data)
	{
		Log("Down: " + data.pointerPressRaycast.gameObject.name);
	}

	public void OnPointerEnter(PointerEventData data)
	{
		Log("Enter: " + data.pointerCurrentRaycast.gameObject.name);
	}

	public void OnPointerExit(PointerEventData data)
	{
		Log("Exit: " + data.pointerCurrentRaycast.gameObject.name);
	}
	public void OnPointerUp(PointerEventData data)
	{
		Log("Up: " + data.pointerPressRaycast.gameObject.name);
	}

	public void OnPointerClick(PointerEventData data)
	{
		Log("Click: " + data.pointerPressRaycast.gameObject.name);
	}
*/

	public void OnBeginDrag(PointerEventData data)
	{
		dragStartScreenPosition = data.position;
		if (data.pointerPressRaycast.gameObject == image.gameObject)
		{
			Log("image drag start.");
			var worldPosition = image.transform.position;
			imageDragStartPosition = Camera.main.WorldToScreenPoint(worldPosition);
		}
	}

	public void OnDrag(PointerEventData data)
	{
		if (data.pointerPressRaycast.gameObject == image.gameObject)
		{
			var dragDiff = data.position - dragStartScreenPosition;
			var newImageScreenPosition = imageDragStartPosition + dragDiff;
			newImageScreenPosition.x = Mathf.Clamp(newImageScreenPosition.x, 0f, (float)Screen.width);
			newImageScreenPosition.y = Mathf.Clamp(newImageScreenPosition.y, 0f, (float)Screen.height);
			Vector2 localPoint;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				image.transform.parent as RectTransform,
				newImageScreenPosition,
				Camera.main,
				out localPoint);
			image.transform.localPosition = localPoint;
			Log("image drag. " + newImageScreenPosition);
		}
	}
/*
	public void OnEndDrag(PointerEventData data)
	{
		if (data.pointerPressRaycast.gameObject == image.gameObject)
		{
			Log("image drag end.");
		}
	}
*/
}
