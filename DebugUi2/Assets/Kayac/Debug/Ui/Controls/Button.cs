using System;
using UnityEngine;

namespace Kayac.Debug.Ui
{
    public class Button : Control
    {
        public Color32 TextColor { get; set; }
        public Color32 PointerDownTextColor { get; set; }
        public Color32 Color { get; set; }
        public Color32 PointerDownColor { get; set; }
        public string Text { get; set; }
        public Action OnClick { private get; set; }
        public Texture Texture { get; set; }
        public Sprite Sprite { get; set; }

        public bool Clickable
        {
            get
            {
                return EventEnabled;
            }
            set
            {
                EventEnabled = value;
            }
        }

        public Button(
            string text,
            float width = 80f,
            float height = 50f) : base(string.IsNullOrEmpty(text) ? "Button" : text)
        {
            SetSize(width, height);
            Text = text;
            // イベント取ります
            EventEnabled = true;
            BackgroundEnabled = false;
            BorderEnabled = true;

            Color = new Color32(0, 0, 0, 192);
            PointerDownColor = new Color32(192, 192, 96, 192);
            TextColor = new Color32(255, 255, 255, 255);
            PointerDownTextColor = new Color32(0, 0, 0, 255);
        }

        public override void Update(float deltaTime)
        {
            if (HasJustClicked)
            {
                if (OnClick != null)
                {
                    OnClick();
                }
            }
        }

        public override void Draw(
            float offsetX,
            float offsetY,
            Renderer2D renderer)
        {
            Color32 tmpColor = (IsPointerDown) ? PointerDownColor : Color;
            renderer.Color = tmpColor;
            if (Texture != null)
            {
                renderer.AddTexturedRectangle(
                    offsetX + LocalLeftX + BorderWidth,
                    offsetY + LocalTopY + BorderWidth,
                    Width - (BorderWidth * 2f),
                    Height - (BorderWidth * 2f),
                    Texture);
            }
            else if (Sprite != null)
            {
                renderer.AddSprite(
                    offsetX + LocalLeftX + BorderWidth,
                    offsetY + LocalTopY + BorderWidth,
                    Width - (BorderWidth * 2f),
                    Height - (BorderWidth * 2f),
                    Sprite);
            }
            else
            {
                renderer.AddRectangle(
                    offsetX + LocalLeftX + BorderWidth,
                    offsetY + LocalTopY + BorderWidth,
                    Width - (BorderWidth * 2f),
                    Height - (BorderWidth * 2f));
            }

            Color32 tmpTextColor = (IsPointerDown) ? PointerDownTextColor : TextColor;
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
    }
}