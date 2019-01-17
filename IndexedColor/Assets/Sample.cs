using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sample : MonoBehaviour
{
	[SerializeField]
	Transform _scaler;
	[SerializeField]
	Transform _canvasTransform;
	[SerializeField]
	GameObject _standardImagePrefab;
	[SerializeField]
	GameObject _notIndexedPrefab;
	[SerializeField]
	GameObject _indexed256Prefab;
	[SerializeField]
	GameObject _indexed256BilinearPrefab;
	[SerializeField]
	GameObject _indexed16Prefab;
	[SerializeField]
	GameObject _indexed16BilinearPrefab;
	[SerializeField]
	DragDetector _dragDetector;
	[SerializeField]
	Kayac.IndexedRawImage _paletteModifyingImage;

	int _activeInstanceCount;
	List<GameObject> _instances = new List<GameObject>();
	GameObject _currentPrefab;
	float _count;
	float[] _times = new float[100];
	int _timeIndex;
	bool _benchmarking;
	float _hue;
	Texture2D _originalPalette;

	void Awake()
	{
		_dragDetector.onDrag = OnDrag;
		// パレットをいじるコンポーネントが指定されていれば、テクスチャはコピーを作ってそっちを差しておく
		if (_paletteModifyingImage != null)
		{
			var table = _paletteModifyingImage.tableTexture;
			_originalPalette = table;
			var copy = new Texture2D(table.width, table.height, TextureFormat.RGBA32, false);
			copy.name = table.name + "_copied";
			copy.filterMode = FilterMode.Point;
			copy.SetPixels32(table.GetPixels32());
			copy.Apply();
			_paletteModifyingImage.tableTexture = copy;
		}
	}

	void OnGUI()
	{
		_benchmarking = GUILayout.Toggle(_benchmarking, "benchmark");
		if (_benchmarking)
		{
			_scaler.gameObject.SetActive(false);
			var fps = (float)_times.Length / (Time.realtimeSinceStartup - _times[_timeIndex]);
			GUILayout.Label("count:" + _activeInstanceCount);
			GUILayout.Label("fps:" + fps.ToString("N1"));
			GUILayout.Label("type:" + ((_currentPrefab != null) ? _currentPrefab.name : "None"));
			if (GUILayout.Button("StandardImage"))
			{
				StartBench(_standardImagePrefab);
			}
			if (GUILayout.Button("NotIndexed"))
			{
				StartBench(_notIndexedPrefab);
			}
			if (GUILayout.Button("Indexed256"))
			{
				StartBench(_indexed256Prefab);
			}
			if (GUILayout.Button("Indexed256 Bilinear"))
			{
				StartBench(_indexed256BilinearPrefab);
			}
			if (GUILayout.Button("Indexed16"))
			{
				StartBench(_indexed16Prefab);
			}
			if (GUILayout.Button("Indexed16 Bilinear"))
			{
				StartBench(_indexed16BilinearPrefab);
			}
		}
		else
		{
			_scaler.gameObject.SetActive(true);
			DestroyInstances();
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
			GUILayout.Label("Hue");
			var newHue = GUILayout.HorizontalSlider(_hue, 0f, 360f);
			if (newHue != _hue)
			{
				_hue = newHue;
				ModifyPalette();
			}
		}
	}

	void Update()
	{
		_times[_timeIndex] = Time.realtimeSinceStartup;
		_timeIndex++;
		if (_timeIndex >= _times.Length)
		{
			_timeIndex = 0;
		}
		if (!_benchmarking || (_currentPrefab == null))
		{
			return; // 比較モードなのでベンチの更新はしない
		}
		_count += ((1f / 25f) - Time.unscaledDeltaTime) * 10f;
		if ((int)_count > _activeInstanceCount)
		{
			if (_activeInstanceCount >= _instances.Count)
			{
				_instances.Add(CreateInstance(_currentPrefab));
			}
			else
			{
				_instances[_activeInstanceCount].gameObject.SetActive(true);
			}
			_activeInstanceCount++;
		}
		else if ((int)_count < _activeInstanceCount)
		{
			if (_activeInstanceCount > 0)
			{
				_activeInstanceCount--;
				_instances[_activeInstanceCount].gameObject.SetActive(false);
			}
		}
	}

	void OnDrag(Vector2 delta)
	{
		if (!_benchmarking)
		{
			var pos = _scaler.localPosition;
			pos.x += delta.x * 1f;
			pos.y += delta.y * 1f;
			_scaler.localPosition = pos;
		}
	}

	void StartBench(GameObject prefab)
	{
		_activeInstanceCount = 0;
		_count = 0;
		DestroyInstances();
		_currentPrefab = prefab;
	}

	void DestroyInstances()
	{
		if (_instances != null)
		{
			foreach (var instance in _instances)
			{
				Destroy(instance);
			}
			_instances.Clear();
		}
	}

	GameObject CreateInstance(GameObject prefab)
	{
		var instance = Instantiate(prefab, _canvasTransform, false);
		var rect = instance.GetComponent<RectTransform>();
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.pivot = Vector2.one * 0.5f;
		rect.sizeDelta = Vector2.zero;
		return instance;
	}

	void ModifyPalette()
	{
		if (_paletteModifyingImage == null)
		{
			return;
		}
		var table = _paletteModifyingImage.tableTexture;
		var pixels = _originalPalette.GetPixels32();
		for (int i = 0; i < pixels.Length; i++)
		{
			ModifyHue(ref pixels[i], _hue);
		}
		table.SetPixels32(pixels);
		table.Apply();
	}

	void ModifyHue(ref Color32 color, float hue)
	{
		float h,s,v;
		RgbToHsv(out h, out s, out v, color.r / 255f, color.g / 255f, color.b / 255f);
		h += hue;
		if (h >= 360f)
		{
			h -= 360f;
		}
		float r, g, b;
		HsvToRgb(out r, out g, out b, h, s, v);
		color.r = (byte)(r * 255f);
		color.g = (byte)(g * 255f);
		color.b = (byte)(b * 255f);
	}

	// https://en.wikipedia.org/wiki/HSL_and_HSV 参照のこと
	void RgbToHsv(out float h, out float s, out float v, float r, float g, float b)
	{
		var max = Mathf.Max(Mathf.Max(r, g), b);
		var min = Mathf.Min(Mathf.Min(r, g), b);
		v = max;
		var c = max - min;
		if (c == 0)
		{
			h = s = 0f;
		}
		else
		{
			if (r == max) // 赤最高
			{
				h = (g - b) / c;
				if (h < 0f)
				{
					h += 6;
				}
			}
			else if (g == max) // 緑最高
			{
				h = ((b - r) / c) + 2f;
			}
			else // 青最高
			{
				h = ((r - g) / c) + 4f;
			}
			h *= 60f;
			s = c / v;
		}
	}

	void HsvToRgb(out float r, out float g, out float b, float h, float s, float v)
	{
		h /= 60f;
		var c = v * s;
		var m = v - c;
		if (s == 0f)
		{
			r = g = b = (v - c);
		}
		else if (h > 5f)
		{
			r = v;
			g = m;
			b = (c * (6f - h)) + m;
		}
		else if (h > 4f)
		{
			r = (c * (h - 4f)) + m;
			g = m;
			b = v;
		}
		else if (h > 3f)
		{
			r = m;
			g = (c * (4f - h)) + m;
			b = v;
		}
		else if (h > 2f)
		{
			r = m;
			g = v;
			b = (c * (h - 2f)) + m;
		}
		else if (h > 1f)
		{
			r = (c * (2f - h)) + m;
			g = v;
			b = m;
		}
		else
		{
			r = v;
			g = (c * h) + m;
			b = m;
		}
	}
}
