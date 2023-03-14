using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Kayac
{
    using DebugUi;

    public class DebugUiManager : BaseRaycaster
        , IPointerClickHandler
        , IPointerDownHandler
        , IPointerUpHandler
        , IBeginDragHandler
        , IDragHandler
    {
        [SerializeField] Camera myCamera;
        [SerializeField] Transform scaleOffsetTransform;
        [SerializeField] MeshRenderer meshRenderer;
        [SerializeField] MeshFilter meshFilter;
        [SerializeField] RendererAsset asset;
        [SerializeField] int referenceScreenWidth = 1280;
        [SerializeField] int referenceScreenHeight = 720;
        [SerializeField] float screenPlaneDistance = 1f;
        [SerializeField] int triangleCapacity = 8192;
        [SerializeField] bool autoStart = true;
        [SerializeField] bool autoUpdate = true;

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
        public bool SafeAreaVisualizationEnabled { get; set; }

        public override Camera eventCamera
        {
            get
            {
                return myCamera;
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
                    distance = screenPlaneDistance,
                    worldPosition = eventCamera.transform.position + (eventCamera.transform.forward * screenPlaneDistance),
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

        // GameObjectをシーンの中に置いておいて自動初期化したい場合はこちら
        protected override void Start()
        {
            base.Start();
            if (autoStart)
            {
                ManualStart(myCamera);
            }
        }

        void Update()
        {
            if (autoUpdate)
            {
                ManualUpdate(Time.deltaTime);
            }
        }

        protected override void OnDestroy()
        {
            if (rendererIsMine)
            {
                Renderer.Dispose();
            }
            base.OnDestroy();
        }

        // prefabから動的生成したい場合はこちら
        // 描画するカメラと、もし自前でRenderer2Dを持っていて共用したい場合は渡す
        public void ManualStart(
            Camera cameraOverride = null,
            Renderer2D rendererOverride = null)
        {
            if (cameraOverride != null)
            {
                myCamera = cameraOverride;
            }
            if (rendererOverride != null)
            {
                Renderer = rendererOverride;
            }
            else
            {
                Renderer = new Renderer2D(
                    asset,
                    meshRenderer,
                    meshFilter,
                    triangleCapacity);
                rendererIsMine = true;
            }
            meshRenderer.sortingOrder = 32767;
            meshFilter.mesh.bounds = new Bounds(
                new Vector3(referenceScreenWidth * 0.5f, referenceScreenHeight * 0.5f, 0f),
                new Vector3(referenceScreenWidth, referenceScreenHeight, 0f));

            UnityEngine.Debug.LogFormat(
                "DebugUiManager. physicalResolution {0}x{1}\n  safeArea:{2}",
                Screen.width,
                Screen.height,
                GetSafeArea());
            SafeAreaVisualizationEnabled = true;
            Scale = 1f;
            root = new Container(name: "RootContainer");
            input = new Input();
            InputEnabled = true;
            // 初回サイズ決定
            UpdateTransform();

            // チェック
            if (myCamera.GetComponent<PhysicsRaycaster>() == null)
            {
                UnityEngine.Debug.LogError("Attached Camera doesn't have PhysicsRaycaster.");
            }
            if (EventSystem.current == null)
            {
                UnityEngine.Debug.LogError("any EventSystem doesn't exists.");
            }
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
            UnityEngine.Debug.Assert(Renderer != null, "call ManualStart()");
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

        // UnityのEventSystemの座標をY下向きの仮想解像度座標系に変換
        public void ConvertCoordFromUnityScreen(ref float x, ref float y)
        {
            var world = eventCamera.ScreenToWorldPoint(new Vector3(x, y, screenPlaneDistance));
            var local = meshRenderer.transform.worldToLocalMatrix.MultiplyPoint(world);
            x = local.x;
            y = local.y;
        }

        public void MoveToTop(Control control)
        {
            root.UnlinkChild(control);
            root.AddChildAsTail(control);
        }

        // non-public ---------------
        Container root;
        Input input;
        bool rendererIsMine;

        void UpdatePointer(Vector2 screenPosition)
        {
            float x = screenPosition.x;
            float y = screenPosition.y;
            ConvertCoordFromUnityScreen(ref x, ref y);
            input.pointerX = Mathf.Clamp(x, 0f, root.Width);
            input.pointerY = Mathf.Clamp(y, 0f, root.Height);
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
                halfHeight = screenPlaneDistance * Mathf.Tan(eventCamera.fieldOfView * Mathf.Deg2Rad * 0.5f);
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

            meshRenderer.transform.localPosition = new Vector3(offsetX, offsetY, 0f);

            scaleOffsetTransform.localPosition = new Vector3(0f, 0f, screenPlaneDistance);
            goalScale *= Scale;
            scaleOffsetTransform.localScale = new Vector3(goalScale, -goalScale, 1f); // Y反転

            transform.position = myCamera.transform.position;
            transform.rotation = myCamera.transform.rotation;
            transform.localScale = myCamera.transform.localScale;
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
    }
}
