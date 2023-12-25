using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Main : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	[SerializeField] float cameraSpeed = 4f;
	[SerializeField] float maxTimeScale = 3600f;
	[SerializeField] float landingTimeScale = 360f;
	[SerializeField] float timeAccel = 0.5f;
	[SerializeField] float timeDecel = 4f;
	[SerializeField] float fireSpeed = 0.005f;
	[SerializeField] float guide = 1f;
	[SerializeField] float flyingTimeLimit = 30f;
	[SerializeField] float readyCameraAngleX = 1f;
	[SerializeField] float readyCameraForward = 0.1f;
	[SerializeField] float readyCameraUp = 0.1f;
	[SerializeField] float flyingCameraDistance = 0.001f;
	[SerializeField] float resultCameraDistance = 15f;
	[SerializeField] float resultCameraBlend = 0.5f;
	[SerializeField] float explosionGlowSpeed = 1f;
	[SerializeField] Camera mainCamera;
	[SerializeField] Level levelPrefab;
	[SerializeField] UnityEngine.UI.Text resultText;
	[SerializeField] GameObject explosion;

	void Start()
	{
		Reset();
	}

	void Update()
	{
		var dt = Time.deltaTime;
		phaseTime += dt;

		if (phase == Phase.Ready)
		{
			if (!prevPointerDown && pointerDown)
			{
				var v = -level.Moon.Velocity; // 月速度を相殺
				v += (level.Earth.Position - level.Gift.Position).normalized * fireSpeed;
				level.Gift.Fire(v);
				ChangePhase(Phase.Flying);
			}
		}
		else if (phase == Phase.Flying)
		{
			var distance = (level.Gift.Position - level.Earth.Position).magnitude - level.Earth.Radius;
			if (distance >= (level.Earth.Radius * 10f))
			{
				timeScale = Mathf.Lerp(timeScale, maxTimeScale, timeAccel * dt);
			}
			else
			{
				timeScale = Mathf.Lerp(timeScale, landingTimeScale, timeDecel * dt);
			}

			// 届く見込みがなくなったらtap to
			if (phaseTime >= flyingTimeLimit)
			{
				resultText.text = "宇宙の塵となった";
				if (!prevPointerDown && pointerDown)
				{
					Reset();
				}
			}
			else
			{
				resultText.text = "あと" + (distance * 1000f).ToString("N0") + "km";
			}
		}
		else if (phase == Phase.Result)
		{
			timeScale = Mathf.Lerp(timeScale, landingTimeScale, timeDecel * dt);

			var str = "到着!\n";
			var coord = hitCoord.GetValueOrDefault();
			if (coord.x < 0f)
			{
				str += "南緯";
			}
			else
			{
				str += "北緯";
			}
			str += Mathf.Abs(coord.x).ToString("F0") + "度";

			if (coord.y < 0f)
			{
				str += "西経";
			}
			else
			{
				str += "東経";
			}
			str += Mathf.Abs(coord.y).ToString("F0") + "度";

			resultText.text = str;
			if (!prevPointerDown && pointerDown)
			{
				Reset();
			}
		}
//Debug.Log("timeScale: " + timeScale);

		level.ManualUpdate(dt);
		prevPointerDown = pointerDown;
	}

	void FixedUpdate()
	{
		var dt = Time.fixedDeltaTime * timeScale;
		var inputDelta = Vector2.zero;

		if (pointerDown)
		{
			var delta = pointer - downPointer;
			var vMin = Mathf.Min(Screen.width, Screen.height);
			inputDelta = delta / vMin;
//Debug.Log(pointer + " " + downPointer + " " + vMin + " " + inputDelta);
		}

		var inputVector = mainCamera.transform.TransformVector(new Vector3(inputDelta.x, inputDelta.y, 0f));
//Debug.Log(inputVector.ToString("F4") + " " + inputDelta.ToString("F4"));
		if (phase == Phase.Ready)
		{
			UpdateCameraGoalReady();
			if (!prevPointerDown && pointerDown)
			{
				level.Gift.Fire((level.Earth.transform.position - level.Gift.transform.position).normalized * 0.005f);
				phase = Phase.Flying;
			}
		}
		else if (phase == Phase.Flying)
		{
			UpdateCameraGoalFlying();
			hitEarthLocalVelocity = level.Earth.transform.InverseTransformVector(level.Gift.Velocity);
//Debug.LogError(level.Gift.Velocity.ToString("F5"));
		}
		else if (phase == Phase.Result)
		{
			var exVolume = explosion.transform.localScale.x;
			exVolume = Mathf.Pow(exVolume, 3f);
			exVolume += dt * explosionGlowSpeed;
			var exRad = Mathf.Pow(exVolume, 1f / 3f);
			explosion.transform.localScale = new Vector3(exRad, exRad, exRad);
			explosion.transform.position = level.Earth.transform.TransformPoint(hitEarthLocalPoint);

			UpdateCameraGoalResult();
			UpdateCamera(dt);
		}

		if (level != null)
		{
			level.ManualFixedUpdate(dt, guide, inputVector);

			UpdateCamera(dt);
//Debug.Log("UPDATE: " + mainCamera.transform.position + " " + mainCamera.transform.rotation);

			Physics.Simulate(dt); // 単位を時にする
		}
	}

	public static Vector3 Gravity(Vector3 p0, Vector3 p1, float m0, float m1)
	{
		var g = 6.67408e-11f; // m^3 kg^-1 s^-2。長さがMmなので18乗を乗じ、質量がZgなので18乗を除算。結果このままになる。
		var d = p1 - p0;
		var r = d.magnitude;
		var f = d.normalized * (g * m0 * m1 / (r * r));
		return f;
	}

	public void OnPointerDown(PointerEventData e)
	{
		pointerDown = true;
		pointer = downPointer = e.position;
	}

	public void OnPointerUp(PointerEventData e)
	{
		pointerDown = false;
	}

	public void OnBeginDrag(PointerEventData e)
	{
//Debug.Log("AA");
	}

	public void OnDrag(PointerEventData e)
	{
		pointer = e.position;
//Debug.Log("AA " + pointer + " " + downPointer);
	}

	public void OnEndDrag(PointerEventData e)
	{
//Debug.Log("AA 3");
	}

	public void OnGiftArrive(Collision collision)
	{
//Debug.LogWarning("OnGiftArrive ");
		if (!hitCoord.HasValue)
		{
//Debug.LogWarning("OnGiftArrive 2 ");
			var p = collision.GetContact(0).point;
			hitCoord = level.Earth.GetCoord(p);
			hitEarthLocalPoint = level.Earth.transform.InverseTransformPoint(p);
			explosion.SetActive(true);

			ChangePhase(Phase.Result);
//Debug.LogWarning("OnGiftArrive 2 : " + phase);
		}
	}


	// non public ----
	enum Phase
	{
		Ready,
		Flying,
		Result,
	}
	Phase phase;
	Level level;
	bool pointerDown;
	bool prevPointerDown;
	Vector2 pointer;
	Vector2 downPointer;
	float timeScale;
	float phaseTime;
	Quaternion cameraGoalRotation;
	Vector3 cameraGoalPosition;
	Vector3 hitEarthLocalPoint;
	Vector3 hitEarthLocalVelocity;
	Vector2? hitCoord;
	float approachingSpeed;

	void ChangePhase(Phase phase)
	{
		this.phase = phase;
		phaseTime = 0f;
	}

	void Reset()
	{
		explosion.SetActive(false);
		ChangePhase(Phase.Ready);
		if (level != null)
		{
			Destroy(level.gameObject);
			level = null;
		}

		level = Instantiate(levelPrefab, transform, false);
		level.ManualStart(this);
		timeScale = 1f;
		hitCoord = null;
		UpdateCameraGoalReady();
		UpdateCamera(float.MaxValue);
//Debug.Log("RESET: " + mainCamera.transform.position + " " + mainCamera.transform.rotation);
		resultText.text = "";
	}

	void UpdateCameraGoalReady()
	{
		var gp = level.Gift.Position;
		var mp = level.Moon.Position;
		var ep = level.Earth.Position;
		var up = (gp - mp).normalized;
		var forward = (ep - gp).normalized;
//Debug.Log(gp.ToString("F4") + " " + mp.ToString("F4") + " " + ep.ToString("F4"));
		cameraGoalPosition = gp - (forward * readyCameraForward) + (up * readyCameraUp);
		var q = Quaternion.LookRotation(ep - cameraGoalPosition, up);
		q = Quaternion.AngleAxis(readyCameraAngleX, Vector3.right) * q;
		cameraGoalRotation = q;
//Debug.Log("cameraGoalRotation: " + cameraGoalRotation + "\t" + cameraGoalPosition);
	}

	void UpdateCameraGoalFlying()
	{
		var goalQ = mainCamera.transform.rotation;
		var goalP = mainCamera.transform.position;
		var giftP = level.Gift.Position;
///		cameraGoalRotation = Quaternion.LookRotation(level.Earth.Position - giftP, Vector3.up);
		cameraGoalRotation = Quaternion.LookRotation(level.Gift.Velocity, mainCamera.transform.up);
//		cameraGoalPosition = giftP - (level.Earth.Position - giftP).normalized * flyingCameraDistance;
		cameraGoalPosition = giftP - (level.Gift.Velocity).normalized * flyingCameraDistance;
	}

	void UpdateCameraGoalResult()
	{
//Debug.LogWarning("lT:" + hitEarthLocalPoint + " lV:" + hitEarthLocalVelocity.ToString("F3"));
		var target = level.Earth.transform.TransformPoint(hitEarthLocalPoint);
		var entryVelocity = level.Earth.transform.TransformVector(hitEarthLocalVelocity).normalized;
		var normal = (target - level.Earth.Position).normalized;
		var right = Vector3.Cross(normal, entryVelocity).normalized;
		var tangent = Vector3.Cross(right, normal).normalized;
		var forward = Vector3.Lerp(tangent, -normal, resultCameraBlend).normalized;
//Debug.LogWarning("T:" + target + " V:" + entryVelocity.ToString("F3") + " N:" + normal + " R:" + right + " T:" + tangent + " F:" + forward);
		cameraGoalPosition = target - (forward * resultCameraDistance);
		cameraGoalRotation = Quaternion.LookRotation(forward, normal);

		UpdateCamera(float.MaxValue);
	}

	void UpdateCamera(float dt)
	{
		mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, cameraGoalPosition, cameraSpeed * dt);
		mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, cameraGoalRotation, cameraSpeed * dt);
	}
}
