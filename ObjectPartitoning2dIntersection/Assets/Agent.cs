using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{
	[SerializeField] GameObject physicsRoot;
	[SerializeField] GameObject nonPhysicsRoot;
	[SerializeField] new Rigidbody2D rigidbody;

	public float Radius { get => radius; }

	public void ManualStart(bool physicsEnabled, Vector2 position, float scale)
	{
		this.radius = scale * 0.5f;
		this.physicsEnabled = physicsEnabled;
		physicsRoot.SetActive(physicsEnabled);
		nonPhysicsRoot.SetActive(!physicsEnabled);

		transform.localPosition = new Vector3(position.x, position.y, 0f);
		transform.localScale = new Vector3(scale, scale, 1f);
	}

	public void ManualFixedUpdate(float deltaTime, float centeringForce, float drag, Vector2 force)
	{
		if (physicsEnabled)
		{
			var p = rigidbody.worldCenterOfMass;

			force += -LimitLength(p, centeringForce);
			force -= rigidbody.velocity * drag;
			rigidbody.AddForce(force);
		}
		else
		{
			var p3 = transform.position;
			var p = new Vector2(p3.x, p3.y);

			force += -LimitLength(p, centeringForce);
			force -= velocity * drag;

			velocity += force * deltaTime;
			p += velocity * deltaTime;
			transform.position = new Vector3(p.x, p.y, 0f);
		}
	}

	public void ManualUpdate(float deltaTime)
	{

	}

	// non public ----
	bool physicsEnabled;
	float radius;
	Vector2 velocity;

	Vector2 LimitLength(Vector2 v, float l)
	{
		var vl = v.magnitude;
		if (vl > l)
		{
			v *= (l / vl);
		}
		return v;
	}
}
