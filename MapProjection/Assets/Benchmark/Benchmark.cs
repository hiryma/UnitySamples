using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Benchmark : MonoBehaviour
{
	[SerializeField]
	FillRenderer _fillRenderer;

	float[] _times;
	int _timeIndex;
	float _count;
	float _countVelocity;
	public float averageFrameTime { get; private set; }
	public float count { get { return _count; } }
	public bool heavyMode { get; set; }
	bool _running;

	void Start()
	{
		_times = new float[60];
		_fillRenderer.ManualStart();
	}

	public void Run()
	{
		_running = true;
	}

	public void Stop()
	{
		_running = false;
	}

	void Update()
	{
		// 以下ベンチマーク
		_times[_timeIndex] = Time.realtimeSinceStartup;
		_timeIndex++;
		if (_timeIndex >= _times.Length)
		{
			_timeIndex = 0;
		}
		var latest = ((_timeIndex - 1) < 0) ? (_times.Length - 1) : (_timeIndex - 1);
		this.averageFrameTime = (_times[latest] - _times[_timeIndex]) / (_times.Length - 1);
		if (_running)
		{
			var targetMs = this.heavyMode ? (1000f / 24f) : (1000f / 40f);
			var accel = (((targetMs * 0.001f) - Time.unscaledDeltaTime) * (_count + 1f) * 0.1f) - (_countVelocity * 0.5f);
			_countVelocity += accel;
			_count += _countVelocity;
			_count = Mathf.Clamp(_count, 0f, 10000f);
		}
		else
		{
			_count = 0f;
			_countVelocity = 0f;
		}
		_fillRenderer.SetCount(_count);
		_fillRenderer.ManualUpdate();
	}
}
