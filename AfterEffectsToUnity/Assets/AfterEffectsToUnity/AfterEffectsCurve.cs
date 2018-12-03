using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// TODO: SoAとAoSのどっちが速いか後で確認する必要がある。SoAならnewは2回、AoSなら1回だ。
// TODO: AoSにするなら時刻はfloatのままの方がキャストいらなくて速そう
namespace AfterEffectsToUnity
{
	public struct AfterEffectsCurveBool
	{
		private short[] _times;
		private bool[] _values;

		public AfterEffectsCurveBool(IList<int> times, IList<bool> values, bool removeLeadTime = false)
		{
			Debug.Assert(times.Count == values.Count);
			int timeOffset = (removeLeadTime) ? -times[0] : 0;
			int count = times.Count;
			_times = new short[count];
			_values = new bool[count];
			int prevTime = -0x7fffffff;
			for (int i = 0; i < count; i++)
			{
				Debug.Assert((times[i] >= -0x8000) && (times[i] <= 0x7fff));
				Debug.Assert(times[i] >= prevTime, "times must be sorted in ascending");
				_times[i] = (short)(times[i] + timeOffset);
				prevTime = times[i];

				_values[i] = values[i];
			}
		}

		public bool initialized
		{
			get
			{
				return (_times != null) && (_values != null);
			}
		}

		public float duration
		{
			get
			{
				return (float)(_times[_times.Length - 1]);
			}
		}

		// trueになる時刻とそれが続く時間から設定
		public AfterEffectsCurveBool(int trueStart, int trueDuration)
		{
			Debug.Assert((trueStart >= -0x8000) && (trueStart <= 0x7fff));
			int trueEnd = trueStart + trueDuration;
			Debug.Assert((trueEnd >= -0x8000) && (trueEnd <= 0x7fff));
			if (trueStart == 0)
			{
				_times = new short[2];
				_values = new bool[2];
				_times[0] = (short)trueStart;
				_times[1] = (short)trueEnd;
				_values[0] = true;
				_values[1] = false;
			}
			else
			{
				_times = new short[3];
				_values = new bool[3];
				_times[0] = 0;
				_times[1] = (short)trueStart;
				_times[2] = (short)trueEnd;
				_values[0] = false;
				_values[1] = true;
				_values[2] = false;
			}
		}

		public bool GetLooped(float time)
		{
			time = AfterEffectsUtil.Fmod(time, (float)_times[_times.Length - 1]);
			return Get(time);
		}

		public bool Get(float time)
		{
			int index = AfterEffectsUtil.FindLargestLessEqual(_times, time);
			bool ret;
			if (index < 0)
			{
				ret = _values[0];
			}
			else
			{
				ret = _values[index];
			}
			return ret;
		}
	}

	// TODO: リニア以外の補間
	public struct AfterEffectsCurveFloat
	{
		private short[] _times;
		private float[] _values;

		public AfterEffectsCurveFloat(
			IList<int> times,
			IList<float> values,
			bool isPercent = false,
			bool removeLeadTime = false)
		{
			Debug.Assert(times.Count == values.Count);
			int timeOffset = (removeLeadTime) ? -times[0] : 0;
			int count = times.Count;
			_times = new short[count];
			_values = new float[count];
			int prevTime = -0x7fffffff;
			for (int i = 0; i < count; i++)
			{
				Debug.Assert((times[i] >= -0x8000) && (times[i] <= 0x7fff));
				Debug.Assert(times[i] >= prevTime, "times must be sorted in ascending");
				_times[i] = (short)(times[i] + timeOffset);
				prevTime = times[i];

				_values[i] = values[i];
			}

			if (isPercent)
			{
				for (int i = 0; i < count; i++)
				{
					_values[i] *= 0.01f;
				}
			}
		}

		public bool initialized
		{
			get
			{
				return (_times != null) && (_values != null);
			}
		}

		public float duration
		{
			get
			{
				return (float)(_times[_times.Length - 1]);
			}
		}

		public float GetLooped(float time)
		{
			time = AfterEffectsUtil.Fmod(time, (float)_times[_times.Length - 1]);
			return Get(time);
		}

		public float Get(float time)
		{
			int index = AfterEffectsUtil.FindLargestLessEqual(_times, time);
			float ret;
			if (index < 0)
			{
				ret = _values[0];
			}
			else if (index >= (_times.Length - 1))
			{
				ret = _values[_times.Length - 1];
			}
			else // 補間するよ
			{
				float t0 = (float)(_times[index]);
				float t1 = (float)(_times[index + 1]);
				float v0 = _values[index];
				float v1 = _values[index + 1];
				float t = (time - t0) / (t1 - t0); //[0, 1]
				ret = ((v1 - v0) * t) + v0;
			}
			return ret;
		}
	}

	// TODO: リニア以外の補間
	public struct AfterEffectsCurveVector2
	{
		private short[] _times;
		private Vector2[] _values;

		public AfterEffectsCurveVector2(
			IList<int> times,
			IList<float> valuesX,
			IList<float> valuesY,
			bool isPercent = false,
			bool removeLeadTime = false)
		{
			Debug.Assert(times.Count == valuesX.Count);
			Debug.Assert(times.Count == valuesY.Count);
			int count = times.Count;
			int timeOffset = (removeLeadTime) ? -times[0] : 0;
			_times = new short[count];
			_values = new Vector2[count];
			int prevTime = -0x7fffffff;
			for (int i = 0; i < count; i++)
			{
				Debug.Assert((times[i] >= -0x8000) && (times[i] <= 0x7fff));
				Debug.Assert(times[i] >= prevTime, "times must be sorted in ascending");
				_times[i] = (short)(times[i] + timeOffset);
				prevTime = times[i];

				_values[i].x = valuesX[i];
				_values[i].y = valuesY[i];
			}

			if (isPercent)
			{
				for (int i = 0; i < count; i++)
				{
					_values[i].x *= 0.01f;
					_values[i].y *= 0.01f;
				}
			}
		}

		public AfterEffectsCurveVector2(
			IList<int> times,
			IList<float> valuesX,
			IList<float> valuesY,
			SpriteRenderer spriteRenderer,
			Vector2 parentSpritePivot,
			bool removeLeadTime = false)
		{
			float pixelToUnit = spriteRenderer.sprite.pixelsPerUnit;
			Debug.Assert(times.Count == valuesX.Count);
			Debug.Assert(times.Count == valuesY.Count);
			int count = times.Count;
			int timeOffset = (removeLeadTime) ? -times[0] : 0;
			_times = new short[count];
			_values = new Vector2[count];
			int prevTime = -0x7fffffff;
			for (int i = 0; i < count; i++)
			{
				Debug.Assert((times[i] >= -0x8000) && (times[i] <= 0x7fff));
				Debug.Assert(times[i] >= prevTime, "times must be sorted in ascending");
				_times[i] = (short)(times[i] + timeOffset);
				prevTime = times[i];

				_values[i].x = (valuesX[i] - parentSpritePivot.x) * pixelToUnit;
				_values[i].y = (-valuesY[i] + parentSpritePivot.y) * pixelToUnit;
			}
		}

		public bool initialized
		{
			get
			{
				return (_times != null) && (_values != null);
			}
		}

		public float duration
		{
			get
			{
				return (float)(_times[_times.Length - 1]);
			}
		}

		public void GetLooped(out Vector2 value, float time)
		{
			time = AfterEffectsUtil.Fmod(time, (float)_times[_times.Length - 1]);
			Get(out value, time);
		}

		public void Get(out Vector2 value, float time)
		{
			int index = AfterEffectsUtil.FindLargestLessEqual(_times, time);
			if (index < 0)
			{
				value = _values[0];
			}
			else if (index >= (_times.Length - 1))
			{
				value = _values[_times.Length - 1];
			}
			else // 補間するよ
			{
				float t0 = (float)(_times[index]);
				float t1 = (float)(_times[index + 1]);
				float v0x = _values[index].x;
				float v1x = _values[index + 1].x;
				float v0y = _values[index].y;
				float v1y = _values[index + 1].y;
				float t = (time - t0) / (t1 - t0); //[0, 1]
				value.x = ((v1x - v0x) * t) + v0x;
				value.y = ((v1y - v0y) * t) + v0y;
			}
		}
	}
}
