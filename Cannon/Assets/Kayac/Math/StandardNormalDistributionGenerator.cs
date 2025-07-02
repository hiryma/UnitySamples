using UnityEngine;

namespace Kayac
{
	public struct StandardNormalDistributionGenerator
	{
		public StandardNormalDistributionGenerator(int seed)
		{
			this.rand = new Rand32S(seed);
			this.nextValue = 0f;
			this.hasNextValue = false;
		}

		public void SetSeed(int seed)
		{
			rand.SetSeed(seed);
			hasNextValue = false;
		}

		public float Sample()
		{
			float ret;
			if (hasNextValue)
			{
				ret = nextValue;
				hasNextValue = false;
			}
			else
			{
				SampleStandardNormalDistribution(out ret, out nextValue);
				hasNextValue = true;
			}
			return ret;
		}

		public Vector2 Sample2()
		{
			Vector2 ret;
			SampleStandardNormalDistribution(out ret.x, out ret.y);
			return ret;
		}

		public Vector3 Sample3()
		{
			Vector3 ret;
			if (hasNextValue)
			{
				ret.x = nextValue;
				SampleStandardNormalDistribution(out ret.y, out ret.z);
				hasNextValue = false;
			}
			else
			{
				SampleStandardNormalDistribution(out ret.x, out ret.y);
				SampleStandardNormalDistribution(out ret.z, out nextValue);
				hasNextValue = true;
			}
			return ret;
		}

		// non public ----
		Rand32S rand;
		float nextValue;
		bool hasNextValue;

		void SampleStandardNormalDistribution(
			out float value0,
			out float value1)
		{
			// marsaglia polar method
			float x, y, sqSum;
			while (true) //8割弱くらいは1回で終わる
			{
				x = rand.Range(-1f, 1f);
				y = rand.Range(-1f, 1f);
				sqSum = (x * x) + (y * y);
				if ((sqSum > 0f) && (sqSum < 1f))
				{
					break;
				}
			}

			var s = Mathf.Sqrt(-2f * Mathf.Log(sqSum) / sqSum);
			value0 = x * s;
			value1 = y * s;
		}
	}
}
