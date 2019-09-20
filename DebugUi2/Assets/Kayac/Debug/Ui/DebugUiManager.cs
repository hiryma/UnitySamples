using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Kayac.Debug;

namespace Kayac.Debug.Ui
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
            public Control draggedControl;
            // 以下トリガー系。UpdateEventRecursive後にリセット
            public bool hasJustClicked;
            public bool hasJustDragStarted;
            // 以下UpdateEventRecursiveから書き込み。UpdateEventRecursive前にリセット
            public Control eventConsumer;
        }
        Container root;
        int referenceScreenWidth;
        int referenceScreenHeight;
        Input input;
        Camera attachedCamera;
        Transform meshTransform;
        float _screenPlaneDistance;
        public bool SafeAreaVisualizationEnabled { get; set; }

        public override Camera eventCamera
        {
            get
            {
                return attachedCamera;
            }
        }

        public override void Raycast(
            PointerEventData eventData,
            List<RaycastResult> resultAppendList)
        {
            if ((Renderer == null) || !InputEnabled)
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
            if (IsDragging)
            {
                hit = true;
            }
            // 何かに当たればtrue
            else if (root.RaycastRecursive(0, 0, x, y))
            {
                hit = true;
            }
            else
            {
                // 外れたら離したものとみなす。
                input.isPointerDown = false;
                input.pointerX = -float.MaxValue;
                input.pointerY = -float.MaxValue;
            }

            // 当たったらraycastResult足す
            if (hit)
            {
                var result = new RaycastResult
                {
                    gameObject = gameObject, // 自分
                    module = this,
                    distance = _screenPlaneDistance,
                    worldPosition = eventCamera.transform.position + (eventCamera.transform.forward * _screenPlaneDistance),
                    worldNormal = -eventCamera.transform.forward,
                    screenPosition = eventData.position,
                    index = resultAppendList.Count,
                    sortingLayer = 0,
                    sortingOrder = 32767
                };
                resultAppendList.Add(result);
            }
        }

        public Renderer2D Renderer { get; private set; }

        public bool IsDragging
        {
            get
            {
                return (input.draggedControl != null);
            }
        }

        public Vector2 PointerPosition
        {
            get
            {
                Vector2 ret;
                ret.x = input.pointerX;
                ret.y = input.pointerY;
                return ret;
            }
        }

        public float Scale { private get; set; }

        public static DebugUiManager Create(
            Camera camera,
            DebugRendererAsset asset,
            int referenceScreenWidth,
            int referenceScreenHeight,
            float screenPlaneDistance,
            int triangleCapacity)
        {
            UnityEngine.Debug.LogFormat(
                "Create DebugUiManager. physicalResolution {0}x{1}\n  safeArea:{2}",
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

            var renderer = new Renderer2D(
                asset,
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
            Renderer2D renderer,
            Camera camera,
            Transform meshTransform,
            int referenceScreenWidth,
            int referenceScreenHeight,
            float screenPlaneDistance)
        {
            SafeAreaVisualizationEnabled = true;
            Scale = 1f;
            Renderer = renderer;
            attachedCamera = camera;
            _screenPlaneDistance = screenPlaneDistance;
            this.meshTransform = meshTransform;
            this.referenceScreenHeight = referenceScreenHeight;
            this.referenceScreenWidth = referenceScreenWidth;
            root = new Container(name: "RootContainer");
            input = new Input();
            InputEnabled = true;
            // 初回サイズ決定
            UpdateTransform();
        }

        public void Dispose()
        {
            root = null;
            input = null;
            Renderer = null;
        }

        public bool IsDisposed()
        {
            return (root == null);
        }

        public bool HasInitialized
        {
            get
            {
                return Renderer != null;
            }
        }

        public bool InputEnabled { private get; set; }

        public void Add(
            Control control,
            float offsetX = 0f,
            float offsetY = 0,
            AlignX alignX = AlignX.Left,
            AlignY alignY = AlignY.Top)
        {
            root.Add(control, offsetX, offsetY, alignX, alignY);
        }

        public void Remove(Control control)
        {
            root.RemoveChild(control);
        }

        public void RemoveAll()
        {
            root.RemoveAllChild();
        }

        public void ManualUpdate(float deltaTime)
        {
            UnityEngine.Profiling.Profiler.BeginSample("DebugUiManager.ManualUpdate");
            UnityEngine.Debug.Assert(Renderer != null, "call Initialize()");
            input.eventConsumer = null;

            UnityEngine.Profiling.Profiler.BeginSample("DebugUiManager.UpdateEventRecursive");
            root.UpdateEventRecursive(0, 0, input, true);
            UnityEngine.Profiling.Profiler.EndSample();

            // inputのうち非継続的な状態をリセット
            input.hasJustClicked = false;
            input.hasJustDragStarted = false;

            UnityEngine.Profiling.Profiler.BeginSample("DebugUiManager.UpdateRecursize");
            root.UpdateRecursive(deltaTime);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("DebugUiManager.DrawRecursive");
            root.DrawRecursive(0, 0, Renderer);
            UnityEngine.Profiling.Profiler.EndSample();
            // ドラッグマーク描画
            if (input.draggedControl != null)
            {
                DrawDragMark(input.draggedControl);
            }
            var consumer = input.eventConsumer;
            if (consumer != null)
            {
                if (consumer.OnEventConsume != null)
                {
                    consumer.OnEventConsume();
                }
            }
            UpdateTransform();

            if (SafeAreaVisualizationEnabled)
            {
                DrawSafeArea();
            }
            // 描画本体
            UnityEngine.Profiling.Profiler.BeginSample("DebugPrimitiveRenderer2D.LateUpdate");
            Renderer.UpdateMesh();
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.EndSample();
        }

        static Rect GetSafeArea() // 上書き
        {
            var ret = Screen.safeArea;
            return ret;
        }

        void DrawSafeArea()
        {
            // 左上端
            float x0 = 0f;
            float y0 = (float)Screen.height;
            ConvertCoordFromUnityScreen(ref x0, ref y0);
            // 左上端
            float x1 = (float)Screen.width;
            float y1 = 0f;
            ConvertCoordFromUnityScreen(ref x1, ref y1);
            Renderer.Color = new Color32(255, 0, 0, 64);
            Renderer.AddRectangle(x0, y0, (x1 - x0), -y0); // 上
            Renderer.AddRectangle(x0, root.Height, (x1 - x0), y1 - root.Height); // 下
            Renderer.AddRectangle(x0, 0f, -x0, root.Height); // 左
            Renderer.AddRectangle(root.Width, 0f, x1 - root.Width, root.Height); // 右
        }

        void UpdateTransform()
        {
            // カメラ追随処理
            var safeArea = GetSafeArea();
            var aspect = safeArea.width / safeArea.height;
            var screenWidth = (float)Screen.width;
            var screenHeight = (float)Screen.height;
            var screenAspect = screenWidth / screenHeight;
            float goalScale, halfHeight;
            if (eventCamera.orthographic)
            {
                halfHeight = eventCamera.orthographicSize;
            }
            else
            {
                halfHeight = _screenPlaneDistance * Mathf.Tan(eventCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            }

            var width = (float)referenceScreenWidth;
            var height = (float)referenceScreenHeight;
            var refAspect = width / height;
            float offsetX, offsetY;
            if (refAspect > aspect) // Yが余る
            {
                goalScale = (halfHeight * screenAspect) / ((float)referenceScreenWidth * 0.5f);
                goalScale *= safeArea.width / screenWidth;
                height = width / aspect;
            }
            else
            {
                goalScale = halfHeight / ((float)referenceScreenHeight * 0.5f);
                goalScale *= safeArea.height / screenHeight;
                width = height * aspect;
            }
            var fullWidth = width * screenWidth / safeArea.width;
            var fullHeight = height * screenHeight / safeArea.height;
            offsetX = -fullWidth * 0.5f;
            offsetY = -fullHeight * 0.5f;
            offsetX += fullWidth * safeArea.x / screenWidth;
            offsetY += fullHeight * safeArea.y / screenHeight;
            root.SetSize(width, height);

            meshTransform.localPosition = new Vector3(offsetX, offsetY, 0f);
            gameObject.transform.localPosition = new Vector3(0f, 0f, _screenPlaneDistance);

            goalScale *= Scale;
            gameObject.transform.localScale = new Vector3(goalScale, -goalScale, 1f); // Y反転
        }

        void DrawDragMark(Control dragged)
        {
            float s = dragged.DragMarkSize;
            float x0 = input.pointerX;
            float y0 = input.pointerY;
            float x1 = x0 - (s * 0.5f);
            float y1 = y0 - s;
            float x2 = x0 + (s * 0.5f);
            float y2 = y0 - s;
            // 背景
            Renderer.Color = dragged.DragMarkColor;
            Renderer.AddTriangle(x0, y0, x1, y1, x2, y2);
            // 枠
            Renderer.Color = new Color32(255, 255, 255, 255);
            Renderer.AddTriangleFrame(x0, y0, x1, y1, x2, y2, s * 0.125f);
            // テキスト
            if (dragged.DragMarkLetter != '\0')
            {
                Renderer.Color = dragged.DragMarkLetterColor;
                Renderer.AddText(
                    new string(dragged.DragMarkLetter, 1),
                    x0,
                    y0,
                    s * 0.5f,
                    AlignX.Center,
                    AlignY.Center);
            }
        }

        public void OnPointerClick(PointerEventData data)
        {
            UpdatePointer(data.position);
            input.hasJustClicked = true;
        }

        public void OnPointerDown(PointerEventData data)
        {
            UpdatePointer(data.position);
            input.isPointerDown = true;
        }

        public void OnPointerUp(PointerEventData data)
        {
            UpdatePointer(data.position);
            input.isPointerDown = false;
            // TODO: UpdatePerFrameの中で呼びたいなできれば...コールバック類は同じ場所から呼ばれる保証が欲しい
            var dragged = input.draggedControl;
            if (dragged != null)
            {
                if (dragged.OnDragEnd != null)
                {
                    dragged.OnDragEnd();
                }
            }
            input.draggedControl = null;
        }

        public void OnBeginDrag(PointerEventData data)
        {
            UpdatePointer(data.position);
            input.isPointerDown = true;
            input.hasJustDragStarted = true;
        }

        public void OnDrag(PointerEventData data)
        {
            UpdatePointer(data.position);
            input.isPointerDown = true;
        }

        void UpdatePointer(Vector2 screenPosition)
        {
            float x = screenPosition.x;
            float y = screenPosition.y;
            ConvertCoordFromUnityScreen(ref x, ref y);
            input.pointerX = Mathf.Clamp(x, 0f, root.Width);
            input.pointerY = Mathf.Clamp(y, 0f, root.Height);
        }

        // UnityのEventSystemの座標をY下向きの仮想解像度座標系に変換
        public void ConvertCoordFromUnityScreen(ref float x, ref float y)
        {
            var world = eventCamera.ScreenToWorldPoint(new Vector3(x, y, _screenPlaneDistance));
            var local = meshTransform.worldToLocalMatrix.MultiplyPoint(world);
            x = local.x;
            y = local.y;
        }

        public void MoveToTop(Control control)
        {
            root.UnlinkChild(control);
            root.AddChildAsTail(control);
        }
    }
}
