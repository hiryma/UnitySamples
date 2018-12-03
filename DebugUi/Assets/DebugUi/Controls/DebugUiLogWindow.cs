using UnityEngine;

namespace Kayac
{
	public class DebugUiLogWindow : DebugUiControl
	{
		public Color32 color{ get; set; }
		private float _fontSize;
		private float _lineHeight;
		private string[] _lines;
		private Color32[] _colors;
		private int _nextLinePos;

		// 左整列ならwidth,heightが0でも正常に動く
		public DebugUiLogWindow(
			float fontSize,
			float lineHeight,
			int lineCount,
			float width) : base("LogWindow")
		{
			_fontSize = fontSize;
			_lineHeight = lineHeight;

			float height = ((float)lineHeight * lineCount) + (borderWidth * 4f);
			SetSize(width, height);

			_lines = new string[lineCount];
			_colors = new Color32[lineCount];
			_nextLinePos = 0;
			backgroundEnabled = true;
			borderEnabled = true;
			color = new Color32(255, 255, 255, 255);
		}

		public void Add(string text)
		{
			// デフォルト色
			Add(text, color);
		}

		public void Clear()
		{
			for (int i = 0; i < _lines.Length; i++)
			{
				_lines[i] = null;
			}
		}

		public void Add(string text, Color32 lineColor)
		{
			_lines[_nextLinePos] = text;
			_colors[_nextLinePos] = lineColor;
			_nextLinePos++;
			_nextLinePos = (_nextLinePos >= _lines.Length) ? 0 : _nextLinePos;
		}

		public override void Draw(
			float offsetX,
			float offsetY,
			DebugPrimitiveRenderer2D renderer)
		{
			float x = offsetX + localLeftX + (2f * borderWidth);
			float y = offsetY + localTopY + (2f * borderWidth);
			float textWidth = width - (4f * borderWidth);
			int lineCount = _lines.Length;
			for (int i = 0; i < lineCount; i++)
			{
				int index = i + _nextLinePos;
				index = (index >= lineCount) ? (index - lineCount) : index;
				if (_lines[index] != null)
				{
					DrawTextSingleLine(
						renderer,
						_lines[index],
						_colors[index],
						_fontSize,
						x,
						y,
						textWidth,
						_lineHeight);
					y += _lineHeight;
				}
			}
		}
	}
}
