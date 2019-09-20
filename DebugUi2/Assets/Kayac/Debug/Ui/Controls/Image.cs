using UnityEngine;

namespace Kayac.Debug.Ui
{
    public class Image : Control
    {
        Texture texture;
        public Texture Texture
        {
            set
            {
                sprite = null;
                texture = value;
            }
        }
        Sprite sprite;
        public Sprite Sprite
        {
            set
            {
                texture = null;
                sprite = value;
            }
        }

        // 大きさだけ指定。textureかspriteは後で差す
        public Image(
            float width,
            float height)
        {
            SetSize(width, height);
        }

        // width,heightが0ならspriteのwidth,heightを使う
        public Image(
            Sprite sprite = null,
            float width = 0f,
            float height = 0f)
        {
            if ((width <= 0f) || (height <= 0f))
            {
                if (sprite != null)
                {
                    width = sprite.rect.width;
                    height = sprite.rect.height;
                }
            }
            SetSize(width, height);
            this.sprite = sprite;
        }

        // width,heightが0ならtetureのwidth,heightを使う
        public Image(
            Texture texture = null,
            float width = 0f,
            float height = 0f)
        {
            if ((width <= 0f) || (height <= 0f))
            {
                if (texture != null)
                {
                    width = texture.width;
                    height = texture.height;
                }
            }
            SetSize(width, height);
            this.texture = texture;
        }

        public override void Draw(
            float offsetX,
            float offsetY,
            Renderer2D renderer)
        {
            renderer.Color = new Color32(255, 255, 255, 255);
            if (sprite != null)
            {
                renderer.AddSprite(
                    offsetX + LocalLeftX,
                    offsetY + LocalTopY,
                    Width,
                    Height,
                    sprite);
            }
            else if (texture != null)
            {
                renderer.AddTexturedRectangle(
                    offsetX + LocalLeftX,
                    offsetY + LocalTopY,
                    Width,
                    Height,
                    texture);
            }
            else
            {
                renderer.AddRectangle(
                    offsetX + LocalLeftX,
                    offsetY + LocalTopY,
                    Width,
                    Height);
            }
        }
    }
}