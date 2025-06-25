using UnityEngine;

namespace Kayac
{
	[System.Serializable]
	public class PidSettings
	{
		public PidSettings(float kp, float ki, float kd)
		{
			this.kp = kp;
			this.ki = ki;
			this.kd = kd;
		}
		public float kp;
		public float ki;
		public float kd;
	}

	public struct PidState<T>
	{
		public PidState(T prevE, T eSum)
		{
			this.prevE = prevE;
			this.eSum = eSum;
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

		public float Update(float current, float goal, float deltaTime)
		{
			var f = 0f;
			// p
			var e = goal - current;
			var fp = settings.kp * e;
			f += fp;
			// i
			var fi = settings.ki * state.eSum;
			f += fi;
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

		public Vector3 Update(Vector3 current, Vector3 goal, float deltaTime)
		{
			var f = Vector3.zero;
			// p
			var e = goal - current;
			f += settings.kp * e;
			// i
			f += settings.ki * state.eSum;
			state.eSum += (e + state.prevE) * 0.5f * deltaTime;
			// d
			if (started)
			{
				if (deltaTime > 0f)
				{
					f += settings.kd * (e - state.prevE) / deltaTime;
				}
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

		public abstract Vector3 Update(Quaternion current, Quaternion goal, float deltaTime);
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

		public Vector3 Update(Quaternion current, Quaternion goal, float deltaTime, float updateRatio = 1f)
		{
			// 軸変換
			var invC = localToNatural * Quaternion.Inverse(current);
			// r = dq*x より r*inv(x) = dq
			var dq = goal * invC;
			var e = Log(dq);
			e *= updateRatio;
	
			var t = settings.kp * e;
			// i
			t += settings.ki * state.eSum;
			state.eSum += (e + state.prevE) * 0.5f * deltaTime;
			// d
			if (started)
			{
				if (deltaTime > 0f)
				{
					t += settings.kd * (e - state.prevE) / deltaTime;
				}
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
}
