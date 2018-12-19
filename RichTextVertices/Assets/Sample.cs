using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Sample : MonoBehaviour
{
	public Text textPrefab;
	public Image imagePrefab;
	public Sprite[] sprites;
	public Canvas canvas;
	public int graphicCount = 100;
	public int spriteCount = int.MaxValue;

	bool _setAll = true;
	bool _setText;
	bool _setGraphicColor;
	bool _setCanvasRendererColor;
	bool _setCanvasRendererAlpha;
	bool _setPosition;
	bool _setRotation;
	bool _setScale;
	bool _setSizeDelta;
	bool _useRichText = true;
	bool _insertImage = true;
	bool _outLine = true;
	bool _degenerationRemoval = true;

	bool _uiEnabled = true;
	Graphic[] _graphics;
	int _frame;

	void OnGUI()
	{
		if (!_uiEnabled)
		{
			return;
		}
		if (GUILayout.Toggle(_setAll, "setAll") != _setAll)
		{
			_setAll = !_setAll;
		}
		if (GUILayout.Toggle(_setText, "setText") != _setText)
		{
			_setText = !_setText;
		}
		if (GUILayout.Toggle(_setGraphicColor, "setGraphicColor") != _setGraphicColor)
		{
			_setGraphicColor = !_setGraphicColor;
		}
		if (GUILayout.Toggle(_setCanvasRendererColor, "setCanvasRendererColor") != _setCanvasRendererColor)
		{
			_setCanvasRendererColor = !_setCanvasRendererColor;
		}
		if (GUILayout.Toggle(_setCanvasRendererAlpha, "setCanvasRendererAlpha") != _setCanvasRendererAlpha)
		{
			_setCanvasRendererAlpha = !_setCanvasRendererAlpha;
		}
		if (GUILayout.Toggle(_setPosition, "setPosition") != _setPosition)
		{
			_setPosition = !_setPosition;
		}
		if (GUILayout.Toggle(_setRotation, "setRotation") != _setRotation)
		{
			_setRotation = !_setRotation;
		}
		if (GUILayout.Toggle(_setScale, "setScale") != _setScale)
		{
			_setScale = !_setScale;
		}
		if (GUILayout.Toggle(_setSizeDelta, "setSizeDelta") != _setSizeDelta)
		{
			_setSizeDelta = !_setSizeDelta;
		}
		if (GUILayout.Toggle(_useRichText, "useRichText") != _useRichText)
		{
			_useRichText = !_useRichText;
			Reset();
		}
		if (GUILayout.Toggle(_insertImage, "insertImage") != _insertImage)
		{
			_insertImage = !_insertImage;
			Reset();
		}
		if (GUILayout.Toggle(_outLine, "outLine") != _outLine)
		{
			_outLine = !_outLine;
			foreach (var graphic in _graphics)
			{
				var text = graphic as Text;
				if (text != null)
				{
					var component = text.gameObject.GetComponent<Outline>();
					component.enabled = _outLine;
				}
			}
		}
		if (GUILayout.Toggle(_degenerationRemoval, "degenerationRemoval") != _degenerationRemoval)
		{
			_degenerationRemoval = !_degenerationRemoval;
			foreach (var graphic in _graphics)
			{
				var text = graphic as Text;
				if (text != null)
				{
					var remover = text.gameObject.GetComponent<Kayac.DegenerateQuadRemover>();
					remover.enabled = _degenerationRemoval;
				}
			}
		}
	}

	void Start()
	{
		string str = null;
		if (_useRichText)
		{
			str = "<color=#ff0000>赤</color>";
		}
		else
		{
			str = "少し長めにしてみたよ!!";
		}

		_graphics = new Graphic[graphicCount];
		var parent = canvas.gameObject.transform;
		var spriteIndex = 0;
		for (int i = 0; i < graphicCount; i++)
		{
			if (!_insertImage || ((i % 2) == 0))
			{
				var text = Instantiate(textPrefab, parent, false);
				text.text = str;
				_graphics[i] = text;
			}
			else
			{
				var image = Instantiate(imagePrefab, parent, false);
				image.sprite = sprites[spriteIndex];
				_graphics[i] = image;
				spriteIndex++;
				if ((spriteIndex >= spriteCount) || (spriteIndex >= sprites.Length))
				{
					spriteIndex = 0;
				}
			}
		}
	}
	void OnDestroy()
	{
		if (_graphics != null)
		{
			foreach (var item in _graphics)
			{
				Destroy(item.gameObject);
			}
			_graphics = null;
		}
	}
	void Reset()
	{
		OnDestroy();
		Start();
	}
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.F1))
		{
			_uiEnabled = !_uiEnabled;
		}
		string str = null;
		if (_setText)
		{
			if (_useRichText)
			{
				str = "<color=#ff0000>" + _frame.ToString("D5") + "</color>";
			}
			else
			{
				str = _frame.ToString("D5");
			}
		}
		float alpha = UnityEngine.Random.Range(0f, 1f);
		Color color = new Color(1f, 1f, 1f, alpha);

		int count = _setAll ? _graphics.Length : 1;

		for (int i = 0; i < count; i++)
		{
			var graphic = _graphics[i];
			var transform = graphic.rectTransform;

			if (_setText)
			{
				var text = graphic as Text;
				if (text != null)
				{
					text.text = str;
				}
			}

			if (_setGraphicColor)
			{
				graphic.color = color;
			}

			if (_setCanvasRendererColor)
			{
				var renderer = graphic.gameObject.GetComponent<CanvasRenderer>();
				if (renderer != null)
				{
					renderer.SetColor(color);
				}
			}

			if (_setCanvasRendererAlpha)
			{
				var renderer = graphic.gameObject.GetComponent<CanvasRenderer>();
				if (renderer != null)
				{
					renderer.SetAlpha(alpha);
				}
			}

			if (_setPosition)
			{
				var pos = transform.anchoredPosition;
				pos.x = UnityEngine.Random.Range(-320f, 320f);
				pos.y = UnityEngine.Random.Range(-240f, 240f);
				transform.anchoredPosition = pos;
			}

			if (_setRotation)
			{
				var rot = transform.localRotation;
				rot = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
				transform.localRotation = rot;
			}

			if (_setScale)
			{
				var scale = transform.localScale;
				scale.x = UnityEngine.Random.Range(0f, 1f);
				scale.y = UnityEngine.Random.Range(0f, 1f);
				transform.localScale = scale;
			}

			if (_setSizeDelta)
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
