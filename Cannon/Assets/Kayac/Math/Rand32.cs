using UnityEngine;

namespace Kayac
{
	public class Rand32
	{
		public float Value
		{
			get
			{
				var ret = Rand32Utility.ToFloat01(x);
				x = Rand32Utility.Next(x);
				return ret;
			}
		}

		public Vector2 InsideUnitCircle
		{
			get
			{
				Vector2 ret;
				Rand32Utility.InsideUnitCircle(out ret, out x, x);
				return ret;
			}
		}

		public Rand32()
		{
			x = (uint)(System.DateTime.Now.Ticks & 0xfffffffe) + 1; // 上位31bitもらって1足して奇数にする
		}

		public Rand32(int seed)
		{
			SetSeed(seed);
		}

		public void SetSeed(int seed)
		{
			seed = (seed & 0x7fffffff) + 1; // 31bitでループする。絶対に0にはならない。
			x = (uint)seed;
		}

		public int Range(int minIncluded, int maxExcluded)
		{
			var ret = Rand32Utility.ToRange(minIncluded, maxExcluded, x);
			x = Rand32Utility.Next(x);
			return ret;
		}

		public float Range(float minIncluded, float maxIncluded)
		{
			var ret = Rand32Utility.ToRange(minIncluded, maxIncluded, x);
			x = Rand32Utility.Next(x);
			return ret;
		}

		public int Next(int maxExcluded)
		{
			var ret = Rand32Utility.ToRange(maxExcluded, x);
			x = Rand32Utility.Next(x);
			return ret;
		}

		public float Next(float maxIncluded)
		{
			var ret = Rand32Utility.ToRange(maxIncluded, x);
			x = Rand32Utility.Next(x);
			return ret;
		}

		public uint NextRaw()
		{
			var ret = x;
			x = Rand32Utility.Next(x);
			return ret;
		}

		// non public ----
		uint x;
	}

	public struct Rand32S
	{
		public float Value
		{
			get
			{
				var ret = Rand32Utility.ToFloat01(x);
				x = Rand32Utility.Next(x);
				return ret;
			}
		}

		public Vector2 InsideUnitCircle
		{
			get
			{
				Vector2 ret;
				Rand32Utility.InsideUnitCircle(out ret, out x, x);
				return ret;
			}
		}

		public Rand32S(int seed)
		{
			x = 0; // structの制約により一旦初期化
			SetSeed(seed);
		}

		public void SetSeed(int seed)
		{
			seed = (seed & 0x7fffffff) + 1; // 31bitでループする。絶対に0にはならない。
			x = (uint)seed;
		}

		public int Range(int minIncluded, int maxExcluded)
		{
			var ret = Rand32Utility.ToRange(minIncluded, maxExcluded, x);
			x = Rand32Utility.Next(x);
			return ret;
		}

		public float Range(float minIncluded, float maxIncluded)
		{
			var ret = Rand32Utility.ToRange(minIncluded, maxIncluded, x);
			x = Rand32Utility.Next(x);
			return ret;
		}

		public int Next(int maxExcluded)
		{
			var ret = Rand32Utility.ToRange(maxExcluded, x);
			x = Rand32Utility.Next(x);
			return ret;
		}

		public float Next(float maxIncluded)
		{
			var ret = Rand32Utility.ToRange(maxIncluded, x);
			x = Rand32Utility.Next(x);
			return ret;
		}

		public uint NextRaw()
		{
			var ret = x;
			x = Rand32Utility.Next(x);
			return ret;
		}

		// non public ----
		uint x;
	}

	public static class Rand32Utility
	{
		public static uint Next(uint x)
		{
			x ^= x << 13;
			x ^= x >> 17;
			x ^= x << 5;
			return x;
		}

		public static float ToFloat01(uint x)
		{
			return ((float)(x - 1) / (float)0xfffffffe);
		}

		public static float ToRange(float maxIncluded, uint x)
		{
			return ((float)(x - 1) * maxIncluded / (float)0xfffffffe);
		}

		public static float ToRange(float minIncluded, float maxIncluded, uint x)
		{
			var r = maxIncluded - minIncluded;
			return ((float)(x - 1) * r / (float)0xfffffffe) + minIncluded;
		}

		public static int ToRange(int maxExcluded, uint x)
		{
			return (int)(x % (uint)maxExcluded);
		}

		public static int ToRange(int minIncluded, int maxExcluded, uint x)
		{
			var r = maxExcluded - minIncluded;
			return (int)(x % (uint)r) + minIncluded;
		}

		public static void InsideUnitCircle(
			out Vector2 result,
			out uint newX,
			uint x)
		{
			var theta = ToRange(Mathf.PI * 2f, x);
			x = Next(x);
			var r = Mathf.Sqrt(ToFloat01(x));
			result.x = r * Mathf.Cos(theta);
			result.y = r * Mathf.Sin(theta);
			newX = Next(x);
		}
	}
}