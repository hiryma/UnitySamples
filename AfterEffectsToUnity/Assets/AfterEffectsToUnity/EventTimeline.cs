using System.Collections.Generic;
using System;
using UnityEngine;

namespace AfterEffectsToUnity
{
	// イベントトリガー。トリガーは「通過した時」に発火する。t=1にトリガーがあったとして、1ぴったりになっても発火しない。
	public class EventTimeline<T>
	{
		public struct Item : IComparable<Item>
		{
			public int CompareTo(Item other)
			{
				return frame - other.frame;
			}
			public int frame;
			public T value;
		}
		private short[] _times;
		private T[] _values;
		private List<Item> _items; // 作り途中のものを格納する場所
		private bool _modified;

		// 空で生成
		public EventTimeline(bool loop)
		{
		}

		// 足されたものから初期化する
		private void ApplyModification()
		{
			Debug.Assert(_items != null);
			_items.Sort();
			int count = _items.Count;

			_times = new short[count];
			_values = new T[count];
			int prevTime = -0x7fffffff;
			for (int i = 0; i < count; i++)
			{
				var item = _items[i];
				Debug.Assert((item.frame >= -0x8000) && (item.frame <= 0x7fff));
				Debug.Assert(item.frame >= prevTime, "times must be sorted in ascending");
				_times[i] = (short)(item.frame);
				prevTime = item.frame;
				_values[i] = item.value;
			}
			_modified = false;
		}

		public EventTimeline<T> Add(int frame, T value)
		{
			if (_items == null)
			{
				_items = new List<Item>();
			}
			Item item;
			item.frame = frame;
			item.value = value;
			_items.Add(item);
			_modified = true;
			return this;
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

		public IEnumerable<T> EnumerateFired(float prevTime, float currentTime, bool loop = false)
		{
			if (_modified)
			{
				ApplyModification();
			}
			if (loop)
			{
				return EnumerateFiredLooped(prevTime, currentTime);
			}
			else
			{
				return EnumerateFiredNotLooped(prevTime, currentTime);
			}
		}

		private IEnumerable<T> EnumerateFiredLooped(float prevTime, float currentTime)
		{
			float dur = duration;
			int cycles = (int)(prevTime / dur);
			float offset = (float)cycles * dur;
			prevTime -= offset;
			currentTime -= offset;
			if (prevTime < 0f) // 負なら1回足して正にする
			{
				prevTime += dur;
				currentTime += dur;
			}

			while (prevTime < currentTime)
			{
				var enumerator = EnumerateFiredNotLooped(prevTime, currentTime);
				foreach (var item in enumerator)
				{
					yield return item;
				}

				if (currentTime < dur) // 周期内につきこれで終了
				{
					prevTime = currentTime;
				}
				else // 次の周期へ
				{
					prevTime -= dur;
					currentTime -= dur;
				}
			}
		}

		private IEnumerable<T> EnumerateFiredNotLooped(float prevTime, float currentTime)
		{
			Debug.Assert(initialized);
			// 全く進んでいなければ発火させない
			if (currentTime - prevTime <= 0f)
			{
				yield break;
			}
			// 始点を取得
			int index = AfterEffectsUtil.FindSmallestGreaterEqual(_times, prevTime);
			if (index < 0) // まだ発火せず
			{
				yield break;
			}
			else if (index >= _times.Length) // すでに発火しきっている
			{
				yield break;
			}
			else
			{
				while ((index < _times.Length) && (_times[index] < currentTime)) // 範囲内で、キー時刻を過ぎていれば発火させる
				{
					yield return _values[index];
					index++;
				}
			}
		}

		protected IEnumerable<T> EnumerateValues()
		{
			if (_modified)
			{
				ApplyModification();
			}
			foreach (var v in _values)
			{
				yield return v;
			}
		}
	}
}
