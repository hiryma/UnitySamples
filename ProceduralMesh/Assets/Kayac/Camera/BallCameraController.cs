using UnityEngine;
using UnityEngine.EventSystems;
using Q = Kayac.QuaternionHelper;

namespace Kayac
{
	public class BallCameraController :
		MonoBehaviour,
		IPointerDownHandler,
		IPointerUpHandler,
		IDragHandler
	{
		// serialize fields --------------
		[SerializeField] Camera attachedCamera;
		[SerializeField] SphereCollider sphereCollider;
		[SerializeField] Transform centerTransform;
		[SerializeField] float accel = 10f;
		[SerializeField] float drag = 1f;
		[SerializeField] float distance = 10f;
		[SerializeField] float rollMargin = 0.2f;

		// public -------------
		public float Accel { get { return accel; } set { accel = value; } }
		public float Drag { get { return drag; } set { drag = value; } }
		public float Distance { get { return distance; } set { distance = value; } }
		public SphereCollider Collider
		{
			set
			{
				sphereCollider = value;
			}
		}

		// event system handlers --------------
		public void OnPointerDown(PointerEventData data)
		{
			angularVelocity = Vector3.zero;
			downScreenPoint = currentScreenPoint = prevScreenPoint = data.position;
			pointerDown = true;
		}

		public void OnPointerUp(PointerEventData data)
		{
			pointerDown = false;
		}

		public void OnDrag(PointerEventData data)
		{
			currentScreenPoint = data.position;
		}

		// monobehaviour event functions ----------
		void Start()
		{
			orientation = Quaternion.identity;
		}

		void Update()
		{
			if (pointerDown)
			{
				Rotate();
			}
			float dt = Time.deltaTime;
			angularVelocity *= (1f - (dt * Drag));
			orientation = Q.Integrate(orientation, angularVelocity, dt);
			var vw = Q.Transform(orientation, new Vector3(0f, 0f, -1f));
			var ow = Vector3.zero;
			if (centerTransform != null)
			{
				ow = centerTransform.position;
			}
			attachedCamera.transform.position = ow + (vw * distance);
			attachedCamera.transform.rotation = orientation;
		}

		// non-public ---------------
		Quaternion orientation;
		Vector3 angularVelocity;
		Vector2 prevScreenPoint;
		Vector2 currentScreenPoint;
		Vector2 downScreenPoint;
		bool pointerDown;

		Vector3 CalcPointOnSphere(
			Vector2 screenSphereCenter,
			Vector2 screenPoint,
			float screenSphereRadius)
		{
			var ret = new Vector3(
				screenPoint.x - screenSphereCenter.x,
				screenPoint.y - screenSphereCenter.y,
				0f);
			ret /= screenSphereRadius;
			var sqrMagnitude = (ret.x * ret.x) + (ret.y * ret.y);
			if (sqrMagnitude > 1f)
			{
				ret /= Mathf.Sqrt(sqrMagnitude);
				sqrMagnitude = 1f;
			}
			var sqZ = Mathf.Max(0f, 1f - sqrMagnitude); // 誤差でマイナスに落ちてnanになるのを防ぐ
			ret.z = -Mathf.Sqrt(sqZ);
			return ret;
		}

		float CalcSphereScreenRadius(Vector3 worldSphereCenter)
		{
			var camToSphere = worldSphereCenter - attachedCamera.transform.position;
			var sphereZ = Vector3.Dot(attachedCamera.transform.forward, camToSphere);
			var screenHeight = sphereZ * Mathf.Tan(attachedCamera.fieldOfView * Mathf.Deg2Rad * 0.5f) * 2f;
			var sphereRadius = sphereCollider.radius * (1f - rollMargin);
			var viewportHeight = attachedCamera.rect.height;
			if (attachedCamera.activeTexture != null)
			{
				viewportHeight *= (float)attachedCamera.activeTexture.height;
			}
			else
			{
				viewportHeight *= (float)Screen.height;
			}
			var screenRadius = sphereRadius * viewportHeight / screenHeight;
			return screenRadius;
		}

		void Rotate()
		{
			if (currentScreenPoint == prevScreenPoint)
			{
				angularVelocity = Vector3.zero;
				return;
			}
			var worldSphereCenter = sphereCollider.transform.position;
			// 球のスクリーン上の半径を算出
			var screenSphereRadius = CalcSphereScreenRadius(worldSphereCenter);
			// 球中心のスクリン座標を算出
			var screenSphereCenter = attachedCamera.WorldToScreenPoint(worldSphereCenter);
			// Down時の点と、現在の点の球面上座標を算出
			var downPointOnSphere = CalcPointOnSphere(
				screenSphereCenter,
				downScreenPoint,
				screenSphereRadius);
			var currentPointOnSphere = CalcPointOnSphere(
				screenSphereCenter,
				currentScreenPoint,
				screenSphereRadius);
			// 球のでっぱった所に触れたら、それを最初に触った点と置換する
			if (currentPointOnSphere.z < downPointOnSphere.z)
			{
				downScreenPoint = currentScreenPoint;
				downPointOnSphere = currentPointOnSphere;
			}

			var screenPointDelta = currentScreenPoint - prevScreenPoint;
			screenPointDelta *= Accel / screenSphereRadius;
			angularVelocity = Vector3.Cross(screenPointDelta, downPointOnSphere);
			prevScreenPoint = currentScreenPoint;
		}
	}
}