using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
	[SerializeField] Kayac.PidSettings cameraPidSettings;
	[SerializeField] Transform cameraTransform;
	[SerializeField] Man man;
	[SerializeField] Text text;
	[SerializeField] float smoothing = 1f;

	void Start()
	{
		man.ManualStart();	
		cameraPid = new Kayac.PidController3(cameraPidSettings);
	}

	void FixedUpdate()
	{
		var dt = Time.fixedDeltaTime;
		man.ManualFixedUpdate(dt);

		smoothedV = Vector3.Lerp(smoothedV, man.Velocity, smoothing * dt);
		var f = cameraPid.Update(cameraTransform.position, man.Position, dt);
		cameraVelocity += f * dt;
		var p = cameraTransform.position + cameraVelocity * dt;
		p.x = man.Position.x + 5f;
		cameraTransform.position = p;

		updateTimer -= dt;
		if (updateTimer <= 0f)
		{
//			text.text = (smoothedV.z * 3.6f).ToString("F2") + " km/h";// + "\t" + Time.realtimeSinceStartup.ToString("F2");
			text.text = smoothedV.z.ToString("F2") + " m/s";// + "\t" + Time.realtimeSinceStartup.ToString("F2");
			updateTimer = 1f / smoothing;
		}
	}

	// non public ----
	Vector3 smoothedV;
	Vector3 smoothedP;
	float updateTimer;
	Kayac.PidController3 cameraPid;
	Vector3 cameraVelocity;
}
