using UnityEngine;

namespace Kayac
{
	public static class ProjectileMath
	{
		// ある角度で撃った時の最近接距離を返す。
		// 慣性抵抗によって減速することを鑑み、加速度が大きい時ほどtimeStepを小さくする。
		public static Vector3 CalculateError(
			Vector3 projectilePosition,
			Vector3 projectileVelocity,
			Vector3 targetPosition,
			Vector3 targetVelocity,
			Vector3 targetAccelAverage,
			Matrix3x3 targetAccelCovarianceCholesky,
			int targetAccelRandSeed,
			Vector3 gravity,
			float mass,
			float inertiaResistance,
			float timeStepFactor,
			int maxIterations,
			out float timeToClosestOut,
			out bool convergedOut)
		{
			timeToClosestOut = 0f; // 初期値
			convergedOut = false;

			Debug.Assert(mass > 0f);
			Debug.Assert(inertiaResistance >= 0f);
			// 別名をつける
			var p = projectilePosition;
			var v = projectileVelocity;
			var tp = targetPosition;
			var tv = targetVelocity;

			var e = tp - p;
			var error = e;
			var minError = error;
			var dot = Vector3.Dot(e, v);

			var absG = gravity.magnitude;
			var rand = new StandardNormalDistributionGenerator(targetAccelRandSeed);

			float dt;
			for (var iterationIndex = 0; iterationIndex < maxIterations; iterationIndex++)
			{
				var absV = v.magnitude;
				var f = -(inertiaResistance * absV) * v; // 空気抵抗
				f += mass * gravity; 
				var a = f / mass; // 加速度
				// fが大きいほどdtを小さくする
				var absA = a.magnitude;
				dt = timeStepFactor / Mathf.Max(absA, absG);

//Debug.Log(dt + "\ta=" + a + "\tv=" + v + "\tp=" + p + "\te=" + error + "\tme=" + minError);
				// 積分
				var halfDt = dt * 0.5f;
				// 中点法(弾体)
				var midV = v + (a * halfDt);
				var midF = -(inertiaResistance * midV.magnitude) * midV; // 空気抵抗
				midF += mass * gravity; 
				var midA = midF / mass;
				var dv = midA * dt;
				var dp = midV * dt; 
				var newV = v + dv;
				var newP = p + dp;

				// ターゲット
				var ta = SampleAcceleration(
					in targetAccelAverage,
					in targetAccelCovarianceCholesky,
					ref rand);
ta = Vector3.zero;
				var midTv = v + (a * halfDt);
				var newTv = tv + (ta * dt); // ターゲットの速度を更新。オイラー。
				// ターゲットの位置は台形積分
				var newTp = tp + (midTv * dt);

				// 距離
				var newE = newTp - newP;
				var newError = newE;
				if (newError.sqrMagnitude < minError.sqrMagnitude) // 最小値を更新
				{
					minError = newError;
				}

				// はさんだ時には補間を行う
				var newDot = Vector3.Dot(newE, newV); 
				if ((dot * newDot) < 0f) // 内積の符合が変化しているので、間で補間する
				{
					float t;
					var interpolatedP = Geometry.ClosestPointHermite(p, newP, v, newV, newTp, 3, out t);
					newError = tp - interpolatedP;
					if (newError.sqrMagnitude < minError.sqrMagnitude) // 最小値を更新
					{
						minError = newError;
						timeToClosestOut += (t * dt); // 補間したので、時間も補正
					}
					else
					{
						timeToClosestOut += dt;
					}
				}
				else
				{
					timeToClosestOut += dt;
				}

				// 打ち切り判定
				// 鉛直速度が負で、前回より誤差が増した場合は打ち切り
				if ((newV.y < 0f) && (newError.sqrMagnitude > error.sqrMagnitude))
				{
					convergedOut = true;
					break; // 打ち切り					
				}

				v = newV;
				p = newP;
				error = newError;
				tp = newTp; 
				tv = newTv; // ターゲット位置と速度を更新
			}

//Debug.Log("END: " + targetPosition + " -> " + tp + " " + targetAccelAverage + " e=" + minError.magnitude);
			return minError;
		}

		public static Vector3 SampleAcceleration(
			in Vector3 accelAverage,
			in Matrix3x3 accelCovarianceCholesky,
			ref StandardNormalDistributionGenerator rand)
		{
			var ret = accelAverage;
			var n = rand.Sample3();
			ret += accelCovarianceCholesky.MultiplyVector(n);
			return ret;
		}

		// ある角度で撃った時の最近接距離を返す。
		// 慣性抵抗によって減速することを鑑み、加速度が大きい時ほどtimeStepを小さくする。
		public static float CalculateErrorStatic(
			float elevationAngle,
			float horizontalDistance,
			float verticalDistance,
			float speed,
			float gravity,
			float mass,
			float inertiaResistance,
			float timeStepFactor,
			int maxIterations)
		{
			Debug.Assert(speed >= 0f);
			Debug.Assert(mass > 0f);
			Debug.Assert(inertiaResistance >= 0f);
			if ((speed == 0f) && (gravity == 0f))
			{
				return float.NaN; // 速度0かつ重力0なら計算不能
			}

			var tp = new Vector2(horizontalDistance, verticalDistance);
			var p = Vector2.zero;
			var v = new Vector2(speed * Mathf.Cos(elevationAngle), speed * Mathf.Sin(elevationAngle));
			var e = tp - p;
			var error = e.magnitude;
			var minError = error;
			var dot = Vector2.Dot(e, v);
			// 速度はオイラー、位置は台形積分を行う
			float dt;
			for (var i = 0; i < maxIterations; i++)
			{
				var absV = v.magnitude;
				var f = -(inertiaResistance * absV) * v; // 空気抵抗
				f.y -= mass * gravity; // 重力は下向きなので負の値
				var a = f / mass; // 加速度
				// fが大きいほどdtを小さくする
				var absA = a.magnitude;
				dt = timeStepFactor / Mathf.Max(absA, Mathf.Abs(gravity));

//Debug.Log(dt + "\ta=" + a + "\tv=" + v + "\tp=" + p + "\te=" + error + "\tme=" + minError);
				// 積分
				var halfDt = dt * 0.5f;
#if false // 速度オイラー、位置台形
				var dv = a * dt;
				var newV = v + dv;
				var dp = (v + newV) * halfDt; // 台形積分
#elif true // 中点法
				var midV = v + (a * halfDt);
				var midP = p + (v * halfDt);
				var midF = -(inertiaResistance * midV.magnitude) * midV; // 空気抵抗
				midF.y -= mass * gravity; // 重力は下向きなので負の値
				var midA = midF / mass;
				var dv = midA * dt;
				var dp = midV * dt; 
				var newV = v + dv;
#endif
				var newP = p + dp;

				// 距離
				var newE = tp - newP;
				var newError = newE.magnitude;
				if (newError < minError) // 最小値を更新
				{
					minError = newError;
				}
				// はさんだ時には補間を行う
				var newDot = Vector2.Dot(newE, newV); 
				if ((dot * newDot) < 0f) // 内積の符合が変化しているので、間で補間する
				{
#if true // hermite spline
					float t;
					var interpolatedP = Geometry.ClosestPointHermite(p, newP, v, newV, tp, 3, out t);
#else
					// 線分p, newPとtpの距離を求めるv
					var t = Geometry.ClosestPointParameter(p, dp, tp);
					t = Mathf.Clamp01(t); // 線分内に限定
					var interpolatedP = p + (t * dp);
#endif
					newError = Vector2.Distance(tp, interpolatedP);
					if (newError < minError) // 最小値を更新
					{
						minError = newError;
					}
				}
				// 打ち切り判定
				// 鉛直速度が負で、前回より誤差が増した場合は打ち切り
				if ((newV.y < 0f) && (newError > error))
				{
					break; // 打ち切り
				}

				v = newV;
				p = newP;
				error = newError;
			}
//Debug.Log(debugCount + "\tΘ=" + elevationAngle + "\tE=" + minError);
			return minError;
		}

		// 水平距離、垂直距離(+なら標的が高い所にいる)、を与えた時に仰角を返す。空気抵抗その他外力はないものとする。
		public static float CalculateElevationAngle(
			float horizontalDistance,
			float verticalDistance,
			float speed,
			float gravity) // gravityは下向きなら正の値。例えば9.81
		{
			/*
			p.x(t) = speed * t * cos(Θ)
			p.y(t) = speed * t * sin(Θ) - (0.5 * gravity * t^2)

			これを解く。長いので別名をつける。
			speed = s;
			horizontalDistance = h;
			verticalDistance = v;
			gravity = g;

			h = s * t * cos(Θ)
			v = s * t * sin(Θ) - (0.5 * g * t^2)

			t = h / (s * cos(Θ))

			v = s * (h / (s * cos(Θ))) * sin(Θ) - (0.5 * g * (h / (s * cos(Θ)))^2)
			v = h * tan(Θ) - (0.5 * g * h^2) / (s^2 * cos^2(Θ))

			cos(Θ)^2 = 1/(1 + tan(Θ)^2)を使って、
			v = h * tan(Θ) - (0.5 * g * h^2 * (1 + tan(Θ)^2)) / s^2

			全部左辺に移して、2次方程式として整理する
			2次: 0.5 * g * h^2 / s^2
			1次: -h
			0次: (0.5 * g * h^2 / s^2) + v

			*/
			var a2 = 0.5f * gravity * horizontalDistance * horizontalDistance / (speed * speed);
			var a1 = -horizontalDistance;
			var a0 = a2 + verticalDistance;
			float tan1, tan2;
			var rootCount = Equations.SolveQuadratic(a2, a1, a0, out tan1, out tan2);
			var ret = float.NaN;
			if (rootCount == 0)
			{
			}
			else if (rootCount == 1) 
			{
				ret = Mathf.Atan(tan1); // 1次方程式の解
			}
			else // 解が2つある場合、角が小さい方が早く当たるので小さい方を返す。tan(Θ)とΘは大小関係が同じなので、
			{
				var tan = Mathf.Min(tan1, tan2);
				ret = Mathf.Atan(tan);
			}
			return ret;
		}
	}
}