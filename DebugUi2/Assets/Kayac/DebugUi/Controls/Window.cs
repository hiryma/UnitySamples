using UnityEngine;

namespace Kayac.DebugUi
{
    public class Window : Container
    {
        public Color32 HeaderColor
        {
            set
            {
                headerPanel.BackgroundColor = value;
            }
        }

        readonly Panel headerPanel;
        readonly Panel contentPanel;
        readonly DebugUiManager manager;
        float prevPointerX;
        float prevPointerY;
        const float DefaultHeaderSize = 50f;

        public float LineSpace
        {
            set
            {
                contentPanel.LineSpace = value;
            }
        }

        // TODO: とある事情によりmanagerが必要
        public Window(
            DebugUiManager manager,
            string title,
            float headerHeight = DefaultHeaderSize) : base(string.IsNullOrEmpty(title) ? "Window" : title)
        {
            this.manager = manager;
            headerPanel = new Panel(
                float.MaxValue,
                float.MaxValue,
                true,
                true,
                true)
            {
                BackgroundColor = new Color32(0, 0, 128, 128),
                Draggable = true,
                OnDragStart = () =>
                {
                    prevPointerX = PointerX;
                    prevPointerY = PointerY;
                },
                // このヘッダをタップしたら、windowを手前に持ってくる
                OnEventConsume = () =>
                {
                    this.manager.MoveToTop(this);
                }
            };

            var closeButton = new Button(
                "Ｘ",
                DefaultHeaderSize,
                DefaultHeaderSize)
            {
                OnClick = () =>
                {
                    Enabled = false;
                }
            };
            headerPanel.AddAuto(closeButton);

            var minimizeButton = new Button(
                "＿",
                DefaultHeaderSize,
                DefaultHeaderSize)
            {
                OnClick = ToggleMinimize
            };
            headerPanel.AddAuto(minimizeButton);
            var titleText = new Text(manager, title, headerHeight * 0.75f);
            headerPanel.AddAuto(titleText);
            headerPanel.FitSize();
            AddChildAsTail(headerPanel);

            contentPanel = new Panel(0f, 0f, false, false);
            AddChildAsTail(contentPanel);

            BackgroundEnabled = true;
            BorderEnabled = true;

            // とりあえず空でレイアウト
            Layout();
        }

        // Windowを継承した場合はこっちを上書きすること。Updateの上書きは禁止する。
        public virtual void UpdateWindow()
        {
        }

        public sealed override void Update(float deltaTime)
        {
            if (headerPanel.IsDragging)
            {
                float dx = PointerX - prevPointerX;
                float dy = PointerY - prevPointerY;
                prevPointerX = PointerX;
                prevPointerY = PointerY;
                SetLocalPosition(LocalLeftX + dx, LocalTopY + dy);
            }
            UpdateWindow();
        }

        new public void Add(
            Control child,
            float offsetX = 0f,
            float offsetY = 0f,
            AlignX alignX = AlignX.Left,
            AlignY alignY = AlignY.Top)
        {
            contentPanel.Add(child, offsetX, offsetY, alignX, alignY);
        }

        public override void RemoveChild(Control child)
        {
            contentPanel.RemoveChild(child);
        }

        public void AddAuto(Control child)
        {
            // 一旦無限に広げて配置後、再配置
            contentPanel.SetSize(float.MaxValue, float.MaxValue);
            contentPanel.AddAuto(child);
            contentPanel.FitSize();
            Layout();
        }

        public void BreakLine()
        {
            contentPanel.BreakLine();
        }

        public void AddToNextX(float dx)
        {
            contentPanel.AddToNextX(dx);
        }

        public void AddToNextY(float dy)
        {
            contentPanel.AddToNextY(dy);
        }

        public void ToggleMinimize()
        {
            contentPanel.ToggleEnabled();
            Layout();
        }

        public void SetAutoPosition(float x, float y)
        {
            contentPanel.SetAutoPosition(x, y);
        }

        void Layout()
        {
            // 幅を大きい方に合わせる
            float contentWidth = 0f;
            if (contentPanel.Enabled)
            {
                contentWidth = contentPanel.Width;
            }
            float headerWidth = headerPanel.Width;
            float maxWidth = Mathf.Max(contentWidth, headerWidth);
            if (contentPanel.Enabled)
            {
                contentPanel.SetSize(maxWidth, contentPanel.Height);
            }
            headerPanel.SetSize(maxWidth, headerPanel.Height);

            // レイアウト開始
            float x = 2f * BorderWidth;
            float y = 2f * BorderWidth;
            headerPanel.SetLocalPosition(x, y);
            y += headerPanel.Height;
            y += BorderWidth;
            contentPanel.SetLocalPosition(x, y);
            FitSize();
        }
    }
}
