using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kayac.Debug.Ui
{
    // 等サイズのボタンが一列に並んだもの。木を構成できる。
    public class SubMenu : Panel
    {
        public Color32 TextColor { get; set; }
        public Color32 PointerDownTextColor { get; set; }
        public Color32 Color { get; set; }
        public Color32 PointerDownColor { get; set; }

        readonly float itemWidth;
        readonly float itemHeight;
        readonly Direction direction;
        public struct Item
        {
            public Button button;
            public SubMenu menu;
            public Direction menuDirection;
        }

        readonly List<Item> items;

        public SubMenu(
            string name,
            float itemWidth = 100f,
            float itemHeight = 40f,
            Direction direction = Direction.Right)
            : base(0f, 0f, false, false, false, name)
        {
            Color = new Color32(0, 0, 0, 192);
            PointerDownColor = new Color32(192, 192, 96, 192);
            TextColor = new Color32(255, 255, 255, 255);
            PointerDownTextColor = new Color32(0, 0, 0, 255);
            this.itemWidth = itemWidth;
            this.itemHeight = itemHeight;
            items = new List<Item>();
            this.direction = direction;
        }

        public int ItemCount { get { return items.Count; } }
        public SubMenu Parent { get; private set; }

        public Item GetItem(int index)
        {
            return items[index];
        }

        public Button AddSubMenu(
            SubMenu subMenu,
            Direction subMenuDirection = Direction.Down)
        {
            subMenu.Parent = this;
            subMenu.Enabled = false;
            subMenu.Color = Color;
            subMenu.TextColor = TextColor;
            subMenu.PointerDownColor = PointerDownColor;
            subMenu.PointerDownTextColor = PointerDownTextColor;
            var button = new Button(subMenu.Name, itemWidth, itemHeight)
            {
                Color = Color,
                TextColor = TextColor,
                PointerDownColor = PointerDownColor,
                PointerDownTextColor = PointerDownTextColor,
                OnClick = () =>
                {
                    // サブメニューを閉じる
                    bool opened = subMenu.Enabled;
                    CloseSub();
                    if (!opened)
                    {
                        subMenu.Enabled = true;
                    }
                }
            };
            Add(button);
            Add(subMenu);
            Item item;
            item.button = button;
            item.menu = subMenu;
            item.menuDirection = subMenuDirection;
            items.Add(item);
            Layout();
            return button;
        }

        public Button AddItem(string name, Action action)
        {
            var button = new Button(name, itemWidth, itemHeight)
            {
                Color = Color,
                TextColor = TextColor,
                PointerDownColor = PointerDownColor,
                PointerDownTextColor = PointerDownTextColor,
                OnClick = () =>
                {
                    // サブメニューを閉じる
                    CloseSub();
                    if (action != null)
                    {
                        action();
                    }
                }
            };
            Add(button);
            Item item;
            item.button = button;
            item.menu = null;
            item.menuDirection = Direction.Unknown;
            items.Add(item);
            Layout();
            return button;
        }

        public void CloseSub()
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.menu != null)
                {
                    item.menu.CloseSub();
                    item.menu.Enabled = false;
                }
            }
        }

        new void Layout()
        {
            float x = 0;
            float y = 0;
            float dw = (itemWidth + BorderWidth);
            float dh = (itemHeight + BorderWidth);

            for (int i = 0; i < items.Count; i++)
            {
                items[i].button.SetLocalPosition(x, y);
                if (items[i].menu != null)
                {
                    float subX = x;
                    float subY = y;
                    switch (items[i].menuDirection)
                    {
                        case Direction.Left: subX -= dw; break;
                        case Direction.Right: subX += dw; break;
                        case Direction.Up: subY -= dh; break;
                        case Direction.Down: subY += dh; break;
                    }
                    items[i].menu.SetLocalPosition(subX, subY);
                }
                switch (direction)
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

            switch (direction)
            {
                case Direction.Left:
                case Direction.Right:
                    SetSize(x, itemHeight);
                    break;
                case Direction.Up:
                case Direction.Down:
                    SetSize(itemWidth, y);
                    break;
            }
        }

        public void RemoveSubMenu(SubMenu subMenu)
        {
            CloseSub();
            int dst = 0;
            for (int i = 0; i < items.Count; i++)
            {
                items[dst] = items[i];
                if (items[dst].menu == subMenu)
                {
                    RemoveChild(items[dst].button);
                    RemoveChild(subMenu);
                }
                else
                {
                    dst++;
                }
            }
            items.RemoveRange(dst, items.Count - dst);
            Layout();
        }
    }
}
