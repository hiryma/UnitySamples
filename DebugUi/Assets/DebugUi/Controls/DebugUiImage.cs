using UnityEngine;

namespace Kayac
{
	public class DebugUiImage : DebugUiControl
	{
		private Texture _texture;
		public Texture texture
		{
			set
			{
				_sprite = null;
				_texture = value;
			}
		}
		private Sprite _sprite;
		public Sprite sprite
		{
			set
			{
				_texture = null;
				_sprite = value;
			}
		}

		// 大きさだけ指定。textureかspriteは後で差す
		public DebugUiImage(
			float width,
			float height)
		{
			SetSize(width, height);
		}

		// width,heightが0ならspriteのwidth,heightを使う
		public DebugUiImage(
			Sprite sprite = null,
			float width = 0,
			float height = 0)
		{
			if ((width == 0) || (height == 0))
			{
				if (sprite != null)
				{
					width = sprite.rect.width;
					height = sprite.rect.height;
				}
			}
			SetSize(width, height);
			_sprite = sprite;
		}

		// width,heightが0ならtetureのwidth,heightを使う
		public DebugUiImage(
			Texture texture = null,
			float width = 0,
			float height = 0)
		{
			if ((width == 0) || (height == 0))
			{
				if (texture != null)
				{
					width = texture.width;
					height = texture.height;
				}
			}
			SetSize(width, height);
			_texture = texture;
		}

		public override void Draw(
			float offsetX,
			float offsetY,
			DebugPrimitiveRenderer2D renderer)
		{
			renderer.color = new Color32(255, 255, 255, 255);
			if (_sprite != null)
			{
				renderer.AddSprite(
					offsetX + localLeftX,
					offsetY + localTopY,
					width,
					height,
					_sprite);
			}
			else if (_texture != null)
			{
				renderer.AddTexturedRectangle(
					offsetX + localLeftX,
					offsetY + localTopY,
					width,
					height,
					_texture);
			}
			else
			{
				renderer.AddRectangle(
					offsetX + localLeftX,
					offsetY + localTopY,
					width,
					height);
			}
		}
	}
}