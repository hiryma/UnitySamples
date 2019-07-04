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

		public static void SetCross(out Vector3 o, ref Vector3 a, ref Vector3 b)
		{
			o.x = (a.y * b.z) - (a.z * b.y);
			o.y = (a.z * b.x) - (a.x * b.z);
			o.z = (a.x * b.y) - (a.y * b.x);
		}

		public static float Dot(ref Vector3 a, ref Vector3 b)
		{
			return (a.x * b.x) + (a.y * b.y) + (a.z * b.z);
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

		public static void RotateVector(
			out Vector3 output,
			ref Vector3 v,
			ref Vector3 axisNormalized, // 軸ベクトルは要正規化
			float radian)
		{
			Vector3 c, p, q;
			// vを軸に射影して、回転円中心cを得る
			float dot = Dot(ref v, ref axisNormalized);
			SetMul(out c, ref axisNormalized, dot);
			// 中心からv終点=Vへと向かうpを得る
			SetSub(out p, ref v, ref c);

			// p及びaと直交するベクタを得る
			SetCross(out q, ref axisNormalized, ref p);
			// a,pは直交しているから、|q|=|p|

			// 回転後のv'の終点V'は、V' = c + s*p + t*q と表せる。
			// ここで、s=cosθ t=sinθ
			var s = Mathf.Cos(radian);
			var t = Mathf.Sin(radian);
			SetMadd(out output, ref c, ref p, s);
			Madd(ref output, ref q, t);
		}

		public static void RotateVectorOrthogonal(
			out Vector3 output,
			ref Vector3 v, // axisNormalizedと直交していること
			ref Vector3 axisNormalized, // 軸ベクトルは要正規化
			float radian)
		{
			// vを軸に射影して、回転円中心cを得る操作は省略。
			// 中心からv終点=Vへと向かうpはvそのまま

			// p及びaと直交するベクタを得る
			Vector3 q;
			SetCross(out q, ref axisNormalized, ref v);
			// a,pは直交しているから、|q|=|p|

			// 回転後のv'の終点V'は、V' = c + s*p + t*q と表せる。今cは原点なので省略。
			// ここで、s=cosθ t=sinθ
			var s = Mathf.Cos(radian);
			var t = Mathf.Sin(radian);
			SetMul(out output, ref v, s);
			Madd(ref output, ref q, t);
		}

		// 0ベクタチェックしないよ。事前にやっといてね
		public static void SetNormalized(out Vector3 output, ref Vector3 a)
		{
			float l2 = (a.x * a.x) + (a.y * a.y) + (a.z * a.z);
			float l = l = Mathf.Sqrt(l2);
			float rcpL = 1f / l;
			output.x = a.x * rcpL;
			output.y = a.y * rcpL;
			output.z = a.z * rcpL;
		}

		public static void Normalize(ref Vector3 a)
		{
			float l2 = (a.x * a.x) + (a.y * a.y) + (a.z * a.z);
			float l = l = Mathf.Sqrt(l2);
			float rcpL = 1f / l;
			a.x *= rcpL;
			a.y *= rcpL;
			a.z *= rcpL;
		}
	}
}