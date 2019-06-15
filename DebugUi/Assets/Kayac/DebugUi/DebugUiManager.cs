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
		public bool safeAreaVisualizationEnabled{ get; set; }

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
			bool hit = false;

			// ドラッグ中ならtrueにする。でないと諸々のイベントが取れなくなる
			if (isDragging)
			{
				hit = true;
			}
			// 何かに当たればtrue
			else if (_root.RaycastRecursive(0, 0, x, y))
			{
				hit = true;
			}
			else
			{
				// 外れたら離したものとみなす。
				_input.isPointerDown = false;
				_input.pointerX = -float.MaxValue;
				_input.pointerY = -float.MaxValue;
			}

			// 当たったらraycastResult足す
			if (hit)
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
			Debug.LogFormat(
				"Create DebugUiManager. physicalResolution {0}x{1} safeArea:{2}",
				Screen.width,
				Screen.height,
				GetSafeArea());
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
			safeAreaVisualizationEnabled = true;
			this.scale = 1f;
			_renderer = renderer;
			_camera = camera;
			_screenPlaneDistance = screenPlaneDistance;
			_meshTransform = meshTransform;
			_referenceScreenHeight = referenceScreenHeight;
			_referenceScreenWidth = referenceScreenWidth;
			_root = new DebugUiContainer(name: "RootContainer");
			_input = new Input();
			inputEnabled = true;
			// 初回サイズ決定
			UpdateTransform();
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
			UpdateTransform();

			if (safeAreaVisualizationEnabled)
			{
				DrawSafeArea();
			}
			// 描画本体
			UnityEngine.Profiling.Profiler.BeginSample("DebugPrimitiveRenderer2D.LateUpdate");
			_renderer.UpdateMesh();
			UnityEngine.Profiling.Profiler.EndSample();

			UnityEngine.Profiling.Profiler.EndSample();
		}

		static Rect GetSafeArea() // 上書き
		{
			var ret = Screen.safeArea;
ret = new Rect(50f, 100f, Screen.width - 50f - 75f, Screen.height - 100f - 150f); // SafeAreaテスト
			return ret;
		}

		void DrawSafeArea()
		{
			var safeArea = GetSafeArea();
			// 左上端
			float x0 = 0f;
			float y0 = (float)Screen.height;
			ConvertCoordFromUnityScreen(ref x0, ref y0);
			// 左上端
			float x1 = (float)Screen.width;
			float y1 = 0f;
			ConvertCoordFromUnityScreen(ref x1, ref y1);
			_renderer.color = new Color32(255, 0, 0, 64);
			_renderer.AddRectangle(x0, y0, (x1 - x0), -y0); // 上
			_renderer.AddRectangle(x0, _root.height, (x1 - x0), y1 - _root.height); // 下
			_renderer.AddRectangle(x0, 0f, -x0, _root.height); // 左
			_renderer.AddRectangle(_root.width, 0f, x1 - _root.width, _root.height); // 右
		}

		void UpdateTransform()
		{
			// カメラ追随処理
			var safeArea = GetSafeArea();
			var aspect = safeArea.width / safeArea.height;
			float goalScale, halfHeight;
			if (_camera.orthographic)
			{
				halfHeight = _camera.orthographicSize;
			}
			else
			{
				halfHeight = _screenPlaneDistance * Mathf.Tan(_camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
			}

			var width = (float)_referenceScreenWidth;
			var height = (float)_referenceScreenHeight;
			var refAspect = width / height;
			float offsetX, offsetY;
			if (refAspect > aspect) // Yが余る
			{
				goalScale = halfHeight * aspect / ((float)_referenceScreenWidth * 0.5f);
				goalScale *= safeArea.width / (float)Screen.width;
				height = width / aspect;
			}
			else
			{
				goalScale = halfHeight / ((float)_referenceScreenHeight * 0.5f);
				goalScale *= safeArea.height / (float)Screen.height;
				width = height * aspect;
			}
			var fullWidth = width * (float)Screen.width / safeArea.width;
			var fullHeight = height * (float)Screen.height / safeArea.height;
			offsetX = -fullWidth * 0.5f;
			offsetY = -fullHeight * 0.5f;
			offsetX += fullWidth * safeArea.x / (float)Screen.width;
			offsetY += fullHeight * safeArea.y / (float)Screen.height;
			_root.SetSize(width, height);

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
			_input.pointerX = Mathf.Clamp(x, 0f, _root.width);
			_input.pointerY = Mathf.Clamp(y, 0f, _root.height);
		}

		// UnityのEventSystemの座標をY下向きの仮想解像度座標系に変換
		public void ConvertCoordFromUnityScreen(ref float x, ref float y)
		{
			var world = _camera.ScreenToWorldPoint(new Vector3(x, y, _screenPlaneDistance));
			var local = _meshTransform.worldToLocalMatrix.MultiplyPoint(world);
			x = local.x;
			y = local.y;
		}

		public void MoveToTop(DebugUiControl control)
		{
			_root.UnlinkChild(control);
			_root.AddChildAsTail(control);
		}
	}
}
