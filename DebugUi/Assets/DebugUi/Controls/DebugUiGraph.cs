using UnityEngine;
using System.Collections.Generic;

namespace Kayac
{
	// TODO: 高機能化。180228現在、電力の表示に使うギリギリしか用意しない。x軸は時間限定とする。
	public class DebugUiGraph : DebugUiControl
	{
		private struct Data
		{
			public Data(float time, float value)
			{
				this.time = time;
				this.value = value;
			}
			public float time;
			public float value;
		}
		private float _duration;
		private float _yMin;
		private float _yMax;
		private List<Data> _data;
		private float _startTime;
		private float _latestTime;

		public DebugUiGraph(
			float duration,
			float yMin,
			float yMax,
			float width,
			float height) : base("Graph")
		{
			_duration = duration;
			_yMin = yMin;
			_yMax = yMax;

			backgroundEnabled = true;
			borderEnabled = true;

			SetSize(width, height);
			_data = new List<Data>();
			_latestTime = _startTime = -(float.MaxValue);
		}

		public float currentTime
		{
			get
			{
				return _latestTime - _startTime;
			}
		}

		public void Start()
		{
			_data.Clear();
			_startTime = Time.realtimeSinceStartup; // TODO: timeScaleの影響受ける版も後で欲しかろうな
			_latestTime = -(float.MaxValue);
		}

		public bool AddData(float v)
		{
			if (_startTime < 0f)
			{
				Start();
			}
			// 最後のデータからの経過時間を計算し、それが1ピクセルに相当する幅だけなければ無視。
			var now = Time.realtimeSinceStartup;
			var diff = now - _latestTime;
			bool ret = false;
			if ((diff * CalcXScale()) >= 1f)
			{
				_data.Add(new Data(now - _startTime, v));
				_latestTime = now;
				ret = true;
			}
			return ret;
		}

		private float CalcXScale()
		{
			float netWidth = width - (2f * borderWidth);
			float xScale = netWidth / _duration;
			return xScale;
		}

		public override void Draw(
			float offsetX,
			float offsetY,
			DebugPrimitiveRenderer2D renderer)
		{
			// 単純な折れ線だけとりあえず用意
			float netHeight = height - (2f * borderWidth);
			float xScale = CalcXScale();
			float xOffset = offsetX + localLeftX;
			float yScale = -netHeight / (_yMax - _yMin);
			float yOffset = offsetY + localTopY + height - borderWidth;
			for (int i = 1; i < _data.Count; i++)
			{
				var d0 = _data[i - 1];
				var d1 = _data[i];
				var x0 = (d0.time * xScale) + xOffset;
				var x1 = (d1.time * xScale) + xOffset;
				var y0 = (d0.value * yScale) + yOffset;
				var y1 = (d1.value * yScale) + yOffset;
				renderer.AddLine(x0, y0, x1, y1, 1f);
			}
		}
	}
}
