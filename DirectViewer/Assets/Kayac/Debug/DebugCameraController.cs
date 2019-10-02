using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kayac.Debug
{
    public class DebugCameraController : BaseRaycaster, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] Camera myCamera;
        [SerializeField] float rotationSpeed = 1f;

        List<Vector3> tmpPoints;
        Vector2 angle;
        Vector2 prevScreenPosition0;
        Vector2 prevScreenPosition1;
        float prevPointerDistance;
        int pointerId0 = int.MaxValue;
        int pointerId1 = int.MaxValue;
        Vector3 rotationCenter;
        float distance = 1f;

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
            if (myCamera == null)
            {
                return;
            }
            var result = new RaycastResult
            {
                gameObject = gameObject, // 自分
                module = this,
                distance = float.MaxValue,
                worldPosition = rotationCenter,
                worldNormal = -myCamera.transform.forward,
                screenPosition = eventData.position,
                index = resultAppendList.Count,
            };
            resultAppendList.Add(result);

            // 何かに当たるならイベントを取り、何にも当たらないならスルーする
            Vector2 dsp = Vector2.zero;
            if (eventData.pointerId == pointerId0)
            {
                dsp = eventData.position - prevScreenPosition0;
                prevScreenPosition0 = eventData.position;
            }
            else if (eventData.pointerId == pointerId1)
            {
                dsp = eventData.position - prevScreenPosition1;
                prevScreenPosition1 = eventData.position;
            }

            // 1本も触れてない時は何もしない
            if ((pointerId0 == int.MaxValue) &&  (pointerId1 == int.MaxValue))
            {
                return;
            }
            // 2本触れていれば拡大縮小
            if ((pointerId0 != int.MaxValue) && (pointerId1 != int.MaxValue))
            {
                var pointerDistance = (prevScreenPosition0 - prevScreenPosition1).magnitude;
                distance *= prevPointerDistance / pointerDistance;
                prevPointerDistance = pointerDistance;
                var rotation = myCamera.transform.rotation;
                myCamera.transform.position = rotationCenter + (rotation * new Vector3(0f, 0f, -distance));
            }
            else // 1本なら回転
            {
                float scale = (Screen.width > Screen.height) ? (1f / (float)Screen.height) : (1f / (float)Screen.width);
                dsp *= scale * rotationSpeed;
                angle.y += dsp.x;
                angle.x -= dsp.y;
                var rotation = Quaternion.Euler(angle.x, angle.y, 0f);
                myCamera.transform.rotation = rotation;
                myCamera.transform.position = rotationCenter + (rotation * new Vector3(0f, 0f, -distance));
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.pointerId == pointerId0) // すでに触っているものがもう一回くることはありえないはずだが抜ける
            {
                return;
            }
            if (eventData.pointerId == pointerId1) // すでに触っているものがもう一回くることはありえないはずだが抜ける
            {
                return;
            }
            if (pointerId0 == int.MaxValue)
            {
                pointerId0 = eventData.pointerId;
                prevScreenPosition0 = eventData.position;
            }
            else if (pointerId1 == int.MaxValue)
            {
                pointerId1 = eventData.pointerId;
                prevScreenPosition1 = eventData.position;
            }
            // 3本目は無視。本当は古い方を捨てるのが良いと思うが

            // 2本触ってるなら距離を設定
            if ((pointerId0 != int.MaxValue) && (pointerId1 != int.MaxValue))
            {
                prevPointerDistance = (prevScreenPosition0 - prevScreenPosition1).magnitude;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == pointerId0)
            {
                pointerId0 = int.MaxValue;
            }
            if (eventData.pointerId == pointerId1)
            {
                pointerId1 = int.MaxValue;
            }
        }

        public void Focus(IList<Vector3> worldPoints)
        {
            if (myCamera == null)
            {
                return;
            }
            Vector3 position, target;
            CalcCameraPositionToFocus(
                out position,
                out target,
                worldPoints,
                tmpPoints,
                myCamera.fieldOfView,
                myCamera.aspect,
                myCamera.transform);
            rotationCenter = target;
            myCamera.transform.position = position;
            myCamera.transform.LookAt(target);
            distance = (target - position).magnitude;
        }

        static void CalcCameraPositionToFocus(
            out Vector3 positionOut,
            out Vector3 targetOut,
            IList<Vector3> worldPoints,
            List<Vector3> tmpPoints,
            float fieldOfViewY,
            float aspect,
            Transform cameraTransform)
        {
            tmpPoints.Clear();
            // 全点をビュー空間に移動
            var toView = cameraTransform.worldToLocalMatrix;
            for (int i = 0; i < worldPoints.Count; i++)
            {
                tmpPoints.Add(toView.MultiplyPoint3x4(worldPoints[i]));
            }
            // 各種傾き
            var ay1 = Mathf.Tan(fieldOfViewY * 0.5f * Mathf.Deg2Rad);
            var ay0 = -ay1;
            var ax1 = ay1 * aspect;
            var ax0 = -ax1;
            // 最大最小を求めて不要な点を捨てる
            var y0Min = float.MaxValue;
            var y1Max = -float.MaxValue;
            var x0Min = float.MaxValue;
            var x1Max = -float.MaxValue;
            var zMin = float.MaxValue;
            var zMax = -float.MaxValue;
            for (int i = 0; i < tmpPoints.Count; i++)
            {
                var p = tmpPoints[i];
                var by0 = p.y - (ay0 * p.z);
                var by1 = p.y - (ay1 * p.z);
                var bx0 = p.x - (ax0 * p.z);
                var bx1 = p.x - (ax1 * p.z);
                y0Min = Mathf.Min(y0Min, by0);
                y1Max = Mathf.Max(y1Max, by1);
                x0Min = Mathf.Min(x0Min, bx0);
                x1Max = Mathf.Max(x1Max, bx1);
                zMax = Mathf.Max(zMax, p.z);
                zMin = Mathf.Min(zMin, p.z);
            }
            // 求まった2点から位置を計算。Y軸で決めた点とX軸で決めた点が出来る。
            float zy = (y1Max - y0Min) / (ay0 - ay1);
            float y = y0Min + (ay0 * zy);
            float zx = (x1Max - x0Min) / (ax0 - ax1);
            float x = x0Min + (ax0 * zx);
            // より手前の方を選択。x,yはそのまま使う。
            var posInView = new Vector3(x, y, Mathf.Min(zy, zx));
            var targetInView = new Vector3(x, y, (zMax + zMin) * 0.5f);

            // ワールド座標に戻す
            var toWorld = cameraTransform.localToWorldMatrix;
            positionOut = toWorld.MultiplyPoint3x4(posInView);
            targetOut = toWorld.MultiplyPoint3x4(targetInView);
        }

        protected override void Start()
        {
            base.Start();
            tmpPoints = new List<Vector3>();
            if (myCamera == null)
            {
                myCamera = gameObject.GetComponent<Camera>();
            }
        }
    }
}