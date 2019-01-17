using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Sample : MonoBehaviour
{
	[SerializeField]
	RectTransform _exact;
	[SerializeField]
	RectTransform _euler;
	[SerializeField]
	RectTransform _fbEuler;
	[SerializeField]
	RectTransform _verlet;

	float _springStiffness = 1f;
	float _springDumper = 1f;

	delegate Vector2 AccelFunc(Vector2 position, Vector2 velocity);
	delegate Vector2 PositionFunc(float t);
	delegate Vector2 VelocityFunc(float t);
	delegate float EnergyFunc(Vector2 position, Vector2 velocity);

	class EulerState
	{
		public void Set(Vector2 position, Vector2 velocity)
		{
			_position = position;
			_velocity = velocity;
		}
		public void Update(float dt, AccelFunc accelFunc)
		{
			_position += _velocity * dt;
			_velocity += accelFunc(_position, _velocity) * dt;
		}
		public Vector2 _position;
		public Vector2 _velocity;
	}
	class FbEulerState
	{
		public void Set(Vector2 position, Vector2 velocity)
		{
			_position = position;
			_velocity = velocity;
		}
		public void Update(float dt, AccelFunc accelFunc)
		{
			_velocity += accelFunc(_position, _velocity) * dt;
			_position += _velocity * dt;
		}
		public Vector2 _position;
		public Vector2 _velocity;
	}

	EulerState _eulerState = new EulerState();
	FbEulerState _fbEulerState = new FbEulerState();
	AccelFunc _accelFunc;
	PositionFunc _positionFunc;
	VelocityFunc _velocityFunc;
	EnergyFunc _energyFunc;

	enum FuncType
	{
		Spring,
		SpringDumper,
	}
	FuncType _funcType = FuncType.Spring;
	bool _startRequested;
	float _time = 0;

	void Start()
	{
		_startRequested = true;
	}

	void FixedUpdate()
	{
		if (_startRequested)
		{
			_time = 0f;
			_startRequested = false;
			switch (_funcType)
			{
				case FuncType.Spring: StartSpring(); break;
				case FuncType.SpringDumper: StartSpringDumper(); break;
			}
		}
		var dt = Time.fixedDeltaTime;
		_eulerState.Update(dt, _accelFunc);
		_euler.anchoredPosition = _eulerState._position;
		_fbEulerState.Update(dt, _accelFunc);
		_fbEuler.anchoredPosition = _fbEulerState._position;
		_time += dt;
		_exact.anchoredPosition = _positionFunc(_time);
	}

	void OnGUI()
	{
		var enumNames = System.Enum.GetNames(typeof(FuncType));
		var newFuncType = (FuncType)GUILayout.SelectionGrid((int)_funcType, enumNames, enumNames.Length);
		if (newFuncType != _funcType)
		{
			_startRequested = true;
			_funcType = newFuncType;
		}
		GUILayout.Label("spring stiffness: " + _springStiffness.ToString("N4"));
		var log = Mathf.Log10(_springStiffness);
		var newLog = GUILayout.HorizontalSlider(log, -3f, 4f);
		if (newLog != log)
		{
			_startRequested = true;
			_springStiffness = Mathf.Pow(10f, newLog);
		}
		GUILayout.Label("spring dumper: " + _springDumper.ToString("N4"));
		log = Mathf.Log10(_springDumper);
		newLog = GUILayout.HorizontalSlider(log, -3f, 4f);
		if (newLog != log)
		{
			_startRequested = true;
			_springDumper = Mathf.Pow(10f, newLog);
		}

		if (_energyFunc != null)
		{
			GUILayout.Label("ExactEnergy: " + _energyFunc(_positionFunc(_time), _velocityFunc(_time)));
			GUILayout.Label("EulerEnergy: " + _energyFunc(_eulerState._position, _eulerState._velocity));
			GUILayout.Label("FbEulerEnergy: " + _energyFunc(_fbEulerState._position, _fbEulerState._velocity));
		}
	}

	void StartSpring()
	{
		var position = new Vector2(300f, 0f);
		var velocity = Vector2.zero;
		_eulerState.Set(position, velocity);
		_fbEulerState.Set(position, velocity);
		_accelFunc = AccelFuncSpring;
		_positionFunc = PositionFuncSpring;
		_velocityFunc = VelocityFuncSpring;
		_energyFunc = EnergyFuncSpring;
	}

	Vector2 AccelFuncSpring(Vector2 position, Vector2 velocity)
	{
		return -position * _springStiffness;
	}

	Vector2 PositionFuncSpring(float t)
	{
		var w = Mathf.Sqrt(_springStiffness);
		return new Vector2(300f * Mathf.Cos(w * t), 0f);
	}

	Vector2 VelocityFuncSpring(float t)
	{
		var w = Mathf.Sqrt(_springStiffness);
		return new Vector2(-300f * w * Mathf.Sin(w * t), 0f);
	}

	float EnergyFuncSpring(Vector2 position, Vector2 velocity)
	{
		return (0.5f * velocity.sqrMagnitude) + (0.5f * _springStiffness * position.sqrMagnitude);
	}

	void StartSpringDumper()
	{
		var position = new Vector2(300f, 0f);
		var velocity = Vector2.zero;
		_eulerState.Set(position, velocity);
		_fbEulerState.Set(position, velocity);
		_accelFunc = AccelFuncSpringDumper;
		_positionFunc = PositionFuncSpringDumper;
		_velocityFunc = VelocityFuncSpringDumper;
		_energyFunc = EnergyFuncSpring;
	}

	Vector2 AccelFuncSpringDumper(Vector2 position, Vector2 velocity)
	{
		return -(position * _springStiffness) - (velocity * _springDumper);
	}

	Vector2 PositionFuncSpringDumper(float t)
	{
		var w = Mathf.Sqrt(_springStiffness);
		return new Vector2(300f * Mathf.Cos(w * t), 0f);
	}

	Vector2 VelocityFuncSpringDumper(float t)
	{
		var w = Mathf.Sqrt(_springStiffness);
		return new Vector2(-300f * w * Mathf.Sin(w * t), 0f);
	}
}
