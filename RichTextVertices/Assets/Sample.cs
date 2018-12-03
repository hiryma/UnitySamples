using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Sample : MonoBehaviour
{
	public Text textPrefab;
	public Image imagePrefab;
	public Canvas canvas;
	public bool setAll;
	public bool setText;
	public bool setPosition;
	public bool setRotation;
	public bool setScale;
	public bool setSizeDelta;
	public bool useRichText;
	public bool insertImage;
	public int graphicCount = 100;

	Graphic[] _graphics;
	int _frame;

	void Start()
	{
		string str = null;
		if (useRichText)
		{
			str = "<color=#ff0000>赤</color>";
		}
		else
		{
			str = "少し長めにしてみたよ!!";
		}

		_graphics = new Graphic[graphicCount];
		var parent = canvas.gameObject.transform;
		for (int i = 0; i < graphicCount; i++)
		{
			if (!insertImage || ((i % 2) == 0))
			{
				var text = Instantiate(textPrefab, parent, false);
				text.text = str;
				_graphics[i] = text;
			}
			else
			{
				_graphics[i] = Instantiate(imagePrefab, parent, false);
			}
		}
	}
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.F1)) // Outlineの有効無効
		{
			foreach (var graphic in _graphics)
			{
				var text = graphic as Text;
				if (text != null)
				{
					var outline = text.gameObject.GetComponent<Outline>();
					outline.enabled = !outline.enabled;
				}
			}
		}

		if (Input.GetKeyDown(KeyCode.F2)) // DegenerateQuadRemoverの有効無効
		{
			foreach (var graphic in _graphics)
			{
				var text = graphic as Text;
				if (text != null)
				{
					var remover = text.gameObject.GetComponent<Kayac.DegenerateQuadRemover>();
					remover.enabled = !remover.enabled;
				}
			}
		}

		string str = null;
		if (setText)
		{
			if (useRichText)
			{
				str = "<color=#ff0000>" + _frame.ToString("D5") + "</color>";
			}
			else
			{
				str = _frame.ToString("D5");
			}
		}

		int count = setAll ? _graphics.Length : 1;

		for (int i = 0; i < count; i++)
		{
			var graphic = _graphics[i];
			var transform = graphic.rectTransform;

			if (setText)
			{
				var text = graphic as Text;
				if (text != null)
				{
					text.text = str;
				}
			}

			if (setPosition)
			{
				var pos = transform.anchoredPosition;
				pos.x = UnityEngine.Random.Range(-320f, 320f);
				pos.y = UnityEngine.Random.Range(-240f, 240f);
				transform.anchoredPosition = pos;
			}

			if (setRotation)
			{
				var rot = transform.localRotation;
				rot = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
				transform.localRotation = rot;
			}

			if (setScale)
			{
				var scale = transform.localScale;
				scale.x = UnityEngine.Random.Range(0f, 1f);
				scale.y = UnityEngine.Random.Range(0f, 1f);
				transform.localScale = scale;
			}

			if (setSizeDelta)
			{
				var size = transform.sizeDelta;
				size.x = UnityEngine.Random.Range(50f, 300f);
				size.y = UnityEngine.Random.Range(50f, 300f);
				transform.sizeDelta = size;
			}
		}
		_frame++;
	}
}
