using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Moon : MonoBehaviour
{
	[SerializeField] float radius = 3.4743f * 0.5f;
	[SerializeField] int meshDivision = 16;
	[SerializeField] new Rigidbody rigidbody;
	[SerializeField] MeshFilter meshFilter;
	[SerializeField] new SphereCollider collider;

	public Rigidbody Body => rigidbody;
	public Vector3 Position => rigidbody.position;
	public Vector3 Velocity => rigidbody.velocity;
	public float Mass => rigidbody.mass;
	public float Radius => radius;

	public void ManualStart()
	{
		mesh = MeshGenerator.GenerateSphere(meshDivision * 2, meshDivision, radius, Vector2.zero, Vector3.one);
		meshFilter.sharedMesh = mesh;
		collider.radius = radius;
		// 30*86400で380*2PIなので、
		var v = (transform.position.magnitude * Mathf.PI * 2f) / (30f * 24f * 60f * 60f);
		rigidbody.AddForce(new Vector3(v, 0f, 0f), ForceMode.VelocityChange);
	}

	public void ManualUpdate(float deltaTime)
	{

	}

	public void ManualFixedUpdate(float deltaTime, Earth earth, Gift gift)
	{
		var f = Vector3.zero;
		f += Main.Gravity(rigidbody.position, earth.Body.position, rigidbody.mass, earth.Body.mass);
		f += Main.Gravity(rigidbody.position, gift.Body.position, rigidbody.mass, gift.Body.mass);
		rigidbody.AddForce(f);		
	}

	// non public ----
	Mesh mesh;
}
