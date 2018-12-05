using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

	void Start()
	{
		Apply();
	}

	void Apply()
	{
		// エディタ拡張経由で呼ばれた場合、Screen.widthがInspectorウィンドウの寸法を返す。なので、全画面とわかっているcanvasから寸法をもらう。
		// https://forum.unity.com/threads/screen-width-is-wrong-in-editor-mode.94572/ で言われている問題が2017.4ではまだ直っていない。もはや仕様と考えた方がいいだろう。
		var canvasSize = canvasRectTransform.sizeDelta;

		Kayac.RectTransformScaler.SetNormalizedMargin(
			marginLeft / canvasSize.x,
			marginRight / canvasSize.x,
			marginTop / canvasSize.y,
			marginBottom / canvasSize.y);
		leftMarginImage.rectTransform.sizeDelta = new Vector2(marginLeft, 0f);
		rightMarginImage.rectTransform.sizeDelta = new Vector2(marginRight, 0f);
		topMarginImage.rectTransform.sizeDelta = new Vector2(0f, marginTop);
		bottomMarginImage.rectTransform.sizeDelta = new Vector2(0f, marginBottom);
#if UNITY_EDITOR
		Kayac.RectTransformScaler.ApplyRecursive(canvasRectTransform);
#endif
	}

#if UNITY_EDITOR
	[CustomEditor (typeof(Sample))]
	public class SampleInspector : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			var self = (Sample)target;
			if (GUILayout.Button("Apply"))
			{
				self.Apply();
			}
		}
	}
#endif
}
