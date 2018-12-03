using UnityEngine;
using System;

// TODO: ボタン風とチェックボックスっぽいのとどっちがいいんだ?区別つかないのは良くないよな
namespace Kayac
{
	public class DebugUiToggle : DebugUiControl
	{
		public Color32 offColor { get; set; }
		public Color32 onColor { get; set; }
		public Color32 offTextColor { get; set; }
		public Color32 onTextColor { get; set; }
		private Texture _texture;
		private Sprite _sprite;

		public Texture texture
		{
			set
			{
				_sprite = null;
				_texture = value;
			}
		}
		public Sprite sprite
		{
			set
			{
				_texture = null;
				_sprite = value;
			}
		}
		public bool on { get; private set; }
		public Action onChangeToOn { private get; set; }
		public Action<bool> onChange { private get; set; }
		public string text { get; set; }
		private bool _rotateToVertical;
		private bool _on;
		private DebugUiToggleGroup _group;

		public DebugUiToggle(
			string text,
			float width = 80f,
			float height = 50f,
			DebugUiToggleGroup group = null,
			bool rotateToVertical = false)
		{
			SetSize(width, height);
			_group = group;
			// グループが与えられて、中身がなければ自分をonにする
			if (_group != null)
			{
				if (_group.selected == null)
				{
					_group.SetOnToggle(this);
					on = true;
				}
			}
			this.text = text;
			_rotateToVertical = rotateToVertical;
			// イベント取ります
			eventEnabled = true;
			borderEnabled = true;

			offColor = new Color32(0, 0, 0, 192);
			onColor = new Color32(192, 192, 96, 192);
			offTextColor = new Color32(255, 255, 255, 255);
			onTextColor = new Color32(0, 0, 0, 255);
		}

		public override void Update()
		{
			if (hasJustClicked || hasJustDragStarted)
			{
				Toggle();
			}
		}

		public void Toggle()
		{
			bool oldOn = on;
			// グループがある場合、自分をoffにはできない。
			if (_group != null)
			{
				// 自分がoffであれonであれ、押されたことをグループに通知する
				_group.SetOnToggle(this);
				on = true;
			}
			// グループがなければ自由にon/offできる
			else
			{
				on = !on;
			}
			if (on != oldOn)
			{
				if (on && (onChangeToOn != null))
				{
					onChangeToOn();
				}
				if (onChange != null)
				{
					onChange(on);
				}
			}
		}

		public void SetOn()
		{
			if (!on)
			{
				Toggle();
			}
		}

		public override void Draw(
			float offsetX,
			float offsetY,
			DebugPrimitiveRenderer2D renderer)
		{
			Color32 tmpColor = (on) ? onColor : offColor;
			renderer.color = tmpColor;
			if (_sprite != null)
			{
				renderer.AddSprite(
					offsetX + localLeftX + borderWidth,
					offsetY + localTopY + borderWidth,
					width - (borderWidth * 2f),
					height - (borderWidth * 2f),
					_sprite);
			}
			else if (_texture != null)
			{
				renderer.AddTexturedRectangle(
					offsetX + localLeftX + borderWidth,
					offsetY + localTopY + borderWidth,
					width - (borderWidth * 2f),
					height - (borderWidth * 2f),
					_texture);
			}
			else
			{
				renderer.AddRectangle(
					offsetX + localLeftX + borderWidth,
					offsetY + localTopY + borderWidth,
					width - (borderWidth * 2f),
					height - (borderWidth * 2f));
			}

			Color32 tmpTextColor = (on) ? onTextColor : offTextColor;
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

		// publicだがGroupから以外呼ぶな
		public void SetOffFromGroup()
		{
			if (on && (onChange != null))
			{
				onChange(false);
			}
			on = false;
		}
	}
}
