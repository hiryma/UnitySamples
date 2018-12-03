using UnityEngine;

namespace Kayac
{
	public class DebugUiGauge : DebugUiControl
	{
		public enum Mode
		{
			Float,
			Integer,
			Log10,
		}
		public float value{ get; set; }
		public Color32 textColor{ private get; set; }
		public Color32 gaugeColor{ protected get; set; }

		private string _text;
		protected float _min;
		protected float _max;
		protected Mode _mode;

		public DebugUiGauge(
			string text,
			float min,
			float max,
			float width = 0,
			float height = 0,
			Mode mode = Mode.Float) : base(string.IsNullOrEmpty(text) ? "Gauge" : text)
		{
			_min = min;
			_max = max;
			_text = text;
			_mode = mode;
			textColor = new Color32(255, 255, 255, 255);
			gaugeColor = new Color32(255, 0, 0, 128);

			backgroundEnabled = true;
			borderEnabled = true;

			SetSize(width, height);
		}

		public override void Draw(
			float offsetX,
			float offsetY,
			DebugPrimitiveRenderer2D renderer)
		{
			// 棒を描画
			float ratio;
			if (_mode == Mode.Log10)
			{
				float logMin = Mathf.Log10(_min);
				float logMax = Mathf.Log10(_max);
				ratio = (Mathf.Log10(value) - logMin) / (logMax - logMin);
			}
			else
			{
				ratio = (value - _min) / (_max - _min);
			}
			ratio = (ratio < 0f) ? 0f : ((ratio > 1f) ? 1f : ratio);
			float barWidth = (width - (2f * borderWidth)) * ratio;

			renderer.color = gaugeColor;
			renderer.AddRectangle(
				offsetX + localLeftX + borderWidth,
				offsetY + localTopY + borderWidth,
				barWidth,
				height - (2f * borderWidth));

			// 題名
			float fontSize = height - (2f * borderWidth);
			fontSize *= 0.8f;
			renderer.color = textColor;
			if (_text.Length > 0)
			{
				renderer.AddText(
					_text,
					fontSize,
					offsetX + localLeftX + (borderWidth * 2f),
					offsetY + localTopY + (borderWidth * 2f),
					width - (borderWidth * 2f),
					height - (borderWidth * 2f));
			}

			string formatString = (_mode == Mode.Integer) ? "F0" : "F2";

			// 数字は右寄せ
			renderer.AddText(
				value.ToString(formatString),
				fontSize,
				offsetX + localLeftX + (borderWidth * 2f),
				offsetY + localTopY + (borderWidth * 2f),
				width - (borderWidth * 2f),
				height - (borderWidth * 2f),
				DebugPrimitiveRenderer.Alignment.Right);
		}
	}

}