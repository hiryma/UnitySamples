using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Sample : MonoBehaviour
{
	public Image leftMarginImage;
	public Image rightMarginImage;
	public Image topMarginImage;
	public Image bottomMarginImage;
	public RectTransform canvasRectTransform;
	public float marginLeft;
	public float marginRight;
	public float marginTop;
	public float marginBottom;
	public bool updateSwitch;

	void Update()
	{
		if (updateSwitch)
		{
			updateSwitch = false;
			Kayac.RectTransformScaler.SetDefaults(
				1280f,
				720f,
				marginLeft / Screen.width,
				marginRight / Screen.width,
				marginTop / Screen.height,
				marginBottom / Screen.height);
			leftMarginImage.rectTransform.sizeDelta = new Vector2(marginLeft, 0f);
			rightMarginImage.rectTransform.sizeDelta = new Vector2(marginRight, 0f);
			topMarginImage.rectTransform.sizeDelta = new Vector2(0f, marginTop);
			bottomMarginImage.rectTransform.sizeDelta = new Vector2(0f, marginBottom);
#if UNITY_EDITOR
			Kayac.RectTransformScaler.ApplyRecursive(canvasRectTransform);
#endif
		}
	}
}
