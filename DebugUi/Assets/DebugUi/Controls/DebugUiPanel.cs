using UnityEngine;

namespace Kayac
{
	/// 自動レイアウト機能付き。ただし左揃えのみ。
	public class DebugUiPanel : DebugUiControl
	{
		private float _x;
		private float _y;
		private float _currentLineHeight;
		public float lineSpace{ get; set; }

		public DebugUiPanel(
			float width = float.MaxValue,
			float height = float.MaxValue,
			bool borderEnabled = true,
			bool backgroundEnabled = true,
			bool blockRaycast = false) : base("Panel")
		{
			SetSize(width, height);
			this.backgroundEnabled = backgroundEnabled;
			this.borderEnabled = borderEnabled;

			_x = (borderEnabled) ? (borderWidth * 2f) : 0f;
			_y = _x;
			_currentLineHeight = 0f;
			lineSpace = borderWidth;
			eventEnabled = blockRaycast;
		}

		// 自動レイアウトを改行する
		public void BreakLine()
		{
			_y += _currentLineHeight + lineSpace;
			_x = (borderEnabled) ? (borderWidth * 2f) : 0f;
			_currentLineHeight = 0f;
		}

		/// 自動配置位置を上書き
		public void SetAutoPosition(float x, float y)
		{
			_x = x;
			_y = y;
		}

		public void AddChildAuto(DebugUiControl control)
		{
			// 今の位置に入れて入るかを判定
			float childWidth = control.width;
			float childHeight = control.height;
			float childRight = _x + borderWidth + childWidth;
			float maxRight = width;
			float maxBottom = height;
			if (borderEnabled)
			{
				maxRight -= borderWidth * 2f;
				maxBottom -= borderWidth * 2f;
			}
			// 右にあふれた。改行する。
			if (childRight > maxRight)
			{
				BreakLine();
			}
			// 下にあふれる分には使い手の責任とする。
			AddChild(control, _x, _y);
			_x += childWidth + borderWidth;
			_currentLineHeight = Mathf.Max(_currentLineHeight, childHeight);
		}

		public void AddToNextX(float dx)
		{
			_x += dx;
		}

		public void AddToNextY(float dy)
		{
			_y += dy;
		}
	}
}
