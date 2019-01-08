using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sample : MonoBehaviour
{
	[SerializeField]
	Camera _camera3d;
	[SerializeField]
	Camera _camera2d;
	[SerializeField]
	float _mouseDragAccel = 10f;
	[SerializeField]
	float _mouseDumperTouchOff = 0.99f;
	[SerializeField]
	float _mouseDumperTouchOn = 0.8f;
	[SerializeField]
	Transform[] _3dEffectTransforms;
	[SerializeField]
	RectTransform[] _2dEffectTransforms;
	[SerializeField]
	Transform _heroTransform;
	[SerializeField]
	Transform[] _enemyTransforms;
	[SerializeField]
	RectTransform _footerLeftUiTransform;
	[SerializeField]
	RectTransform _headerCenterUiTransform;
	[SerializeField]
	RectTransform _headerRightUiTransform;

	float _headerUiPlaneDistanceFromCamera = 40f;
	float _footerUiPlaneDistanceFromCamera = 2f;
	float _effectDuration = 2f;
	bool _useBezier = true;
	// カメラ関連
	Vector2 _cameraVelocityXZ;
	Vector3 _prevMousePosition;
	// エフェクト関連
	class Effect
	{
		public Transform transform;
		public Transform srcTransform;
		public Transform dstTransform;
		public float time;
		public float uiPlaneDistanceFromCamera;
	}

	Effect[] _3dEffects;
	Effect[] _2dEffects;
	int _3dEffectIndex;
	int _2dEffectIndex;

	void Start()
	{
		_2dEffects = new Effect[_2dEffectTransforms.Length];
		for (int i = 0; i < _2dEffects.Length; i++)
		{
			_2dEffects[i] = new Effect();
			_2dEffects[i].transform = _2dEffectTransforms[i];
			_2dEffects[i].transform.gameObject.SetActive(false);
		}
		_3dEffects = new Effect[_3dEffectTransforms.Length];
		for (int i = 0; i < _3dEffects.Length; i++)
		{
			_3dEffects[i] = new Effect();
			_3dEffects[i].transform = _3dEffectTransforms[i];
			_3dEffects[i].transform.gameObject.SetActive(false);
		}
	}

	void Update()
	{
		float dt = Time.deltaTime;
		ControlCamera(dt);

		// なんか押すとパーティクル飛ばす処理
		if (Input.anyKeyDown)
		{
			// 3D側ランダムに選ぶ
			Transform transform3d = null;
			var r = UnityEngine.Random.Range(0f, 1f);
			if (r < 0.5f)
			{
				transform3d = _heroTransform;
			}
			else
			{
				var index = UnityEngine.Random.Range(0, _enemyTransforms.Length);
				transform3d = _enemyTransforms[index];
			}
			// 2D側をランダムに選ぶ
			RectTransform transform2d = null;
			r = UnityEngine.Random.Range(0f, 1f);
			float uiDistanceFromCamera = 0f;
			if (r < (1f / 3f))
			{
				transform2d = _footerLeftUiTransform;
				uiDistanceFromCamera = _footerUiPlaneDistanceFromCamera;
			}
			else if (r < (2f / 3f))
			{
				transform2d = _headerCenterUiTransform;
				uiDistanceFromCamera = _headerUiPlaneDistanceFromCamera;
			}
			else
			{
				transform2d = _headerRightUiTransform;
				uiDistanceFromCamera = _headerUiPlaneDistanceFromCamera;
			}

			if (UnityEngine.Random.Range(0f, 1f) < 0.5f)
			{
				// 3D→2D
				EmitEffect3dTo2d(transform3d, transform2d, uiDistanceFromCamera, _2dEffectIndex);
				_2dEffectIndex++;
				if (_2dEffectIndex >= _2dEffects.Length)
				{
					_2dEffectIndex = 0;
				}
			}
			else
			{
				// 2D→3D
				EmitEffect2dTo3d(transform2d, transform3d, uiDistanceFromCamera, _3dEffectIndex);
				_3dEffectIndex++;
				if (_3dEffectIndex >= _3dEffects.Length)
				{
					_3dEffectIndex = 0;
				}
			}
		}
		UpdateEffects(dt);
	}

	void EmitEffect3dTo2d(Transform src, Transform dst, float uiPlaneDistanceFromCamera, int index)
	{
		var effect = _2dEffects[index];
		effect.time = 0f;
		effect.transform.gameObject.SetActive(true);
		effect.uiPlaneDistanceFromCamera = uiPlaneDistanceFromCamera;
		effect.srcTransform = src;
		effect.dstTransform = dst;
	}

	void EmitEffect2dTo3d(Transform src, Transform dst, float uiPlaneDistanceFromCamera, int index)
	{
		var effect = _3dEffects[index];
		effect.time = 0f;
		effect.transform.gameObject.SetActive(true);
		effect.uiPlaneDistanceFromCamera = uiPlaneDistanceFromCamera;
		effect.srcTransform = src;
		effect.dstTransform = dst;
	}

	void UpdateEffects(float dt)
	{
		for (int i = 0; i < _2dEffects.Length; i++)
		{
			UpdateEffect2D(_2dEffects[i], dt);
		}
		for (int i = 0; i < _3dEffects.Length; i++)
		{
			UpdateEffect3D(_3dEffects[i], dt);
		}
	}

	// 2D要素が3D空間にあったとしたらどこにあるか、を算出する
	static void Calc3dWorldPositionOfUi(
		out Vector3 worldPositionOut,
		Transform uiTransform,
		Camera camera2d,
		Camera camera3d,
		float uiPlaneDistanceFromCamera)
	{
		var worldPos2d = uiTransform.position; // 2D世界おけるワールド座標を得る
		var screenPos = camera2d.WorldToScreenPoint(worldPos2d); // スクリーン座標に変換
		var ray = camera3d.ScreenPointToRay(screenPos); // 3D世界におけるカメラから出たレイに変換
		var cosine = Vector3.Dot(ray.direction, camera3d.transform.forward)
			/ camera3d.transform.forward.magnitude; // ray.directionの長さは1固定なので割らずに済む
		var distance = uiPlaneDistanceFromCamera / cosine;
		worldPositionOut = camera3d.transform.position + (ray.direction * distance);
	}

	void UpdateEffect2D(Effect effect, float dt)
	{
		if (!effect.transform.gameObject.activeSelf)
		{
			return;
		}
		// 開始点のワールド座標を得る
		var srcWorldPos = effect.srcTransform.position;

		// 目標点のワールド座標を求める
		Vector3 dstWorldPos;
		Calc3dWorldPositionOfUi(
			out dstWorldPos,
			effect.dstTransform,
			_camera2d,
			_camera3d,
			effect.uiPlaneDistanceFromCamera);
		// 正規化時刻を求める
		var t = effect.time / _effectDuration;
		t *= t; // 加速した方がかっこいいので加速させてみる。このサンプルの本質には関係ない。
		// 補間する
		Vector3 pos;
		if (_useBezier)
		{
			Vector3 controlPoint = (srcWorldPos + dstWorldPos) * 0.5f;
			controlPoint.y += 5f;
			Bezier(out pos, ref srcWorldPos, ref dstWorldPos, ref controlPoint, t);
		}
		else
		{
			pos = Vector3.Lerp(srcWorldPos, dstWorldPos, t);
		}
		// この3Dワールド座標が2Dカメラのキャンバス内でどこに来るのかを計算する
		var screenPos = _camera3d.WorldToScreenPoint(pos); // スクリーン座標に変換
		var ray = _camera2d.ScreenPointToRay(screenPos);
		effect.transform.position = ray.origin; // レイ上のどこを選んでも2Dならば同じなので、originを使う。
		// 遠くなら小さく描画する必要があるのでスケールを計算
		var camToPos = pos - _camera3d.transform.position;
		var zDistance = Vector3.Dot(_camera3d.transform.forward, camToPos)
			/ _camera3d.transform.forward.magnitude;
		/* 内積で距離を求めるのが理解し難ければ、以下のようにしても良い。一旦ビュー座標に移し、そのzだけを見る。
		var zDistance = _camera3d.transform.worldToLocalMatrix.MultiplyPoint3x4(pos).z; //Zしか使わないので、ここの乗算は部分的に行うとより良い。
		*/
		// distance == _effectDstUiPlaneDistanceFromCameraの時に1で、distanceが2倍になればスケールは半分になる。よって割り算。
		var scale = effect.uiPlaneDistanceFromCamera / zDistance;
		effect.transform.localScale = new Vector3(scale, scale, scale);

		// 終了判定
		if (effect.time >= _effectDuration)
		{
			effect.transform.gameObject.SetActive(false);
		}
		effect.time += dt;
	}

	void UpdateEffect3D(Effect effect, float dt)
	{
		if (!effect.transform.gameObject.activeSelf)
		{
			return;
		}
		// 開始のワールド座標を求める
		Vector3 srcWorldPos;
		Calc3dWorldPositionOfUi(
			out srcWorldPos,
			effect.srcTransform,
			_camera2d,
			_camera3d,
			effect.uiPlaneDistanceFromCamera);
		// 目標点のワールド座標を得る
		var dstWorldPos = effect.dstTransform.position;
		// 正規化時刻を求める
		var t = effect.time / _effectDuration;
		t *= t; // 加速した方がかっこいいので加速させてみる。このサンプルの本質には関係ない。
		// 補間する
		Vector3 pos;
		if (_useBezier)
		{
			Vector3 controlPoint = (srcWorldPos + dstWorldPos) * 0.5f;
			controlPoint.y += 5f;
			Bezier(out pos, ref srcWorldPos, ref dstWorldPos, ref controlPoint, t);
		}
		else
		{
			pos = Vector3.Lerp(srcWorldPos, dstWorldPos, t);
		}
		// 値をセット
		effect.transform.position = pos;

		// 終了判定
		if (effect.time >= _effectDuration)
		{
			effect.transform.gameObject.SetActive(false);
		}
		effect.time += dt;
	}

	void ControlCamera(float dt)
	{
		// ドラッグでカメラ動かす処理
		var newPos = Input.mousePosition;
		if (Input.GetMouseButton(0))
		{
			_cameraVelocityXZ *= _mouseDumperTouchOn;
			var dx = newPos.x - _prevMousePosition.x;
			var dy = newPos.y - _prevMousePosition.y;
			_cameraVelocityXZ.x -= dx * _mouseDragAccel * dt;
			_cameraVelocityXZ.y -= dy * _mouseDragAccel * dt;
		}
		else
		{
			_cameraVelocityXZ *= _mouseDumperTouchOff;
		}
		_prevMousePosition = newPos;

		var p = _camera3d.transform.localPosition;
		p.x += _cameraVelocityXZ.x * dt;
		p.z += _cameraVelocityXZ.y * dt;
		_camera3d.transform.localPosition = p;
	}

	void OnGUI()
	{
		GUILayout.Label("headerDistance: " + _headerUiPlaneDistanceFromCamera.ToString("N1"));
		_headerUiPlaneDistanceFromCamera = GUILayout.HorizontalSlider(_headerUiPlaneDistanceFromCamera, 0.1f, 100f);
		GUILayout.Label("footerDistance: " + _footerUiPlaneDistanceFromCamera.ToString("N1"));
		_footerUiPlaneDistanceFromCamera = GUILayout.HorizontalSlider(_footerUiPlaneDistanceFromCamera, 0.1f, 100f);
		GUILayout.Label("effectDuration: " + _effectDuration.ToString("N1"));
		_effectDuration = GUILayout.HorizontalSlider(_effectDuration, 0.1f, 5f);
		_useBezier = GUILayout.Toggle(_useBezier, "Bezier");
	}

	static void Bezier(out Vector3 pOut, ref Vector3 p0, ref Vector3 p1, ref Vector3 controlPoint, float t)
	{
		pOut = p1 - (controlPoint * 2f) + p0;
		pOut *= t;
		pOut += (controlPoint - p0) * 2f;
		pOut *= t;
		pOut += p0;
	}
}
