using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
	[SerializeField] Kayac.PidSettings cameraPidSettings;
	[SerializeField] Transform cameraTransform;
	[SerializeField] Text text;
	[SerializeField] Slider angleSlider;
	[SerializeField] Text angleText;
	[SerializeField] float smoothing = 1f;
	[SerializeField] float cameraDistance = 5f;
	[SerializeField] float cameraLerp = 1f;
	[SerializeField] bool downCamera = false;

	void Start()
	{
		this.walker = gameObject.GetComponentInChildren<Walker>();
		walker.ManualStart();	
		cameraPid = new Kayac.PidController3(cameraPidSettings);
		cameraTargetPosition = walker.Position;
	}

	void FixedUpdate()
	{
		var dt = Time.fixedDeltaTime;
		var angle = angleSlider.value;
		angleText.text = Mathf.Round(angle).ToString();
		walker.ManualFixedUpdate(dt, angle, Vector3.forward, 1f, 4f);

		smoothedV = Vector3.Lerp(smoothedV, walker.Velocity, smoothing * dt);

		var f = cameraPid.Update(cameraTargetPosition, walker.Position, dt);
		cameraVelocity += f * dt;
		cameraTargetPosition += cameraVelocity * dt;

		if (downCamera)
		{
			var lerp = Vector3.Lerp(Vector3.down, Vector3.forward, cameraLerp);
			cameraTransform.rotation = Quaternion.LookRotation(lerp, Vector3.left);
		}
		else
		{
			var lerp = Vector3.Lerp(Vector3.left, Vector3.forward, cameraLerp);
			cameraTransform.rotation = Quaternion.LookRotation(lerp, Vector3.up);
		}
		cameraTransform.position = cameraTargetPosition - cameraTransform.forward * cameraDistance;
		updateTimer -= dt;
		if (updateTimer <= 0f)
		{
//			text.text = (smoothedV.z * 3.6f).ToString("F2") + " km/h";// + "\t" + Time.realtimeSinceStartup.ToString("F2");
			text.text = smoothedV.z.ToString("F2") + " m/s\n" + (cameraTargetPosition.z / Time.time).ToString("F2") + "\t" + Time.time.ToString("F1");
			updateTimer = 1f / smoothing;
		}
	}

	// non public ----
	Vector3 smoothedV;
	Vector3 smoothedP;
	float updateTimer;
	Kayac.PidController3 cameraPid;
	Vector3 cameraVelocity;
	Vector3 cameraTargetPosition;
	Walker walker;
}
