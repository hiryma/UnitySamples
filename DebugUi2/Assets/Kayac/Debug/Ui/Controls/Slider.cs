using UnityEngine;

namespace Kayac.Debug.Ui
{
    public class Slider : Gauge
    {
        public Color32 SliderColor { private get; set; }
        public Color32 ActiveSliderColor { private get; set; }
        public Slider(
            string text,
            float min,
            float max,
            float width = 200f,
            float height = 50f,
            Mode mode = Mode.Float) : base(string.IsNullOrEmpty(text) ? "Slider" : text, min, max, width, height, mode)
        {
            SliderColor = GaugeColor;
            ActiveSliderColor = new Color32(255, 64, 64, 255);

            EventEnabled = true;
            Draggable = true;
        }

        public override void Update(float deltaTime)
        {
            if (IsDragging)
            {
                GaugeColor = ActiveSliderColor;
                float clientX = LocalPointerX - LocalLeftX;
                float x;
                float ratio = clientX / Width;
                if (mode == Mode.Log10)
                {
                    // ratio = (Mathf.Log10(value) - Mathf.Log10(_min)) / (Mathf.Log10(_max) - Mathf.Log10(_min)); // Gauge側がこれ。この逆関数
                    // Mathf.Log10(value) = ratio * (Mathf.Log10(_max) - Mathf.Log10(_min)) + Mathf.Log10(_min)
                    float logMin = Mathf.Log10(min);
                    float logMax = Mathf.Log10(max);
                    float logX = (ratio * (logMax - logMin)) + logMin;
                    x = Mathf.Pow(10f, logX);
                }
                else
                {
                    // 幅で割って[0,1]にし、(max-min)を乗じ、minを加える
                    x = (ratio * (max - min)) + min;
                }
                x = (x < min) ? min : ((x > max) ? max : x);
                if (mode == Mode.Integer)
                {
                    x = Mathf.RoundToInt(x);
                }
                Value = x;
            }
            else
            {
                GaugeColor = SliderColor;
            }
        }
    }
}