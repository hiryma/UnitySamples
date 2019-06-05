using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Kayac
{
	public class DebugUiManager : BaseRaycaster
		, IPointerClickHandler
		, IPointerDownHandler
		, IPointerUpHandler
		, IBeginDragHandler
		, IDragHandler
	{
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
		DebugUiContainer _root;
		int _referenceScreenWidth;
		int _referenceScreenHeight;
		Input _input;
		DebugPrimitiveRenderer2D _renderer;
		Camera _camera;
		Transform _meshTransform;
		float _screenPlaneDistance;


		public override Camera eventCamera
		{
			get
			{
				return _camera;
			}
		}

		public override void Raycast(
			PointerEventData eventData,
			List<RaycastResult> resultAppendList)
		{
			if ((_renderer == null) || !inputEnabled)
			{
				return;
			}
			// 何かに当たるならイベントを取り、何にも当たらないならスルーする
			var sp = eventData.position;
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
			// 当たったらraycastResult足す
			if (ret)
			{
				var result = new RaycastResult
				{
					gameObject = gameObject, // 自分
					module = this,
					distance = _screenPlaneDistance,
					worldPosition = _camera.transform.position + (_camera.transform.forward * _screenPlaneDistance),
					worldNormal = -_camera.transform.forward,
					screenPosition = eventData.position,
					index = resultAppendList.Count,
					sortingLayer = 0,
					sortingOrder = 32767
				};
				resultAppendList.Add(result);
			}
		}

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

		public float scale { private get; set; }

		public static DebugUiManager Create(
			Camera camera,
			Shader textShader,
			Shader texturedShader,
			Font font,
			int referenceScreenWidth,
			int referenceScreenHeight,
			float screenPlaneDistance,
			int triangleCapacity)
		{
			var gameObject = new GameObject("DebugUi");
			gameObject.transform.SetParent(camera.gameObject.transform, false);

			var self = gameObject.AddComponent<DebugUiManager>();

			var meshObject = new GameObject("debugUiMesh");
			meshObject.transform.SetParent(gameObject.transform, false);
			var meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshRenderer.sortingOrder = 32767;
			var meshFilter = meshObject.AddComponent<MeshFilter>();

			var renderer = new Kayac.DebugPrimitiveRenderer2D(
				textShader,
				texturedShader,
				font,
				meshRenderer,
				meshFilter,
				triangleCapacity);

			self.Initialize(
				renderer,
				camera,
				meshObject.transform,
				referenceScreenWidth,
				referenceScreenHeight,
				screenPlaneDistance);
			return self;
		}

		void Initialize(
			DebugPrimitiveRenderer2D renderer,
			Camera camera,
			Transform meshTransform,
			int referenceScreenWidth,
			int referenceScreenHeight,
			float screenPlaneDistance)
		{
			this.scale = 1f;
			_renderer = renderer;
			_camera = camera;
			_screenPlaneDistance = screenPlaneDistance;
			_meshTransform = meshTransform;
			_referenceScreenHeight = referenceScreenHeight;
			_referenceScreenWidth = referenceScreenWidth;
			// サイズ決定(縦横余る可能性がある)
			var refAspect = (float)_referenceScreenWidth / (float)_referenceScreenHeight;
			var aspect = (float)Screen.width / (float)Screen.height;
			float width = _referenceScreenWidth;
			float height = _referenceScreenHeight;
			if (refAspect > aspect) // Yが余る
			{
				height = width / aspect;
			}
			else // Xが余る
			{
				width = height * aspect;
			}

			_root = new DebugUiContainer(name: "RootContainer");
			_root.SetSize(width, height);
			_input = new Input();
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

		public bool inputEnabled { private get; set; }

		public void Add(
			DebugUiControl control,
			float offsetX = 0f,
			float offsetY = 0,
			DebugUi.AlignX alignX = DebugUi.AlignX.Left,
			DebugUi.AlignY alignY = DebugUi.AlignY.Top)
		{
			_root.Add(control, offsetX, offsetY, alignX, alignY);
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
			UnityEngine.Profiling.Profiler.BeginSample("DebugUiManager.ManualUpdate");
			Debug.Assert(_renderer != null, "call Initialize()");
			_input.eventConsumer = null;

			UnityEngine.Profiling.Profiler.BeginSample("DebugUiManager.UpdateEventRecursive");
			_root.UpdateEventRecursive(0, 0, _input, true);
			UnityEngine.Profiling.Profiler.EndSample();

			// inputのうち非継続的な状態をリセット
			_input.hasJustClicked = false;
			_input.hasJustDragStarted = false;

			UnityEngine.Profiling.Profiler.BeginSample("DebugUiManager.UpdateRecursize");
			_root.UpdateRecursive(deltaTime);
			UnityEngine.Profiling.Profiler.EndSample();

			UnityEngine.Profiling.Profiler.BeginSample("DebugUiManager.DrawRecursive");
			_root.DrawRecursive(0, 0, _renderer);
			UnityEngine.Profiling.Profiler.EndSample();
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
			// 描画本体
			UnityEngine.Profiling.Profiler.BeginSample("DebugPrimitiveRenderer2D.LateUpdate");
			_renderer.UpdateMesh();
			UnityEngine.Profiling.Profiler.EndSample();
			UpdateTransform();
			UnityEngine.Profiling.Profiler.EndSample();
		}

		void UpdateTransform()
		{
			// カメラ追随処理
			var refAspect = (float)_referenceScreenWidth / (float)_referenceScreenHeight;
			var aspect = (float)Screen.width / (float)Screen.height;
			float goalScale;
			if (_camera.orthographic)
			{
				if (refAspect > aspect)
				{
					goalScale = _camera.orthographicSize * aspect / ((float)_referenceScreenWidth * 0.5f);
				}
				else
				{
					goalScale = _camera.orthographicSize / ((float)_referenceScreenHeight * 0.5f);
				}
			}
			else
			{
				var halfHeight = _screenPlaneDistance * Mathf.Tan(_camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
				if (refAspect > aspect)
				{
					goalScale = halfHeight * aspect / ((float)_referenceScreenWidth * 0.5f);
				}
				else
				{
					goalScale = halfHeight / ((float)_referenceScreenHeight * 0.5f);
				}
			}

			float offsetX = -_referenceScreenWidth * 0.5f;
			float offsetY = -_referenceScreenHeight * 0.5f;
			float width = _referenceScreenWidth;
			float height = _referenceScreenHeight;
			if (refAspect > aspect) // Yが余る
			{
				offsetY = -(_referenceScreenWidth / aspect) * 0.5f;
				height = width / aspect;
			}
			else // Xが余る
			{
				offsetX = -(_referenceScreenHeight * aspect) * 0.5f;
				width = height * aspect;
			}
			_meshTransform.localPosition = new Vector3(offsetX, offsetY, 0f);
			gameObject.transform.localPosition = new Vector3(0f, 0f, _screenPlaneDistance);

			goalScale *= this.scale;
			gameObject.transform.localScale = new Vector3(goalScale, -goalScale, 1f); // Y反転
		}

		void DrawDragMark(DebugUiControl dragged)
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
					x0,
					y0,
					s * 0.5f,
					DebugPrimitiveRenderer.AlignX.Center,
					DebugPrimitiveRenderer.AlignY.Center);
			}
		}

		public void OnPointerClick(PointerEventData data)
		{
			UpdatePointer(data.position);
			_input.hasJustClicked = true;
		}

		public void OnPointerDown(PointerEventData data)
		{
			UpdatePointer(data.position);
			_input.isPointerDown = true;
		}

		public void OnPointerUp(PointerEventData data)
		{
			UpdatePointer(data.position);
			_input.isPointerDown = false;
			// TODO: UpdatePerFrameの中で呼びたいなできれば...コールバック類は同じ場所から呼ばれる保証が欲しい
			var dragged = _input.draggedControl;
			if (dragged != null)
			{
				if (dragged.onDragEnd != null)
				{
					dragged.onDragEnd();
				}
			}
			_input.draggedControl = null;
		}

		public void OnBeginDrag(PointerEventData data)
		{
			UpdatePointer(data.position);
			_input.isPointerDown = true;
			_input.hasJustDragStarted = true;
		}

		public void OnDrag(PointerEventData data)
		{
			UpdatePointer(data.position);
			_input.isPointerDown = true;
		}

		void UpdatePointer(Vector2 screenPosition)
		{
			float x = screenPosition.x;
			float y = screenPosition.y;
			ConvertCoordFromUnityScreen(ref x, ref y);
			_input.pointerX = x;
			_input.pointerY = y;
		}

		// UnityのEventSystemの座標をY下向きの仮想解像度座標系に変換
		public void ConvertCoordFromUnityScreen(ref float x, ref float y)
		{
			Debug.Assert(_renderer != null, "call SetRenderer()");

			var ray = _camera.ScreenPointToRay(new Vector3(x, y, 0f));
			var t = _screenPlaneDistance / ray.direction.z;
			x = ray.origin.x + (ray.direction.x * t);
			y = ray.origin.y + (ray.direction.y * t);
			var scale = gameObject.transform.localScale;
			x /= scale.x;
			y /= scale.y;
			var offset = _meshTransform.localPosition;
			x -= offset.x;
			y -= offset.y;
		}

		public void MoveToTop(DebugUiControl control)
		{
			_root.UnlinkChild(control);
			_root.AddChildAsTail(control);
		}
	}
}
