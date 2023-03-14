using System;
using UnityEngine;

namespace Kayac.DebugUi
{
    public abstract class Control : IDisposable
    {
        // デバグ用にしか使っていない
        public string Name { get; private set; }
        public Action OnDragEnd { get; set; }
        public Action OnDragStart { get; set; }
        // 押された時など、そのフレームでイベントを消費したコントロールになった時。呼ばれるのはフレームの末尾。TODO: 名前とかどうにかしたい
        public Action OnEventConsume { get; set; }
        // 有効無効化
        public bool Enabled
        {
            get
            {
                return enabled;
            }
            set
            {
                var oldValue = enabled;
                enabled = value;
                if (!oldValue && value) //有効化イベントを下流に流す
                {
                    OnEnableRecursive();
                }
            }
        }
        public bool Draggable { get; set; }

        // イベント状態取得
        public bool HasJustClicked { get; private set; }
        public bool HasJustDragStarted { get; private set; }
        public bool HasFocus { get; private set; }
        public bool IsPointerDown { get; private set; }
        /// 単にこの矩形の上にポインタがあるかどうかを返す。イベント無効でも動作。
        public bool IsPointerOver { get; private set; }
        public bool IsDragging { get; set; }

        // スタイル
        public Color32 BackgroundColor { get; set; }
        public Color32 BorderColor { get; set; }
        public float BorderWidth { get; set; }
        public Color32 DragMarkColor { get; set; }
        public float DragMarkSize { get; set; }
        public char DragMarkLetter { get; set; }
        public Color32 DragMarkLetterColor { get; set; }

        // 派生から設定する特徴変数
        protected bool EventEnabled { get; set; }
        protected bool BorderEnabled { get; set; }
        protected bool BackgroundEnabled { get; set; }
        // 位置とサイズ情報
        public float LocalLeftX { get; private set; }
        public float LocalTopY { get; private set; }
        public float LeftX { get; private set; }
        public float TopY { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }
        // Update内で使うためのローカルポインタ座標(コントロール内ではない)
        protected float LocalPointerX { get; private set; }
        protected float LocalPointerY { get; private set; }
        protected float PointerX
        {
            get
            {
                return LocalPointerX - LocalLeftX + LeftX;
            }
        }
        protected float PointerY
        {
            get
            {
                return LocalPointerY - LocalTopY + TopY;
            }
        }
        // Clickが呼ばれたか
        protected bool ClickedFromCode { get; private set; }

        bool enabled;
        public Control NextBrother { get; private set; }
        public Control PreviousBrother { get; private set; }

        protected Control(string name = "")
        {
            Name = name;
            enabled = true;
            EventEnabled = false;
            BackgroundEnabled = false;
            BorderEnabled = false;
            HasFocus = false;

            // スタイルデフォルト
            BorderWidth = 2f;
            BackgroundColor = new Color32(0, 0, 0, 128);
            BorderColor = new Color32(255, 255, 255, 128);
            DragMarkColor = new Color32(255, 0, 0, 192);
            DragMarkSize = 100f;
            DragMarkLetter = '↓';
            DragMarkLetterColor = new Color32(255, 255, 255, 255);
        }

        public void Destroy()
        {
            Dispose();
            NextBrother = PreviousBrother = null;
            enabled = false;
            // 以下まともな値が取れないようにしてバグを発覚しやすく
            LocalLeftX = float.NaN;
            LocalTopY = float.NaN;
            Width = float.NaN;
            Height = float.NaN;
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
            ClickedFromCode = true;
        }

        // Panelの派生でのみ実装される
        protected virtual void EnableChildren()
        {
        }

        public void OnEnableRecursive()
        {
            UnityEngine.Debug.Assert(enabled);
            OnEnable(); // 自分を呼ぶ

            EnableChildren();
        }

        public void SetLocalPosition(
            float localLeftX,
            float localTopY)
        {
            this.LocalLeftX = localLeftX;
            this.LocalTopY = localTopY;
        }

        public void SetSize(
            float width,
            float height)
        {
            Width = width;
            Height = height;
        }

        /// enabledをtrue-falseで切り換える。古い値が返る。
        public bool ToggleEnabled()
        {
            bool ret = Enabled;
            Enabled = !Enabled;
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
            HasJustClicked = false;
            HasJustDragStarted = false;
            IsPointerDown = false;
            IsPointerOver = false;
            bool prevDragging = IsDragging;
            IsDragging = false;
            // グローバル座標を計算して子優先。enabled=falseでもリセットのために回す必要あり
            LeftX = offsetX + LocalLeftX;
            TopY = offsetY + LocalTopY;
            // 逆順で更新。後のものほど手前に描画されるので、手前のものを優先してイベントを処理する。
            UpdateEventChildren(LeftX, TopY, input, parentEnabled && Enabled);
            // 無効なら以降処理しない。ここまでは初期化が絡むので毎回やらねばならない。
            if ((parentEnabled && Enabled) == false)
            {
                return;
            }

            // 自分の当たり判定。isPointerOver自体はイベント無効でも更新。
            LocalPointerX = input.pointerX - offsetX;
            LocalPointerY = input.pointerY - offsetY;
            float clientX = LocalPointerX - LocalLeftX;
            float clientY = LocalPointerY - LocalTopY;
            if ((clientX >= 0f)
                && (clientX < Width)
                && (clientY >= 0f)
                && (clientY < Height))
            {
                IsPointerOver = true;
            }

            // 有効で、イベントも取り、下流で当たっていなければ、イベント処理
            if (EventEnabled)
            {
                // ポインタがどこにあろうが、前のフレームがドラッグ中でPointerDownならドラッグを維持
                if (Draggable && prevDragging && input.isPointerDown)
                {
                    IsDragging = true;
                }

                // まだ他のコントロールにイベントを処理されていなくて、ポインタが上にあれば、
                if ((input.eventConsumer == null) && IsPointerOver)
                {
                    if (input.hasJustClicked)
                    {
                        input.eventConsumer = this;
                        HasJustClicked = true;
                    }

                    if (Draggable)
                    {
                        // ドラッグ開始
                        if (input.hasJustDragStarted)
                        {
                            IsDragging = true;
                            HasJustDragStarted = true;
                            input.eventConsumer = this;
                            input.draggedControl = this;
                            if (OnDragStart != null)
                            {
                                OnDragStart();
                            }
                        }
                    }

                    if (input.isPointerDown)
                    {
                        input.eventConsumer = this;
                        IsPointerDown = true;
                    }
                }

                if (input.hasJustClicked)
                {
                    if ((input.eventConsumer == this) && IsPointerOver)
                    {
                        HasFocus = true;
                    }
                    else
                    {
                        HasFocus = false;
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
            if (!enabled)
            {
                return false;
            }
            // グローバル座標を計算して子に回す
            float globalLeftX = offsetX + LocalLeftX;
            float globalTopY = offsetY + LocalTopY;
            if (RaycastChildren(globalLeftX, globalTopY, pointerX, pointerY))
            {
                return true;
            }

            // イベント取る場合のみ判定
            if (EventEnabled)
            {
                // 自分の当たり判定
                float globalRightX = globalLeftX + Width;
                float globalBottomY = globalTopY + Height;
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
            if (!enabled)
            {
                return;
            }
            // 手動クリックされていれば、クリックされたことにする
            if (this.ClickedFromCode)
            {
                this.HasJustClicked = true;
                this.ClickedFromCode = false;
            }
            Update(deltaTime);
            UpdateChildren(deltaTime);
        }

        public virtual void Draw(
            float offsetX,
            float offsetY,
            Renderer2D renderer)
        {
        }

        public virtual void DrawPostChild(
           float offsetX,
           float offsetY,
           Renderer2D renderer)
        {
        }

        protected virtual void DrawChildren(
            float offsetX,
            float offsetY,
            Renderer2D renderer)
        {
        }

        public void InsertAfter(Control newNode)
        {
            UnityEngine.Debug.Assert(newNode.PreviousBrother == null, "already in tree.");
            UnityEngine.Debug.Assert(newNode.NextBrother == null, "already in tree.");
            var next = NextBrother;
            newNode.PreviousBrother = this;
            newNode.NextBrother = next;
            NextBrother = newNode;
            if (next != null)
            {
                next.PreviousBrother = newNode;
            }
        }

        public void InsertBefore(Control newNode)
        {
            UnityEngine.Debug.Assert(newNode.PreviousBrother == null, "already in tree.");
            UnityEngine.Debug.Assert(newNode.NextBrother == null, "already in tree.");
            var prev = PreviousBrother;
            newNode.NextBrother = this;
            newNode.PreviousBrother = prev;
            PreviousBrother = newNode;
            if (prev != null)
            {
                prev.NextBrother = newNode;
            }
        }

        public void Unlink()
        {
            var prev = PreviousBrother;
            var next = NextBrother;
            PreviousBrother = NextBrother = null;
            if (prev != null)
            {
                prev.NextBrother = next;
            }
            if (next != null)
            {
                next.PreviousBrother = prev;
            }
        }

        public void ClearLink()
        {
            PreviousBrother = NextBrother = null;
        }

        public void DrawRecursive(
            float offsetX,
            float offsetY,
            Renderer2D renderer)
        {
            if (!enabled)
            {
                return;
            }

            // まず自分描画
            if (BackgroundEnabled)
            {
                renderer.Color = BackgroundColor;
                renderer.AddRectangle(
                    offsetX + LocalLeftX,
                    offsetY + LocalTopY,
                    Width,
                    Height);
            }
            if (BorderEnabled)
            {
                renderer.Color = BorderColor;
                renderer.AddRectangleFrame(
                    offsetX + LocalLeftX,
                    offsetY + LocalTopY,
                    Width,
                    Height,
                    BorderWidth);
            }
            Draw(offsetX, offsetY, renderer);

            // グローバル座標を計算して子を描画
            float globalLeftX = offsetX + LocalLeftX;
            float globalTopY = offsetY + LocalTopY;

            DrawChildren(globalLeftX, globalTopY, renderer);
            DrawPostChild(offsetX, offsetY, renderer);
        }
    }
}