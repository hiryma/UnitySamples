using System;
using UnityEngine;

// TODO: ボタン風とチェックボックスっぽいのとどっちがいいんだ?区別つかないのは良くないよな
namespace Kayac.DebugUi
{
    public class Toggle : Control
    {
        public Color32 OffColor { get; set; }
        public Color32 OnColor { get; set; }
        public Color32 OffTextColor { get; set; }
        public Color32 OnTextColor { get; set; }
        private Texture texture;
        private Sprite sprite;

        public Texture Texture
        {
            set
            {
                sprite = null;
                texture = value;
            }
        }
        public Sprite Sprite
        {
            set
            {
                texture = null;
                sprite = value;
            }
        }
        public bool On { get; private set; }
        public Action OnChangeToOn { private get; set; }
        public Action<bool> OnChange { private get; set; }
        public string Text { get; set; }
        private readonly bool on;
        private readonly ToggleGroup group;

        public Toggle(
            string text,
            float width = 80f,
            float height = 50f,
            ToggleGroup group = null)
        {
            SetSize(width, height);
            this.group = group;
            // グループが与えられて、中身がなければ自分をonにする
            if (this.group != null)
            {
                if (this.group.Selected == null)
                {
                    this.group.SetOnToggle(this);
                    On = true;
                }
            }
            Text = text;
            // イベント取ります
            EventEnabled = true;
            BorderEnabled = true;

            OffColor = new Color32(0, 0, 0, 192);
            OnColor = new Color32(192, 192, 96, 192);
            OffTextColor = new Color32(255, 255, 255, 255);
            OnTextColor = new Color32(0, 0, 0, 255);
        }

        public override void Update(float deltaTime)
        {
            if (HasJustClicked || HasJustDragStarted)
            {
                Change();
            }
        }

        public void Change()
        {
            bool oldOn = On;
            // グループがある場合、自分をoffにはできない。
            if (group != null)
            {
                // 自分がoffであれonであれ、押されたことをグループに通知する
                group.SetOnToggle(this);
                On = true;
            }
            // グループがなければ自由にon/offできる
            else
            {
                On = !On;
            }
            if (On != oldOn)
            {
                if (On && (OnChangeToOn != null))
                {
                    OnChangeToOn();
                }
                if (OnChange != null)
                {
                    OnChange(On);
                }
            }
        }

        public void SetOn()
        {
            if (!On)
            {
                Change();
            }
        }

        public override void Draw(
            float offsetX,
            float offsetY,
            Renderer2D renderer)
        {
            Color32 tmpColor = (On) ? OnColor : OffColor;
            renderer.Color = tmpColor;
            if (sprite != null)
            {
                renderer.AddSprite(
                    offsetX + LocalLeftX + BorderWidth,
                    offsetY + LocalTopY + BorderWidth,
                    Width - (BorderWidth * 2f),
                    Height - (BorderWidth * 2f),
                    sprite);
            }
            else if (texture != null)
            {
                renderer.AddTexturedRectangle(
                    offsetX + LocalLeftX + BorderWidth,
                    offsetY + LocalTopY + BorderWidth,
                    Width - (BorderWidth * 2f),
                    Height - (BorderWidth * 2f),
                    texture);
            }
            else
            {
                renderer.AddRectangle(
                    offsetX + LocalLeftX + BorderWidth,
                    offsetY + LocalTopY + BorderWidth,
                    Width - (BorderWidth * 2f),
                    Height - (BorderWidth * 2f));
            }

            Color32 tmpTextColor = (On) ? OnTextColor : OffTextColor;
            renderer.Color = tmpTextColor;
            renderer.AddText(
                Text,
                offsetX + LocalLeftX + (Width * 0.5f),
                offsetY + LocalTopY + (Height * 0.5f),
                Width - (BorderWidth * 4f),
                Height - (BorderWidth * 4f),
                AlignX.Center,
                AlignY.Center);
        }

        // publicだがGroupから以外呼ぶな
        public void SetOffFromGroup()
        {
            if (On && (OnChange != null))
            {
                OnChange(false);
            }
            On = false;
        }
    }
}
