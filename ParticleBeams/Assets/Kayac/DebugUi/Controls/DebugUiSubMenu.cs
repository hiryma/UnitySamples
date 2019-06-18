using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kayac
{
	// 等サイズのボタンが一列に並んだもの。木を構成できる。
	public class DebugUiSubMenu : DebugUiPanel
	{
		public Color32 textColor { get; set; }
		public Color32 pointerDownTextColor { get; set; }
		public Color32 color { get; set; }
		public Color32 pointerDownColor { get; set; }

		float _itemWidth;
		float _itemHeight;
		Direction _direction;
		public struct Item
		{
			public DebugUiButton button;
			public DebugUiSubMenu menu;
			public Direction menuDirection;
		}
		List<Item> _items;
		DebugUiSubMenu _parent;

		public DebugUiSubMenu(
			string name,
			float itemWidth = 100f,
			float itemHeight = 40f,
			Direction direction = Direction.Right)
			: base(0f, 0f, false, false, false, name)
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

		public Item GetItem(int index)
		{
			return _items[index];
		}

		public DebugUiButton AddSubMenu(
			DebugUiSubMenu subMenu,
			Direction subMenuDirection = Direction.Down)
		{
			subMenu._parent = this;
			subMenu.enabled = false;
			subMenu.color = this.color;
			subMenu.textColor = this.textColor;
			subMenu.pointerDownColor = this.pointerDownColor;
			subMenu.pointerDownTextColor = this.pointerDownTextColor;
			var button = new DebugUiButton(subMenu.name, _itemWidth, _itemHeight);
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
			Add(button);
			Add(subMenu);
			Item item;
			item.button = button;
			item.menu = subMenu;
			item.menuDirection = subMenuDirection;
			_items.Add(item);
			Layout();
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
			Add(button);
			Item item;
			item.button = button;
			item.menu = null;
			item.menuDirection = Direction.Unknown;
			_items.Add(item);
			Layout();
			return button;
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

		void Layout()
		{
			float x = 0;
			float y = 0;
			float dw = (_itemWidth + borderWidth);
			float dh = (_itemHeight + borderWidth);

			for (int i = 0; i < _items.Count; i++)
			{
				_items[i].button.SetLocalPosition(x, y);
				if (_items[i].menu != null)
				{
					float subX = x;
					float subY = y;
					switch (_items[i].menuDirection)
					{
						case Direction.Left: subX -= dw; break;
						case Direction.Right: subX += dw; break;
						case Direction.Up: subY -= dh; break;
						case Direction.Down: subY += dh; break;
					}
					_items[i].menu.SetLocalPosition(subX, subY);
				}
				switch (_direction)
				{
					case Direction.Left:
						x -= dw;
						break;
					case Direction.Right:
						x += dw;
						break;
					case Direction.Up:
						y -= dh;
						break;
					case Direction.Down:
						y += dh;
						break;
				}
			}

			switch (_direction)
			{
				case Direction.Left:
				case Direction.Right:
					SetSize(x, _itemHeight);
					break;
				case Direction.Up:
				case Direction.Down:
					SetSize(_itemWidth, y);
					break;
			}
		}

		public void RemoveSubMenu(DebugUiSubMenu subMenu)
		{
			CloseSub();
			int dst = 0;
			for (int i = 0; i < _items.Count; i++)
			{
				_items[dst] = _items[i];
				if (_items[dst].menu == subMenu)
				{
					RemoveChild(_items[dst].button);
					RemoveChild(subMenu);
				}
				else
				{
					dst++;
				}
			}
			_items.RemoveRange(dst, _items.Count - dst);
			Layout();
		}
	}
}
