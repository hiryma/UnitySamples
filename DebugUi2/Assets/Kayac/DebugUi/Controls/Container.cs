using UnityEngine;

namespace Kayac.DebugUi
{
    // 何かを子に持てるもの(Panel,Window,Menu等々)の基底
    public class Container : Control
    {
        Control firstChild;
        Control lastChild;

        public Container(string name = "Container") : base(name)
        {
        }

        protected override void EnableChildren()
        {
            var child = firstChild;
            while (child != null)
            {
                if (child.Enabled)
                {
                    child.OnEnableRecursive();
                }
                child = child.NextBrother;
            }
        }

        public virtual void RemoveChild(Control child)
        {
            // リストから切断
            UnlinkChild(child);
            child.Destroy();
        }

        public void RemoveAllChild()
        {
            var child = firstChild;
            while (child != null)
            {
                var next = child.NextBrother;
                child.Destroy();
                child = next;
            }
            firstChild = lastChild = null;
        }

        protected override void UpdateEventChildren(
            float offsetX,
            float offsetY,
            DebugUiManager.Input input,
            bool parentEnabled)
        {
            var child = lastChild;
            while (child != null)
            {
                child.UpdateEventRecursive(
                    LeftX,
                    TopY,
                    input,
                    parentEnabled && Enabled);
                child = child.PreviousBrother;
            }
        }

        protected override bool RaycastChildren(
            float offsetX,
            float offsetY,
            float pointerX,
            float pointerY)
        {
            var child = firstChild;
            while (child != null)
            {
                bool result = child.RaycastRecursive(
                    offsetX,
                    offsetY,
                    pointerX,
                    pointerY);
                if (result)
                {
                    return true;
                }
                child = child.NextBrother;
            }
            return false;
        }

        protected override void UpdateChildren(float deltaTime)
        {
            var child = firstChild;
            while (child != null)
            {
                child.UpdateRecursive(deltaTime);
                child = child.NextBrother;
            }
        }

        protected override void DrawChildren(
            float offsetX,
            float offsetY,
            Renderer2D renderer)
        {
            var child = firstChild;
            while (child != null)
            {
                child.DrawRecursive(offsetX, offsetY, renderer);
                child = child.NextBrother;
            }
        }

        // リストのつなぎ換えだけ
        public void AddChildAsHead(Control child)
        {
            UnityEngine.Debug.Assert(child.PreviousBrother == null, "already in tree.");
            UnityEngine.Debug.Assert(child.NextBrother == null, "already in tree.");
            // 先頭がnullの場合、末尾もnull。
            if (firstChild == null)
            {
                UnityEngine.Debug.Assert(lastChild == null);
                firstChild = lastChild = child;
            }
            // 先頭が非nullの場合、リンクを生成
            else
            {
                if (firstChild == child) // 同じものを二連続足したバグは無限ループして厄介なので検出して殺してやる
                {
                    throw new System.InvalidOperationException("same object added. it must be BUG.");
                }
                firstChild.InsertBefore(child);
                firstChild = child;
            }
            UnityEngine.Debug.Assert(child.PreviousBrother != child);
            UnityEngine.Debug.Assert(child.NextBrother != child);
        }

        // リストのつなぎ換えだけ
        public void AddChildAsTail(Control child)
        {
            UnityEngine.Debug.Assert(child.PreviousBrother == null, "already in tree.");
            UnityEngine.Debug.Assert(child.NextBrother == null, "already in tree.");
            // 末尾がnullの場合、先頭もnull。
            if (lastChild == null)
            {
                UnityEngine.Debug.Assert(firstChild == null);
                firstChild = lastChild = child;
            }
            // 末尾が非nullの場合、リンクを生成
            else
            {
                if (lastChild == child) // 同じものを二連続足したバグは無限ループして厄介なので検出して殺してやる
                {
                    throw new System.InvalidOperationException("same object added. it must be BUG.");
                }
                lastChild.InsertAfter(child);
                lastChild = child;
            }
            UnityEngine.Debug.Assert(child.PreviousBrother != child);
            UnityEngine.Debug.Assert(child.NextBrother != child);
        }

        // リストのつなぎ換えだけ
        public void UnlinkChild(Control child)
        {
            // 先頭であれば、
            if (child == firstChild)
            {
                firstChild = child.NextBrother;
            }
            // 末尾であれば
            if (child == lastChild)
            {
                lastChild = child.PreviousBrother;
            }
            child.Unlink();
        }

        /// 子を全て含むギリギリのサイズに調整する。遅いので注意。width,heightしかいじらない。
        public void FitSize()
        {
            float maxX = 0f;
            float maxY = 0f;
            var child = firstChild;
            while (child != null)
            {
                if (child.Enabled)
                {
                    float right = child.LocalLeftX + child.Width;
                    float bottom = child.LocalTopY + child.Height;
                    maxX = Mathf.Max(maxX, right);
                    maxY = Mathf.Max(maxY, bottom);
                }
                child = child.NextBrother;
            }

            if (BorderEnabled)
            {
                maxX += BorderWidth * 2f;
                maxY += BorderWidth * 2f;
            }
            SetSize(maxX, maxY);
        }

        // offsetはLocalPositionに足されるので注意
        public void Add(
            Control child,
            float offsetX = 0f,
            float offsetY = 0f,
            AlignX alignX = AlignX.Left,
            AlignY alignY = AlignY.Top)
        {
            float x = 0f;
            switch (alignX)
            {
                case AlignX.Center: x = (this.Width - child.Width) * 0.5f; break;
                case AlignX.Right: x = this.Width - child.Width; break;
            }
            float y = 0f;
            switch (alignY)
            {
                case AlignY.Center: y = (this.Height - child.Height) * 0.5f; break;
                case AlignY.Bottom: y = (this.Height - child.Height); break;
            }
            child.SetLocalPosition(
                child.LocalLeftX + offsetX + x,
                child.LocalTopY + offsetY + y);
            AddChildAsTail(child);
        }
    }
}
