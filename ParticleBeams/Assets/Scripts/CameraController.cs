using UnityEngine;

public class CameraController
{
	Vector3 position;
	Vector3 target;
	public Vector3 PositionGoal{ get; set; }
	public Vector3 TargetGoal{ get; set; }
	public float Stiffness { get; set; }
	public Camera Camera { get; private set; }

	public CameraController(Camera camera)
	{
		Camera = camera;
		position = PositionGoal = camera.transform.position;
		target = TargetGoal = position + camera.transform.forward;
		Stiffness = 1f;
	}

	/// 画角、回転固定で位置調整
	public void FitByMove(Vector3 center, float radius)
	{
		var fov = GetFovMinRadian();
		// d*tan(fov/2)=radiusとなるdを求める
		var d = radius / Mathf.Tan(fov * 0.5f);
		// 目標点からforwardベクタにdを乗じて引く
		var forward = Camera.transform.forward; // 前ベクトル
		var position = center - (forward.normalized * d);
		PositionGoal = position;
		TargetGoal = center;
	}

	// 2点を縦に含めるように、移動で調整。
	public void FitByMove2PointVertical(
		Vector3 p0,
		Vector3 p1,
		Vector3 upHint,
		float interpolator01,
		float margin)
	{
		// p0, p1を通る円周上にカメラを置く。
		// カメラ位置をqとして、画角をΘとすれば、
		// 半径rを使って、sin(Θ)=(|p1-p0|/2)/r
		// r = (|p1-p0|/2)/sin(Θ)

		var p01 = p1 - p0;
		var p01mag = p01.magnitude;
		var tanFov = Mathf.Tan(Camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
		tanFov *= 1f - margin;
		var theta = Mathf.Atan(tanFov) * 2f;
		var r = (p01mag * 0.5f) / Mathf.Sin(theta);

		// 上ベクタを求める。
		var p01nrm = p01 / p01mag; // 正規化
		var right = Vector3.Cross(upHint, p01nrm).normalized;
		var up = Vector3.Cross(p01nrm, right);

		// 円中心を求める
		// p0 + p01/2 + up*r*cos(theta)
		var c = p0 + (p01 * 0.5f) + up * (r * Mathf.Cos(theta));

		// 真上をφ=0とする角度を定義する。
		// p0における角度は-PI+Θ、p1においてはPI-Θ
		// これを補間してφを求める。
		float halfWidth = Mathf.PI - theta;
		float phi = -halfWidth + (halfWidth * 2f * interpolator01);

		// 上ベクタをφ回転させてrを乗じたベクトルをcに加えた所が視点e
		var q = Quaternion.AngleAxis(phi * Mathf.Rad2Deg, right);
		var rotated = q * up;
		var e = c + (rotated * r);

		// ターゲットは(p0-e)と(p1-e)の平均ベクトルをforwardとして設定する
		// 長さp0とp1の中点までの距離としておく。
		var p0e = (p0 - e).normalized;
		var p1e = (p1 - e).normalized;
		var distance = (((p0 + p1) * 0.5f) - e).magnitude;
		var t = e + ((p0e + p1e) * distance * 0.5f);
		LookAt(t);
		PositionGoal = e;
	}

	// 画角、回転固定で位置調整して箱が入るようにする
	Vector3[] tmpPoints = new Vector3[8];
	public void FitByMove(Vector3 min, Vector3 max)
	{
		// 8点を取得
		var v = tmpPoints;
		v[0] = new Vector3(min.x, min.y, min.z);
		v[1] = new Vector3(min.x, min.y, max.z);
		v[2] = new Vector3(min.x, max.y, min.z);
		v[3] = new Vector3(min.x, max.y, max.z);
		v[4] = new Vector3(max.x, min.y, min.z);
		v[5] = new Vector3(max.x, min.y, max.z);
		v[6] = new Vector3(max.x, max.y, min.z);
		v[7] = new Vector3(max.x, max.y, max.z);
		// ビュー空間に射影
		var toView = Camera.transform.worldToLocalMatrix;
		for (int i = 0; i < 8; i++)
		{
			v[i] = toView.MultiplyPoint3x4(v[i]);
		}
		// アスペクト比でXをスケール
		var aspect = Camera.aspect;
		for (int i = 0; i < 8; i++)
		{
			v[i].x = v[i].x / aspect;
		}
		// 簡易的に球を生成
		var center = (min + max) * 0.5f;
		var viewCenter = toView.MultiplyPoint3x4(center);
		var sqRadius = 0f;
		for (int i = 0; i < 8; i++)
		{
			sqRadius = Mathf.Max(sqRadius, (v[i] - viewCenter).sqrMagnitude);
		}
		var radius = Mathf.Sqrt(sqRadius);
		// 距離算出
		var d = radius / Mathf.Tan(Camera.fieldOfView * Mathf.Deg2Rad * 0.5f);

		var forward = Camera.transform.forward; // 前ベクトル
		var position = center - (forward.normalized * d);
		PositionGoal = position;
		TargetGoal = center;
	}

	/// 画角固定で、newForwardVectorが新しい視線ベクタになるように回転しつつ、距離で位置調整
	public void FitByRotateAndMove(Vector3 newForwardVector, Vector3 center, float radius)
	{
		var fov = GetFovMinRadian();
		// d*tan(fov/2)=radiusとなるdを求める
		var d = radius / Mathf.Tan(fov * 0.5f);
		// 目標点からforwardベクタにdを乗じて引く
		PositionGoal = center - (newForwardVector * d);
		TargetGoal = center;
	}

	/// 位置、回転固定で角度調整
	public void LookAt(Vector3 center)
	{
		TargetGoal = center;
	}

	Vector2 CalcAngle(Vector3 v)
	{
		// 仰角を計算
		var xzLength = (v.x * v.x) + (v.z * v.z);
		Vector2 ret;
		ret.x = Mathf.Atan(-v.y / xzLength) * Mathf.Rad2Deg;
		// 方位角を計算
		ret.y = Mathf.Atan2(-v.x, v.z) * Mathf.Rad2Deg;
		return ret;
	}

	float GetFovMinRadian()
	{
		var fov = Camera.fieldOfView * Mathf.Deg2Rad;
		if (Camera.aspect < 1f) // 縦長の場合横で合わせるので修正
		{
			var tanFov = Mathf.Tan(fov * 0.5f);
			tanFov *= Camera.aspect;
			fov = Mathf.Atan(tanFov) * 2f;
		}
		return fov;
	}

	public void Converge()
	{
		position = PositionGoal;
		target = TargetGoal;
		ManualUpdate(0f);
	}

	public void ManualUpdate(float deltaTime)
	{
		position += (PositionGoal - position) * deltaTime * Stiffness;
		target += (TargetGoal - target) * deltaTime * Stiffness;
		Camera.transform.position = position;
		Camera.transform.LookAt(target);
	}
}
