using UnityEngine;

namespace Kayac.DebugUi
{
    /// 自動レイアウト機能付き。ただし左揃えのみ。
    public class Panel : Container
    {
        float x;
        float y;
        float currentLineSize; // 現在のライン(縦横いずれか)のサイズ

        public float LineSpace { get; set; }
        public enum Layout
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
        Layout layout;
        
        public void SetLayout(Layout layout)
        {
            SetAutoPosition(0f, 0f);
            this.layout = layout;
        }

        public Panel(
            float width = float.MaxValue,
            float height = float.MaxValue,
            bool borderEnabled = true,
            bool backgroundEnabled = true,
            bool blockRaycast = false,
            string name = "Panel") : base(name)
        {
            SetSize(width, height);
            this.BackgroundEnabled = backgroundEnabled;
            this.BorderEnabled = borderEnabled;

            x = (borderEnabled) ? (BorderWidth * 2f) : 0f;
            y = x;
            currentLineSize = 0f;
            this.LineSpace = BorderWidth;
            this.EventEnabled = blockRaycast;
            this.layout = Layout.RightDown;
        }

        // 自動レイアウトを改行する
        public void BreakLine()
        {
            float move = currentLineSize + LineSpace;
            float borderOffset = BorderEnabled ? (BorderWidth * 2f) : 0f;
            switch (layout)
            {
                case Layout.RightDown:
                case Layout.LeftDown:
                    y += move;
                    break;
                case Layout.RightUp:
                case Layout.LeftUp:
                    y -= move;
                    break;
            }
            switch (layout)
            {
                case Layout.DownRight:
                case Layout.UpRight:
                    x += move;
                    break;
                case Layout.DownLeft:
                case Layout.UpLeft:
                    x -= move;
                    break;
            }
            switch (layout)
            {
                case Layout.RightDown:
                case Layout.RightUp:
                    x = borderOffset;
                    break;
                case Layout.LeftDown:
                case Layout.LeftUp:
                    x = this.Width - borderOffset;
                    break;
            }
            switch (layout)
            {
                case Layout.DownRight:
                case Layout.DownLeft:
                    y = borderOffset;
                    break;
                case Layout.UpRight:
                case Layout.UpLeft:
                    y = this.Height - borderOffset;
                    break;
            }
            currentLineSize = 0f;
        }

        /// 自動配置位置を上書き
        public virtual void SetAutoPosition(float x, float y)
        {
            this.x = x;
            this.y = y;
            currentLineSize = 0f;
        }

        public virtual void AddAuto(Control child)
        {
            float minX = 0f;
            float minY = 0f;
            float maxX = Width;
            float maxY = Height;
            float borderOffset = BorderEnabled ? (BorderWidth * 2f) : 0f;
            if (BorderEnabled)
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
            if ((layout == Layout.RightDown) || (layout == Layout.RightUp))
            {
                alignX = AlignX.Left;
                alignY = (layout == Layout.RightDown) ? AlignY.Top : AlignY.Bottom;
                float childRight = x + BorderWidth + child.Width;
                if (childRight > maxX) // あふれた。改行する。
                {
                    BreakLine();
                }
                dx = child.Width + BorderWidth;
                size = child.Height;
            }
            else if ((layout == Layout.LeftDown) || (layout == Layout.LeftUp))
            {
                alignX = AlignX.Right;
                alignY = (layout == Layout.LeftDown) ? AlignY.Top : AlignY.Bottom;
                float childLeft = x - BorderWidth - child.Width;
                if (childLeft < minX) // あふれた。改行する。
                {
                    BreakLine();
                }
                dx = -child.Width - BorderWidth;
                size = child.Height;
            }
            else if ((layout == Layout.DownRight) || (layout == Layout.DownLeft))
            {
                alignY = AlignY.Top;
                alignX = (layout == Layout.DownRight) ? AlignX.Left : AlignX.Right;
                float childBottom = y + BorderWidth + child.Height;
                if (childBottom > maxY) // あふれた。改行する。
                {
                    BreakLine();
                }
                dy = child.Height + BorderWidth;
                size = child.Width;
            }
            else if ((layout == Layout.UpRight) || (layout == Layout.UpLeft))
            {
                alignY = AlignY.Bottom;
                alignX = (layout == Layout.UpRight) ? AlignX.Left : AlignX.Right;
                float childTop = y - BorderWidth - child.Height;
                if (childTop < minY) // あふれた。改行する。
                {
                    BreakLine();
                }
                dy = -child.Height - BorderWidth;
                size = child.Width;
            }
            Add(child, x, y, alignX, alignY);
            x += dx;
            y += dy;
            currentLineSize = Mathf.Max(currentLineSize, size);
        }

        public void AddToNextX(float dx)
        {
            x += dx;
        }

        public void AddToNextY(float dy)
        {
            y += dy;
        }
    }
}
