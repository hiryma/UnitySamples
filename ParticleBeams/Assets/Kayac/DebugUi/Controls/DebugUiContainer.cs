using UnityEngine;

namespace Kayac
{
	// 何かを子に持てるもの(Panel,Window,Menu等々)の基底
	public class DebugUiContainer : DebugUiControl
	{
		DebugUiControl _firstChild;
		DebugUiControl _lastChild;

		public DebugUiContainer(string name = "Container") : base(name)
		{
		}

		protected override void EnableChildren()
		{
			var child = _firstChild;
			while (child != null)
			{
				if (child.enabled)
				{
					child.OnEnableRecursive();
				}
				child = child.nextBrother;
			}
		}

		public virtual void RemoveChild(DebugUiControl child)
		{
			// リストから切断
			UnlinkChild(child);
			child.Destroy();
		}

		public void RemoveAllChild()
		{
			var child = _firstChild;
			while (child != null)
			{
				var next = child.nextBrother;
				child.Destroy();
				child = next;
			}
			_firstChild = _lastChild = null;
		}

		protected override void UpdateEventChildren(
			float offsetX,
			float offsetY,
			DebugUiManager.Input input,
			bool parentEnabled)
		{
			var child = _lastChild;
			while (child != null)
			{
				child.UpdateEventRecursive(
					leftX,
					topY,
					input,
					parentEnabled && enabled);
				child = child.previousBrother;
			}
		}

		protected override bool RaycastChildren(
			float offsetX,
			float offsetY,
			float pointerX,
			float pointerY)
		{
			var child = _firstChild;
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
				child = child.nextBrother;
			}
			return false;
		}

		protected override void UpdateChildren(float deltaTime)
		{
			var child = _firstChild;
			while (child != null)
			{
				child.UpdateRecursive(deltaTime);
				child = child.nextBrother;
			}
		}

		protected override void DrawChildren(
			float offsetX,
			float offsetY,
			DebugPrimitiveRenderer2D renderer)
		{
			var child = _firstChild;
			while (child != null)
			{
				child.DrawRecursive(offsetX, offsetY, renderer);
				child = child.nextBrother;
			}
		}

		// リストのつなぎ換えだけ
		public void AddChildAsHead(DebugUiControl child)
		{
			Debug.Assert(child.previousBrother == null, "already in tree.");
			Debug.Assert(child.nextBrother == null, "already in tree.");
			// 先頭がnullの場合、末尾もnull。
			if (_firstChild == null)
			{
				Debug.Assert(_lastChild == null);
				_firstChild = _lastChild = child;
			}
			// 先頭が非nullの場合、リンクを生成
			else
			{
				if (_firstChild == child) // 同じものを二連続足したバグは無限ループして厄介なので検出して殺してやる
				{
					throw new System.InvalidOperationException("same object added. it must be BUG.");
				}
				_firstChild.InsertBefore(child);
				_firstChild = child;
			}
			Debug.Assert(child.previousBrother != child);
			Debug.Assert(child.nextBrother != child);
		}

		// リストのつなぎ換えだけ
		public void AddChildAsTail(DebugUiControl child)
		{
			Debug.Assert(child.previousBrother == null, "already in tree.");
			Debug.Assert(child.nextBrother == null, "already in tree.");
			// 末尾がnullの場合、先頭もnull。
			if (_lastChild == null)
			{
				Debug.Assert(_firstChild == null);
				_firstChild = _lastChild = child;
			}
			// 末尾が非nullの場合、リンクを生成
			else
			{
				if (_lastChild == child) // 同じものを二連続足したバグは無限ループして厄介なので検出して殺してやる
				{
					throw new System.InvalidOperationException("same object added. it must be BUG.");
				}
				_lastChild.InsertAfter(child);
				_lastChild = child;
			}
			Debug.Assert(child.previousBrother != child);
			Debug.Assert(child.nextBrother != child);
		}

		// リストのつなぎ換えだけ
		public void UnlinkChild(DebugUiControl child)
		{
			// 先頭であれば、
			if (child == _firstChild)
			{
				_firstChild = child.nextBrother;
			}
			// 末尾であれば
			if (child == _lastChild)
			{
				_lastChild = child.previousBrother;
			}
			child.Unlink();
		}

		/// 子を全て含むギリギリのサイズに調整する。遅いので注意。width,heightしかいじらない。
		public void FitSize()
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
				child = child.nextBrother;
			}

			if (borderEnabled)
			{
				maxX += borderWidth * 2f;
				maxY += borderWidth * 2f;
			}
			SetSize(maxX, maxY);
		}

		// offsetはLocalPositionに足されるので注意
		public void Add(
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
			AddChildAsTail(child);
		}
	}
}
