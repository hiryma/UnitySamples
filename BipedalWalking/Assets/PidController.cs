using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kayac
{
	[System.Serializable]
	public class PidSettings
	{
		public PidSettings(float kp, float ki, float kd, float eSumDecay = 0f)
		{
			this.kp = kp;
			this.ki = ki;
			this.kd = kd;
			this.eSumDecay = eSumDecay;
		}

		public void CopyFrom(PidSettings src)
		{
			this.kp = src.kp;
			this.ki = src.ki;
			this.kd = src.kd;
			this.eSumDecay = src.eSumDecay;
		}
		public float kp;
		public float ki;
		public float kd;
		public float eSumDecay;
	}

	public struct PidState<T>
	{
		public PidState(T prevE, T eSum)
		{
			this.prevE = prevE;
			this.eSum = eSum;
		}

		public override string ToString()
		{
			return "prevE=" + prevE + " eSum=" + eSum;
		}

		public T prevE;
		public T eSum;
	}

	public class PidController1 : IPidController<float>
	{
		public PidState<float> State { get => state; }
		public float ErrorSum { get => state.eSum; }
		public float PrevError { get => state.prevE; }

		public PidController1(PidSettings settings)
		{
			this.settings = settings;
		}

		public void SetSettings(PidSettings settings)
		{
			this.settings = settings;
		}

		public void Restore(PidState<float> state)
		{
			this.state = state;
		}

		public float Update(float x, float r, float deltaTime, float outputCoeff = 1f, float backCalculationTt = 0f)
		{
			var f = 0f;
			// p
			var e = r - x;
			var fp = settings.kp * e;
			f += fp;
			// i
			var fi = settings.ki * state.eSum;
			f += fi;
			state.eSum *= 1f - Mathf.Clamp01(settings.eSumDecay * deltaTime);
			state.eSum += (e + state.prevE) * 0.5f * deltaTime;
			// d
			var fd = 0f;
			if (started)
			{
				if (deltaTime > 0f)
				{
					fd = settings.kd * (e - state.prevE) / deltaTime;
					f += fd;
				}
			}

			// back-calculation
			if ((backCalculationTt > 0f) && (settings.ki > 0f))
			{
				var df = (1f - outputCoeff) * f; // かけられなかった力の大きさ
				state.eSum -= df * (deltaTime / (backCalculationTt * settings.ki));
			}

			started = true;
			state.prevE = e;
			return f;
		}

		public void Reset()
		{
			state.prevE = state.eSum = 0f;
			started = false;
		}

		PidState<float> state;
		PidSettings settings;
		bool started;
	}

	public class PidController3 : IPidController<Vector3>
	{
		public PidState<Vector3> State { get => state; }
		public Vector3 ErrorSum { get => state.eSum; }
		public Vector3 PrevError { get => state.prevE; }

		public PidController3(PidSettings settings)
		{
			this.settings = settings;
		}

		public void SetSettings(PidSettings settings)
		{
			this.settings = settings;
		}

		public void Restore(PidState<Vector3> state)
		{
			this.state = state;
		}

		public Vector3 Update(Vector3 x, Vector3 r, float deltaTime, float outputCoeff = 1f, float backCalculationTt = 0f)
		{
			var f = Vector3.zero;
			// p
			var e = r - x;
			f += settings.kp * e;
			// i
			f += settings.ki * state.eSum;
			state.eSum *= 1f - Mathf.Clamp01(settings.eSumDecay * deltaTime);
			state.eSum += (e + state.prevE) * 0.5f * deltaTime;
			// d
			if (started)
			{
				if (deltaTime > 0f)
				{
					f += settings.kd * (e - state.prevE) / deltaTime;
				}
			}

			// back-calculation
			if ((backCalculationTt > 0f) && (settings.ki > 0f))
			{
				var df = (1f - outputCoeff) * f; // かけられなかった力の大きさ
				state.eSum -= df * (deltaTime / (backCalculationTt * settings.ki));
			}

			started = true;
			state.prevE = e;
			return f;
		}

		public void Reset()
		{
			state.prevE = state.eSum = Vector3.zero;
			started = false;
		}

		PidState<Vector3> state;
		PidSettings settings;
		bool started;
	}

	public abstract class PidControllerRotationBase : PidController1
	{
		public PidControllerRotationBase(PidSettings settings, Quaternion localToNatural) : base(settings)
		{
			this.localToNatural = localToNatural;
		}

		public abstract Vector3 Update(Quaternion x, Quaternion r, float deltaTime);
		// non public ----
		protected Quaternion localToNatural;

		protected static float NormalizeRadian(float x)
		{
			//[-PI,PI]に正規化。Atan2の仕様によっては不要だが念のため
			if (x < -Mathf.PI)
			{
				x += Mathf.PI * 2f;
			}
			else if (x > Mathf.PI)
			{
				x -= Mathf.PI * 2f;
			}
			return x;
		}
	}

	public class PidControllerRotation : IPidController<Vector3>
	{
		public PidState<Vector3> State { get => new PidState<Vector3>(prevE: PrevError, eSum: ErrorSum); } 
		public Vector3 ErrorSum { get => state.eSum; }
		public Vector3 PrevError { get => state.prevE; }

		public PidControllerRotation(PidSettings settings, Quaternion localToNatural)
		{
			this.localToNatural = localToNatural;
			this.settings = settings;
		}

		public PidControllerRotation(PidSettings settings)
		{
			this.localToNatural = Quaternion.identity;
			this.settings = settings;
		}

		public Vector3 Update(Quaternion x, Quaternion r, float deltaTime, float outputCoeff = 1f, float backCalculationTt = 0f)
		{
			// 軸変換
			var invX = localToNatural * Quaternion.Inverse(x);
			// r = dq*x より r*inv(x) = dq
			var dq = r * invX;
			var e = Log(dq);
	
			var t = settings.kp * e;
			// i
			t += settings.ki * state.eSum;
			state.eSum *= 1f - Mathf.Clamp01(settings.eSumDecay * deltaTime);
			state.eSum += (e + state.prevE) * 0.5f * deltaTime;
			// d
			if (started)
			{
				if (deltaTime > 0f)
				{
					t += settings.kd * (e - state.prevE) / deltaTime;
				}
			}

			// back-calculation
			if ((backCalculationTt > 0f) && (settings.ki > 0f))
			{
				var dTorque = (1f - outputCoeff) * t; // かけられなかった力の大きさ
				state.eSum -= dTorque * (deltaTime / (backCalculationTt * settings.ki));
			}

if (PidDebug.D == 1)
{
Debug.LogWarning("T:" + t.ToString("F4") + " E:" + state.prevE.ToString("F4") + " -> " + e.ToString("F4") + "\n" + 
	"\tKP=" + (settings.kp * e).ToString("F4") + "\tKI=" + (settings.ki * state.eSum).ToString("F4") + "\tKD=" + (settings.kd * (e - state.prevE) / deltaTime).ToString("F4") + "\n" +
	"\tX=" + Quaternion.Inverse(invX).ToString("F4") + "\tR=" + r.ToString("F4") + "\tDQ=" + dq.ToString("F4") + "\tES=" + state.eSum.ToString("F4") + "\n" +
	"\t\tF=" + (Quaternion.Inverse(invX) * new Vector3(0f, 0f, 1f)).ToString("F4") + "\tGF=" + (r * new Vector3(0f, 0f, 1f)).ToString("F4") + "\n" +
	"\t\tU=" + (Quaternion.Inverse(invX) * new Vector3(0f, 1f, 0f)).ToString("F4") + "\tGU=" + (r * new Vector3(0f, 1f, 0f)).ToString("F4") + "\n");
}

			started = true;
			state.prevE = e;
			return t;
		}

		public void SetSettings(PidSettings settings)
		{
			this.settings = settings;
		}

		public void Reset()
		{
			state.prevE = state.eSum = Vector3.zero;
			started = false;
		}

		public void Restore(PidState<Vector3> state)
		{
			this.state = state;
		}
		// non public ----
		Quaternion localToNatural;
		PidSettings settings;
		PidState<Vector3> state;

		bool started;

		// 注意: 角度を2倍してしまっているので本当のLogではない。この中でしか使わないこと。パラメータの互換が崩れるので製品変わる時にしか触らない。
		static Vector3 Log(Quaternion q)
		{
			Vector3 ret;
			// ノルム1を仮定する
			var vlSq = (q.x * q.x) + (q.y * q.y) + (q.z * q.z);
			if (vlSq == 0f) // w = 1か-1で、いずれにせよ0度
			{
				ret = Vector3.zero;
			}
			else 
			{
				var theta = Mathf.Acos(Mathf.Clamp(q.w, -1f, 1f)) * 2f; // [-1,1] → [2pi, 0]
				if (theta >= Mathf.PI) // [-pi,pi]
				{
					theta -= Mathf.PI * 2f;
				}
				var mul = theta / Mathf.Sqrt(vlSq);
				ret.x = q.x * mul;
				ret.y = q.y * mul;
				ret.z = q.z * mul;
if (PidDebug.D == 1)
{
	Debug.Log("Log: " + theta + " " + vlSq + " " + ret.ToString("F4") + " " + q.ToString("F4"));
}
			}
			return ret;
		}
	}

	public interface IPidController<T>
	{
		PidState<T> State { get; }
		T ErrorSum { get; } 
		T PrevError { get; }
		void Restore(PidState<T> state);
		void Reset(); 
		void SetSettings(PidSettings settings);
	}


	public static class PidDebug
	{
		public static int D;
	}
}
