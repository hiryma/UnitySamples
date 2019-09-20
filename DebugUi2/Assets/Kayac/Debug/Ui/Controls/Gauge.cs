using UnityEngine;

namespace Kayac.Debug.Ui
{
    public class Gauge : Control
    {
        public enum Mode
        {
            Float,
            Integer,
            Log10,
        }
        public float Value { get; set; }
        public Color32 TextColor { private get; set; }
        public Color32 GaugeColor { protected get; set; }

        readonly string text;
        protected float min;
        protected float max;
        protected Mode mode;

        public Gauge(
            string text,
            float min,
            float max,
            float width = 0,
            float height = 0,
            Mode mode = Mode.Float) : base(string.IsNullOrEmpty(text) ? "Gauge" : text)
        {
            this.min = min;
            this.max = max;
            this.text = text;
            this.mode = mode;
            TextColor = new Color32(255, 255, 255, 255);
            GaugeColor = new Color32(255, 0, 0, 128);

            BackgroundEnabled = true;
            BorderEnabled = true;

            SetSize(width, height);
        }

        public override void Draw(
            float offsetX,
            float offsetY,
            Renderer2D renderer)
        {
            // 棒を描画
            float ratio;
            if (mode == Mode.Log10)
            {
                float logMin = Mathf.Log10(min);
                float logMax = Mathf.Log10(max);
                ratio = (Mathf.Log10(Value) - logMin) / (logMax - logMin);
            }
            else
            {
                ratio = (Value - min) / (max - min);
            }
            ratio = (ratio < 0f) ? 0f : ((ratio > 1f) ? 1f : ratio);
            float barWidth = (Width - (2f * BorderWidth)) * ratio;

            renderer.Color = GaugeColor;
            renderer.AddRectangle(
                offsetX + LocalLeftX + BorderWidth,
                offsetY + LocalTopY + BorderWidth,
                barWidth,
                Height - (2f * BorderWidth));

            // 題名
            renderer.Color = TextColor;
            if (text.Length > 0)
            {
                renderer.AddText(
                    text,
                    offsetX + LocalLeftX + (BorderWidth * 2f),
                    offsetY + LocalTopY + (BorderWidth * 2f),
                    Width - (BorderWidth * 2f),
                    Height - (BorderWidth * 2f));
            }

            string formatString = (mode == Mode.Integer) ? "F0" : "F2";

            // 数字は右寄せ
            renderer.AddText(
                Value.ToString(formatString),
                offsetX + LocalLeftX + Width - (BorderWidth * 2f),
                offsetY + LocalTopY + (BorderWidth * 2f),
                Width - (BorderWidth * 2f),
                Height - (BorderWidth * 2f),
                AlignX.Right);
        }
    }

}