using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level : MonoBehaviour
{
	[SerializeField] Earth earth;
	[SerializeField] Moon moon;
	[SerializeField] Gift gift;

	public Gift Gift => gift;
	public Earth Earth => earth;
	public Moon Moon => moon;

	public void ManualStart(Main main)
	{
		this.main = main;
		earth.ManualStart();
		moon.ManualStart();
		gift.ManualStart(this, moon);
	}

	public void ManualUpdate(float deltaTime)
	{
		earth.ManualUpdate(deltaTime);
		moon.ManualUpdate(deltaTime);
		gift.ManualUpdate(deltaTime);
	}

	public void ManualFixedUpdate(float deltaTime, float guide, Vector3 inputVector)
	{
		earth.ManualFixedUpdate(deltaTime, moon, gift);
		moon.ManualFixedUpdate(deltaTime, earth, gift);
		gift.ManualFixedUpdate(deltaTime, earth, moon, guide, inputVector);
	}

	public void OnGiftArrive(Collision collision)
	{
		main.OnGiftArrive(collision);
	}

	// non public ----
	Main main;
}
