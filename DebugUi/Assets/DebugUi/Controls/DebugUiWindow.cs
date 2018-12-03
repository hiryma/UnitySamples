using UnityEngine;

namespace Kayac
{
	public class DebugUiWindow : DebugUiControl
	{
		public Color32 headerColor
		{
			set
			{
				_headerPanel.backgroundColor = value;
			}
		}
		private DebugUiPanel _headerPanel;
		private DebugUiPanel _contentPanel;
		private float _prevPointerX;
		private float _prevPointerY;
		private const float DefaultHeaderSize = 50f;

		public float lineSpace
		{
			set
			{
				_contentPanel.lineSpace = value;
			}
		}

		// TODO: とある事情によりmanagerが必要
		public DebugUiWindow(
			DebugUiManager manager,
			string title,
			float headerHeight = DefaultHeaderSize) : base(string.IsNullOrEmpty(title) ? "Window" : title)
		{
			_headerPanel = new DebugUiPanel(
				float.MaxValue,
				float.MaxValue,
				true,
				true,
				true);
			_headerPanel.backgroundColor = new Color32(0, 0, 128, 128);
			_headerPanel.draggable = true;
			_headerPanel.onDragStart = () =>
			{
				_prevPointerX = pointerX;
				_prevPointerY = pointerY;
			};
			// このヘッダをタップしたら、windowを手前に持ってくる
			_headerPanel.onEventConsume = () =>
			{
				SetAsLastSibling();
			};

			var closeButton = new DebugUiButton(
				"Ｘ",
				DefaultHeaderSize,
				DefaultHeaderSize);
			closeButton.onClick = () =>
			{
				enabled = false;
			};
			_headerPanel.AddChildAuto(closeButton);

			var minimizeButton = new DebugUiButton(
				"＿",
				DefaultHeaderSize,
				DefaultHeaderSize);
			minimizeButton.onClick = () =>
			{
				ToggleMinimize();
			};
			_headerPanel.AddChildAuto(minimizeButton);
			var titleText = new DebugUiText(manager, title, DefaultHeaderSize * 0.75f);
			_headerPanel.AddChildAuto(titleText);
			_headerPanel.AdjustSize();
			base.AddChild(_headerPanel);

			_contentPanel = new DebugUiPanel(0f, 0f, false, false);
			base.AddChild(_contentPanel);

			backgroundEnabled = true;
			borderEnabled = true;

			// とりあえず空でレイアウト
			Layout();
		}

		// Windowを継承した場合はこっちを上書きすること。Updateの上書きは禁止する。
		public virtual void UpdateWindow()
		{
		}

		public sealed override void Update()
		{
			if (_headerPanel.isDragging)
			{
				float dx = pointerX - _prevPointerX;
				float dy = pointerY - _prevPointerY;
				_prevPointerX = pointerX;
				_prevPointerY = pointerY;
                SetLocalPosition(localLeftX + dx, Mathf.Max(0, localTopY + dy));
			}
			UpdateWindow();
		}

		public override void AddChild(
			DebugUiControl child,
			float offsetX = 0f,
			float offsetY = 0f)
		{
			_contentPanel.AddChild(child, offsetX, offsetY);
		}

		public override void RemoveChild(DebugUiControl child)
		{
			_contentPanel.RemoveChild(child);
		}

		public void AddChildAuto(DebugUiControl child)
		{
			// 一旦無限に広げて配置後、再配置
			_contentPanel.SetSize(float.MaxValue, float.MaxValue);
			_contentPanel.AddChildAuto(child);
			_contentPanel.AdjustSize();
			Layout();
		}

		public void BreakLine()
		{
			_contentPanel.BreakLine();
		}

		public void AddToNextX(float dx)
		{
			_contentPanel.AddToNextX(dx);
		}

		public void AddToNextY(float dy)
		{
			_contentPanel.AddToNextY(dy);
		}

		public void ToggleMinimize()
		{
			_contentPanel.ToggleEnabled();
			Layout();
		}

		public void SetAutoPosition(float x, float y)
		{
			_contentPanel.SetAutoPosition(x, y);
		}

		private void Layout()
		{
			// 幅を大きい方に合わせる
			float contentWidth = 0f;
			if (_contentPanel.enabled)
			{
				contentWidth = _contentPanel.width;
			}
			float headerWidth = _headerPanel.width;
			float maxWidth = Mathf.Max(contentWidth, headerWidth);
			if (_contentPanel.enabled)
			{
				_contentPanel.SetSize(maxWidth, _contentPanel.height);
			}
			_headerPanel.SetSize(maxWidth, _headerPanel.height);

			// レイアウト開始
			float x = 2f * borderWidth;
			float y = 2f * borderWidth;
			_headerPanel.SetLocalPosition(x, y);
			y += _headerPanel.height;
			y += borderWidth;
			_contentPanel.SetLocalPosition(x, y);
			AdjustSize();
		}
	}
}
