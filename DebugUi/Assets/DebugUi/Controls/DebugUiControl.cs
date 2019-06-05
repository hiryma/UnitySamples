using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kayac
{
	public abstract class DebugUiControl : DebugUi, IDisposable
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
		bool _enabled;
		public DebugUiControl nextBrother { get { return _nextBrother; } }
		public DebugUiControl previousBrother { get { return _previousBrother; } }

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

		public void Destroy()
		{
			Dispose();
			_nextBrother = _previousBrother = null;
			_enabled = false;
			// 以下まともな値が取れないようにしてバグを発覚しやすく
			localLeftX = float.NaN;
			localTopY = float.NaN;
			width = float.NaN;
			height = float.NaN;
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

		// Panelの派生でのみ実装される
		protected virtual void EnableChildren()
		{
		}

		public void OnEnableRecursive()
		{
			Debug.Assert(_enabled);
			OnEnable(); // 自分を呼ぶ

			EnableChildren();
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

		protected virtual void UpdateEventChildren(
			float offsetX,
			float offsetY,
			DebugUiManager.Input input,
			bool parentEnabled)
		{
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
			UpdateEventChildren(leftX, topY, input, parentEnabled && enabled);
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

		// 子要素に対してレイキャストして、当たりを発見し次第抜けてtrueを返す。
		protected virtual bool RaycastChildren(
			float offsetX,
			float offsetY,
			float pointerX,
			float pointerY)
		{
			return false;
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
			if (RaycastChildren(globalLeftX, globalTopY, pointerX, pointerY))
			{
				return true;
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

		protected virtual void UpdateChildren(float deltaTime)
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
			UpdateChildren(deltaTime);
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

		protected virtual void DrawChildren(
			float offsetX,
			float offsetY,
			DebugPrimitiveRenderer2D renderer)
		{
		}

		public void InsertAfter(DebugUiControl newNode)
		{
			Debug.Assert(newNode._previousBrother == null, "already in tree.");
			Debug.Assert(newNode._nextBrother == null, "already in tree.");
			var next = _nextBrother;
			newNode._previousBrother = this;
			newNode._nextBrother = next;
			_nextBrother = newNode;
			if (next != null)
			{
				next._previousBrother = newNode;
			}
		}

		public void InsertBefore(DebugUiControl newNode)
		{
			Debug.Assert(newNode._previousBrother == null, "already in tree.");
			Debug.Assert(newNode._nextBrother == null, "already in tree.");
			var prev = _previousBrother;
			newNode._nextBrother = this;
			newNode._previousBrother = prev;
			_previousBrother = newNode;
			if (prev != null)
			{
				prev._nextBrother = newNode;
			}
		}

		public void Unlink()
		{
			var prev = _previousBrother;
			var next = _nextBrother;
			_previousBrother = _nextBrother = null;
			if (prev != null)
			{
				prev._nextBrother = next;
			}
			if (next != null)
			{
				next._previousBrother = prev;
			}
		}

		public void ClearLink()
		{
			_previousBrother = _nextBrother = null;
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

			DrawChildren(globalLeftX, globalTopY, renderer);
			DrawPostChild(offsetX, offsetY, renderer);
		}
	}
}