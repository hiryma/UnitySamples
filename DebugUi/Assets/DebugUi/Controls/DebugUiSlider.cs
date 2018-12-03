using UnityEngine;

namespace Kayac
{
	public class DebugUiSlider : DebugUiGauge
	{
		public Color32 sliderColor { private get; set; }
		public Color32 activeSliderColor { private get; set; }
		public DebugUiSlider(
			string text,
			float min,
			float max,
			float width = 200f,
			float height = 50f,
			Mode mode = Mode.Float) : base(string.IsNullOrEmpty(text) ? "Slider" : text, min, max, width, height, mode)
		{
			sliderColor = gaugeColor;
			activeSliderColor = new Color32(255, 64, 64, 255);

			eventEnabled = true;
			draggable = true;
		}

		public override void Update()
		{
			if (isDragging)
			{
				gaugeColor = activeSliderColor;
				float clientX = localPointerX - localLeftX;
				float x;
				float ratio = clientX / width;
				if (_mode == Mode.Log10)
				{
					// ratio = (Mathf.Log10(value) - Mathf.Log10(_min)) / (Mathf.Log10(_max) - Mathf.Log10(_min)); // Gauge側がこれ。この逆関数
					// Mathf.Log10(value) = ratio * (Mathf.Log10(_max) - Mathf.Log10(_min)) + Mathf.Log10(_min)
					float logMin = Mathf.Log10(_min);
					float logMax = Mathf.Log10(_max);
					float logX = (ratio * (logMax - logMin)) + logMin;
					x = Mathf.Pow(10f, logX);
				}
				else
				{
					// 幅で割って[0,1]にし、(max-min)を乗じ、minを加える
					x = (ratio * (_max - _min)) + _min;
				}
				x = (x < _min) ? _min : ((x > _max) ? _max : x);
				if (_mode == Mode.Integer)
				{
					x = Mathf.RoundToInt(x);
				}
				value = x;
			}
			else
			{
				gaugeColor = sliderColor;
			}
		}
	}
}