using UnityEngine;

namespace Kayac.DebugUi
{
    public class Text : Control
    {
        public Color32 Color { get; set; }
        public float LineSpacing { get; set; }
        public string Value { get; set; }
        public AlignX AlignX { get; set; }
        public AlignY AlignY { get; set; }
        readonly float fontSize;

        public Text(
            string text,
            float fontSize,
            float width,
            float height,
            bool backgroundEnabled = false) : base(string.IsNullOrEmpty(text) ? "Text" : text)
        {
            UnityEngine.Debug.Assert(height > (fontSize * 1.1f), "fontSize might be too large relative to height.");
            SetSize(width, height);
            Value = text;
            this.fontSize = fontSize;
            Initialize(backgroundEnabled);
        }

        // 自動測定にはmanagerが必要。TODO: どうにかしたい
        public Text(
            DebugUiManager manager,
            string text,
            float fontSize,
            bool backgroundEnabled = false) : base(string.IsNullOrEmpty(text) ? "Text" : text)
        {
            Value = text;
            this.fontSize = fontSize;
            var size = manager.Renderer.MeasureText(text, fontSize, LineSpacing);
            // ギリギリだと自動改行が不規則に走り得るので少し余裕をもたせる TODO: ちゃんと計算しろ
            size.x += fontSize * 0.2f;
            size.y += fontSize * 0.1f;
            SetSize(size.x, size.y);
            Initialize(backgroundEnabled);
        }

        void Initialize(bool backgroundEnabled)
        {
            LineSpacing = RendererBase.DefaultLineSpacingRatio;
            BackgroundEnabled = backgroundEnabled;
            BorderEnabled = false;
            Color = new Color32(255, 255, 255, 255);
            AlignX = AlignX.Left;
            AlignY = AlignY.Top;
        }

        public override void Draw(
            float offsetX,
            float offsetY,
            Renderer2D renderer)
        {
            float x = offsetX + LocalLeftX;
            float y = offsetY + LocalTopY;
            var primAlignX = AlignX.Left;
            var primAlignY = AlignY.Top;
            switch (AlignX)
            {
                case AlignX.Center:
                    x += Width * 0.5f;
                    primAlignX = AlignX.Center;
                    break;
                case AlignX.Right:
                    x += Width;
                    primAlignX = AlignX.Right;
                    break;
            }
            switch (AlignY)
            {
                case AlignY.Center:
                    y += Height * 0.5f;
                    primAlignY = AlignY.Center;
                    break;
                case AlignY.Bottom:
                    y += Height;
                    primAlignY = AlignY.Bottom;
                    break;
            }
            renderer.Color = Color;
            renderer.AddText(
                 Value,
                 x,
                 y,
                 fontSize,
                 Width,
                 Height,
                 primAlignX,
                 primAlignY,
                 LineSpacing);
        }
    }
}
