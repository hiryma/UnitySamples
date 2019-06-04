using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kayac
{
	public class DebugUiControl : DebugUi, IDisposable
	{
		// デバグ用にしか使っていない
		public string name { get; private set; }
		public Action onDragEnd { get; set; }
		public Action onDragStart { get; set; }
		// 押された時など、そのフレームでイベントを消費したコントロールになった時。呼ばれるのはフレームの末尾。TODO: 名前とかどうにかしたい
		public Action onEventConsume { get; set; }
		// 有効無効化
		public bool enabled
		{
			get
			{
				return _enabled;
			}
			set
			{
				var oldValue = _enabled;
				_enabled = value;
				if (!oldValue && value) //有効化イベントを下流に流す
				{
					OnEnableRecursive();
				}
			}
		}
		public bool draggable { get; set; }

		// イベント状態取得
		public bool hasJustClicked { get; private set; }
		public bool hasJustDragStarted { get; private set; }
		public bool hasFocus { get; private set; }
		public bool isPointerDown { get; private set; }
		/// 単にこの矩形の上にポインタがあるかどうかを返す。イベント無効でも動作。
		public bool isPointerOver { get; private set; }
		public bool isDragging { get; set; }

		// スタイル
		public Color32 backgroundColor { get; set; }
		public Color32 borderColor { get; set; }
		public float borderWidth { get; set; }
		public Color32 dragMarkColor { get; set; }
		public float dragMarkSize { get; set; }
		public char dragMarkLetter { get; set; }
		public Color32 dragMarkLetterColor { get; set; }

		// 派生から設定する特徴変数
		protected bool eventEnabled { get; set; }
		protected bool borderEnabled { get; set; }
		protected bool backgroundEnabled { get; set; }
		// 位置とサイズ情報
		public float localLeftX { get; private set; }
		public float localTopY { get; private set; }
		public float leftX { get; private set; }
		public float topY { get; private set; }
		public float width { get; private set; }
		public float height { get; private set; }
		// Update内で使うためのローカルポインタ座標(コントロール内ではない)
		protected float localPointerX { get; private set; }
		protected float localPointerY { get; private set; }
		protected float pointerX
		{
			get
			{
				return localPointerX - localLeftX + leftX;
			}
		}
		protected float pointerY
		{
			get
			{
				return localPointerY - localTopY + topY;
			}
		}
		// Clickが呼ばれたか
		protected bool clickedFromCode { get; private set; }

		// 木を成すための情報
		DebugUiControl _previousBrother;
		DebugUiControl _nextBrother;
		DebugUiControl _firstChild;
		DebugUiControl _lastChild;
		DebugUiControl _parent;
		bool _enabled;

		public DebugUiControl(string name = "")
		{
			this.name = name;
			_enabled = true;
			eventEnabled = false;
			backgroundEnabled = false;
			borderEnabled = false;
			hasFocus = false;

			// スタイルデフォルト
			borderWidth = 2f;
			backgroundColor = new Color32(0, 0, 0, 128);
			borderColor = new Color32(255, 255, 255, 128);
			dragMarkColor = new Color32(255, 0, 0, 192);
			dragMarkSize = 100f;
			dragMarkLetter = '↓';
			dragMarkLetterColor = new Color32(255, 255, 255, 255);
		}

		void Destroy()
		{
			RemoveAllChild();
			_parent = _nextBrother = _previousBrother = null;
			_enabled = false;
			// 以下まともな値が取れないようにしてバグを発覚しやすく
			localLeftX = float.NaN;
			localTopY = float.NaN;
			width = float.NaN;
			height = float.NaN;
		}

		public void SetAsLastSibling()
		{
			if (_parent != null)
			{
				_parent.SetAsLastChild(this);
			}
		}

		void SetAsLastChild(DebugUiControl child)
		{
			UnlinkChild(child);
			LinkChildToTail(child);
		}

		public void SetAsFirstSibling()
		{
			if (_parent != null)
			{
				_parent.SetAsFirstChild(this);
			}
		}

		void SetAsFirstChild(DebugUiControl child)
		{
			UnlinkChild(child);
			LinkChildToHead(child);
		}

		// 必要なら上層で実装
		public virtual void Dispose()
		{
		}

		// 必要なら上層で実装
		protected virtual void OnEnable()
		{
		}

		public void Click()
		{
			this.clickedFromCode = true;
		}

		public void OnEnableRecursive()
		{
			Debug.Assert(_enabled);
			OnEnable(); // 自分を呼ぶ

			var child = _firstChild;
			while (child != null)
			{
				if (child.enabled)
				{
					child.OnEnableRecursive();
				}
				child = child._nextBrother;
			}
		}

		public void SetLocalPosition(
			float localLeftX,
			float localTopY)
		{
			this.localLeftX = localLeftX;
			this.localTopY = localTopY;
		}

		public void SetSize(
			float width,
			float height)
		{
			this.width = width;
			this.height = height;
		}

		/// enabledをtrue-falseで切り換える。古い値が返る。
		public bool ToggleEnabled()
		{
			bool ret = enabled;
			enabled = !enabled;
			return ret;
		}

		/// 子を全て含むギリギリのサイズに調整する。遅いので注意。width,heightしかいじらない。
		public virtual void AdjustSize()
		{
			float maxX = 0f;
			float maxY = 0f;
			var child = _firstChild;
			while (child != null)
			{
				if (child.enabled)
				{
					float right = child.localLeftX + child.width;
					float bottom = child.localTopY + child.height;
					maxX = Mathf.Max(maxX, right);
					maxY = Mathf.Max(maxY, bottom);
				}
				child = child._nextBrother;
			}

			if (borderEnabled)
			{
				maxX += borderWidth * 2f;
				maxY += borderWidth * 2f;
			}
			SetSize(maxX, maxY);
		}

		// offsetはLocalPositionに足されるので注意
		public virtual void AddChild(
			DebugUiControl child,
			float offsetX = 0f,
			float offsetY = 0f,
			AlignX alignX = AlignX.Left,
			AlignY alignY = AlignY.Top)
		{
			float x = 0f;
			switch (alignX)
			{
				case AlignX.Center: x = (this.width - child.width) * 0.5f; break;
				case AlignX.Right: x = this.width - child.width; break;
			}
			float y = 0f;
			switch (alignY)
			{
				case AlignY.Center: y = (this.height - child.height) * 0.5f; break;
				case AlignY.Bottom: y = (this.height - child.height); break;
			}
			child.SetLocalPosition(
				child.localLeftX + offsetX + x,
				child.localTopY + offsetY + y);
			LinkChildToTail(child);
			child._parent = this;
		}

		public virtual void RemoveChild(DebugUiControl child)
		{
			// リストから切断
			UnlinkChild(child);
			// まず内部を破棄
			child.Dispose();
			child.Destroy();
		}

		public void RemoveAllChild()
		{
			var child = _firstChild;
			while (child != null)
			{
				var next = child._nextBrother;
				child.Dispose();
				child.Destroy();

				child = next;
			}
			_firstChild = _lastChild = null;
		}

		public void UpdateEventRecursive(
			float offsetX,
			float offsetY,
			DebugUiManager.Input input,
			bool parentEnabled)
		{
			// イベント状態リセット
			hasJustClicked = false;
			hasJustDragStarted = false;
			isPointerDown = false;
			isPointerOver = false;
			bool prevDragging = isDragging;
			isDragging = false;
			// グローバル座標を計算して子優先。enabled=falseでもリセットのために回す必要あり
			leftX = offsetX + localLeftX;
			topY = offsetY + localTopY;
			// 逆順で更新。後のものほど手前に描画されるので、手前のものを優先してイベントを処理する。
			var child = _lastChild;
			while (child != null)
			{
				child.UpdateEventRecursive(
					leftX,
					topY,
					input,
					parentEnabled && enabled);
				child = child._previousBrother;
			}
			// 無効なら以降処理しない。ここまでは初期化が絡むので毎回やらねばならない。
			if ((parentEnabled && enabled) == false)
			{
				return;
			}

			// 自分の当たり判定。isPointerOver自体はイベント無効でも更新。
			localPointerX = input.pointerX - offsetX;
			localPointerY = input.pointerY - offsetY;
			float clientX = localPointerX - localLeftX;
			float clientY = localPointerY - localTopY;
			if ((clientX >= 0f)
				&& (clientX < width)
				&& (clientY >= 0f)
				&& (clientY < height))
			{
				isPointerOver = true;
			}

			// 有効で、イベントも取り、下流で当たっていなければ、イベント処理
			if (eventEnabled)
			{
				// ポインタがどこにあろうが、前のフレームがドラッグ中でPointerDownならドラッグを維持
				if (draggable && prevDragging && input.isPointerDown)
				{
					isDragging = true;
				}

				// まだ他のコントロールにイベントを処理されていなくて、ポインタが上にあれば、
				if ((input.eventConsumer == null) && isPointerOver)
				{
					if (input.hasJustClicked)
					{
						input.eventConsumer = this;
						hasJustClicked = true;
					}

					if (draggable)
					{
						// ドラッグ開始
						if (input.hasJustDragStarted)
						{
							isDragging = true;
							hasJustDragStarted = true;
							input.eventConsumer = this;
							input.draggedControl = this;
							if (onDragStart != null)
							{
								onDragStart();
							}
						}
					}

					if (input.isPointerDown)
					{
						input.eventConsumer = this;
						isPointerDown = true;
					}
				}

				if (input.hasJustClicked)
				{
					if ((input.eventConsumer == this) && isPointerOver)
					{
						hasFocus = true;
					}
					else
					{
						hasFocus = false;
					}
				}
			}
		}

		public bool RaycastRecursive(
			float offsetX,
			float offsetY,
			float pointerX,
			float pointerY)
		{
			// 無効ならfalse
			if (!_enabled)
			{
				return false;
			}
			// グローバル座標を計算して子に回す
			float globalLeftX = offsetX + localLeftX;
			float globalTopY = offsetY + localTopY;

			var child = _firstChild;
			while (child != null)
			{
				bool result = child.RaycastRecursive(
					globalLeftX,
					globalTopY,
					pointerX,
					pointerY);
				if (result)
				{
					return true;
				}
				child = child._nextBrother;
			}

			// イベント取る場合のみ判定
			if (eventEnabled)
			{
				// 自分の当たり判定
				float globalRightX = globalLeftX + width;
				float globalBottomY = globalTopY + height;
				if (
				(pointerX >= globalLeftX)
				&& (pointerX < globalRightX)
				&& (pointerY >= globalTopY)
				&& (pointerY < globalBottomY))
				{
					return true;
				}
			}
			return false;
		}

		public virtual void Update(float deltaTime)
		{
		}

		public void UpdateRecursive(float deltaTime)
		{
			if (!_enabled)
			{
				return;
			}
			// 手動クリックされていれば、クリックされたことにする
			if (this.clickedFromCode)
			{
				this.hasJustClicked = true;
				this.clickedFromCode = false;
			}
			Update(deltaTime);

			var child = _firstChild;
			while (child != null)
			{
				child.UpdateRecursive(deltaTime);
				child = child._nextBrother;
			}
		}

		public virtual void Draw(
			float offsetX,
			float offsetY,
			DebugPrimitiveRenderer2D renderer)
		{
		}

		public virtual void DrawPostChild(
		   float offsetX,
		   float offsetY,
		   DebugPrimitiveRenderer2D renderer)
		{
		}

		public void DrawRecursive(
			float offsetX,
			float offsetY,
			DebugPrimitiveRenderer2D renderer)
		{
			if (!_enabled)
			{
				return;
			}

			// まず自分描画
			if (backgroundEnabled)
			{
				renderer.color = backgroundColor;
				renderer.AddRectangle(
					offsetX + localLeftX,
					offsetY + localTopY,
					width,
					height);
			}
			if (borderEnabled)
			{
				renderer.color = borderColor;
				renderer.AddRectangleFrame(
					offsetX + localLeftX,
					offsetY + localTopY,
					width,
					height,
					borderWidth);
			}
			Draw(offsetX, offsetY, renderer);

			// グローバル座標を計算して子を描画
			float globalLeftX = offsetX + localLeftX;
			float globalTopY = offsetY + localTopY;

			var child = _firstChild;
			while (child != null)
			{
				child.DrawRecursive(globalLeftX, globalTopY, renderer);
				child = child._nextBrother;
			}
			DrawPostChild(offsetX, offsetY, renderer);
		}

		// リストのつなぎ換えだけ
		void LinkChildToHead(DebugUiControl child)
		{
			child._previousBrother = null;
			// 先頭がnullの場合、末尾もnull。
			if (_firstChild == null)
			{
				Debug.Assert(_lastChild == null);
				_firstChild = _lastChild = child;
				child._nextBrother = null;
			}
			// 先頭が非nullの場合、リンクを生成
			else
			{
				_firstChild._previousBrother = child;
				child._nextBrother = _firstChild;
				_firstChild = child;
			}
		}

		// リストのつなぎ換えだけ
		void LinkChildToTail(DebugUiControl child)
		{
			child._nextBrother = null;
			// 末尾がnullの場合、先頭もnull。
			if (_lastChild == null)
			{
				Debug.Assert(_firstChild == null);
				_firstChild = _lastChild = child;
				child._previousBrother = null;
			}
			// 末尾が非nullの場合、リンクを生成
			else
			{
				if (_lastChild == child) // 同じものを二連続足したバグは無限ループして厄介なので検出して殺してやる
				{
					throw new System.InvalidOperationException("same object added. it must be BUG.");
				}
				_lastChild._nextBrother = child;
				child._previousBrother = _lastChild;
				_lastChild = child;
			}
		}

		// リストのつなぎ換えだけ
		void UnlinkChild(DebugUiControl child)
		{
			Debug.Assert(child._parent == this, "それは樸の子じゃないよ!");
			// 前と後を取得
			var next = child._nextBrother;
			var prev = child._previousBrother;
			if (next != null)
			{
				next._previousBrother = prev;
			}
			// nextがないケースではこれが最後。
			else
			{
				_lastChild = prev;
			}

			if (prev != null)
			{
				prev._nextBrother = next;
			}
			// prevがないケースではこれが最初
			else
			{
				_firstChild = next;
			}
			child._nextBrother = child._previousBrother = null;
		}

		public IEnumerable<DebugUiControl> EnumerateChildren()
		{
			var child = _firstChild;
			while (child != null)
			{
				yield return child;
				child = child._nextBrother;
			}
		}
	}
}