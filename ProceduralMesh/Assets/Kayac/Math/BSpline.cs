using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kayac
{
	// 二次で一様ノット限定のB-Splineを生成する
	public static class BSpline
	{
		public static Vector2[] Generate(
			IList<Vector2> points,
			int div,
			bool looped)
		{
			int beginT, endT;
			if (looped)
			{
				beginT = 0;
				endT = points.Count;
			}
			else
			{
				beginT = -1;
				endT = points.Count;
			}
			var outPointCount = (endT - beginT) * div;
			var ret = new Vector2[outPointCount];
			for (var i = 0; i <= outPointCount; i++)
			{
				var t = beginT + ((endT - beginT) * ((float)i / (float)outPointCount));
				ret[i] = Evaluate(points, t, looped);
			}
			return ret;
		}

		// non-public --------------------------
		static Vector2 Evaluate(
			IList<Vector2> points,
			float t,
			bool looped)
		{
			var ret = Vector2.zero;
			// tと次数でどの範囲の点を足すかを判断する。
			var basisWidth = 3;
			var pointBegin = Mathf.CeilToInt(t - ((float)basisWidth * 0.5f));
			var pointEnd = pointBegin + basisWidth;
			for (var i = 0; i < basisWidth; ++i)
			{
				var index = pointBegin + i;
				var basisT = index;
				var basisWeight = CalculateBasisWeight(basisT, t);
				if (index < 0)
				{
					if (looped)
					{
						index += points.Count;
					}
					else
					{
						index = 0;
					}
				}
				else if (index >= points.Count)
				{
					if (looped)
					{
						index = index % points.Count;
					}
					else
					{
						index = points.Count - 1;
					}
				}
				ret += points[index] * basisWeight;
			}
			return ret;
		}

		static float CalculateBasisWeight(float basisT, float t)
		{
			var ret = 0f;
			float nt;
			if (t < (basisT - 1.5f))
			{
				// 何もしない
			}
			else if (t < (basisT - 0.5f))
			{
				nt = (t - (basisT - 1.5f));
				ret = 0.5f * nt * nt;
			}
			else if (t < (basisT + 0.5f))
			{
				nt = (t - basisT);
				ret = 0.75f - (nt * nt);
			}
			else if (t < (basisT + 1.5f))
			{
				nt = (t - (basisT + 1.5f));
				ret = 0.5f * nt * nt;
			}
			return ret;
		}
	}
}