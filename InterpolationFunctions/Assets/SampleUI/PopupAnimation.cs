using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupAnimation : MonoBehaviour
{
	[SerializeField]
	float _spring = 100f;
	[SerializeField]
	float _openDamper = 14f;
	[SerializeField]
	float _closeDamper = 15f;
	[SerializeField]
	Vector2 _openV0 = new Vector2(10f, 10f);
	[SerializeField]
	Vector2 _closeV0 = new Vector2(-7f, -7f);

	Vector2 _goal;
	Vector2 _velocity;
	float _damper;

	void Awake()
	{
		Close();
		gameObject.transform.localScale = new Vector3(0f, 0f, 1f);
		_velocity = Vector2.zero;
	}

	public void Open()
	{
		_goal = new Vector2(1f, 1f);
		_velocity = _openV0;
		_damper = _openDamper;
	}

	public void Close()
	{
		_goal = new Vector2(0f, 0f);
		_velocity = _closeV0;
		_damper = _closeDamper;
	}

	public void Toggle()
	{
		if (_goal.y > 0f)
		{
			Close();
		}
		else
		{
			Open();
		}
	}

	void Update()
	{
		var transform = gameObject.transform;
		var localScale = transform.localScale;
		var current = new Vector2(localScale.x, localScale.y);
		var a = ((_goal - current) * _spring) - (_velocity * _damper);
		var dt = Time.deltaTime;
		_velocity += a * dt;
		current += _velocity * dt;
		transform.localScale = new Vector3(current.x, current.y, 1f);
	}
}
