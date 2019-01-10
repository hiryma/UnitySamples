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

	Vector3 _prevMousePos;
	int _activeInstanceCount;
	List<GameObject> _instances = new List<GameObject>();
	GameObject _currentPrefab;
	float _count;
	float[] _times = new float[100];
	int _timeIndex;
	bool _benchmarking;

	void OnGUI()
	{
		var mousePos = Input.mousePosition;
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
			if (Input.GetMouseButton(0))
			{
				var dx = mousePos.x - _prevMousePos.x;
				var dy = mousePos.y - _prevMousePos.y;
				var pos = _scaler.localPosition;
				pos.x += dx * 1f;
				pos.y += dy * 1f;
				_scaler.localPosition = pos;
			}
		}
		_prevMousePos = mousePos;
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

	void StartBench(GameObject prefab)
	{
		_activeInstanceCount = 0;
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
}
