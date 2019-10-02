using UnityEngine;

namespace Kayac.Debug.Ui
{
    // 2本のゲージを描く。secondaryを描画後、primaryを描画。
    // 数字は左がsecondary、右がprimary
    public class DualGauge : Control
    {
        public float PrimaryValue { private get; set; }
        public float SecondaryValue { private get; set; }
        public Color32 TextColor { private get; set; }
        public Color32 PrimaryGaugeColor { private get; set; }
        public Color32 SecondaryGaugeColor { private get; set; }
        readonly float min;
        readonly float max;
        public string Label { private get; set; }
        readonly bool asInteger;

        public DualGauge(
            float min,
            float max,
            float width = 0,
            float height = 0,
            bool asInteger = false) : base("DualGauge")
        {
            this.asInteger = asInteger;
            this.min = min;
            this.max = max;
            TextColor = new Color32(255, 255, 255, 255);
            PrimaryGaugeColor = new Color32(0, 255, 0, 128);
            SecondaryGaugeColor = new Color32(255, 0, 0, 128);

            BackgroundEnabled = true;
            BorderEnabled = true;

            SetSize(width, height);
        }

        public override void Draw(
            float offsetX,
            float offsetY,
            Renderer2D renderer)
        {
            DrawGauge(offsetX, offsetY, renderer, SecondaryValue, SecondaryGaugeColor);
            DrawGauge(offsetX, offsetY, renderer, PrimaryValue, PrimaryGaugeColor);

            float fontSize = Height - (2f * BorderWidth);
            fontSize *= 0.8f;
            renderer.Color = TextColor;
            string format = asInteger ? "F0" : "F1";

            if (string.IsNullOrEmpty(Label)) // ラベルがなければ両方の数字を左右に分けて出す
            {
                // primaryは左寄せ
                renderer.AddText(
                    PrimaryValue.ToString(format),
                    offsetX + LocalLeftX + (BorderWidth * 2f),
                    offsetY + LocalTopY + (BorderWidth * 2f),
                    fontSize);

                // secondaryは右寄せ
                renderer.AddText(
                    SecondaryValue.ToString(format),
                    offsetX + LocalLeftX + Width - (BorderWidth * 2f),
                    offsetY + LocalTopY + (BorderWidth * 2f),
                    fontSize,
                    AlignX.Right);
            }
            else // ラベルがあれば左にラベル、右にプライマリの数値
            {
                renderer.AddText(
                    Label,
                    offsetX + LocalLeftX + (BorderWidth * 2f),
                    offsetY + LocalTopY + (BorderWidth * 2f),
                    fontSize);

                // 数字は右寄せ
                renderer.AddText(
                    PrimaryValue.ToString(format),
                    offsetX + LocalLeftX + Width - (BorderWidth * 2f),
                    offsetY + LocalTopY + (BorderWidth * 2f),
                    fontSize,
                    AlignX.Right);
            }
        }

        void DrawGauge(
            float offsetX,
            float offsetY,
            Renderer2D renderer,
            float value,
            Color32 color)
        {
            // seconadary
            float ratio = (value - min) / (max - min);
            ratio = (ratio < 0f) ? 0f : ((ratio > 1f) ? 1f : ratio);
            float length = ratio * (Width - (2f * BorderWidth));

            renderer.Color = color;
            renderer.AddRectangle(
                offsetX + LocalLeftX + BorderWidth,
                offsetY + LocalTopY + BorderWidth,
                length,
                Height - (2f * BorderWidth));
        }
    }
}