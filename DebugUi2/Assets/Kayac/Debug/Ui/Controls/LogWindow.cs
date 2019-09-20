using UnityEngine;

namespace Kayac.Debug.Ui
{
    public class LogWindow : Control
    {
        public Color32 Color { get; set; }

        readonly float fontSize;
        readonly string[] lines;
        readonly Color32[] colors;
        int nextLinePos;
        readonly bool captureUnityLog;

        public LogWindow(
            float fontSize,
            float width,
            float height,
            bool borderEnabled = true,
            bool captureUnityLog = false) : base("LogWindow")
        {
            this.fontSize = fontSize;
            SetSize(width, height);
            var lineCount = Mathf.CeilToInt(height / fontSize);

            lines = new string[lineCount];
            colors = new Color32[lineCount];
            nextLinePos = 0;
            BackgroundEnabled = true;
            BorderEnabled = borderEnabled;
            Color = new Color32(255, 255, 255, 255);
            this.captureUnityLog = captureUnityLog;

            if (captureUnityLog)
            {
                Application.logMessageReceivedThreaded += OnLogReceived;
            }
        }

        public override void Dispose()
        {
            if (captureUnityLog)
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
            Add(text, Color);
        }

        public void Clear()
        {
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = null;
            }
        }

        public void Add(string text, Color32 lineColor)
        {
            lock (lines)
            {
                lines[nextLinePos] = text;
                colors[nextLinePos] = lineColor;
                nextLinePos++;
                nextLinePos = (nextLinePos >= lines.Length) ? 0 : nextLinePos;
            }
        }

        public override void Draw(
            float offsetX,
            float offsetY,
            Renderer2D renderer)
        {
            float margin = BorderEnabled ? (2f * BorderWidth) : 0f;
            float x = offsetX + LocalLeftX + margin;
            float y = offsetY + LocalTopY + Height - margin; // 下端から上へ向かって描画する
            int lineCount = lines.Length;
            int lineIndex = 0;
            while (lineIndex < lineCount)
            {
                int index = nextLinePos - 1 - lineIndex;
                if (index < 0)
                {
                    index += lineCount;
                }
                else if (index >= lineCount)
                {
                    index -= lineCount;
                }
                if (lines[index] != null)
                {
                    renderer.Color = colors[index];
                    var addedLineCount = renderer.AddText(
                        lines[index],
                        x,
                        y,
                        fontSize,
                        Width - (2f * margin),
                        y - margin - (offsetY + LocalTopY),
                        AlignX.Left,
                        AlignY.Bottom);
                    y -= renderer.CalcLineHeight(fontSize) * addedLineCount;
                }
                lineIndex++;
            }
        }
    }
}
