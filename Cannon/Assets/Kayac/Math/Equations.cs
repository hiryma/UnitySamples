using UnityEngine;

namespace Kayac
{
	public static class Equations
	{
		// 実数範囲で二次方程式を解く。戻り値は解の個数だが、重解は「同じ値が2つ」として2を返す。1次方程式であれば1を返す。
		public static int SolveQuadratic(
			float a2,
			float a1,
			float a0,
			out float x1,
			out float x2)
		{
			// (a2 * x^2) + (a1 * x) + a0 = 0 を解く。
			var ret = 0;
			if (a2 == 0f) // 1次方程式である場合
			{
				if (a1 == 0f) // 定数項が0である場合、ここでは解なしとして扱う。
				{
					x1 = x2 = float.NaN;
					ret = 0;
				}
				else
				{
					x1 = x2 = -a0 / a1;
					ret = 1;
				}
			}
			else
			{
				/*
				a2で除して2次の定数を1とする。
				b1 = a1 / a2
				b0 = a0 / a2
				f(x) = x^2 + (b1 * x) + b0 = 0
				*/
				var b1 = a1 / a2;
				var b0 = a0 / a2;
				/*
				b1の半分を加えて二乗を作る
				f(x) = (x + b1/2)^2 + b0 - (b1/2)^2 = 0
				(x + b1/2)^2 = (b1/2)^2 - b0
				x + b1/2 = +- sqrt((b1/2)^2 - b0)
				*/
				var discriminant = (0.25f * b1 * b1) - b0;
				if (discriminant < 0f)
				{
					x1 = x2 = float.NaN;
					ret = 0;
				}
				else
				{
					var sqrt = Mathf.Sqrt(discriminant);
					// ここで絶対値が大きな解を先に求める
					// b1 > 0ならマイナスを、b1 < 0ならプラス側
					if (b1 > 0f)
					{
						x1 = (-0.5f * b1) - sqrt;
					}
					else
					{
						x1 = (-0.5f * b1) + sqrt;
					}

					// 残りの解は桁落ちを防ぐために、解と係数の関係から求める
					// 和を使えば、x1 + x2 = -b1だが、これを使うと激しい桁落ちを生じかねない。
					// 積を使うと、x1 * x2 = b0なので、減算による桁落ちを防げる。
					x2 = b0 / x1;
					ret = 2; // 重解を区別しない
				}
			}
			return ret;		
		}
	}
}
