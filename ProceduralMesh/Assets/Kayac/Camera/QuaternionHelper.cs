using UnityEngine;

// 速度は重視していない
namespace Kayac
{
	// 名前が長いのでusingしてお使いになるのが良いでしょう。Extensionにはしない。
	public static class QuaternionHelper
	{
		public static float SqNorm(Quaternion q)
		{
			return (q.x * q.x) + (q.y * q.y) + (q.z * q.z) + (q.w * q.w);
		}

		public static float Norm(Quaternion q)
		{
			return Mathf.Sqrt(SqNorm(q));
		}

		public static Quaternion SetNorm(Quaternion q, float newNorm)
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

		public static Quaternion Normalize(Quaternion q)
		{
			return SetNorm(q, 1f);
		}

		public static Quaternion Multiply(Quaternion a, Quaternion b)
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

		public static Quaternion Multiply(Quaternion q, Vector3 v)
		{
			return Multiply(q, new Quaternion(v.x, v.y, v.z, 0f));
		}

		public static Quaternion Multiply(Quaternion q, float s)
		{
			return new Quaternion(q.x * s, q.y * s, q.z * s, q.w * s);
		}

		public static Quaternion Add(Quaternion a, Quaternion b)
		{
			return new Quaternion(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
		}

		public static Quaternion Madd(Quaternion a, Quaternion b, float s)
		{
			return new Quaternion(
				a.x + (b.x * s),
				a.y + (b.y * s),
				a.z + (b.z * s),
				a.w + (b.w * s));
		}

		public static Vector3 Transform(Quaternion q, Vector3 v)
		{
			var t = Multiply(Multiply(q, v), Conjugate(q));
			return new Vector3(t.x, t.y, t.z);
		}

		public static Quaternion Integrate(Quaternion q, Vector3 w, float deltaTime)
		{
			// q += q * (w * (0.5 * deltaTime))
			float norm = Norm(q);
			var t = Multiply(q, w);
			var r = Madd(q, t, 0.5f * deltaTime);
			return SetNorm(r, norm); // ノルムを元に戻す。
		}

		public static Quaternion Conjugate(Quaternion q)
		{
			return new Quaternion(-q.x, -q.y, -q.z, q.w);
		}
	}
}
