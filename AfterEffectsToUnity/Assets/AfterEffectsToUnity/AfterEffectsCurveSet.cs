using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AfterEffectsToUnity
{
	public class AfterEffectsCurveSet
	{
		private const int INVALID_INDEX = -0x7fffffff;

		public enum ValueType
		{
			Bool,
			Float,
			Vector2,
			Unknown,
		}

		protected struct CurveId
		{
			public CurveId(ValueType type, int index)
			{
				this.type = type;
				this.index = index;
			}
			public ValueType type;
			public int index;
		}

		protected List<AfterEffectsCurveBool> _boolCurves;
		protected List<AfterEffectsCurveFloat> _floatCurves;
		protected List<AfterEffectsCurveVector2> _vector2Curves;
		protected Dictionary<string, CurveId> _map;
		public int lastKeyTime{ get; protected set; }

		public AfterEffectsCurveSet()
		{
			// TODO: 要調整あるいは初期値を与えられるように
			_boolCurves = new List<AfterEffectsCurveBool>(8);
			_floatCurves = new List<AfterEffectsCurveFloat>(8);
			_vector2Curves = new List<AfterEffectsCurveVector2>(8);
			_map = new Dictionary<string, CurveId>(32);
		}

		public void AddBool(string name, IList<int> times, IList<bool> values)
		{
			var curve = new AfterEffectsCurveBool(times, values);
			_boolCurves.Add(curve);
			Debug.Assert(_map.ContainsKey(name) == false);
			_map.Add(name, new CurveId(ValueType.Bool, _boolCurves.Count - 1));

			int newLastKeyTime = times[times.Count - 1];
			if (newLastKeyTime > lastKeyTime)
			{
				lastKeyTime = newLastKeyTime;
			}
		}

		public void AddBool(string name, int start, int duration)
		{
			var curve = new AfterEffectsCurveBool(start, duration);
			_boolCurves.Add(curve);
			Debug.Assert(_map.ContainsKey(name) == false);
			_map.Add(name, new CurveId(ValueType.Bool, _boolCurves.Count - 1));

			int newLastKeyTime = start + duration;
			if (newLastKeyTime > lastKeyTime)
			{
				lastKeyTime = newLastKeyTime;
			}
		}

		public void AddFloat(string name, IList<int> times, IList<float> values, bool isPercent = false)
		{
			var curve = new AfterEffectsCurveFloat(times, values, isPercent);
			_floatCurves.Add(curve);
			Debug.Assert(_map.ContainsKey(name) == false);
			_map.Add(name, new CurveId(ValueType.Float, _floatCurves.Count - 1));

			int newLastKeyTime = times[times.Count - 1];
			if (newLastKeyTime > lastKeyTime)
			{
				lastKeyTime = newLastKeyTime;
			}
		}

		public void AddVector2(
			string name,
			IList<int> times,
			IList<float> valuesX,
			IList<float> valuesY,
			bool isPercent = false)
		{
			var curve = new AfterEffectsCurveVector2(times, valuesX, valuesY, isPercent);
			_vector2Curves.Add(curve);
			Debug.Assert(_map.ContainsKey(name) == false);
			_map.Add(name, new CurveId(ValueType.Vector2, _vector2Curves.Count - 1));

			int newLastKeyTime = times[times.Count - 1];
			if (newLastKeyTime > lastKeyTime)
			{
				lastKeyTime = newLastKeyTime;
			}
		}

		// 型が一致しなければ名前が合っていてもダメ
		public int FindIndex(ValueType type, string name)
		{
			int ret = INVALID_INDEX;
			if (_map.ContainsKey(name))
			{
				var id = _map[name];
				if (type == id.type)
				{
					ret = id.index;
				}
			}
			return ret;
		}

		// 計算値を取得
		public void GetVector2(out Vector2 value, int index, float time)
		{
			_vector2Curves[index].Get(out value, time);
		}

		public bool GetBool(int index, float time)
		{
			return _boolCurves[index].Get(time);
		}

		public float GetFloat(int index, float time)
		{
			return _floatCurves[index].Get(time);
		}

		// 以下ほぼデバグ機能
		public AfterEffectsCurveBool GetBoolCurve(int index)
		{
			return _boolCurves[index];
		}

		public AfterEffectsCurveFloat GetFloatCurve(int index)
		{
			return _floatCurves[index];
		}

		public AfterEffectsCurveVector2 GetVector2Curve(int index)
		{
			return _vector2Curves[index];
		}
	}
}