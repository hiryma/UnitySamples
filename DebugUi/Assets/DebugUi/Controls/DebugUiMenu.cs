using System;
using System.Collections.Generic;

namespace Kayac
{
	// 等サイズのボタンが一列に並んだもの。木を構成できる。
	public class DebugUiMenu : DebugUiControl
	{
		public enum Direction
		{
			Left,
			Right,
			Up,
			Down,
		}
		private float _x;
		private float _y;
		private float _itemWidth;
		private float _itemHeight;
		private Direction _direction;
		private List<DebugUiMenu> _subMenus;

		// 109x44はmiuraのデバグボタンのサイズ
		public DebugUiMenu(
			float itemWidth = 109f,
			float itemHeight = 44f,
			Direction direction = Direction.Right,
			string name = "") : base(string.IsNullOrEmpty(name) ? "Menu" : name)
		{
			_itemWidth = itemWidth;
			_itemHeight = itemHeight;
			_subMenus = new List<DebugUiMenu>();
			_direction = direction;
		}

		public DebugUiMenu AddMenu(
			string name,
			DebugUiMenu subMenu,
			Direction subMenuDirection = Direction.Down)
		{
			subMenu.enabled = false;
			var button = new DebugUiButton(name, _itemWidth, _itemHeight);
			button.onClick = () =>
			{
				// サブメニューを閉じる
				bool opened = subMenu.enabled;
				CloseSubMenus();
				if (!opened)
				{
					subMenu.enabled = true;
				}
			};
			AddChild(button, _x, _y);
			float subX = _x;
			float subY = _y;
			float dw = (_itemWidth + borderWidth);
			float dh = (_itemHeight + borderWidth);
			switch (subMenuDirection)
			{
				case Direction.Left: subX -= dw; break;
				case Direction.Right: subX += dw; break;
				case Direction.Up: subY -= dh; break;
				case Direction.Down: subY += dh; break;
			}
			AddChild(subMenu, subX, subY);
			_subMenus.Add(subMenu);
			Enlarge();
			return this;
		}

		public DebugUiMenu AddItem(string name, Action action)
		{
			var button = new DebugUiButton(name, _itemWidth, _itemHeight);
			button.onClick = () =>
			{
				// サブメニューを閉じる
				CloseSubMenus();
				if (action != null)
				{
					action();
				}
			};
			AddChild(button, _x, _y);
			Enlarge();
			return this;
		}

		private void Enlarge()
		{
			float dw = (_itemWidth + borderWidth);
			float dh = (_itemHeight + borderWidth);
			switch (_direction)
			{
				case Direction.Left:
					_x -= dw;
					SetSize(width + dw, _itemHeight);
					break;
				case Direction.Right:
					_x += dw;
					SetSize(width + dw, _itemHeight);
					break;
				case Direction.Up:
					_y -= dh;
					SetSize(_itemWidth, height + dh);
					break;
				case Direction.Down:
					_y += dh;
					SetSize(_itemWidth, height + dh);
					break;
			}
		}

		private void CloseSubMenus()
		{
			foreach (var menu in _subMenus)
			{
				menu.CloseSubMenus();
				menu.enabled = false;
			}
		}
	}
}
