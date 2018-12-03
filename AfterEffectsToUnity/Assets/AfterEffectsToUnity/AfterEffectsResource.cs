using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AfterEffectsToUnity
{
	public class AfterEffectsResource : AfterEffectsCurveSet
	{
		public const int Infinity = 0x7fffffff;
		public const int Default = -0x7fffffff;
		private const string DefaultCutName = "";

		public class Cut
		{
			public Cut(
				string name,
				string nextCutName,
				float start,
				float duration,
				int loopCount,
				float loopStart,
				float loopDuration)
			{
				this.name = name;
				this.nextCutName = nextCutName;
				this.start = start;
				this.duration = duration;
				this.loopCount = loopCount;
				this.loopStart = loopStart;
				this.loopDuration = loopDuration;
			}
			public string name;
			public string nextCutName;
			public float start;
			public float duration;
			public int loopCount;
			public float loopStart;
			public float loopDuration;
		}

		private Dictionary<string, Cut> _cuts;
		private Cut _defaultCut;
		public float frameRate { get; private set; }

		public Cut defaultCut
		{
			get
			{
				if (_defaultCut == null)
				{
					AddCut(DefaultCutName, null, 0, -Infinity, 0, -Infinity, Infinity, isDefault: true); // 全体でカットを生成してデフォルトにセット
				}
				return _defaultCut;
			}
		}

		public AfterEffectsResource(float frameRate)
		{
			this.frameRate = frameRate;
			_cuts = new Dictionary<string, Cut>();
		}

		// Sprite専用版
		public AfterEffectsResource AddSpriteRendererPosition(
			string name,
			IList<int> times,
			IList<float> valuesX,
			IList<float> valuesY,
			SpriteRenderer spriteRenderer,
			Vector2 parentSpritePivot)
		{
			var curve = new AfterEffectsCurveVector2(times, valuesX, valuesY, spriteRenderer, parentSpritePivot);
			_vector2Curves.Add(curve);
			Debug.Assert(_map.ContainsKey(name) == false);
			_map.Add(name, new CurveId(ValueType.Vector2, _vector2Curves.Count - 1));

			int newLastKeyTime = times[times.Count - 1];
			if (newLastKeyTime > lastKeyTime)
			{
				lastKeyTime = newLastKeyTime;
			}
			return this;
		}

		public AfterEffectsResource AddScale(
			string name,
			IList<int> times,
			IList<float> valuesX,
			IList<float> valuesY)
		{
			AddVector2(name, times, valuesX, valuesY, true);
			return this;
		}

		public AfterEffectsResource AddPosition(
			string name,
			IList<int> times,
			IList<float> valuesX,
			IList<float> valuesY)
		{
			AddVector2(name, times, valuesX, valuesY, false);
			return this;
		}

		public AfterEffectsResource AddScale(
			string name,
			IList<int> times,
			IList<float> values)
		{
			AddFloat(name, times, values, true);
			return this;
		}

		public AfterEffectsResource AddRotation(
			string name,
			IList<int> times,
			IList<float> values)
		{
			AddFloat(name, times, values, false);
			return this;
		}

		public AfterEffectsResource AddSize(
			string name,
			IList<int> times,
			IList<float> valuesX,
			IList<float> valuesY)
		{
			AddVector2(name, times, valuesX, valuesY, false);
			return this;
		}

		public AfterEffectsResource AddOpacity(string name, IList<int> times, IList<float> values)
		{
			AddFloat(name, times, values, true);
			return this;
		}

		public AfterEffectsResource AddVisibility(string name, int start, int duration)
		{
			AddBool(name, start, duration);
			return this;
		}

		public AfterEffectsResource AddCut(
			string name,
			string nextCutName,
			int start = 0,
			int duration = Infinity,
			int loopCount = 0,
			int loopStart = -Infinity,
			int loopDuration = Infinity,
			bool isDefault = false)
		{
			Debug.Assert(loopCount >= 0, "loopCount must be >= 0. minus value DOES NOT mean infinity. use Infinity constant.");
			if (_cuts.ContainsKey(name) == false)
			{
				// DefaultとInfinityを処理
				if (duration == Infinity) //start負も許す
				{
					duration = lastKeyTime - start;
				}

				if (loopStart == -Infinity) //startより前には行かない
				{
					loopStart = start;
				}

				if (loopDuration == Infinity) //start+durationより後には行かない
				{
					loopDuration = start + duration - loopStart;
				}

				var cut = new Cut(
					name,
					nextCutName,
					start,
					duration,
					loopCount,
					loopStart,
					loopDuration);
				_cuts.Add(name, cut);
				if ((_defaultCut == null) || isDefault) // 初めてAddCutするとデフォルトになる
				{
					_defaultCut = cut;
				}
			}
			else
			{
				Debug.LogError("AddCut : " + name + " is already exists.");
			}
			return this;
		}

		public AfterEffectsResource AddCut(
			string name,
			int start = 0,
			int duration = Default,
			int loopCount = 0,
			int loopStart = -Infinity,
			int loopDuration = Infinity,
			bool isDefault = false)
		{
			return AddCut(name, null, start, duration, loopCount, loopStart, loopDuration, isDefault);
		}

		public Cut FindCut(string name)
		{
			Cut ret = null;
			if (_cuts.ContainsKey(name))
			{
				ret = _cuts[name];
			}
			else
			{
				Debug.LogError("FindCut : " + name + " is not found.");
			}
			return ret;
		}
	}
}
