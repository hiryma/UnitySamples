using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gift : MonoBehaviour
{
	[SerializeField] float inputSensitivity = 1f;
	[SerializeField] float torqueFactor = 0.1f;
	[SerializeField] new Rigidbody rigidbody;
	public Rigidbody Body => rigidbody;
	public Vector3 Position => rigidbody.position;
	public Vector3 Velocity => rigidbody.velocity;
	public float Mass => rigidbody.mass;

	public void ManualStart(Level level, Moon moon)
	{
		this.level = level;
		rigidbody.isKinematic = true;
		SetPositionOnMoon(moon);
	}

	public void ManualUpdate(float deltaTime)
	{

	}

	public void ManualFixedUpdate(float deltaTime, Earth earth, Moon moon, float guide, Vector3 inputVector)
	{
		if (rigidbody.isKinematic)
		{
			// 月の位置に合わせる
			SetPositionOnMoon(moon);
		}
		else
		{
			var eg = Main.Gravity(Position, earth.Position, Mass, earth.Mass);
			var mg = Main.Gravity(Position, moon.Position, Mass, moon.Mass);
			var gf = CalcGuideForce(earth.Position, guide);
			var inputForce = inputVector * inputSensitivity;
			rigidbody.AddForce(eg + mg + gf + inputForce);

			var torque = Vector3.Cross(inputForce, rigidbody.velocity) * torqueFactor;
			rigidbody.AddTorque(torque);
		}

//		Debug.Log(eg.magnitude + " " + mg.magnitude + " " + gf.magnitude);
	}

	public void Fire(Vector3 velocity)
	{
		rigidbody.isKinematic = false;
		rigidbody.AddForce(velocity, ForceMode.VelocityChange);
	}

	void OnCollisionEnter(Collision collision)
	{
//Debug.LogWarning("HIT " + collision.gameObject.name);
		var earth = collision.gameObject.GetComponent<Earth>();
		if (earth != null)
		{
//Debug.LogWarning("HIT 2 " + collision.gameObject.name);
			level.OnGiftArrive(collision);
		}
	}

	// non public ----
	Level level;

	Vector3 CalcGuideForce(Vector3 targetPosition, float guide)
	{
		// 現速度と垂直にだけかける
		var d = targetPosition - Position;
		var vd = Velocity.normalized;
		d -= vd * Vector3.Dot(d, vd);
		var f = d.normalized * guide * Mass;
		return f;
	}

	void SetPositionOnMoon(Moon moon)
	{
		rigidbody.position = moon.Position + (Vector3.up * (moon.Radius + 0.0025f));
		transform.position = rigidbody.position;
	}
}
