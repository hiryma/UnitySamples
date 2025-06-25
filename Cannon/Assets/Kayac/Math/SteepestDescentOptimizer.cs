using UnityEngine;

namespace Kayac
{
	public static class SteepestDescentOptimizer
	{
		public static bool Refine(
			System.Func<Vector3, float> errorFunc,
			Vector3 parameter,
			float alpha,
			float error,
			float alphaScale,
			float minimumAlpha,
			float parameterDelta,
			out Vector3 parameterOut,
			out float alphaOut,
			out float errorOut)
		{
			Debug.Assert(alphaScale > 1f, "alphaScale must be greater than 1");
			// 数値微分(中心差分)
			var e0__ = errorFunc(parameter - new Vector3(parameterDelta, 0f, 0f));
			var e1__ = errorFunc(parameter + new Vector3(parameterDelta, 0f, 0f));
			var e_0_ = errorFunc(parameter - new Vector3(0f, parameterDelta, 0f));
			var e_1_ = errorFunc(parameter + new Vector3(0f, parameterDelta, 0f));
			var e__0 = errorFunc(parameter - new Vector3(0f, 0f, parameterDelta));
			var e__1 = errorFunc(parameter + new Vector3(0f, 0f, parameterDelta));
//Debug.Log("Refine e=" + error + "\te0=" + e0__ + "\te1=" + e1__ + "\te_0_=" + e_0_ + "\te_1_=" + e_1_ + "\te__0=" + e__0 + "\te__1=" + e__1);
			var dError = new Vector3(e1__ - e0__, e_1_ - e_0_, e__1 - e__0) / (2f * parameterDelta);

			var newParameter = parameter - (dError * alpha);
			var newError = errorFunc(newParameter);
//Debug.Log("Refine: " + parameter + " -> " + newParameter + " \t" + error + " -> " + newError + " " + dError);
			bool ret;
			if (newError < error)
			{
				// 更新
				parameterOut = newParameter;
				alphaOut = alpha * alphaScale;
				errorOut = newError;
				ret = true;
			}
			else
			{
				// 更新しない
				parameterOut = parameter;
				alphaOut = Mathf.Max(alpha / alphaScale, minimumAlpha);
				errorOut = error;
				ret = false;
			}
			return ret;
		}

		public static bool Refine(
			System.Func<float, float> errorFunc,
			float parameter,
			float alpha,
			float error,
			float alphaScale,
			float minimumAlpha,
			float parameterDelta,
			out float parameterOut,
			out float alphaOut,
			out float errorOut)
		{
			Debug.Assert(alphaScale > 1f, "alphaScale must be greater than 1");
			// 数値微分(中心差分)
			var e0 = errorFunc(parameter - parameterDelta);
			var e1 = errorFunc(parameter + parameterDelta);
//Debug.Log("Refine e=" + error + "\te0=" + e0 + "\te1=" + e1);
			var dError = (e1 - e0) / (2f * parameterDelta);

			var newParameter = parameter - (dError * alpha);
			var newError = errorFunc(newParameter);
			bool ret;
			if (newError < error)
			{
				// 更新
				parameterOut = newParameter;
				alphaOut = alpha * alphaScale;
				errorOut = newError;
				ret = true;
			}
			else
			{
				// 更新しない
				parameterOut = parameter;
				alphaOut = Mathf.Max(alpha / alphaScale, minimumAlpha);
				errorOut = error;
				ret = false;
			}
			return ret;
		}
	}
}