namespace Kayac
{

	struct Random32
	{
		uint _x;
		public Random32(int seed)
		{
			_x = 0xffff0000 | (uint)(seed & 0xffff);
		}

		public int GetInt()
		{
			Next();
			return (int)_x;
		}

		public int GetInt(int maxExclusive)
		{
			Next();
			return (int)(_x & 0x7fffffff) % maxExclusive;
		}

		public int GetInt(int minInclusive, int maxExclusive)
		{
			Next();
			int range = maxExclusive - minInclusive;
			return ((int)(_x & 0x7fffffff) % range) + minInclusive;
		}

		public float GetFloat()
		{
			Next();
			var shifted = _x >> 1; // そのままだと絶対0にならないので1bitシフト
			return (float)shifted/ 2147483648f;
		}

		public float GetFloat(float maxExclusive)
		{
			return GetFloat() * maxExclusive;
		}

		public float GetFloat(float minInclusive, float maxExclusive)
		{
			float range = maxExclusive - minInclusive;
			return (GetFloat() * range) + minInclusive;
		}

		void Next()
		{
			_x ^= _x << 13;
			_x ^= _x >> 17;
			_x ^= _x << 5;
		}
	}
}
