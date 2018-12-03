using UnityEngine;

namespace Kayac
{
	public class DebugUiNumberInput : DebugUiPanel
	{
		private int _value;
		public int value
		{
			get { return _value; }
			set
			{
				_value = value;
				UpdateDigits();
			}
		}
		private DebugUiButton[] _digits; // 簡易実装

		public Color32 activeColor { get; set; }
		private Color32 defaultColor;

		public delegate void UpdateCallback(int value);
		public UpdateCallback onUpdate;

		public DebugUiNumberInput(
			int digitCount,
			int initialValue,
			float height = 50f)
		{
			this._value = initialValue;
			_digits = new DebugUiButton[digitCount];
			for (int i = 0; i < digitCount; i++)
			{
				var digit = new DebugUiButton("0", height * 0.6f, height);
				AddChildAuto(digit);
				digit.onClick = () =>
				{
					OnClickDigit(digit);
				};
				_digits[i] = digit;
			}
			AdjustSize();
			eventEnabled = true;
			draggable = true;
			UpdateDigits();

			activeColor = new Color32(120, 120, 90, 128);
			defaultColor = backgroundColor;
		}

		private void OnClickDigit(DebugUiButton digit)
		{
			// 何桁目が押されたか識別
			int index = -1;
			int scale = 1;
			for (int i = 0; i < _digits.Length; i++)
			{
				if (_digits[i] == digit)
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
			int v = value;
			v /= scale;
			int d = v % 10;
			if (d == 9)
			{
				value -= d * scale;
			}
			else
			{
				value += scale;
			}
			UpdateDigits();
		}

		private void UpdateDigits()
		{
			int scale = 1;
			for (int i = 0; i < _digits.Length - 1; i++)
			{
				scale *= 10;
			}

			int v = value;
			for (int i = 0; i < _digits.Length; i++)
			{
				var q = v / scale;
				_digits[i].text = q.ToString();
				v -= q * scale;
				scale /= 10;
			}

			if (onUpdate != null)
			{
				onUpdate(value);
			}
		}

		public override void Update()
		{
			base.Update();

			bool focus = hasFocus;
			foreach (var digit in _digits)
			{
				focus = focus || digit.hasFocus;
			}

			if (focus)
			{
				backgroundColor = activeColor;

				int exp = 1;
				for (int i = 0; i < _digits.Length; ++i)
				{
					exp *= 10;
				}

				for (int i = 0; i < 10; ++i)
				{
					if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
					{
						value = (value * 10 + i) % exp;
						UpdateDigits();
					}
				}
			}
			else
			{
				backgroundColor = defaultColor;
			}
		}
	}
}
