using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
	[SerializeField] Kayac.PidSettings cameraPidSettings;
	[SerializeField] Transform cameraTransform;
	[SerializeField] Text text;
	[SerializeField] float smoothing = 1f;
	[SerializeField] float cameraDistance = 5f;
	[SerializeField] bool downCamera = false;

	void Start()
	{
		this.man = gameObject.GetComponentInChildren<Man>();
		man.ManualStart();	
		cameraPid = new Kayac.PidController3(cameraPidSettings);
		cameraTargetPosition = man.Position;
	}

	void FixedUpdate()
	{
		var dt = Time.fixedDeltaTime;
		man.ManualFixedUpdate(dt);

		smoothedV = Vector3.Lerp(smoothedV, man.Velocity, smoothing * dt);

		var f = cameraPid.Update(cameraTargetPosition, man.Position, dt);
		cameraVelocity += f * dt;
		cameraTargetPosition += cameraVelocity * dt;

		if (downCamera)
		{
			cameraTransform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
		}
		else
		{
			cameraTransform.rotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
		}
		cameraTransform.position = cameraTargetPosition - cameraTransform.forward * cameraDistance;
		updateTimer -= dt;
		if (updateTimer <= 0f)
		{
//			text.text = (smoothedV.z * 3.6f).ToString("F2") + " km/h";// + "\t" + Time.realtimeSinceStartup.ToString("F2");
			text.text = smoothedV.z.ToString("F2") + " m/s\n" + (cameraTargetPosition.z / Time.time).ToString("F2");// + "\t" + Time.realtimeSinceStartup.ToString("F2");
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
	Man man;
}
