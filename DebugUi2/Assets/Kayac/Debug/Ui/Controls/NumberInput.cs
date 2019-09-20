using UnityEngine;

namespace Kayac.Debug.Ui
{
    public class NumberInput : Panel
    {
        int value;
        public int Value
        {
            get { return value; }
            set
            {
                this.value = value;
                UpdateDigits();
            }
        }

        readonly Button[] digits; // 簡易実装

        public Color32 ActiveColor { get; set; }
        Color32 defaultColor;
        public delegate void UpdateCallback(int value);
        public UpdateCallback OnUpdate { get; set; }

        public NumberInput(
            int digitCount,
            int initialValue,
            float height = 50f)
        {
            this.value = initialValue;
            digits = new Button[digitCount];
            for (int i = 0; i < digitCount; i++)
            {
                var digit = new Button("0", height * 0.6f, height);
                AddAuto(digit);
                digit.OnClick = () =>
                {
                    OnClickDigit(digit);
                };
                digits[i] = digit;
            }
            FitSize();
            EventEnabled = true;
            Draggable = true;
            UpdateDigits();

            ActiveColor = new Color32(120, 120, 90, 128);
            defaultColor = BackgroundColor;
        }

        void OnClickDigit(Button digit)
        {
            // 何桁目が押されたか識別
            int index = -1;
            int scale = 1;
            for (int i = 0; i < digits.Length; i++)
            {
                if (digits[i] == digit)
                {
                    index = i;
                }
                scale *= 10;
            }

            // 該当する桁を抜き出し
            for (int i = 0; i <= index; i++)
            {
                scale /= 10;
            }
            int v = Value;
            v /= scale;
            int d = v % 10;
            if (d == 9)
            {
                Value -= d * scale;
            }
            else
            {
                Value += scale;
            }
            UpdateDigits();
        }

        void UpdateDigits()
        {
            int scale = 1;
            for (int i = 0; i < digits.Length - 1; i++)
            {
                scale *= 10;
            }

            int v = Value;
            for (int i = 0; i < digits.Length; i++)
            {
                var q = v / scale;
                digits[i].Text = q.ToString();
                v -= q * scale;
                scale /= 10;
            }

            if (OnUpdate != null)
            {
                OnUpdate(Value);
            }
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            bool focus = HasFocus;
            foreach (var digit in digits)
            {
                focus = focus || digit.HasFocus;
            }

            if (focus)
            {
                BackgroundColor = ActiveColor;

                int exp = 1;
                for (int i = 0; i < digits.Length; ++i)
                {
                    exp *= 10;
                }

                for (int i = 0; i < 10; ++i)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                    {
                        Value = (Value * 10 + i) % exp;
                        UpdateDigits();
                    }
                }
            }
            else
            {
                BackgroundColor = defaultColor;
            }
        }
    }
}
