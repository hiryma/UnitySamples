using UnityEngine;

namespace Kayac
{
	// 等サイズのボタンが一列に並んだもの。木を構成できる。
	public class DebugUiMenu : DebugUiSubMenu
	{
		public Color32 selectedTextColor { get; set; }
		public Color32 selectedColor { get; set; }
		DebugUiSubMenu _activeMenu;
		int _activeIndex;
		bool _justClicked;

		// 109x44はmiuraのデバグボタンのサイズ
		public DebugUiMenu(
			float itemWidth = 100f,
			float itemHeight = 40f,
			Direction direction = Direction.Right,
			string name = "") : base((string.IsNullOrEmpty(name) ? "Menu" : name), itemWidth, itemHeight, direction)
		{
			_activeIndex = 0;
			_activeMenu = this;
			selectedColor = new Color32(128, 128, 128, 192);
			selectedTextColor = new Color32(0, 0, 0, 255);
		}

		public bool ActivateNext()
		{
			var ret = false;
			if ((_activeIndex >= 0) && (_activeIndex < _activeMenu.itemCount))
			{
				_activeIndex++;
				if (_activeIndex >= _activeMenu.itemCount)
				{
					_activeIndex = 0;
				}
				ret = true;
			}
			return ret;
		}

		public bool ActivatePrev()
		{
			var ret = false;
			if ((_activeIndex >= 0) && (_activeIndex < _activeMenu.itemCount))
			{
				_activeIndex--;
				if (_activeIndex < 0)
				{
					_activeIndex = _activeMenu.itemCount - 1;
				}
				ret = true;
			}
			return ret;
		}

		public bool ToChild()
		{
			var ret = false;
			if ((_activeIndex >= 0) && (_activeIndex < _activeMenu.itemCount))
			{
				var item = _activeMenu.GetItem(_activeIndex);
				if (item.menu != null)
				{
					item.button.Click();
					_activeMenu = item.menu;
					_activeIndex = 0;
				}
				ret = true;
			}
			return ret;
		}

		public bool ToParent()
		{
			var ret = false;
			var parent = _activeMenu.parent;
			if (parent != null)
			{
				parent.CloseSub();
				int newIndex = 0;
				for (int i = 0; i < parent.itemCount; i++)
				{
					var item = parent.GetItem(i);
					if (item.menu == _activeMenu) // 見つかったらその番号に戻す
					{
						newIndex = i;
						break;
					}
				}
				_activeMenu = parent;
				_activeIndex = newIndex;
				ret = true;
			}
			return ret;
		}

		public bool ClickActivated()
		{
			var ret = false;
			if ((_activeIndex >= 0) && (_activeIndex < _activeMenu.itemCount))
			{
				var item = _activeMenu.GetItem(_activeIndex);
				item.button.Click();
				_justClicked = true;
				if (item.menu != null)
				{
					_activeMenu = item.menu;
					_activeIndex = 0;
				}
				ret = true;
			}
			return ret;
		}

		public override void Update(float deltaTime)
		{
			if ((_activeIndex >= 0) && (_activeIndex < _activeMenu.itemCount))
			{
				var item = _activeMenu.GetItem(_activeIndex);
				var button = item.button;
				if (_justClicked)
				{
					button.color = this.pointerDownColor;
					button.textColor = this.pointerDownTextColor;
				}
				else
				{
					button.color = this.selectedColor;
					button.textColor = this.selectedTextColor;
				}
			}
			_justClicked = false;
		}

		public override void DrawPostChild(
			float offsetX,
			float offsetY,
			DebugPrimitiveRenderer2D renderer)
		{
			if ((_activeIndex >= 0) && (_activeIndex < _activeMenu.itemCount))
			{
				var item = _activeMenu.GetItem(_activeIndex);
				var button = item.button;
				button.color = this.color;
				button.textColor = this.textColor;
			}
		}
	}
}
