using System;

namespace Kayac
{
	public class FrameTimeWatcher
	{
		private long[] _times;
		private int _timeIndex;
		private float _averageFrameTime;
		private float _maxFrameTime;
		private long _startTime;
		private int _frameCount;

		public delegate string OnSpikeLabelRequest();

		public FrameTimeWatcher(int loggingFrameCount = 60, int loggingSpikeCount = 16)
		{
			loggingFrameCount = (loggingFrameCount <= 0) ? 60 : loggingFrameCount;
			_times = new long[loggingFrameCount];
			_startTime = System.DateTime.Now.Ticks;
			Reset();
		}

		public void Reset()
		{
			long now = System.DateTime.Now.Ticks;
			for (int i = 0; i < loggingFrameCount; ++i)
			{
				_times[i] = now;
			}
			_timeIndex = 0;
			// 初期化直後にFpsを呼ばれた時への対処
			_averageFrameTime = 1f;
			_startTime = now;
			_frameCount = 0;
		}

		public float fps { get { return (1f / _averageFrameTime); } }
		// この1秒間の平均フレーム間隔をマイクロ秒で返す
		public int averageFrameTime { get { return (int)(_averageFrameTime * 1000f * 1000f); } }
		// この1秒間で最大のフレーム間隔をマイクロ秒で返す
		public int maxFrameTime { get { return (int)(_maxFrameTime * 1000f * 1000f); } }
		public int loggingFrameCount { get { return _times.Length; } }
		public int avarageFrameTimeSinceStart
		{
			get
			{
				long now = System.DateTime.Now.Ticks;
				long duration = now - _startTime;
				float average = (float)duration / (float)_frameCount;
				float averageSecond = average / (float)(System.TimeSpan.TicksPerSecond);
				return (int)(averageSecond * 1000f * 1000f);
			}
		}

		public void Update()
		{
			float tickToSecond = 1.0f / (float)(System.TimeSpan.TicksPerSecond);
			long now = System.DateTime.Now.Ticks;
 			long oldest = _times[_timeIndex];
			_times[_timeIndex] = now;
			// 最大間隔を探す。O(LogN)のアルゴリズムはある(heap)が、余計なメモリを消費する上に、N=60であれば線形検索の方が速い。
			// インデクスの巻き戻しに伴う分岐削減のためにループを2分しておく
			long max = 0;
			long prev = oldest;
			for (int i = (_timeIndex + 1); i < _times.Length; i++)
			{
				long diff = _times[i] - prev;
				max = (diff > max) ? diff : max;
				prev = _times[i];
			}
			for (int i = 0; i <= _timeIndex; i++)
			{
				long diff = _times[i] - prev;
				max = (diff > max) ? diff : max;
				prev = _times[i];
			}
			_maxFrameTime = max * tickToSecond;
			_averageFrameTime = (float)(now - oldest) * tickToSecond / (float)_times.Length;

			_timeIndex++;
			if (_timeIndex == _times.Length)
			{
				_timeIndex = 0;
			}
			_frameCount++;
		}
	}
}
