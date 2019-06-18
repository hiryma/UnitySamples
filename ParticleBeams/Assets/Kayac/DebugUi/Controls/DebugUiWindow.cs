using UnityEngine;

namespace Kayac
{
	public class DebugUiWindow : DebugUiContainer
	{
		public Color32 headerColor
		{
			set
			{
				_headerPanel.backgroundColor = value;
			}
		}
		DebugUiPanel _headerPanel;
		DebugUiPanel _contentPanel;
		DebugUiManager _manager;
		float _prevPointerX;
		float _prevPointerY;
		const float DefaultHeaderSize = 50f;

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
			_manager = manager;
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
				_manager.MoveToTop(this);
			};

			var closeButton = new DebugUiButton(
					"Ｘ",
					DefaultHeaderSize,
					DefaultHeaderSize);
			closeButton.onClick = () =>
			{
				enabled = false;
			};
			_headerPanel.AddAuto(closeButton);

			var minimizeButton = new DebugUiButton(
					"＿",
					DefaultHeaderSize,
					DefaultHeaderSize);
			minimizeButton.onClick = () =>
			{
				ToggleMinimize();
			};
			_headerPanel.AddAuto(minimizeButton);
			var titleText = new DebugUiText(manager, title, headerHeight * 0.75f);
			_headerPanel.AddAuto(titleText);
			_headerPanel.FitSize();
			AddChildAsTail(_headerPanel);

			_contentPanel = new DebugUiPanel(0f, 0f, false, false);
			AddChildAsTail(_contentPanel);

			backgroundEnabled = true;
			borderEnabled = true;

			// とりあえず空でレイアウト
			Layout();
		}

		// Windowを継承した場合はこっちを上書きすること。Updateの上書きは禁止する。
		public virtual void UpdateWindow()
		{
		}

		public sealed override void Update(float deltaTime)
		{
			if (_headerPanel.isDragging)
			{
				float dx = pointerX - _prevPointerX;
				float dy = pointerY - _prevPointerY;
				_prevPointerX = pointerX;
				_prevPointerY = pointerY;
				SetLocalPosition(localLeftX + dx, localTopY + dy);
			}
			UpdateWindow();
		}

		public void Add(
			DebugUiControl child,
			float offsetX = 0f,
			float offsetY = 0f,
			AlignX alignX = AlignX.Left,
			AlignY alignY = AlignY.Top)
		{
			_contentPanel.Add(child, offsetX, offsetY, alignX, alignY);
		}

		public override void RemoveChild(DebugUiControl child)
		{
			_contentPanel.RemoveChild(child);
		}

		public void AddAuto(DebugUiControl child)
		{
			// 一旦無限に広げて配置後、再配置
			_contentPanel.SetSize(float.MaxValue, float.MaxValue);
			_contentPanel.AddAuto(child);
			_contentPanel.FitSize();
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

		void Layout()
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
			FitSize();
		}
	}
}
