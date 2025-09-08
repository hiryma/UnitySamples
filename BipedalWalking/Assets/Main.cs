using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
	[SerializeField] Transform cameraTransform;
	[SerializeField] Man man;
	[SerializeField] Text text;

	void Start()
	{
		man.ManualStart();	
	}

	void FixedUpdate()
	{
		var dt = Time.fixedDeltaTime;
		man.ManualFixedUpdate(dt);

		var p = cameraTransform.position;
		p.z = man.Position.z;
		cameraTransform.position = p;		

		smoothedVz = Mathf.Lerp(smoothedVz, man.VelocityZ, 1f * dt);

		updateTimer -= dt;
		if (updateTimer <= 0f)
		{
			text.text = smoothedVz.ToString("F2");
			updateTimer = 1f;
		}
	}

	// non public ----
	float smoothedVz;
	float updateTimer;
}
