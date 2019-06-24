using UnityEngine;

namespace Kayac
{
	static class Math
	{
		public static void SetMul(out Vector3 o, ref Vector3 a, float s)
		{
			o.x = a.x * s;
			o.y = a.y * s;
			o.z = a.z * s;
		}

		public static void SetAdd(out Vector3 o, ref Vector3 a, ref Vector3 b)
		{
			o.x = a.x + b.x;
			o.y = a.y + b.y;
			o.z = a.z + b.z;
		}

		public static void SetSub(out Vector3 o, ref Vector3 a, ref Vector3 b)
		{
			o.x = a.x - b.x;
			o.y = a.y - b.y;
			o.z = a.z - b.z;
		}

		public static void SetMadd(out Vector3 o, ref Vector3 a, ref Vector3 b, float s)
		{
			o.x = a.x + (b.x * s);
			o.y = a.y + (b.y * s);
			o.z = a.z + (b.z * s);
		}

		public static void SetMsub(out Vector3 o, ref Vector3 a, ref Vector3 b, float s)
		{
			o.x = a.x - (b.x * s);
			o.y = a.y - (b.y * s);
			o.z = a.z - (b.z * s);
		}

		public static void Mul(ref Vector3 inOut, float s)
		{
			inOut.x *= s;
			inOut.y *= s;
			inOut.z *= s;
		}

		public static void Madd(ref Vector3 inOut, ref Vector3 a, float s)
		{
			inOut.x += a.x * s;
			inOut.y += a.y * s;
			inOut.z += a.z * s;
		}

		public static void Msub(ref Vector3 inOut, ref Vector3 a, float s)
		{
			inOut.x -= a.x * s;
			inOut.y -= a.y * s;
			inOut.z -= a.z * s;
		}

		public static void Add(ref Vector3 inOut, ref Vector3 a)
		{
			inOut.x += a.x;
			inOut.y += a.y;
			inOut.z += a.z;
		}

		public static void Sub(ref Vector3 inOut, ref Vector3 a)
		{
			inOut.x -= a.x;
			inOut.y -= a.y;
			inOut.z -= a.z;
		}

		public static void GetSphericalDistribution(
			out float xAngle,
			out float yAngle,
			ref Random32 random)
		{
			yAngle = random.GetFloat(-Mathf.PI, Mathf.PI);
			var r = random.GetFloat();
			xAngle = Mathf.Asin((2f * r) - 1f);
		}

		public static void GetHemisphericalDistribution(
			out float xAngle,
			out float yAngle,
			ref Random32 random)
		{
			yAngle = random.GetFloat(-Mathf.PI, Mathf.PI);
			var r = random.GetFloat();
			xAngle = Mathf.Acos(r);
		}

		public static void GetHemisphericalCosDistribution(
			out float xAngle,
			out float yAngle,
			ref Random32 random)
		{
			yAngle = random.GetFloat(-Mathf.PI, Mathf.PI);
			var r = random.GetFloat();
			xAngle = Mathf.Asin(Mathf.Sqrt(r));
		}

		public static void GetHemisphericalCosPoweredDistribution(
			out float xAngle,
			out float yAngle,
			float power,
			ref Random32 random)
		{
			yAngle = random.GetFloat(-Mathf.PI, Mathf.PI);
			var r = random.GetFloat();
			var powered = Mathf.Pow(r, 1f / (power + 1f));
			xAngle = Mathf.Acos(powered);
		}

	}
}