using UnityEngine;

namespace Kayac.Debug.Ui
{
    // 等サイズのボタンが一列に並んだもの。木を構成できる。
    public class Menu : SubMenu
    {
        public Color32 SelectedTextColor { get; set; }
        public Color32 SelectedColor { get; set; }
        SubMenu activeMenu;
        int activeIndex;
        bool justClicked;

        // 109x44はmiuraのデバグボタンのサイズ
        public Menu(
            float itemWidth = 100f,
            float itemHeight = 40f,
            Direction direction = Direction.Right,
            string name = "") : base((string.IsNullOrEmpty(name) ? "Menu" : name), itemWidth, itemHeight, direction)
        {
            activeIndex = 0;
            activeMenu = this;
            SelectedColor = new Color32(128, 128, 128, 192);
            SelectedTextColor = new Color32(0, 0, 0, 255);
        }

        public bool ActivateNext()
        {
            var ret = false;
            if ((activeIndex >= 0) && (activeIndex < activeMenu.ItemCount))
            {
                activeIndex++;
                if (activeIndex >= activeMenu.ItemCount)
                {
                    activeIndex = 0;
                }
                ret = true;
            }
            return ret;
        }

        public bool ActivatePrev()
        {
            var ret = false;
            if ((activeIndex >= 0) && (activeIndex < activeMenu.ItemCount))
            {
                activeIndex--;
                if (activeIndex < 0)
                {
                    activeIndex = activeMenu.ItemCount - 1;
                }
                ret = true;
            }
            return ret;
        }

        public bool ToChild()
        {
            var ret = false;
            if ((activeIndex >= 0) && (activeIndex < activeMenu.ItemCount))
            {
                var item = activeMenu.GetItem(activeIndex);
                if (item.menu != null)
                {
                    item.button.Click();
                    activeMenu = item.menu;
                    activeIndex = 0;
                }
                ret = true;
            }
            return ret;
        }

        public bool ToParent()
        {
            var ret = false;
            var parent = activeMenu.Parent;
            if (parent != null)
            {
                parent.CloseSub();
                int newIndex = 0;
                for (int i = 0; i < parent.ItemCount; i++)
                {
                    var item = parent.GetItem(i);
                    if (item.menu == activeMenu) // 見つかったらその番号に戻す
                    {
                        newIndex = i;
                        break;
                    }
                }
                activeMenu = parent;
                activeIndex = newIndex;
                ret = true;
            }
            return ret;
        }

        public bool ClickActivated()
        {
            var ret = false;
            if ((activeIndex >= 0) && (activeIndex < activeMenu.ItemCount))
            {
                var item = activeMenu.GetItem(activeIndex);
                item.button.Click();
                justClicked = true;
                if (item.menu != null)
                {
                    activeMenu = item.menu;
                    activeIndex = 0;
                }
                ret = true;
            }
            return ret;
        }

        public override void Update(float deltaTime)
        {
            if ((activeIndex >= 0) && (activeIndex < activeMenu.ItemCount))
            {
                var item = activeMenu.GetItem(activeIndex);
                var button = item.button;
                if (justClicked)
                {
                    button.Color = PointerDownColor;
                    button.TextColor = PointerDownTextColor;
                }
                else
                {
                    button.Color = SelectedColor;
                    button.TextColor = SelectedTextColor;
                }
            }
            justClicked = false;
        }

        public override void DrawPostChild(
            float offsetX,
            float offsetY,
            Renderer2D renderer)
        {
            if ((activeIndex >= 0) && (activeIndex < activeMenu.ItemCount))
            {
                var item = activeMenu.GetItem(activeIndex);
                var button = item.button;
                button.Color = Color;
                button.TextColor = TextColor;
            }
        }
    }
}
