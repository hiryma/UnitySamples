using UnityEngine;

namespace Kayac
{
	// 2本のゲージを描く。secondaryを描画後、primaryを描画。
	// 数字は左がsecondary、右がprimary
	public class DebugUiDualGauge : DebugUiControl
	{
		public float primaryValue{ private get; set; }
		public float secondaryValue{ private get; set; }
		public Color32 textColor{ private get; set; }
		public Color32 primaryGaugeColor{ private get; set; }
		public Color32 secondaryGaugeColor{ private get; set; }
		private float _min;
		private float _max;
		public string label{ private get; set; }
		private bool _asInteger;

		public DebugUiDualGauge(
			float min,
			float max,
			float width = 0,
			float height = 0,
			bool asInteger = false) : base("DualGauge")
		{
			_asInteger = asInteger;
			_min = min;
			_max = max;
			textColor = new Color32(255, 255, 255, 255);
			primaryGaugeColor = new Color32(0, 255, 0, 128);
			secondaryGaugeColor = new Color32(255, 0, 0, 128);

			backgroundEnabled = true;
			borderEnabled = true;

			SetSize(width, height);
		}

		public override void Draw(
			float offsetX,
			float offsetY,
			DebugPrimitiveRenderer2D renderer)
		{
			DrawGauge(offsetX, offsetY, renderer, secondaryValue, secondaryGaugeColor);
			DrawGauge(offsetX, offsetY, renderer, primaryValue, primaryGaugeColor);

			float fontSize = height - (2f * borderWidth);
			fontSize *= 0.8f;
			renderer.color = textColor;
			string format = _asInteger ? "F0" : "F1";

			if (string.IsNullOrEmpty(label)) // ラベルがなければ両方の数字を左右に分けて出す
			{
				// primaryは左寄せ
				renderer.AddText(
					primaryValue.ToString(format),
					fontSize,
					offsetX + localLeftX + (borderWidth * 2f),
					offsetY + localTopY + (borderWidth * 2f),
					width - (borderWidth * 2f),
					height - (borderWidth * 2f));

				// secondaryは右寄せ
				renderer.AddText(
					secondaryValue.ToString(format),
					fontSize,
					offsetX + localLeftX + (borderWidth * 2f),
					offsetY + localTopY + (borderWidth * 2f),
					width - (borderWidth * 2f),
					height - (borderWidth * 2f),
					DebugPrimitiveRenderer.Alignment.Right);
			}
			else // ラベルがあれば左にラベル、右にプライマリの数値
			{
				renderer.AddText(
					label,
					fontSize,
					offsetX + localLeftX + (borderWidth * 2f),
					offsetY + localTopY + (borderWidth * 2f),
					width - (borderWidth * 2f),
					height - (borderWidth * 2f));

				// 数字は右寄せ
				renderer.AddText(
					primaryValue.ToString(format),
					fontSize,
					offsetX + localLeftX + (borderWidth * 2f),
					offsetY + localTopY + (borderWidth * 2f),
					width - (borderWidth * 2f),
					height - (borderWidth * 2f),
					DebugPrimitiveRenderer.Alignment.Right);
			}
		}

		private void DrawGauge(
			float offsetX,
			float offsetY,
			DebugPrimitiveRenderer2D renderer,
			float value,
			Color32 color)
		{
			// seconadary
			float ratio = (value - _min) / (_max - _min);
			ratio = (ratio < 0f) ? 0f : ((ratio > 1f) ? 1f : ratio);
			float length = ratio * (width - (2f * borderWidth));

			renderer.color = color;
			renderer.AddRectangle(
				offsetX + localLeftX + borderWidth,
				offsetY + localTopY + borderWidth,
				length,
				height - (2f * borderWidth));
		}
	}
}