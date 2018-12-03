using UnityEngine;

namespace Kayac
{
	public class DebugUiText : DebugUiControl
	{
		public const float DefaultLineHeight = DebugPrimitiveRenderer.DefaultLineHeight;
		public Color32 color{ get; set; }
		public float lineHeight{ get; set; }
		public string text{ get; set; }
		private float _fontSize;
		private bool _rotateToVertical;

		public DebugUiText(
			string text,
			float fontSize,
			float width,
			float height,
			bool rotateToVertical = false,
			bool backgroundEnabled = false) : base(string.IsNullOrEmpty(text) ? "Text" : text)
		{
			SetSize(width, height);
			this.text = text;
			_fontSize = fontSize;
			_rotateToVertical = rotateToVertical;
			lineHeight = DefaultLineHeight;
			this.backgroundEnabled = backgroundEnabled;
			borderEnabled = false;
			color = new Color32(255, 255, 255, 255);
		}

		// 自動測定にはmanagerが必要。TODO: どうにかしたい
		public DebugUiText(
			DebugUiManager manager,
			string text,
			float fontSize,
			float lineHeight = DefaultLineHeight,
			bool rotateToVertical = false,
			bool backgroundEnabled = false) : base(string.IsNullOrEmpty(text) ? "Text" : text)
		{
			this.text = text;
			this.lineHeight = lineHeight;
			_fontSize = fontSize;
			_rotateToVertical = rotateToVertical;
			borderEnabled = false;
			this.backgroundEnabled = backgroundEnabled;
			color = new Color32(255, 255, 255, 255);

			var size = manager.primitiveRenderer.MeasureText(text, fontSize, lineHeight);
			// ギリギリだと自動改行が不規則に走り得るので少し余裕をもたせる TODO: ちゃんと計算しろ
			size.x += fontSize * 0.2f;
			size.y += fontSize * 0.1f;
			if (_rotateToVertical)
			{
				SetSize(size.y, size.x);
			}
			else
			{
				SetSize(size.x, size.y);
			}
		}

		public override void Draw(
			float offsetX,
			float offsetY,
			DebugPrimitiveRenderer2D renderer)
		{
			DrawTextMultiLine(
				renderer,
				text,
				color,
				_fontSize,
				offsetX + localLeftX,
				offsetY + localTopY,
				width,
				height,
				true,
				_rotateToVertical,
				lineHeight);
		}
	}
}
