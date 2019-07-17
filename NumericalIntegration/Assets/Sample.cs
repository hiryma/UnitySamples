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
	RectTransform _semiImplicitEuler;
	[SerializeField]
	RectTransform _verlet;
	[SerializeField]
	Graph _graph;
	[SerializeField]
	float _springStiffness = 0.1f;
	[SerializeField]
	float _springDumper = 0.1f;
	[SerializeField]
	float _timeStep = 1f;

	delegate Vector2 AccelFunc(Vector2 position, Vector2 velocity);
	delegate Vector2 PositionFunc(float t);
	delegate Vector2 VelocityFunc(float t);
	delegate float EnergyFunc(Vector2 position, Vector2 velocity);

	interface IState
	{
		Vector2 position{ get; }
		Vector2 velocity{ get; }
		void Set(Vector2 position, Vector2 velocity, float dt, AccelFunc accelFunc);
		void Update(float dt, AccelFunc accelFunc);
	}
	class EulerState : IState
	{
		public void Set(Vector2 position, Vector2 velocity, float dt, AccelFunc accelFunc)
		{
			this.position = position;
			this.velocity = velocity;
			Update(dt, accelFunc);
		}
		public void Update(float dt, AccelFunc accelFunc)
		{
			var accel = accelFunc(this.position, this.velocity);
			this.position += this.velocity * dt;
			this.velocity += accel * dt;
		}
		public Vector2 position{ get; private set; }
		public Vector2 velocity{ get; private set; }
	}
	class SemiImplicitEulerState : IState
	{
		public void Set(Vector2 position, Vector2 velocity, float dt, AccelFunc accelFunc)
		{
			this.position = position;
			this.velocity = velocity;
			Update(dt, accelFunc);
		}
		public void Update(float dt, AccelFunc accelFunc)
		{
			this.velocity += accelFunc(this.position, this.velocity) * dt;
			this.position += this.velocity * dt;
		}
		public Vector2 position{ get; private set; }
		public Vector2 velocity{ get; private set; }
	}
	class VerletState : IState
	{
		public void Set(Vector2 position, Vector2 velocity, float dt, AccelFunc accelFunc)
		{
			_prevPosition = position;
			this.velocity = velocity;
			var accel = accelFunc(position, velocity);
			this.position = _prevPosition + (velocity * dt) + (accel * (0.5f * dt * dt));
		}
		public void Update(float dt, AccelFunc accelFunc)
		{
			var accel = accelFunc(this.position, this.velocity); // 速度が近似なのでこの加速度計算の速度による項はウソ。
			var newPosition = (this.position * 2f) - _prevPosition + (accel * (dt * dt));
			_prevPosition = this.position;
			this.position = newPosition;
			this.velocity = (this.position - _prevPosition) / dt; // この速度は近似値。正しい計算には次の位置が必要。
		}
		public Vector2 position{ get; private set; }
		public Vector2 _prevPosition;
		public Vector2 velocity{ get; private set; }
	}
	IState _eulerState = new EulerState();
	IState _semiImplicitEulerState = new SemiImplicitEulerState();
	IState _verletState = new VerletState();
	AccelFunc _accelFunc;
	PositionFunc _positionFunc;
	EnergyFunc _energyFunc;
	float _initialEnergy;
	float _time;
	float _globalTime;
	float _prevTimeStep;

	enum FuncType
	{
		Spring,
		SpringDumper,
	}
	FuncType _funcType = FuncType.Spring;
	bool _startRequested;
	bool _showError;

	void Start()
	{
		_startRequested = true;
	}

	void Update()
	{
		if (_prevTimeStep != _timeStep)
		{
			_startRequested = true;
		}
		if (_startRequested)
		{
			_time = 0f;
			_startRequested = false;
			switch (_funcType)
			{
				case FuncType.Spring: StartSpring(); break;
				case FuncType.SpringDumper: StartSpringDumper(); break;
			}
			_initialEnergy = _energyFunc(_verletState.position, _verletState.velocity);
		}
		// 描画位置更新
		_exact.anchoredPosition = _positionFunc(_time);
		if (_eulerState.position.magnitude < 1e10f)
		{
			_euler.anchoredPosition = _eulerState.position;
		}
		if (_semiImplicitEulerState.position.magnitude < 1e10f)
		{
			_semiImplicitEuler.anchoredPosition = _semiImplicitEulerState.position;
		}
		if (_verletState.position.magnitude < 1e10f)
		{
			_verlet.anchoredPosition = _verletState.position;
		}

		// 更新
		var dt = _timeStep;
		_eulerState.Update(dt, _accelFunc);
		_semiImplicitEulerState.Update(dt, _accelFunc);
		_verletState.Update(dt, _accelFunc);
		var t = _globalTime;
		if (_showError)
		{
			_graph.SetYRange(0f, _positionFunc(0f).x * 2f);
			var exact = _positionFunc(_time).x;
			_graph.AddData(t, Mathf.Abs(_eulerState.position.x - exact), 0);
			_graph.AddData(t, Mathf.Abs(_semiImplicitEulerState.position.x - exact), 1);
			_graph.AddData(t, Mathf.Abs(_verletState.position.x - exact), 2);
		}
		else
		{
			_graph.SetYRange(0f, _initialEnergy * 2f);
			var eulerEnergy = _energyFunc(_eulerState.position, _eulerState.velocity);
			var semiImplicitEulerEnergy = _energyFunc(_semiImplicitEulerState.position, _semiImplicitEulerState.velocity);
			var verletEnergy = _energyFunc(_verletState.position, _verletState.velocity);
			_graph.AddData(t, eulerEnergy, 0);
			_graph.AddData(t, semiImplicitEulerEnergy, 1);
			_graph.AddData(t, verletEnergy, 2);
		}
		_graph.SetXEnd(t);

		_time += dt;
		_globalTime += dt;
		_prevTimeStep = _timeStep;
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
		if (GUILayout.Button("Restart"))
		{
			_startRequested = true;
		}
		_showError = GUILayout.Toggle(_showError, "ShowError");
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
		newLog = GUILayout.HorizontalSlider(log, -2f, 2f);
		if (newLog != log)
		{
			_startRequested = true;
			_springDumper = Mathf.Pow(10f, newLog);
		}

		if (_showError)
		{
			var exact = _positionFunc(_time).x;
			GUILayout.Label("euler error: " + Mathf.Abs(_eulerState.position.x - exact) / 300f);
			GUILayout.Label("semi-implicit Euler error: " + Mathf.Abs(_semiImplicitEulerState.position.x - exact) / 300f);
			GUILayout.Label("verlet error: " + Mathf.Abs(_verletState.position.x - exact) / 300f);
		}
		else
		{
			var eulerEnergy = _energyFunc(_eulerState.position, _eulerState.velocity);
			var siEulerEnergy = _energyFunc(_semiImplicitEulerState.position, _semiImplicitEulerState.velocity);
			var verletEnergy = _energyFunc(_verletState.position, _verletState.velocity);
			GUILayout.Label("euler energy: " + (eulerEnergy / _initialEnergy));
			GUILayout.Label("semi-implicit Euler energy: " + (siEulerEnergy / _initialEnergy));
			GUILayout.Label("verlet energy: " + (verletEnergy / _initialEnergy));
		}
	}

	void ResetStates()
	{
		var position = new Vector2(300f, 0f);
		var velocity = Vector2.zero;
		_eulerState.Set(position, velocity, _timeStep, _accelFunc);
		_semiImplicitEulerState.Set(position, velocity, _timeStep, _accelFunc);
		_verletState.Set(position, velocity, _timeStep, _accelFunc);
		_time += _timeStep;
	}

	void StartSpring()
	{
		_accelFunc = AccelFuncSpring;
		_positionFunc = PositionFuncSpring;
		_energyFunc = EnergyFuncSpring;
		ResetStates();
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

	float EnergyFuncSpring(Vector2 position, Vector2 velocity)
	{
		return (0.5f * velocity.sqrMagnitude) + (0.5f * _springStiffness * position.sqrMagnitude);
	}

	void StartSpringDumper()
	{
		_accelFunc = AccelFuncSpringDumper;
		_positionFunc = PositionFuncSpringDumper;
		_energyFunc = EnergyFuncSpring;
		ResetStates();
	}

	Vector2 AccelFuncSpringDumper(Vector2 position, Vector2 velocity)
	{
		return -(position * _springStiffness) - (velocity * _springDumper);
	}

	// 一般解。http://www.cp.cmc.osaka-u.ac.jp/~kikuchi/kougi/mechanics1/gensui.pdf
	Vector2 PositionFuncSpringDumper(float t)
	{
		var w = Mathf.Sqrt(_springStiffness);
		var r = _springDumper / (2f * w);
		var one_r2 = Mathf.Sqrt(1f - (r * r));
		var delta = Mathf.Atan(one_r2 / r);
		var x = 300f / one_r2 * Mathf.Exp(-0.5f * _springDumper * t) * Mathf.Sin((w * one_r2 * t) + delta);
		return new Vector2(x, 0f);
	}
}
