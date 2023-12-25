using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Earth : MonoBehaviour
{
	[SerializeField] float radius = 6.378137f;
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
		// 24時間で360度回るような角速度を与える
		var angularVelocity = Vector3.up;
		angularVelocity *= Mathf.PI * 2f / (24f * 60f * 60f);
		rigidbody.AddTorque(angularVelocity, ForceMode.VelocityChange);
	}

	public void ManualUpdate(float deltaTime)
	{

	}

	public void ManualFixedUpdate(float deltaTime, Moon moon, Gift gift)
	{
		var f = Vector3.zero;
		f += Main.Gravity(rigidbody.position, moon.Body.position, rigidbody.mass, moon.Body.mass);
		f += Main.Gravity(rigidbody.position, gift.Body.position, rigidbody.mass, gift.Body.mass);
		rigidbody.AddForce(f);
	}

	public Vector2 GetCoord(Vector3 worldPoint)
	{
		var lp = transform.InverseTransformPoint(worldPoint);
		var lat = Mathf.Atan(lp.y / Mathf.Sqrt(lp.x * lp.x + lp.z * lp.z));
		var lon = Mathf.Atan2(-lp.x, lp.z);
		return new Vector2(lat, lon) * Mathf.Rad2Deg;
	}

	// non public ----
	Mesh mesh;
}
