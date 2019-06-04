using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kayac
{
	// 等サイズのボタンが一列に並んだもの。木を構成できる。
	public class DebugUiSubMenu : DebugUiControl
	{
		public enum Direction
		{
			Left,
			Right,
			Up,
			Down,
		}
		public Color32 textColor { get; set; }
		public Color32 pointerDownTextColor { get; set; }
		public Color32 color { get; set; }
		public Color32 pointerDownColor { get; set; }

		float _x;
		float _y;
		float _itemWidth;
		float _itemHeight;
		Direction _direction;
		public struct Item
		{
			public DebugUiButton button;
			public DebugUiSubMenu menu;
		}
		List<Item> _items;
		DebugUiSubMenu _parent;

		// 109x44はmiuraのデバグボタンのサイズ
		public DebugUiSubMenu(
			float itemWidth = 100f,
			float itemHeight = 40f,
			Direction direction = Direction.Right,
			string name = "") : base(string.IsNullOrEmpty(name) ? "SubMenu" : name)
		{
			color = new Color32(0, 0, 0, 192);
			pointerDownColor = new Color32(192, 192, 96, 192);
			textColor = new Color32(255, 255, 255, 255);
			pointerDownTextColor = new Color32(0, 0, 0, 255);
			_itemWidth = itemWidth;
			_itemHeight = itemHeight;
			_items = new List<Item>();
			_direction = direction;
		}

		public int itemCount { get { return _items.Count; } }
		public DebugUiSubMenu parent { get { return _parent; } }

		void SetParent(DebugUiSubMenu parent)
		{
			_parent = parent;
		}

		public Item GetItem(int index)
		{
			return _items[index];
		}

		public DebugUiButton AddSubMenu(
			string name,
			DebugUiSubMenu subMenu,
			Direction subMenuDirection = Direction.Down)
		{
			subMenu._parent = this;
			subMenu.enabled = false;
			subMenu.color = this.color;
			subMenu.textColor = this.textColor;
			subMenu.pointerDownColor = this.pointerDownColor;
			subMenu.pointerDownTextColor = this.pointerDownTextColor;
			var button = new DebugUiButton(name, _itemWidth, _itemHeight);
			button.color = this.color;
			button.textColor = this.textColor;
			button.pointerDownColor = this.pointerDownColor;
			button.pointerDownTextColor = this.pointerDownTextColor;
			button.onClick = () =>
			{
				// サブメニューを閉じる
				bool opened = subMenu.enabled;
				CloseSub();
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
			Item item;
			item.button = button;
			item.menu = subMenu;
			_items.Add(item);
			Enlarge();
			return button;
		}

		public DebugUiButton AddItem(string name, Action action)
		{
			var button = new DebugUiButton(name, _itemWidth, _itemHeight);
			button.color = this.color;
			button.textColor = this.textColor;
			button.pointerDownColor = this.pointerDownColor;
			button.pointerDownTextColor = this.pointerDownTextColor;
			button.onClick = () =>
			{
				// サブメニューを閉じる
				CloseSub();
				if (action != null)
				{
					action();
				}
			};
			AddChild(button, _x, _y);
			Item item;
			item.button = button;
			item.menu = null;
			_items.Add(item);
			Enlarge();
			return button;
		}

		void Enlarge()
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

		public void CloseSub()
		{
			for (int i = 0; i < _items.Count; i++)
			{
				var item = _items[i];
				if (item.menu != null)
				{
					item.menu.CloseSub();
					item.menu.enabled = false;
				}
			}
		}
	}
}
