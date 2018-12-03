using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kayac
{
	public class DebugUiManager :
		Graphic
		, IPointerClickHandler
		, IPointerDownHandler
		, IPointerUpHandler
		, IBeginDragHandler
		, IDragHandler
		, ICanvasRaycastFilter
	{
		private DebugUiControl _root;
		public class Input
		{
			// 以下はフレームをまたいで状態が保たれる。扱いに注意
			public float pointerX;
			public float pointerY;
			public bool isPointerDown;
			public DebugUiControl draggedControl;
			// 以下トリガー系。UpdateEventRecursive後にリセット
			public bool hasJustClicked;
			public bool hasJustDragStarted;
			// 以下UpdateEventRecursiveから書き込み。UpdateEventRecursive前にリセット
			public DebugUiControl eventConsumer;
		}

		private Input _input;
		private DebugPrimitiveRenderer2D _renderer;
		public DebugPrimitiveRenderer2D primitiveRenderer
		{
			get
			{
				return _renderer;
			}
		}

		public bool isDragging
		{
			get
			{
				return (_input.draggedControl != null);
			}
		}

		public Vector2 pointerPosition
		{
			get
			{
				Vector2 ret;
				ret.x = _input.pointerX;
				ret.y = _input.pointerY;
				return ret;
			}
		}

		public Vector2 unityPointerPosition
		{
			get
			{
				Vector2 ret;
				ret.x = _input.pointerX;
				ret.y = _input.pointerY;
				ConvertCoordToUnityScreen(ref ret.x, ref ret.y);
				return ret;
			}
		}

		public static DebugUiManager Create(
			GameObject parentGameObject,
			DebugPrimitiveRenderer2D renderer)
		{
			var self = parentGameObject.AddComponent<DebugUiManager>();
			self.Initialize(renderer);
			Debug.Assert(self.rectTransform != null, "RectTransformがない!canvasの下にあるGameObjectを指定してください!");
			if (self.rectTransform != null)
			{
				self.rectTransform.anchorMin = new Vector2(0f, 0f);
				self.rectTransform.anchorMax = new Vector2(1f, 1f);
				self.rectTransform.offsetMin = new Vector2(0f, 0f);
				self.rectTransform.offsetMax = new Vector2(0f, 0f);
			}
			return self;
		}

		public void Initialize(DebugPrimitiveRenderer2D renderer)
		{
			_renderer = renderer;
			_root = new DebugUiControl();
			_input = new Input();
			this.raycastTarget = true;
			inputEnabled = true;
		}

		public void Dispose()
		{
			_root = null;
			_input = null;
			_renderer = null;
		}

		public bool IsDisposed()
		{
			return (_root == null);
		}

		public bool hasInitialized
		{
			get
			{
				return _renderer != null;
			}
		}

		public bool inputEnabled{ private get; set; }

		public void Add(
			DebugUiControl control,
			float offsetX = 0f,
			float offsetY = 0)
		{
			_root.AddChild(control, offsetX, offsetY);
		}

		public void Remove(DebugUiControl control)
		{
			_root.RemoveChild(control);
		}

		public void RemoveAll()
		{
			_root.RemoveAllChild();
		}

		public void ManualUpdate(float deltaTime)
		{
			Debug.Assert(_renderer != null, "call Initialize()");
			_input.eventConsumer = null;

			_root.UpdateEventRecursive(0, 0, _input, true);

			// inputのうち非継続的な状態をリセット
			_input.hasJustClicked = false;
			_input.hasJustDragStarted = false;

			_root.UpdateRecursive();
			_root.DrawRecursive(0, 0, _renderer);
			// ドラッグマーク描画
			if (_input.draggedControl != null)
			{
				DrawDragMark(_input.draggedControl);
			}
			var consumer = _input.eventConsumer;
			if (consumer != null)
			{
				if (consumer.onEventConsume != null)
				{
					consumer.onEventConsume();
				}
			}
		}

		private void DrawDragMark(DebugUiControl dragged)
		{
			float s = dragged.dragMarkSize;
			float x0 = _input.pointerX;
			float y0 = _input.pointerY;
			float x1 = x0 - (s * 0.5f);
			float y1 = y0 - s;
			float x2 = x0 + (s * 0.5f);
			float y2 = y0 - s;
			// 背景
			_renderer.color = dragged.dragMarkColor;
			_renderer.AddTriangle(x0, y0, x1, y1, x2, y2);
			// 枠
			_renderer.color = new Color32(255, 255, 255, 255);
			_renderer.AddTriangleFrame(x0, y0, x1, y1, x2, y2, s * 0.125f);
			// テキスト
			if (dragged.dragMarkLetter != '\0')
			{
				_renderer.color = dragged.dragMarkLetterColor;
				_renderer.AddText(
					new string(dragged.dragMarkLetter, 1),
					s * 0.5f,
					x0 - (s * 0.25f),
					y1,
					dragged.dragMarkSize,
					dragged.dragMarkSize);
			}
		}

		public void OnPointerClick(PointerEventData data)
		{
//Debug.LogWarning("DebugUi: click");
			UpdatePointer(data.position);
			_input.hasJustClicked = true;
		}

		public void OnPointerDown(PointerEventData data)
		{
//Debug.LogWarning("DebugUi: pointerDown");
			UpdatePointer(data.position);
			_input.isPointerDown = true;
		}

		public void OnPointerUp(PointerEventData data)
		{
//Debug.LogWarning("DebugUi: pointerUp");
			UpdatePointer(data.position);
			_input.isPointerDown = false;
			// TODO: UpdatePerFrameの中で呼びたいなできれば...コールバック類は同じ場所から呼ばれる保証が欲しい
			var dragged = _input.draggedControl;
			if (dragged != null)
			{
//Debug.LogWarning("DragEnd : " + _input.draggedControl);
				if (dragged.onDragEnd != null)
				{
//Debug.LogWarning("\tcall onDragEnd()");
					dragged.onDragEnd();
				}
			}
			_input.draggedControl = null;
		}

		public void OnBeginDrag(PointerEventData data)
		{
//Debug.LogWarning("DebugUi: beginDrag");
			UpdatePointer(data.position);
			_input.isPointerDown = true;
			_input.hasJustDragStarted = true;
		}

		public void OnDrag(PointerEventData data)
		{
//Debug.LogWarning("DebugUi: drag");
			UpdatePointer(data.position);
			_input.isPointerDown = true;
		}

		private void UpdatePointer(Vector2 screenPosition)
		{
			float x = screenPosition.x;
			float y = screenPosition.y;
			ConvertCoordFromUnityScreen(ref x, ref y);
			_input.pointerX = x;
			_input.pointerY = y;
		}

		// このカーソル位置でレイをスルーするか否か。内部のイベントを取るコントロールと一つでも当たるかどうかを調べる。
		public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
		{
			if ((_renderer == null) || !inputEnabled)
			{
				return false;
			}
			// 何かに当たるならイベントを取り、何にも当たらないならスルーする
			float x = sp.x;
			float y = sp.y;
			ConvertCoordFromUnityScreen(ref x, ref y);
			bool ret = false;

			// ドラッグ中ならtrueにする。でないと諸々のイベントが取れなくなる
			if (isDragging)
			{
				ret = true;
			}
			// 何かに当たればtrue
			else if (_root.RaycastRecursive(0, 0, x, y))
			{
				ret = true;
			}
			else
			{
				// 外れたら離したものとみなす。
				_input.isPointerDown = false;
				_input.pointerX = -float.MaxValue;
				_input.pointerY = -float.MaxValue;
				ret = false;
			}
			return ret;
		}

		// UnityのEventSystemの座標をY下向きの仮想解像度座標系に変換
		public void ConvertCoordFromUnityScreen(ref float x, ref float y)
		{
			Debug.Assert(_renderer != null, "call SetRenderer()");
			// 座標系を仮想解像度向け(1136x640とかでY下向き)に変換
			float referenceHeight = _renderer.referenceScreenHeight;
			float referenceWidth = _renderer.referenceScreenWidth;
			float actualWidth = (float)Screen.width;
			float actualHeight = (float)Screen.height;

			// Xはスケールするだけ
			x = x * referenceWidth / actualWidth;
			// Yはスケールして反転
			y = referenceHeight - (y * referenceHeight / actualHeight);
		}

		// Y下向きの仮想解像度座標系からUnityのEventSystemの座標系に変換
		public void ConvertCoordToUnityScreen(ref float x, ref float y)
		{
			Debug.Assert(_renderer != null, "call SetRenderer()");
			// 座標系を仮想解像度向け(1136x640とかでY下向き)に変換
			float referenceHeight = _renderer.referenceScreenHeight;
			float referenceWidth = _renderer.referenceScreenWidth;
			float actualWidth = (float)Screen.width;
			float actualHeight = (float)Screen.height;

			// Xはスケールするだけ
			x = x * actualWidth / referenceWidth;
			// Yはスケールして反転
			y = actualHeight - (y * actualHeight / referenceHeight);
		}

		// 以下UI.Graphicのデフォルト挙動を殺すためのコード。
		// 素のGraphicでもRectTransformのサイズに合わせて4頂点作って描画するので頂点を消してやる必要がある!!!
		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();
		}
	}
}