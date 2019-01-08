using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sample : MonoBehaviour
{
	[SerializeField]
	GameObject _billboardPrefab;
	[SerializeField]
	Transform _root;
	[SerializeField]
	Camera _camera;

	int _billboardCount = 100;
	GameObject[] _billboards;
	// カメラ角度
	Quaternion _cameraOrientation;
	Vector3 _prevMousePosition;
	float _cameraMoveSpeed = 0.005f;
	float _cameraRollSpeed = 0.1f;
	bool _rollEnabled;
	bool _faceToViewVector;
	bool _randomArrangement;

	void Start()
	{
		_cameraOrientation = Quaternion.identity;
		_billboards = new GameObject[_billboardCount];
		for (int i = 0; i < _billboards.Length; i++)
		{
			_billboards[i] = Instantiate(_billboardPrefab, _root, false);
		}
		ArrangeBillboards();
	}

	void ArrangeBillboards()
	{
		if (_randomArrangement)
		{
			for (int i = 0; i < _billboards.Length; i++)
			{
				Vector3 pos;
				pos.x = Random.Range(-10f, 10f);
				pos.y = Random.Range(0f, 10f);
				pos.z = Random.Range(-10f, 10f);
				_billboards[i].transform.localPosition = pos;
			}
		}
		else
		{
			var z = -10f;
			var x = -10f;
			var xCount = (int)(Mathf.Sqrt((float)_billboards.Length));
			var step = 20f / (xCount - 1);
			var xIndex = 0;
			for (int i = 0; i < _billboards.Length; i++)
			{
				_billboards[i].transform.localPosition = new Vector3(x, 0f, z);
				x += step;
				xIndex++;
				if (xIndex == xCount)
				{
					x = -10f;
					xIndex = 0;
					z += step;
				}
			}
		}
	}

	void Update()
	{
		UpdateCamera();
		for (int i = 0; i < _billboards.Length; i++)
		{
			var rotation = Quaternion.identity;
			if (_rollEnabled)
			{
				if (_faceToViewVector)
				{
					rotation = Quaternion.LookRotation(
						-_camera.transform.forward,
						_camera.transform.up);
				}
				else
				{
					rotation = Quaternion.LookRotation(
						_camera.transform.position - _billboards[i].transform.position,
						_camera.transform.up);
				}
			}
			else
			{
				if (_faceToViewVector)
				{
					rotation = Quaternion.LookRotation(
						-_camera.transform.forward,
						new Vector3(0f, 1f, 0f));
				}
				else
				{
					rotation = Quaternion.LookRotation(
						_camera.transform.position - _billboards[i].transform.position,
						new Vector3(0f, 1f, 0f));
				}
			}
			_billboards[i].transform.rotation = rotation;
		}
	}

	void OnGUI()
	{
		var distance = SqNorm(_cameraOrientation);
		GUILayout.Label("distance: " + distance.ToString("N2"));
		if (GUILayout.RepeatButton("zoomOut"))
		{
			distance *= 1.02f;
			_cameraOrientation = SetNorm(_cameraOrientation, Mathf.Sqrt(distance));
		}
		if (GUILayout.RepeatButton("zoomIn"))
		{
			distance *= 0.98f;
			_cameraOrientation = SetNorm(_cameraOrientation, Mathf.Sqrt(distance));
		}
		if (GUILayout.RepeatButton("cameraRoll"))
		{
			var w = new Vector3(0f, 0f, _cameraRollSpeed);
			_cameraOrientation = Integrate(_cameraOrientation, w);
		}
		GUILayout.Label("cameraMoveSpeed: " + _cameraMoveSpeed.ToString("N3"));
		_cameraMoveSpeed = GUILayout.HorizontalSlider(_cameraMoveSpeed, 0.001f, 0.02f);
		_rollEnabled = GUILayout.Toggle(_rollEnabled, "Roll");
		_faceToViewVector = GUILayout.Toggle(_faceToViewVector, "ToViewVector");
		var newRandomArrangement = GUILayout.Toggle(_randomArrangement, "RandomArrangement");
		if (newRandomArrangement != _randomArrangement)
		{
			_randomArrangement = newRandomArrangement;
			ArrangeBillboards();
		}

	}

	// ---- 以下このサンプルの目的と関係ないコード ----

	void UpdateCamera()
	{
		var dx = Input.mousePosition.x - _prevMousePosition.x;
		var dy = Input.mousePosition.y - _prevMousePosition.y;
		_prevMousePosition = Input.mousePosition;
		if (Input.GetMouseButton(0))
		{
			var w = new Vector3(dy * _cameraMoveSpeed, -dx * _cameraMoveSpeed, 0f);
			_cameraOrientation = Integrate(_cameraOrientation, w);
		}

		_camera.transform.localRotation = Normalize(_cameraOrientation);
		_camera.transform.localPosition = Transform(_cameraOrientation, new Vector3(0f, 0f, -1f));
	}

	float SqNorm(Quaternion q)
	{
		return (q.x * q.x) + (q.y * q.y) + (q.z * q.z) + (q.w * q.w);
	}

	float Norm(Quaternion q)
	{
		return Mathf.Sqrt(SqNorm(q));
	}

	Quaternion SetNorm(Quaternion q, float newNorm)
	{
		float norm = Norm(q);
		if (norm == 0f)
		{
			return new Quaternion(0f, 0f, 0f, 0f);
		}
		else
		{
			return Multiply(q, newNorm / norm);
		}
	}

	Quaternion Normalize(Quaternion q)
	{
		return SetNorm(q, 1f);
	}

	Quaternion Multiply(Quaternion a, Quaternion b)
	{
		Quaternion r;
		/*
		r = (a.x * b.x) + (a.x * b.y) + (a.x * b.z) + (a.x * b.w) +
			(a.y * b.x) + (a.y * b.y) + (a.y * b.z) + (a.y * b.w) +
			(a.z * b.x) + (a.z * b.y) + (a.z * b.z) + (a.z * b.w) +
			(a.w * b.x) + (a.w * b.y) + (a.w * b.z) + (a.w * b.w);
		x*x = y*y = z*z = -1
		x*y = z, y*x = -z, y*z = x, z*y = -x, z*x = y, x*z = -yを使って整理すると、
		*/
		r.x = (a.x * b.w) + (a.y * b.z) - (a.z * b.y) + (a.w * b.x);
		r.y = -(a.x * b.z) + (a.y * b.w) + (a.z * b.x) + (a.w * b.y);
		r.z = (a.x * b.y) - (a.y * b.x) + (a.z * b.w) + (a.w * b.z);
		r.w = -(a.x * b.x) - (a.y * b.y) - (a.z * b.z) + (a.w * b.w);
		return r;
	}

	Quaternion Multiply(Quaternion q, Vector3 v)
	{
		return Multiply(q, new Quaternion(v.x, v.y, v.z, 0f));
	}

	Quaternion Multiply(Quaternion q, float s)
	{
		return new Quaternion(q.x * s, q.y * s, q.z * s, q.w * s);
	}

	Quaternion Add(Quaternion a, Quaternion b)
	{
		return new Quaternion(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
	}

	Vector3 Transform(Quaternion q, Vector3 v)
	{
		var transformed = Multiply(Multiply(q, v), Conjugate(q));
		return new Vector3(transformed.x, transformed.y, transformed.z);
	}

	Quaternion Integrate(Quaternion q, Vector3 w)
	{
		float norm = Norm(q);
		Quaternion dq = Multiply(Multiply(q, w), 0.5f);
		var r = Add(q, dq);
		return SetNorm(r, norm); // ノルム不変にする
	}

	Quaternion Conjugate(Quaternion q)
	{
		return new Quaternion(-q.x, -q.y, -q.z, q.w);
	}
}
