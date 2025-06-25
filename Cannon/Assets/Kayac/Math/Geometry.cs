using UnityEngine;

namespace Kayac
{
	public static class Geometry
	{
		public static void MakeHermiteCurve(
			Vector3 beginPosition,
			Vector3 endPosition,
			Vector3 beginVelocity,
			Vector3 endVelocity,
			out Vector3 a3,
			out Vector3 a2,
			out Vector3 a1,
			out Vector3 a0)
		{
			/*
			まず別名
			p0, p1, v0, v1とする

			at^3 + bt^2 + ct + d
			において、t=0の時に
			d = p0
			c = v0
			t=1の時に
			a + b + c + d = p1
			3a + 2b + c = v1
			
			c,dを即座に置き換えて、
			a + b + v0 + p0 = p1
			3a + 2b + v0 = v1

			上を3倍して引く
			b + 2v0 + 3p0 = 3p1 - v1
			*/

			a0 = beginPosition;
			a1 = beginVelocity;
			a2 = 3f * (endPosition - beginPosition) - (2f * beginVelocity) - endVelocity;
			a3 = endPosition - a2 - a1 - a0;
		}

		public static void MakeHermiteCurve(
			Vector2 beginPosition,
			Vector2 endPosition,
			Vector2 beginVelocity,
			Vector2 endVelocity,
			out Vector2 a3,
			out Vector2 a2,
			out Vector2 a1,
			out Vector2 a0)
		{
			// 手法は3Dと同様
			a0 = beginPosition;
			a1 = beginVelocity;
			a2 = 3f * (endPosition - beginPosition) - (2f * beginVelocity) - endVelocity;
			a3 = endPosition - a2 - a1 - a0;
		}

		// 両端位置と、両端速度から3次曲線を生成し、与えられた点(point)との最近接点を返す
		public static Vector3 ClosestPointHermite(
			Vector3 beginPosition,
			Vector3 endPosition,
			Vector3 beginVelocity,
			Vector3 endVelocity,
			Vector3 point,
			int newtonIteration,
			out float tOut)
		{
			Vector3 a3, a2, a1, a0;
			MakeHermiteCurve(beginPosition, endPosition, beginVelocity, endVelocity, out a3, out a2, out a1, out a0);
			/*
			P(t) = a3*t^3 + a2*t^2 + a1*t + a0
			で、pointをQと置くと、
			F(t) = dot(P(t) - Q, P(t) - Q)が最小になるtを求める
			これを微分するが、積の微分法から、
			F'(t) = 2*dot(P(t) - Q, P'(t)) = 0

			ニュートン法のためにさらに微分し、
			F''(t) = 2*dot(P'(t), P'(t)) + 2*dot(P(t) - Q, P''(t)) 
				   = 2(dot(P'(t), P'(t)) + dot(P(t) - Q, P''(t))

			あとは、
			xn+1 = xn - F'(xn) / F''(xn)
			で繰り返す。
			*/

			var e = a0 - point;
			// 初期値は直線近似で得られたものを使う
			var t = ClosestPointParameter(beginPosition, endPosition - beginPosition, point);
			var p = Polynominal3d(t, a3, a2, a1, a0);
			var error = Vector3.Distance(p, point);
			var ret = p;
			tOut = t;

			for (var i = 0; i < newtonIteration; i++)
			{
				var p1 = Polynominal3d(t, 3f * a3, 2f * a2, a1);
				var p2 = Polynominal3d(t, 6f * a3, 2f * a2);

				var f1 = Vector3.Dot(p - point, p1); // * 2は省略
				var f2 = Vector3.Dot(p1, p1) + Vector3.Dot(p - point, p2); // * 2は省略

				if (f2 == 0f)
				{
					break;
				}

				var newT = Mathf.Clamp01(t - (f1 / f2));
				var newP = Polynominal3d(newT, a3, a2, a1, a0);
				var newError = Vector3.Distance(newP, point);
//Debug.Log(i + "\t" + t + " -> " + newT + " \t" + p + " -> " + newP + " \t" + error + " -> " + newError);
				if (newError < error) // 最小値を更新
				{
					error = newError;
					ret = newP;
					tOut = newT;
				}
				t = newT;
				p = newP;
			}
			return ret;
		}

		// 両端位置と、両端速度から3次曲線を生成し、与えられた点(point)との最近接点を返す
		public static Vector2 ClosestPointHermite(
			Vector2 beginPosition,
			Vector2 endPosition,
			Vector2 beginVelocity,
			Vector2 endVelocity,
			Vector2 point,
			int newtonIteration,
			out float tOut)
		{
			Vector2 a3, a2, a1, a0;
			MakeHermiteCurve(beginPosition, endPosition, beginVelocity, endVelocity, out a3, out a2, out a1, out a0);
			// 手法は3Dと同様

			var e = a0 - point;
			// 初期値は直線近似で得られたものを使う
			var t = ClosestPointParameter(beginPosition, endPosition - beginPosition, point);
			var p = Polynominal3d(t, a3, a2, a1, a0);
			var error = Vector2.Distance(p, point);
			var ret = p;
			tOut = t;

			for (var i = 0; i < newtonIteration; i++)
			{
				var p1 = Polynominal3d(t, 3f * a3, 2f * a2, a1);
				var p2 = Polynominal3d(t, 6f * a3, 2f * a2);

				var f1 = Vector2.Dot(p - point, p1); // * 2は省略
				var f2 = Vector2.Dot(p1, p1) + Vector2.Dot(p - point, p2); // * 2は省略

				if (f2 == 0f)
				{
					break;
				}

				var newT = Mathf.Clamp01(t - (f1 / f2));
				var newP = Polynominal3d(newT, a3, a2, a1, a0);
				var newError = Vector2.Distance(newP, point);
//Debug.Log(i + "\t" + t + " -> " + newT + " \t" + p + " -> " + newP + " \t" + error + " -> " + newError);
				if (newError < error) // 最小値を更新
				{
					error = newError;
					ret = newP;
					tOut = newT;
				}
				t = newT;
				p = newP;
			}
			return ret;
		}

		public static Vector3 Polynominal3d(float t, Vector3 a3, Vector3 a2, Vector3 a1, Vector3 a0)
		{
			var ret = a3;
			ret *= t;
			ret += a2;
			ret *= t;
			ret += a1;
			ret *= t;
			ret += a0;
			return ret;
		}

		public static Vector2 Polynominal3d(float t, Vector2 a3, Vector2 a2, Vector2 a1, Vector2 a0)
		{
			var ret = a3;
			ret *= t;
			ret += a2;
			ret *= t;
			ret += a1;
			ret *= t;
			ret += a0;
			return ret;
		}

		public static Vector3 Polynominal3d(float t, Vector3 a2, Vector3 a1, Vector3 a0)
		{
			var ret = a2;
			ret *= t;
			ret += a1;
			ret *= t;
			ret += a0;
			return ret;
		}

		public static Vector2 Polynominal3d(float t, Vector2 a2, Vector2 a1, Vector2 a0)
		{
			var ret = a2;
			ret *= t;
			ret += a1;
			ret *= t;
			ret += a0;
			return ret;
		}

		public static Vector3 Polynominal3d(float t, Vector3 a1, Vector3 a0)
		{
			var ret = a1;
			ret *= t;
			ret += a0;
			return ret;
		}

		public static Vector2 Polynominal3d(float t, Vector2 a1, Vector2 a0)
		{
			var ret = a1;
			ret *= t;
			ret += a0;
			return ret;
		}

		// 直線と点の最近接点を求めるためにパラメータを返す。すなわちa+btのt
		// 線分内に限定したい場合は呼び出し側でClamp01すること
		public static float ClosestPointParameter(
			Vector3 segmentBegin,
			Vector3 segmentVector,
			Vector3 point)
		{
			/*
			|a + bt - p|
			が最小になるtを求める
			a - p = cと置き、
			|c + bt|^2
			内積で展開して(以下aとbの内積をabと書く)
			cc + 2*bc*t + bb*t^2
			これの微分が0であれば、2次の係数が正なので最小値になる。
			2*bc + 2*bb*t = 0

			t = -bc / bb
			*/

			var c = segmentBegin - point;
			var b = segmentVector;
			var bb = b.sqrMagnitude;
			float ret;
			if (bb <= 0f) // 計算不能。正負があるのでInfinityでなくNaNを返す。必ず確認せよ。
			{
				ret = float.NaN;
			}
			else
			{
				var bc = Vector3.Dot(b, c);
				ret = -bc / bb;
			}
			return ret;			
		}

		// 2D版
		public static float ClosestPointParameter(
			Vector2 segmentBegin,
			Vector2 segmentVector,
			Vector2 point)
		{
			var c = segmentBegin - point;
			var b = segmentVector;
			var bb = b.sqrMagnitude;
			float ret;
			if (bb <= 0f) // 計算不能。正負があるのでInfinityでなくNaNを返す。必ず確認せよ。
			{
				ret = float.NaN;
			}
			else
			{
				var bc = Vector2.Dot(b, c);
				ret = -bc / bb;
			}
			return ret;			
		}
	}
}
