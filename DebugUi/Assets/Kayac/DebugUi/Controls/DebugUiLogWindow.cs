using UnityEngine;

namespace Kayac
{
	public class DebugUiLogWindow : DebugUiControl
	{
		public Color32 color { get; set; }
		float _fontSize;
		string[] _lines;
		Color32[] _colors;
		int _nextLinePos;
		bool _captureUnityLog;

		public DebugUiLogWindow(
			float fontSize,
			float width,
			float height,
			bool borderEnabled = true,
			bool captureUnityLog = false) : base("LogWindow")
		{
			_fontSize = fontSize;
			SetSize(width, height);
			var lineCount = Mathf.CeilToInt(height / fontSize);

			_lines = new string[lineCount];
			_colors = new Color32[lineCount];
			_nextLinePos = 0;
			this.backgroundEnabled = true;
			this.borderEnabled = borderEnabled;
			color = new Color32(255, 255, 255, 255);
			_captureUnityLog = captureUnityLog;

			if (captureUnityLog)
			{
				Application.logMessageReceivedThreaded += OnLogReceived;
			}
		}

		public override void Dispose()
		{
			if (_captureUnityLog)
			{
				Application.logMessageReceivedThreaded -= OnLogReceived;
			}
		}

		void OnLogReceived(string text, string callStack, LogType type)
		{
			Color32 color;
			switch (type)
			{
				case LogType.Log: color = new Color32(255, 255, 255, 255); break;
				case LogType.Warning: color = new Color32(255, 255, 0, 255); break;
				default: color = new Color32(255, 0, 64, 255); break;
			}
			Add(text, color);
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
			lock (_lines)
			{
				_lines[_nextLinePos] = text;
				_colors[_nextLinePos] = lineColor;
				_nextLinePos++;
				_nextLinePos = (_nextLinePos >= _lines.Length) ? 0 : _nextLinePos;
			}
		}

		public override void Draw(
			float offsetX,
			float offsetY,
			DebugPrimitiveRenderer2D renderer)
		{
			float margin = borderEnabled ? (2f * borderWidth) : 0f;
			float x = offsetX + localLeftX + margin;
			float y = offsetY + localTopY + this.height - margin; // 下端から上へ向かって描画する
			float textWidth = width - (2f * margin);
			int lineCount = _lines.Length;
			int lineIndex = 0;
			while (lineIndex < lineCount)
			{
				int index = _nextLinePos - 1 - lineIndex;
				if (index < 0)
				{
					index += lineCount;
				}
				else if (index >= lineCount)
				{
					index -= lineCount;
				}
				if (_lines[index] != null)
				{
					renderer.color = _colors[index];
					var lines = renderer.AddText(
						_lines[index],
						x,
						y,
						_fontSize,
						width - (2f * margin),
						y - margin - (offsetY + localTopY),
						DebugPrimitiveRenderer.AlignX.Left,
						DebugPrimitiveRenderer.AlignY.Bottom);
					y -= renderer.CalcLineHeight(_fontSize) * lines;
				}
				lineIndex++;
			}
		}
	}
}
