namespace Kayac.Debug
{
	public class FrameTimeWatcher
	{
        public float Fps { get { return (1f / averageFrameTime); } }
        // この1秒間の平均フレーム間隔をマイクロ秒で返す
        public int AverageFrameTime { get { return (int)(averageFrameTime * 1000f * 1000f); } }
        // この1秒間で最大のフレーム間隔をマイクロ秒で返す
        public int MaxFrameTime { get { return (int)(maxFrameTime * 1000f * 1000f); } }
        public int LoggingFrameCount { get { return times.Length; } }
        public int AvarageFrameTimeSinceStart
        {
            get
            {
                long now = System.DateTime.Now.Ticks;
                long duration = now - startTime;
                float average = (float)duration / (float)frameCount;
                float averageSecond = average / (float)(System.TimeSpan.TicksPerSecond);
                return (int)(averageSecond * 1000f * 1000f);
            }
        }

        public delegate string OnSpikeLabelRequest();

		public FrameTimeWatcher(int loggingFrameCount = 60)
		{
			loggingFrameCount = (loggingFrameCount <= 0) ? 60 : loggingFrameCount;
			times = new long[loggingFrameCount];
			startTime = System.DateTime.Now.Ticks;
			Reset();
		}

		public void Reset()
		{
			long now = System.DateTime.Now.Ticks;
			for (int i = 0; i < LoggingFrameCount; ++i)
			{
				times[i] = now;
			}
			timeIndex = 0;
			// 初期化直後にFpsを呼ばれた時への対処
			averageFrameTime = 1f;
			startTime = now;
			frameCount = 0;
		}

		public void Update()
		{
			float tickToSecond = 1.0f / (float)(System.TimeSpan.TicksPerSecond);
			long now = System.DateTime.Now.Ticks;
			long oldest = times[timeIndex];
			times[timeIndex] = now;
			// 最大間隔を探す。O(LogN)のアルゴリズムはある(heap)が、余計なメモリを消費する上に、N=60であれば線形検索の方が速い。
			// インデクスの巻き戻しに伴う分岐削減のためにループを2分しておく
			long max = 0;
			long prev = oldest;
			for (int i = (timeIndex + 1); i < times.Length; i++)
			{
				long diff = times[i] - prev;
				max = (diff > max) ? diff : max;
				prev = times[i];
			}
			for (int i = 0; i <= timeIndex; i++)
			{
				long diff = times[i] - prev;
				max = (diff > max) ? diff : max;
				prev = times[i];
			}
			maxFrameTime = max * tickToSecond;
			averageFrameTime = (float)(now - oldest) * tickToSecond / (float)times.Length;

			timeIndex++;
			if (timeIndex == times.Length)
			{
				timeIndex = 0;
			}
			frameCount++;
		}

        // non-public -------------
        readonly long[] times;
        int timeIndex;
        float averageFrameTime;
        float maxFrameTime;
        long startTime;
        int frameCount;
    }
}
