using UnityEngine;

namespace Kayac
{
	public class DebugUiText : DebugUiControl
	{
		public Color32 color { get; set; }
		public float lineSpacing { get; set; }
		public string text { get; set; }
		public AlignX alignX { get; set; }
		public AlignY alignY { get; set; }
		float _fontSize;

		public DebugUiText(
			string text,
			float fontSize,
			float width,
			float height,
			bool backgroundEnabled = false) : base(string.IsNullOrEmpty(text) ? "Text" : text)
		{
			Debug.Assert(height > (fontSize * 1.1f), "fontSize might be too large relative to height.");
			SetSize(width, height);
			this.text = text;
			_fontSize = fontSize;
			Initialize();
		}

		// 自動測定にはmanagerが必要。TODO: どうにかしたい
		public DebugUiText(
			DebugUiManager manager,
			string text,
			float fontSize,
			bool backgroundEnabled = false) : base(string.IsNullOrEmpty(text) ? "Text" : text)
		{
			this.text = text;
			_fontSize = fontSize;
			var size = manager.primitiveRenderer.MeasureText(text, fontSize, lineSpacing);
			// ギリギリだと自動改行が不規則に走り得るので少し余裕をもたせる TODO: ちゃんと計算しろ
			size.x += fontSize * 0.2f;
			size.y += fontSize * 0.1f;
			SetSize(size.x, size.y);
		}

		void Initialize()
		{
			this.lineSpacing = DebugPrimitiveRenderer.DefaultLineSpacingRatio;
			this.backgroundEnabled = false;
			this.borderEnabled = false;
			this.color = new Color32(255, 255, 255, 255);
			this.alignX = AlignX.Left;
			this.alignY = AlignY.Top;
		}

		public override void Draw(
			float offsetX,
			float offsetY,
			DebugPrimitiveRenderer2D renderer)
		{
			float x = offsetX + this.localLeftX;
			float y = offsetY + this.localTopY;
			var primAlignX = DebugPrimitiveRenderer.AlignX.Left;
			var primAlignY = DebugPrimitiveRenderer.AlignY.Top;
			switch (this.alignX)
			{
				case AlignX.Center:
					x += this.width * 0.5f;
					primAlignX = DebugPrimitiveRenderer.AlignX.Center;
					break;
				case AlignX.Right:
					x += this.width;
					primAlignX = DebugPrimitiveRenderer.AlignX.Right;
					break;
			}
			switch (this.alignY)
			{
				case AlignY.Center:
					y += this.height * 0.5f;
					primAlignY = DebugPrimitiveRenderer.AlignY.Center;
					break;
				case AlignY.Bottom:
					y += this.height;
					primAlignY = DebugPrimitiveRenderer.AlignY.Bottom;
					break;
			}
			renderer.color = this.color;
			renderer.AddText(
				 this.text,
				 x,
				 y,
				 _fontSize,
				 this.width,
				 this.height,
				 primAlignX,
				 primAlignY,
				 this.lineSpacing);
		}
	}
}
