using System.Collections.Generic;
using UnityEngine;

namespace Kayac
{
	//　毎フレーム全データセットする前提で、時間で流れていくグラフ
	public class DebugUiGraph : DebugUiControl
	{
		struct Series
		{
			public List<Data> data;
			public int dataBegin;
			public Color32 color;
		}

		struct Data
		{
			public Data(float time, float value)
			{
				this.time = time;
				this.value = value;
			}
			public float time;
			public float value;
		}
		float _duration;
		List<Series> _series;
		float _yCenter;
		float _yCenterGoal;
		float _yWidth;
		float _yWidthGoal;

		public DebugUiGraph(
			float duration,
			float width,
			float height) : base("Graph")
		{
			_duration = duration;
			backgroundEnabled = true;
			borderEnabled = true;

			SetSize(width, height);
			_series = new List<Series>();
			_yCenter = _yCenterGoal = 0f;
			_yWidth = _yWidthGoal = 1f;
		}

		public int AddSeries(Color32 color)
		{
			Series series;
			series.color = color;
			series.data = new List<Data>();
			series.dataBegin = 0;
			_series.Add(series);
			return _series.Count - 1;
		}

		public void AddData(int seriesIndex, float v)
		{
			Debug.Assert(seriesIndex < _series.Count, "call AddSeries(). invalid seriesIndex.");
			var series = _series[seriesIndex];
			var now = Time.time;
			series.data.Add(new Data(now, v));
		}

		float CalcXScale()
		{
			float netWidth = width - (2f * borderWidth);
			float xScale = netWidth / _duration;
			return xScale;
		}

		public override void Update(float deltaTime)
		{
			// 幅と中心の調整
			_yWidth += (_yWidthGoal - _yWidth) * deltaTime * 4f;
			_yCenter += (_yCenterGoal - _yCenter) * deltaTime * 4f;
		}

		public override void Draw(
			float offsetX,
			float offsetY,
			DebugPrimitiveRenderer2D renderer)
		{
			UnityEngine.Profiling.Profiler.BeginSample("DebugUiGraph.Draw");
			var now = Time.time;
			var startTime = now - _duration;
			// 単純な折れ線だけとりあえず用意
			float netHeight = height - (2f * borderWidth);
			float xScale = CalcXScale();
			float xOffset = offsetX + localLeftX + borderWidth;
			float yScale = -netHeight / _yWidth;
			float yOffset = offsetY + localTopY + height - borderWidth - (netHeight * 0.5f);
			float yMin = float.MaxValue;
			float yMax = -yMin;

			for (int seriesIndex = 0; seriesIndex < _series.Count; seriesIndex++)
			{
				var series = _series[seriesIndex];
				int dst = 0;
				int dataCount = series.data.Count;
				renderer.color = series.color;
				// 最初のデータを打ち込むところまでまず回す
				UnityEngine.Profiling.Profiler.BeginSample("DebugUiGraph.Draw FirstLine");
				int dataIndex = 0;
				while (dataIndex < (dataCount - 1))
				{
					series.data[dst] = series.data[dataIndex];
					var d0 = series.data[dataIndex];
					var d1 = series.data[dataIndex + 1];
					dataIndex++;
					if ((d0.time >= startTime) && (d1.time >= startTime)) // どちらか範囲内なら描画
					{
						var x0 = ((d0.time - startTime) * xScale) + xOffset;
						var x1 = ((d1.time - startTime) * xScale) + xOffset;
						var y0 = ((d0.value - _yCenter) * yScale) + yOffset;
						var y1 = ((d1.value - _yCenter) * yScale) + yOffset;
						renderer.AddLine(x0, y0, x1, y1, 1f);
						dst++;
						yMin = Mathf.Min(yMin, d0.value);
						yMax = Mathf.Max(yMax, d0.value);
						break;
					}
				}
				UnityEngine.Profiling.Profiler.EndSample();

				// 続き描画
				UnityEngine.Profiling.Profiler.BeginSample("DebugUiGraph.Draw FollowingLines");
				while (dataIndex < (dataCount - 1))
				{
					series.data[dst] = series.data[dataIndex];
					var d0 = series.data[dataIndex];
					var d1 = series.data[dataIndex + 1];
					dataIndex++;
					var x1 = ((d1.time - startTime) * xScale) + xOffset;
					var y1 = ((d1.value - _yCenter) * yScale) + yOffset;
					renderer.ContinueLine(x1, y1, 1f);
					dst++;
					yMin = Mathf.Min(yMin, d0.value);
					yMax = Mathf.Max(yMax, d0.value);
				}
				UnityEngine.Profiling.Profiler.EndSample();

				if (dataCount > 0)
				{
					var last = series.data[dataCount - 1];
					series.data[dst] = last;
					dst++;
					series.data.RemoveRange(dst, dataCount - dst);
					yMin = Mathf.Min(yMin, last.value);
					yMax = Mathf.Max(yMax, last.value);
				}
			}

			if (yMin != float.MaxValue) // データがある
			{
				_yCenterGoal = (yMin + yMax) * 0.5f;
				if (yMin != yMax) // 最低2種以上値がある
				{
					_yWidthGoal = (yMax - yMin);
				}
			}
			yMin = _yCenter - (_yWidth * 0.5f);
			yMax = _yCenter + (_yWidth * 0.5f);

			renderer.color = new Color32(255, 255, 255, 255);
			renderer.AddText(
				yMax.ToString("F3"),
				offsetX + localLeftX + borderWidth,
				offsetY + localTopY + borderWidth,
				10f);
			renderer.AddText(
				yMin.ToString("F3"),
				offsetX + localLeftX + borderWidth,
				offsetY + localTopY + borderWidth + netHeight - 10f,
				10f);
			UnityEngine.Profiling.Profiler.EndSample();
		}
	}
}
