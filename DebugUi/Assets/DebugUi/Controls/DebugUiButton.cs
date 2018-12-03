using UnityEngine;
using System;

namespace Kayac
{
	public class DebugUiButton : DebugUiControl
	{
		public Color32 textColor { get; set; }
		public Color32 pointerDownTextColor { get; set; }
		public Color32 color { get; set; }
		public Color32 pointerDownColor { get; set; }
		public string text { get; set; }
		public Action onClick { private get; set; }
		public Texture texture { get; set; }
		public Sprite sprite { get; set; }

		public bool clickable
		{
			get
			{
				return eventEnabled;
			}
			set
			{
				eventEnabled = value;
			}
		}

		private bool _rotateToVertical;

		public DebugUiButton(
			string text,
			float width = 80f,
			float height = 50f,
			bool rotateToVertical = false) : base(string.IsNullOrEmpty(text) ? "Button" : text)
		{
			SetSize(width, height);
			this.text = text;
			_rotateToVertical = rotateToVertical;
			// イベント取ります
			eventEnabled = true;
			backgroundEnabled = false;
			borderEnabled = true;

			color = new Color32(0, 0, 0, 192);
			pointerDownColor = new Color32(192, 192, 96, 192);
			textColor = new Color32(255, 255, 255, 255);
			pointerDownTextColor = new Color32(0, 0, 0, 255);
		}

		public override void Update()
		{
			if (hasJustClicked)
			{
				if (onClick != null)
				{
					onClick();
				}
			}
		}

		public override void Draw(
			float offsetX,
			float offsetY,
			DebugPrimitiveRenderer2D renderer)
		{
			Color32 tmpColor = (isPointerDown) ? pointerDownColor : color;
			renderer.color = tmpColor;
			if (texture != null)
			{
				renderer.AddTexturedRectangle(
					offsetX + localLeftX + borderWidth,
					offsetY + localTopY + borderWidth,
					width - (borderWidth * 2f),
					height - (borderWidth * 2f),
					texture);
			}
			else if (sprite != null)
			{
				renderer.AddSprite(
					offsetX + localLeftX + borderWidth,
					offsetY + localTopY + borderWidth,
					width - (borderWidth * 2f),
					height - (borderWidth * 2f),
					sprite);
			}
			else
			{
				renderer.AddRectangle(
					offsetX + localLeftX + borderWidth,
					offsetY + localTopY + borderWidth,
					width - (borderWidth * 2f),
					height - (borderWidth * 2f));
			}

			Color32 tmpTextColor = (isPointerDown) ? pointerDownTextColor : textColor;
			DrawTextAuto(
				renderer,
				text,
				tmpTextColor,
				offsetX + localLeftX + (borderWidth * 2f),
				offsetY + localTopY + (borderWidth * 2f),
				width - (borderWidth * 4f),
				height - (borderWidth * 4f),
				_rotateToVertical);
		}
	}
}