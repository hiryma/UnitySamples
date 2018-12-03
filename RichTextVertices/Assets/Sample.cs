#define TEST_0
//#define SET_ALL
//#define SET_TEXT
//#define SET_POSITION
//#define SET_SCALE
#define SET_SIZEDELTA
//#define USE_RICH_TEXT
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Sample : MonoBehaviour
{
#if TEST_0
	public Text textPrefab;
	public Canvas canvas;
	Text[] _texts;
	const int N = 1;
	int _frame;

	void Start()
	{
		_texts = new Text[N];
		for (int i = 0; i < N; i++)
		{
			_texts[i] = Instantiate(textPrefab, canvas.gameObject.transform, false);
		}
	}
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			foreach (var text in _texts)
			{
				var outline = text.gameObject.GetComponent<Outline>();
				outline.enabled = !outline.enabled;
			}
		}

#if USE_RICH_TEXT // リッチテキスト付き
		var str = "<color=#ff0000>" + _frame.ToString("D5") + "</color>";
#else
		var str = _frame.ToString("D5");
#endif

#if SET_ALL // 全部
		int M = _texts.Length;
#else // 一個だけ
		int M = 1;
#endif

		for (int i = 0; i < M; i++)
		{
			var text = _texts[i];
			var transform = text.rectTransform;
#if SET_TEXT
			text.text = str;
#endif

#if SET_POSITION
			var pos = transform.anchoredPosition;
			pos.x = UnityEngine.Random.Range(-320f, 320f);
			pos.y = UnityEngine.Random.Range(-240f, 240f);
			transform.anchoredPosition = pos;
#endif

#if SET_SCALE
			var scale = transform.localScale;
			scale.x = UnityEngine.Random.Range(0f, 1f);
			scale.y = UnityEngine.Random.Range(0f, 1f);
			transform.localScale = scale;
#endif

#if SET_SIZEDELTA
			var size = transform.sizeDelta;
			size.x = UnityEngine.Random.Range(50f, 300f);
			size.y = UnityEngine.Random.Range(50f, 300f);
			transform.sizeDelta = size;
#endif
		}
		_frame++;
	}
#endif

#if TEST_1
	public Text textPrefab;
	public Canvas canvas;
	Text[] _texts;
	const int N = 100;

	void Start()
	{
		_texts = new Text[N];
		for (int i = 0; i < N; i++)
		{
			_texts[i] = Instantiate(textPrefab, canvas.gameObject.transform, false);
#if true
			_texts[i].fontSize = i + 1;
#endif

			_texts[i].text = "少し長めにしてみたよ!!";
		}
	}
	void Update()
	{
		foreach (var text in _texts)
		{
			var transform = text.rectTransform;
#if false // 位置
			var pos = transform.anchoredPosition;
			pos.x = UnityEngine.Random.Range(-320f, 320f);
			pos.y = UnityEngine.Random.Range(-240f, 240f);
			transform.anchoredPosition = pos;
#elif false // スケール
			var scale = transform.localScale;
			scale.x = UnityEngine.Random.Range(0f, 1f);
			scale.y = UnityEngine.Random.Range(0f, 1f);
			transform.localScale = scale;
#elif true // サイズ
			var size = transform.sizeDelta;
			size.x = UnityEngine.Random.Range(50f, 300f);
			size.y = UnityEngine.Random.Range(50f, 300f);
			transform.sizeDelta = size;
#endif
		}
	}
#endif

#if TEST_2
	public Text textPrefab;
	public Image imagePrefab;
	public Canvas canvas;

	Graphic[] _graphics;
	const int N = 200;

	void Start()
	{
		_graphics = new Graphic[N];
		for (int i = 0; i < N; i++)
		{
			var parent = canvas.gameObject.transform;
			if ((i % 2) == 0)
			{
				var text = Instantiate(textPrefab, parent, false);
				text.text = "少し長めにしてみたよ!!";
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
		foreach (var graphic in _graphics)
		{
			var transform = graphic.rectTransform;
			var pos = transform.anchoredPosition;
			pos.x = UnityEngine.Random.Range(-320f, 320f);
			pos.y = UnityEngine.Random.Range(-240f, 240f);
			transform.anchoredPosition = pos;
		}
	}
#endif
}
