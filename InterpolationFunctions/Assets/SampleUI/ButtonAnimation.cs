using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class ButtonAnimation : MonoBehaviour
	, IPointerExitHandler
	, IPointerDownHandler
	, IPointerUpHandler
{
	[SerializeField]
	float _spring = 100f;
	[SerializeField]
	float _damper = 15f;
	[SerializeField]
	float _fireV0 = 15f;
	[SerializeField]
	float _downGoalScale = 0.8f;
	[SerializeField]
	UnityEvent _onClick;

	float _velocity;
	float _goal = 1f;
	bool _down;

	public void OnPointerExit(PointerEventData data)
	{
		_down = false;
		StartRelax();
	}

	public void OnPointerDown(PointerEventData data)
	{
		_down = true;
		StartDown();
	}

	public void OnPointerUp(PointerEventData data)
	{
		if (_down)
		{
			StartFire();
			if (_onClick != null)
			{
				_onClick.Invoke();
			}
		}
		_down = false;
	}

	void StartDown()
	{
		_goal = _downGoalScale;
	}

	void StartRelax()
	{
		_goal = 1f;
	}

	void StartFire()
	{
		_goal = 1f;
		_velocity = _fireV0;
	}

	void Update()
	{
		var localScale = gameObject.transform.localScale;
		var current = localScale.x;
		var a = ((_goal - current) * _spring) - (_velocity * _damper);
		var dt = Time.deltaTime;
		_velocity += a * dt;
		current += _velocity * dt;
		gameObject.transform.localScale = new Vector3(current, current, 1f);
	}
}
