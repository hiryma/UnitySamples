using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
	[SerializeField]
	Material[] _fillTestMaterials;
	[SerializeField]
	Material _embossMaterial;
	[SerializeField]
	Material _embossTextMaterial;
	[SerializeField]
	GameObject _titleRoot;
	[SerializeField]
	Text _fillModeText;
	[SerializeField]
	Toggle _fillTestToggle;
	[SerializeField]
	Image _blackFilter;
	[SerializeField]
	FillRenderer _fillRenderer;

	Coroutine _coroutine;
	float _count;
	float _countVelocity;
	float[] _times;
	int _timeIndex;
	float[] _counts;
	int _countIndex;
	int _fillTestMaterialIndex;
	System.Text.StringBuilder _stringBuilder;
	bool _autoTest;
	int _fillTestFrameCount;
	string _result;

	void Start()
	{
#if UNITY_EDITOR || UNITY_STANDALONE
		Application.runInBackground = true;
#else
		Application.runInBackground = false;
#endif
		_blackFilter.enabled = false;
		_stringBuilder = new System.Text.StringBuilder();
		_times = new float[60];
		_counts = new float[120];
		_embossTextMaterial.EnableKeyword("FOR_TEXT");
		_fillRenderer.ManualStart();
	}

	void UpdateAutoTest()
	{
		if (_coroutine != null)
		{
			// CPUテスト中。何もしない
		}
		else if (!_fillTestToggle.isOn)
		{
			_fillTestToggle.isOn = true;
			_fillTestMaterialIndex = 0; // 0番からテスト開始
		}
		else // フィルテスト中。カウントが安定したら次へ行く。
		{
			_fillTestFrameCount++;
			// 最小二乗法フィッティングして、傾きが十分に小さいことを確認する
			int oldest = (_countIndex == 0) ? (_counts.Length - 1) : (_countIndex - 1);
			float sumXy = 0f;
			float sumX = 0f;
			float sumY = 0f;
			float sumX2 = 0f;
			for (int i = 0; i < _counts.Length; i++)
			{
				int idx = oldest + i;
				if (idx >= _counts.Length)
				{
					idx -= _counts.Length;
				}
				sumX += (float)i;
				sumX2 += (float)(i * i);
				sumY += _counts[idx];
				sumXy += _counts[idx] * (float)i;
			}
			float a = ((float)_counts.Length * sumXy) - (sumX * sumY);
			a /= ((float)_counts.Length * sumX2) - (sumX * sumX);
			if (((_fillTestFrameCount >= _counts.Length) && (a < 0f))
				|| (_count >= 10000f))
			{
				OnFillModeButton();
				if (_fillTestMaterialIndex == 0)
				{
					_fillTestToggle.isOn = false;
					_autoTest = false;
					_stringBuilder.Length = 0;
				}
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
		_counts[_countIndex] = _count;
		_countIndex++;
		if (_countIndex >= _counts.Length)
		{
			_countIndex = 0;
		}

		if (_fillTestToggle.isOn)
		{
			var accel = (((1f / 24f) - Time.unscaledDeltaTime) * (_count + 1f) * 0.1f) - (_countVelocity * 0.5f);
			_countVelocity += accel;
			_count += _countVelocity;
			_count = Mathf.Clamp(_count, 0f, 10000f);
			_fillRenderer.SetCount((int)_count);
			_fillRenderer.SetMaterial(_fillTestMaterials[_fillTestMaterialIndex]);
			_fillRenderer.ManualUpdate();
		}
		_titleRoot.SetActive(!_fillTestToggle.isOn && (_coroutine == null));
		if (_autoTest)
		{
			UpdateAutoTest();
			Screen.sleepTimeout = SleepTimeout.NeverSleep;
		}
		else
		{
			Screen.sleepTimeout = SleepTimeout.SystemSetting;
		}

		if (_fillTestToggle.isOn)
		{
			_fillModeText.text = _fillTestMaterials[_fillTestMaterialIndex].name;
		}
		else
		{
			_fillModeText.text = "FillTest";
		}
	}

	void OnGUI()
	{
		var latest = ((_timeIndex - 1) < 0) ? (_times.Length - 1) : (_timeIndex - 1);
		var avg = (_times[latest] - _times[_timeIndex]) / (_times.Length - 1);
		var sb = new System.Text.StringBuilder();
		sb.Append("v0.2.4: " + SystemInfo.deviceModel + "\n");
		sb.Append("Os: " + SystemInfo.operatingSystem + "\n");
		sb.Append("Gpu: " + SystemInfo.graphicsDeviceName + "\n");
		sb.Append("FrameTime: " + (avg * 1000f).ToString("N2") + " frame:" + Time.frameCount + "\n");
		sb.Append("Count: " + _count.ToString("N0") + "\n");
		GUILayout.Label(sb.ToString());
		if (_result != null)
		{
			_blackFilter.enabled = true;
			GUILayout.Label(_result);
		}
#if !UNITY_WEBGL
		if (GUILayout.Button("CopyToClipboard"))
		{
			GUIUtility.systemCopyBuffer	= _result;
		}
#endif
	}

	public void OnClickAutoButton()
	{
		_autoTest = true;
		OnClickCpuButton();
	}

	public void OnClickCpuButton()
	{
		_stringBuilder.Length = 0;
		_coroutine = StartCoroutine(CoBenchmark());
	}

	public void OnFillModeButton()
	{
		_fillTestFrameCount = 0;
		_stringBuilder.Append(_fillTestMaterials[_fillTestMaterialIndex].name + " " + _count.ToString("N2") + "\n");
		_result = _stringBuilder.ToString();
		_count = _countVelocity = 0f;
		for (int i = 0; i < _counts.Length; i++)
		{
			_counts[i] = 0f;
		}
		_countIndex = 0;
		_fillTestMaterialIndex++;
		if (_fillTestMaterialIndex >= _fillTestMaterials.Length)
		{
			_fillTestMaterialIndex = 0;
		}
	}

	public void OnClickSysInfoButton()
	{
		var sb = _stringBuilder;
		sb.Length = 0;
		sb.Append("Cpu Type/Count: " + SystemInfo.processorType + " " + SystemInfo.processorCount + "\n");
		sb.Append("Gpu Type: " + SystemInfo.graphicsDeviceType + " " + SystemInfo.graphicsDeviceVersion + "\n");
		sb.Append("Memory Cpu/Gpu: " + SystemInfo.systemMemorySize + " " + SystemInfo.graphicsMemorySize + "\n");
		sb.Append("\tTexture Size/NPOT: " + SystemInfo.maxTextureSize + "\n");
		sb.Append("\tTexture NPOT: " + SystemInfo.npotSupport + "\n");
		sb.Append("\tInstancing: " + SystemInfo.supportsInstancing + "\n");
		sb.Append("\tComputeShader: " + SystemInfo.supportsComputeShaders + "\n");
		sb.Append("\tIndexingInFragment: " + SystemInfo.hasDynamicUniformArrayIndexingInFragmentShaders + "\n");
		sb.Append("\tShadow: " + SystemInfo.supportsShadows + "\n");
		sb.Append("\tDepthSampling: " + SystemInfo.supportsRawShadowDepthSampling + "\n");
		sb.Append("\tRenderToCube: " + SystemInfo.supportsRenderToCubemap + "\n");
		_result = sb.ToString();
		sb.Length = 0;
	}

	IEnumerator CoBenchmark()
	{
		var sb = _stringBuilder;
		yield return null;

		var stackInt = new int[64];
		var t0 = Time.realtimeSinceStartup;
		var r2 = FibonacciInt(stackInt, 35);
		var t1 = Time.realtimeSinceStartup;
		sb.Append("FibonacchiInt: " + (t1 - t0).ToString("N3") + "\n");
		_result = _stringBuilder.ToString();
		yield return null;

		var stackFloat = new float[64];
		t0 = Time.realtimeSinceStartup;
		var r3 = FibonacciFloat(stackFloat, 35);
		t1 = Time.realtimeSinceStartup;
		sb.Append("FibonacchiFloat: " + (t1 - t0).ToString("N3") + "\n");
		_result = _stringBuilder.ToString();
		yield return null;

		var array = new int[1024 * 1024 * 4];
		int rnd = 1;
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = rnd;
			rnd ^= rnd << 13;
			rnd ^= rnd >> 17;
			rnd ^= rnd << 5;
		}
		t0 = Time.realtimeSinceStartup;
		HeapSort(array);
		t1 = Time.realtimeSinceStartup;
		sb.Append("HeapSort: " + (t1 - t0).ToString("N3") + "\n");
		_result = _stringBuilder.ToString();
		int prev = int.MinValue;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] < prev)
			{
				Debug.LogError("HeapSort has bug.");
			}
			prev = array[i];
		}
		yield return null;

		t0 = Time.realtimeSinceStartup;
		var q = Quaternion.identity;
		var w = new Vector3(0.1f, 0.2f, 0.3f);
		for (int i = 0; i < (1024 * 1024 * 4); i++)
		{
			q = Integrate(q, w);
		}
		t1 = Time.realtimeSinceStartup;
		sb.Append("QuaternionIntegration: " + (t1 - t0).ToString("N3") + "\n");
		_result = _stringBuilder.ToString();
		yield return null;
		Debug.Log("result log for anti-optimization " + (r2 + r3) + " " + q);

		_coroutine = null;
	}

	int FibonacciInt(int[] stack, int x)
	{
		stack[0] = x;
		int stackPos = 1;
		int r = 0;
		while (stackPos > 0)
		{
			stackPos--;
			var top = stack[stackPos];
			if (top <= 1)
			{
				r += top;
			}
			else
			{
				stack[stackPos] = top - 1;
				stack[stackPos + 1] = top - 2;
				stackPos += 2;
			}
		}
		return r;
	}

	float FibonacciFloat(float[] stack, float x)
	{
		stack[0] = x;
		int stackPos = 1;
		float r = 0;
		while (stackPos > 0)
		{
			stackPos--;
			var top = stack[stackPos];
			if (top <= 1f)
			{
				r += top;
			}
			else
			{
				stack[stackPos] = top - 1f;
				stack[stackPos + 1] = top - 2f;
				stackPos += 2;
			}
		}
		return r;
	}

	// https://ja.wikipedia.org/wiki/%E3%83%92%E3%83%BC%E3%83%97%E3%82%BD%E3%83%BC%E3%83%88 そのまま
	void HeapSort(int[] a)
	{
		int i = 1;
		while (i < a.Length)
		{
			Upheap(a, i);
			i++;
		}
		i--;
		while (i > 0)
		{
			var t = a[0];
			a[0] = i;
			a[i] = t;
			Downheap(a, i);
			i--;
		}
	}

	void Upheap(int[] a, int n)
	{
		while (n > 0)
		{
			int m = ((n + 1) / 2) - 1;
			if (a[m] < a[n])
			{
				var t = a[m];
				a[m] = a[n];
				a[n] = t;
			}
			else
			{
				break;
			}
			n = m;
		}
	}

	void Downheap(int[] a, int n)
	{
		int m = 0;
		int t = 0;
		while (true)
		{
			int r = (m + 1) * 2;
			int l = r - 1;
			if (l >= n)
			{
				break;
			}
			if (a[l] > a[t])
			{
				t = l;
			}
			if ((r < n) && (a[r] > a[t]))
			{
				t = r;
			}
			if (t == m)
			{
				break;
			}
			var tmp = a[t];
			a[t] = a[m];
			a[m] = tmp;

			m = t;
		}
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
