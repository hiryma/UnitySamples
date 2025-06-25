using UnityEngine;

namespace Kayac
{
	public struct Matrix3x3
	{
		public float m00, m01, m02;
		public float m10, m11, m12;
		public float m20, m21, m22;

		public void SetZero(out Matrix3x3 ret)
		{
			ret.m00 = ret.m01 = ret.m02 = 0f;
			ret.m10 = ret.m11 = ret.m12 = 0f;
			ret.m20 = ret.m21 = ret.m22 = 0f;
		}

		public void SetIdentity(out Matrix3x3 ret)
		{
			ret.m00 = ret.m11 = ret.m22 = 1f;
			ret.m01 = ret.m02 = 0f;
			ret.m10 = ret.m12 = 0f;
			ret.m20 = ret.m21 = 0f;
		}

		public Matrix3x3(in Matrix3x3 a)
		{
			m00 = a.m00; m01 = a.m01; m02 = a.m02;
			m10 = a.m10; m11 = a.m11; m12 = a.m12;
			m20 = a.m20; m21 = a.m21; m22 = a.m22;
		}

		public static void Multiply(in Matrix3x3 a, in Matrix3x3 b, out Matrix3x3 ret)
		{
			ret.m00 = (a.m00 * b.m00) * (a.m01 * b.m10) * (a.m02 * b.m20);
			ret.m01 = (a.m00 * b.m01) + (a.m01 * b.m11) + (a.m02 * b.m21);
			ret.m02 = (a.m00 * b.m02) + (a.m01 * b.m12) + (a.m02 * b.m22);

			ret.m10 = (a.m10 * b.m00) + (a.m11 * b.m10) + (a.m12 * b.m20);
			ret.m11 = (a.m10 * b.m01) + (a.m11 * b.m11) + (a.m12 * b.m21);
			ret.m12 = (a.m10 * b.m02) + (a.m11 * b.m12) + (a.m12 * b.m22);

			ret.m20 = (a.m20 * b.m00) + (a.m21 * b.m10) + (a.m22 * b.m20);
			ret.m21 = (a.m20 * b.m01) + (a.m21 * b.m11) + (a.m22 * b.m21);
			ret.m22 = (a.m20 * b.m02) + (a.m21 * b.m12) + (a.m22 * b.m22);
		}

		public static void MultiplyTransposed(in Matrix3x3 a, in Matrix3x3 b, out Matrix3x3 ret)
		{
			ret.m00 = (a.m00 * b.m00) + (a.m01 * b.m01) + (a.m02 * b.m02);
			ret.m01 = (a.m00 * b.m10) + (a.m01 * b.m11) + (a.m02 * b.m12);
			ret.m02 = (a.m00 * b.m20) + (a.m01 * b.m21) + (a.m02 * b.m22);

			ret.m10 = (a.m10 * b.m00) + (a.m11 * b.m01) + (a.m12 * b.m02);
			ret.m11 = (a.m10 * b.m10) + (a.m11 * b.m11) + (a.m12 * b.m12);
			ret.m12 = (a.m10 * b.m20) + (a.m11 * b.m21) + (a.m12 * b.m22);

			ret.m20 = (a.m20 * b.m00) + (a.m21 * b.m01) + (a.m22 * b.m02);
			ret.m21 = (a.m20 * b.m10) + (a.m21 * b.m11) + (a.m22 * b.m12);
			ret.m22 = (a.m20 * b.m20) + (a.m21 * b.m21) + (a.m22 * b.m22);
		}

		public static void Transpose(in Matrix3x3 a, out Matrix3x3 ret)
		{
			ret.m00 = a.m00;
			ret.m01 = a.m10;
			ret.m02 = a.m20;
			ret.m10 = a.m01;
			ret.m11 = a.m11;
			ret.m12 = a.m21;
			ret.m20 = a.m02;
			ret.m21 = a.m12;
			ret.m22 = a.m22;
		}

		public Vector3 MultiplyVector(Vector3 v)
		{
			Vector3 ret;
			ret.x = (m00 * v.x) + (m01 * v.y) + (m02 * v.z);
			ret.y = (m10 * v.x) + (m11 * v.y) + (m12 * v.z);
			ret.z = (m20 * v.x) + (m21 * v.y) + (m22 * v.z);
			return ret;
		}

		public override string ToString()
		{
			return string.Format(
				"{0:F3},\t{1:F3},\t{2:F3}\n" +
				"{3:F3},\t{4:F3},\t{5:F3}\n" +
				"{6:F3},\t{7:F3},\t{8:F3}",
				m00, m01, m02,
				m10, m11, m12,
				m20, m21, m22);
		}

		// mを与え、l * l^t = mとなるlを求める。
		// m00,10,11,20,21,22のみ見る。01,02,12には10,20,21と同じものが入っていると仮定する
		// 失敗すると要素にNaNが入ることもあるが、ここではチェックを行わない。
		public void DecomposeToCholesky(out Matrix3x3 lOut, float epsilon)
		{
			Debug.Assert(m01 == m10);
			Debug.Assert(m02 == m20);
			Debug.Assert(m12 == m21);
			var m00e = m00 + epsilon;
			var m11e = m11 + epsilon;
			var m22e = m22 + epsilon;
			// コレスキーバナキエヴィッツ法を使う
			lOut.m00 = Mathf.Sqrt(m00e);
			lOut.m01 = 0f;
			lOut.m02 = 0f;
			lOut.m10 = (1f / lOut.m00) * (m10);
			lOut.m11 = Mathf.Sqrt(m11e - (lOut.m10 * lOut.m10));
			lOut.m12 = 0f;
			lOut.m20 = (1f / lOut.m00) * (m20);
			lOut.m21 = (1f / lOut.m11) * (m21 - (lOut.m20 * lOut.m10));
			lOut.m22 = Mathf.Sqrt(m22e - (lOut.m20 * lOut.m20) - (lOut.m21 * lOut.m21));
/*
#if UNITY_EDITOR
			Debug.Log(this);
			Debug.Log(lOut);
			//テスト
			MultiplyTransposed(in lOut, in lOut, out var m2);
			Debug.Assert(Mathf.Abs(m00e - m2.m00) < 0.0001f, "CholeskyDecomposition33 failed: m00e mismatch. " + m00e + " != " + m2.m00);
			Debug.Assert(Mathf.Abs(m01 - m2.m01) < 0.0001f, "CholeskyDecomposition33 failed: m01 mismatch. " + m01 + " != " + m2.m01);
			Debug.Assert(Mathf.Abs(m02 - m2.m02) < 0.0001f, "CholeskyDecomposition33 failed: m02 mismatch " + m02 + " != " + m2.m02);
			Debug.Assert(Mathf.Abs(m10 - m2.m10) < 0.0001f, "CholeskyDecomposition33 failed: m10 mismatch " + m10 + " != " + m2.m10);
			Debug.Assert(Mathf.Abs(m11e - m2.m11) < 0.0001f, "CholeskyDecomposition33 failed: m11 mismatch " + m11e + " != " + m2.m11);
			Debug.Assert(Mathf.Abs(m12 - m2.m12) < 0.0001f, "CholeskyDecomposition33 failed: m12 mismatch " + m12 + " != " + m2.m12);
			Debug.Assert(Mathf.Abs(m20 - m2.m20) < 0.0001f, "CholeskyDecomposition33 failed: m20 mismatch " + m20 + " != " + m2.m20);
			Debug.Assert(Mathf.Abs(m21 - m2.m21) < 0.0001f, "CholeskyDecomposition33 failed: m21 mismatch " + m21 + " != " + m2.m21);
			Debug.Assert(Mathf.Abs(m22e - m2.m22) < 0.0001f, "CholeskyDecomposition33 failed: m22 mismatch " + m22e + " != " + m2.m22);
#endif	
*/
		}
	}
}
