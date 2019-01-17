using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sample : MonoBehaviour
{
	[SerializeField]
	RectTransform _transform;
	[SerializeField]
	Graph _graph;

	struct State
	{
		public State(Vector2 position, float rotation, float scale, float time)
		{
			this.position = position;
			this.rotation = rotation;
			this.scale = scale;
			this.time = time;
		}

		public Vector2 position;
		public float rotation;
		public float scale;
		public float time;
	}

	State _state;
	State _velocity; // SpringDumper用
	bool _playing;
	float _speed = 1f;
	bool _positionEnabled = true;
	bool _rotationEnabled = true;
	bool _scaleEnabled = true;
	float _exponentialCoefficient = 8f;
	float _springCoefficient = 40f;
	float _dumperCoefficient = 20f;
	bool _popup;

	enum FunctionType
	{
		Linear,
		QuadraticBeginV0,
		QuadraticEndV0,
		CubicBoundaryV0,
		Exponential,
		SpringDumper,
	}
	FunctionType _functionType = FunctionType.Linear;
	State _key0 = new State(new Vector2(300f, 150f), 0f, 0f, 0f);
	State _key1 = new State(new Vector2(-250f, -100f), 1050f, 1.25f, 2f);
	State _key2 = new State(new Vector2(250f, -200f), 1120f, 0.6f, 4f);
	bool _nextIsKey2;
	State _currentKey;
	State _nextKey;
	string[] _toolbarItemNames;
	State _key0popup = new State(new Vector2(-400f, -300f), 0f, 0f, 0f);
	State _key1popup = new State(new Vector2(-100f, -50f), 0f, 1f, 0.4f);
	State _key2popup = new State(new Vector2(-400f, -300f), 0f, 0.0f, 0.8f);

	void Start()
	{
		var enumNames = System.Enum.GetNames(typeof(FunctionType));
		_toolbarItemNames = new string[enumNames.Length];
		_nextIsKey2 = false;
		for (int i = 0; i < enumNames.Length; i++)
		{
			_toolbarItemNames[i] = enumNames[i];
		}
		_velocity = new State(); // 0初期化
		if (_popup)
		{
			_state = _currentKey = _key0popup;
			_nextKey = _key1popup;
		}
		else
		{
			_state = _currentKey = _key0;
			_nextKey = _key1;
		}
		_playing = true;
	}

	void Update()
	{
		float dt = Time.deltaTime * _speed;
		if (_playing)
		{
			_state.time += dt;
		}
		switch (_functionType)
		{
			case FunctionType.Linear:
				SetLinear(ref _currentKey, ref _nextKey);
				break;
			case FunctionType.QuadraticBeginV0:
				SetQuadraticBeginV0(ref _currentKey, ref _nextKey);
				break;
			case FunctionType.QuadraticEndV0:
				SetQuadraticEndV0(ref _currentKey, ref _nextKey);
				break;
			case FunctionType.CubicBoundaryV0:
				SetCubicBoundaryV0(ref _currentKey, ref _nextKey);
				break;
			case FunctionType.Exponential:
				UpdateExponential(ref _nextKey, dt, _exponentialCoefficient);
				break;
			case FunctionType.SpringDumper:
				UpdateSpringDumper(ref _nextKey, dt, _springCoefficient, _dumperCoefficient);
				break;
		}

		if (_positionEnabled)
		{
			_transform.anchoredPosition = _state.position;
		}
		if (_rotationEnabled)
		{
			_transform.localRotation = Quaternion.Euler(0f, 0f, _state.rotation);
		}
		if (_scaleEnabled)
		{
			_transform.localScale = new Vector3(_state.scale, _state.scale, 1f);
		}
		float t = Time.time;
		_graph.AddData(t, _state.scale);
		_graph.SetXEnd(t);
	}

	void SetNormalizedTime(float normalizedTime, float t0, float t1)
	{
		_state.time = ((t1 - t0) * normalizedTime) + t0;
	}
	float GetNormalizedTime(float t0, float t1)
	{
		return (_state.time - t0) / (t1 - t0);
	}
	void SetLinear(ref State key0, ref State key1)
	{
		float t = GetNormalizedTime(key0.time, key1.time);
		t = Mathf.Clamp01(t);
		_state.position.x = Linear(key0.position.x, key1.position.x, t);
		_state.position.y = Linear(key0.position.y, key1.position.y, t);
		_state.rotation = Linear(key0.rotation, key1.rotation, t);
		_state.scale = Linear(key0.scale, key1.scale, t);
	}
	void SetQuadraticBeginV0(ref State key0, ref State key1)
	{
		float t = GetNormalizedTime(key0.time, key1.time);
		t = Mathf.Clamp01(t);
		_state.position.x = QuadraticBeginV0(key0.position.x, key1.position.x, t);
		_state.position.y = QuadraticBeginV0(key0.position.y, key1.position.y, t);
		_state.rotation = QuadraticBeginV0(key0.rotation, key1.rotation, t);
		_state.scale = QuadraticBeginV0(key0.scale, key1.scale, t);
	}
	void SetQuadraticEndV0(ref State key0, ref State key1)
	{
		float t = GetNormalizedTime(key0.time, key1.time);
		t = Mathf.Clamp01(t);
		_state.position.x = QuadraticEndV0(key0.position.x, key1.position.x, t);
		_state.position.y = QuadraticEndV0(key0.position.y, key1.position.y, t);
		_state.rotation = QuadraticEndV0(key0.rotation, key1.rotation, t);
		_state.scale = QuadraticEndV0(key0.scale, key1.scale, t);
	}
	void SetCubicBoundaryV0(ref State key0, ref State key1)
	{
		float t = GetNormalizedTime(key0.time, key1.time);
		t = Mathf.Clamp01(t);
		_state.position.x = CubicBoundaryV0(key0.position.x, key1.position.x, t);
		_state.position.y = CubicBoundaryV0(key0.position.y, key1.position.y, t);
		_state.rotation = CubicBoundaryV0(key0.rotation, key1.rotation, t);
		_state.scale = CubicBoundaryV0(key0.scale, key1.scale, t);
	}
	void UpdateExponential(ref State goal, float deltaTime, float coeff)
	{
		_state.position.x = Exponential(_state.position.x, goal.position.x, deltaTime, coeff);
		_state.position.y = Exponential(_state.position.y, goal.position.y, deltaTime, coeff);
		_state.rotation = Exponential(_state.rotation, goal.rotation, deltaTime, coeff);
		_state.scale = Exponential(_state.scale, goal.scale, deltaTime, coeff);
	}
	void UpdateSpringDumper(ref State goal, float deltaTime, float spring, float dumper)
	{
		SpringDumper(ref _state.position.x, ref _velocity.position.x, goal.position.x, deltaTime, spring, dumper);
		SpringDumper(ref _state.position.y, ref _velocity.position.y, goal.position.y, deltaTime, spring, dumper);
		SpringDumper(ref _state.rotation, ref _velocity.rotation, goal.rotation, deltaTime, spring, dumper);
		SpringDumper(ref _state.scale, ref _velocity.scale, goal.scale, deltaTime, spring, dumper);
	}

	/*
	二つのキーフレームの時刻を0,1とした時の時刻をtとし、
	関数型はp=at+bとする。
	t=0でp=p0、t=1でp=p1から、
	p0=b
	p1=a+b
	より、a=p1-p0, b=p0となり、関数型は
	p=(p1-p0)t+y0
	*/
	static float Linear(float p0, float p1, float t)
	{
		return ((p1 - p0) * t) + p0;
	}

	/*
	二つのキーフレームの時刻を0,1とした時の時刻をtとし、
	関数型はp=at^2+bt+cとする。
	t=0でp=p0、t=1でp=p1から、
	p0=c
	p1=a+b+c
	式が足りないので、t=0での微分、つまり速度が0であるとすると(速度が与えられていればそれを使うが、今は0とする)
	微分は p'=2at+b
	t=0を入れると0=bがわかる。

	a=p1-p0
	b=0
	c=p0

	から、関数形は
	p=(p1-p0)t^2 + p0
	*/
	static float QuadraticBeginV0(float p0, float p1, float t)
	{
		float a = p1 - p0;
		return (a * t * t) + p0;
	}

	/*
	二つのキーフレームの時刻を0,1とした時の時刻をtとし、
	関数型はp=at^2+bt+cとする。
	t=0でp=p0、t=1でp=p1から、
	p0=c
	p1=a+b+c
	式が足りないので、t=1での微分、つまり速度が0であるとすると(速度が与えられていればそれを使うが、今は0とする)
	微分は p'=2at+b
	t=1を入れると0=2a+bがわかる。

	p1 = a -2a + p0
	a = (p0 - p1)

	a=p0-p1
	b=-2(p0-p1)
	c=p0

	から、関数形は
	p=(p0-p1)t^2 -2(p0-p1)t + p0
	*/
	static float QuadraticEndV0(float p0, float p1, float t)
	{
		float a = p0 - p1;
		float b = -2f * a;
		// honor法で多項式計算
		var ret = a;
		ret *= t;
		ret += b;
		ret *= t;
		ret += p0;
		return ret;
	}

	/*
	二つのキーフレームの時刻を0,1とした時の時刻をtとし、
	関数型はp=at^3+bt^2+ct+dとする。
	t=0でp=p0、t=1でp=p1から、
	p0=d
	p1=a+b+c+d
	式が足りないので、t=0及びt=1での微分、つまり速度が0であるとすると(速度が与えられていればそれを使うが、今は0とする)
	微分はp'=3at^2+2bt+c
	t=0,t=1の条件を代入し、
	0=c
	0=3a+2b+c

	b=-a+p1-p0
	0=3a+2(-a+p1-p0)
	 =3a-2a+2p1-2p0
	 =a+2p1-2p0
	a=2(p0-p1)

	b=2p1-2p0+p1-p0
	 =3p1-3p0

	式が4本揃ったので解くと、
	a = 2(p0-p1)
	b = 3(p1-p0)
	c = 0
	d = p0

	から、関数形は
	p=2(p0-p1)t^3 + 3(p1-p0)*t^2/2 + p0
	*/
	static float CubicBoundaryV0(float p0, float p1, float t)
	{
		float a = 2f * (p0 - p1);
		float b = 3f * (p1 - p0);
		// honor法で多項式計算する
		var ret = a;
		ret *= t;
		ret += b;
		ret *= t;
		// c=0なので何も足さない
		ret *= t;
		ret += p0;
		return ret;
	}

	/*
	p += ((goal - p) * (1 - Exp(k*dt)))
	で、値を更新する。
	dt=0であればExpの項が1となり、何も足さない。dt=無限であれば、Expの項が0となり、
	goal-pを加えて即座に最終値に至る。
	*/
	static float Exponential(float current, float goal, float dt, float coefficient)
	{
		return current + ((goal - current) * (1f - Mathf.Exp(-coefficient * dt)));
		/*
		Exp(x)はxが小さければ1+xで近似できるため、安定して高FPSであると保証できる場合や、多少値がおかしくなってもかまわない場合は、
		return current + ((goal - current) * coefficient * dt;
		でも良い。ただし、coefficient*dtが1を超えた場合、goalを過ぎてしまう。
		*/
	}

	/*
	速度、位置から、加速度を求めて次の位置、速度を更新する。
	*/
	static void SpringDumper(ref float position, ref float velocity, float goal, float dt, float spring, float dumper)
	{
		float a = ((goal - position) * spring) - (velocity * dumper); // 目的地までの距離に比例した加速度と、現速度に比例した逆向きの加速度を加える。
		velocity += a * dt;
		position += velocity * dt;
	}

	void OnGUI()
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label("time: " + _state.time.ToString("N2"));
		float t = GetNormalizedTime(_currentKey.time, _nextKey.time);
		t = GUILayout.HorizontalSlider(t, 0f, 1f);
		SetNormalizedTime(t, _currentKey.time, _nextKey.time);
		GUILayout.Label("speed: " + _speed.ToString("N2"));
		_speed = GUILayout.HorizontalSlider(_speed, 0f, 2f);
		GUILayout.Label("ExpCoeff: " + _exponentialCoefficient.ToString("N2"));
		float log = Mathf.Log10(_exponentialCoefficient);
		log = GUILayout.HorizontalSlider(log, -1f, 2f);
		_exponentialCoefficient = Mathf.Pow(10f, log);
		GUILayout.Label("spring: " + _springCoefficient.ToString("N2"));
		log = Mathf.Log10(_springCoefficient);
		log = GUILayout.HorizontalSlider(log, -2f, 3f);
		_springCoefficient = Mathf.Pow(10f, log);
		GUILayout.Label("dumper: " + _dumperCoefficient.ToString("N2"));
		log = Mathf.Log10(_dumperCoefficient);
		log = GUILayout.HorizontalSlider(log, -2f, 3f);
		_dumperCoefficient = Mathf.Pow(10f, log);
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		_popup = GUILayout.Toggle(_popup, "popupMotion");
		_positionEnabled = GUILayout.Toggle(_positionEnabled, "position");
		_rotationEnabled = GUILayout.Toggle(_rotationEnabled, "rotation");
		_scaleEnabled = GUILayout.Toggle(_scaleEnabled, "scale");
		GUILayout.EndHorizontal();

		_functionType = (FunctionType)GUILayout.Toolbar(
			(int)_functionType,
			_toolbarItemNames);

		GUILayout.BeginHorizontal();
		if (!_playing)
		{
			if (GUILayout.Button("start"))
			{
				_playing = true;
			}
		}
		else
		{
			if (GUILayout.Button("stop"))
			{
				_playing = false;
			}
		}
		if (GUILayout.Button("rewind"))
		{
			Start();
		}
		if (GUILayout.Button("switchGoal"))
		{
			_nextIsKey2 = !_nextIsKey2;
			_currentKey = _nextKey;
			if (_popup)
			{
				_nextKey = _nextIsKey2 ? _key2popup : _key1popup;
			}
			else
			{
				_nextKey = _nextIsKey2 ? _key2 : _key1;
			}
		}
		if (_functionType == FunctionType.SpringDumper)
		{
			if (GUILayout.Button("pulse"))
			{
				var m = 2000f;
				_velocity.position += new Vector2(Random.Range(-m, m), Random.Range(-m, m));
				m = 5000f;
				_velocity.rotation += Random.Range(-m, m);
				m = 10f;
				_velocity.scale += Random.Range(-m, m);
			}
		}
		GUILayout.EndHorizontal();
	}
}
