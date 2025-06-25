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
				Distributions.SampleStandardNormalDistribution(out ret, out nextValue, rand.Value, rand.Value);
				hasNextValue = true;
			}
			return ret;
		}

		public Vector2 Sample2()
		{
			Vector2 ret;
			Distributions.SampleStandardNormalDistribution(out ret.x, out ret.y, rand.Value, rand.Value);
			return ret;
		}

		public Vector3 Sample3()
		{
			Vector3 ret;
			if (hasNextValue)
			{
				ret.x = nextValue;
				Distributions.SampleStandardNormalDistribution(out ret.y, out ret.z, rand.Value, rand.Value);
				hasNextValue = false;
			}
			else
			{
				Distributions.SampleStandardNormalDistribution(out ret.x, out ret.y, rand.Value, rand.Value);
				Distributions.SampleStandardNormalDistribution(out ret.z, out nextValue, rand.Value, rand.Value);
				hasNextValue = true;
			}
			return ret;
		}

		// non public ----
		Rand32S rand;
		float nextValue;
		bool hasNextValue;
	}

	public static class Distributions
	{
		public static void SampleStandardNormalDistribution(
			out float value0,
			out float value1,
			float rand0, // [0,1]
			float rand1) // [0,1]
		{
			var r = Mathf.Sqrt(-2f * Mathf.Log(rand0));
			var theta = Mathf.PI * 2f * rand1;
			value0 = r * Mathf.Cos(theta);
			value1 = r * Mathf.Sin(theta);
		}
	}
}