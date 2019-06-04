﻿using UnityEngine;

namespace Kayac
{
	/// 自動レイアウト機能付き。ただし左揃えのみ。
	public class DebugUiPanel : DebugUiControl
	{
		float _x;
		float _y;
		float _currentLineSize; // 現在のライン(縦横いずれか)のサイズ

		public float lineSpace { get; set; }
		public enum Direction
		{
			RightDown, // 左上原点。まず右、はみ出したら左に戻して下
			RightUp, // 左下原点。まず右、はみ出したら左に戻して上
			LeftDown, // 右上原点。まず左、はみ出したら右に戻して下
			LeftUp, // 右下原点。まず左、はみ出したら右に戻して上
			DownRight, // 左上原点。まず下、はみ出したら上に戻して右
			DownLeft, // 右上原点。まず下、はみ出したら上に戻して左
			UpRight, // 左下原点。まず上、はみ出したら下に戻して右
			UpLeft, // 右下原点。まず上、はみ出したら下に戻して左
		}
		public Direction direction { get; private set; }

		public void SetDirection(Direction direction)
		{
			SetAutoPosition(0f, 0f);
			this.direction = direction;
		}

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
			_currentLineSize = 0f;
			this.lineSpace = borderWidth;
			this.eventEnabled = blockRaycast;
			this.direction = Direction.RightDown;
		}

		// 自動レイアウトを改行する
		public void BreakLine()
		{
			float move = _currentLineSize + lineSpace;
			float borderOffset = borderEnabled ? (borderWidth * 2f) : 0f;
			switch (direction)
			{
				case Direction.RightDown:
				case Direction.LeftDown:
					_y += move;
					break;
				case Direction.RightUp:
				case Direction.LeftUp:
					_y -= move;
					break;
			}
			switch (direction)
			{
				case Direction.DownRight:
				case Direction.UpRight:
					_x += move;
					break;
				case Direction.DownLeft:
				case Direction.UpLeft:
					_x -= move;
					break;
			}
			switch (direction)
			{
				case Direction.RightDown:
				case Direction.RightUp:
					_x = borderOffset;
					break;
				case Direction.LeftDown:
				case Direction.LeftUp:
					_x = this.width - borderOffset;
					break;
			}
			switch (direction)
			{
				case Direction.DownRight:
				case Direction.DownLeft:
					_y = borderOffset;
					break;
				case Direction.UpRight:
				case Direction.UpLeft:
					_y = this.height - borderOffset;
					break;
			}
			_currentLineSize = 0f;
		}

		/// 自動配置位置を上書き
		public void SetAutoPosition(float x, float y)
		{
			_x = x;
			_y = y;
			_currentLineSize = 0f;
		}

		public void Add(
			DebugUiControl child,
			float offsetX = 0f,
			float offsetY = 0f,
			AlignX alignX = AlignX.Left,
			AlignY alignY = AlignY.Top)
		{
			base.AddChild(child, offsetX, offsetY, alignX, alignY);
		}

		public void AddAuto(DebugUiControl child)
		{
			float minX = 0f;
			float minY = 0f;
			float maxX = width;
			float maxY = height;
			float borderOffset = borderEnabled ? (borderWidth * 2f) : 0f;
			if (borderEnabled)
			{
				minX += borderOffset;
				minY += borderOffset;
				maxX -= borderOffset;
				maxY -= borderOffset;
			}
			// 右
			float dx = 0f;
			float dy = 0f;
			float size = 0f;
			AlignX alignX = AlignX.Left;
			AlignY alignY = AlignY.Top;
			if ((direction == Direction.RightDown) || (direction == Direction.RightUp))
			{
				alignX = AlignX.Left;
				alignY = (direction == Direction.RightDown) ? AlignY.Top : AlignY.Bottom;
				float childRight = _x + borderWidth + child.width;
				if (childRight > maxX) // あふれた。改行する。
				{
					BreakLine();
				}
				dx = child.width + borderWidth;
				size = child.height;
			}
			else if ((direction == Direction.LeftDown) || (direction == Direction.LeftUp))
			{
				alignX = AlignX.Right;
				alignY = (direction == Direction.LeftDown) ? AlignY.Top : AlignY.Bottom;
				float childLeft = _x - borderWidth - child.width;
				if (childLeft < minX) // あふれた。改行する。
				{
					BreakLine();
				}
				dx = -child.width - borderWidth;
				size = child.height;
			}
			else if ((direction == Direction.DownRight) || (direction == Direction.DownLeft))
			{
				alignY = AlignY.Top;
				alignX = (direction == Direction.DownRight) ? AlignX.Left : AlignX.Right;
				float childBottom = _y + borderWidth + child.height;
				if (childBottom > maxY) // あふれた。改行する。
				{
					BreakLine();
				}
				dy = child.height + borderWidth;
				size = child.width;
			}
			else if ((direction == Direction.UpRight) || (direction == Direction.UpLeft))
			{
				alignY = AlignY.Bottom;
				alignX = (direction == Direction.UpRight) ? AlignX.Left : AlignX.Right;
				float childTop = _y - borderWidth - child.height;
				if (childTop < minY) // あふれた。改行する。
				{
					BreakLine();
				}
				dy = -child.height - borderWidth;
				size = child.width;
			}
			AddChild(child, _x, _y, alignX, alignY);
			_x += dx;
			_y += dy;
			_currentLineSize = Mathf.Max(_currentLineSize, size);
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
