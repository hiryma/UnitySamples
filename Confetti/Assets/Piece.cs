using UnityEngine;
using Kayac;
struct Piece
{
	public void Init(
		Vector3 position,
		Quaternion orientation, //Z軸方向に
		Vector2 uv,
		float halfZSize)
	{
		this.uv = uv;
		this.orientation = orientation;
		this.position = position;
		var forward = orientation * new Vector3(0f, 0f, -1f);
		var t = forward * halfZSize;
		prevPosition0 = position + t;
		prevPosition1 = position - t;
	}

	public void Update(
		float deltaTime,
		ref Vector3 wind,
		ref Vector3 gravity,
		float resistance,
		float halfZSize,
		float halfXSize,
		float normalBendRatio)
	{
		var forward = orientation * new Vector3(0f, 0f, -1f);
		var t = forward * halfZSize;
		var p0 = position - t;
		var p1 = position + t;
		// 相対的な風ベクトルを出す
		var v0 = (p0 - prevPosition0) / deltaTime;
		var v1 = (p1 - prevPosition1) / deltaTime;
		var relativeWind0 = wind - v0;
		var relativeWind1 = wind - v1;
		// 頂点ごとの法線を生成
		var n = orientation * new Vector3(0f, 1f, 0f);
		// 曲げる
		t = forward * normalBendRatio;
		var n0 = n + t;
		var n1 = n - t;
		// 正規化。n1はn0と同じ長さなので長さを計算して使い回す
		n0.Normalize();
		n1.Normalize();
		// 風ベクトルの法線方向成分を、加速度とする
		var dot0 = Vector3.Dot(n0, relativeWind0);
		var dot1 = Vector3.Dot(n1, relativeWind1);
		var accel0 = n0 * (dot0 * resistance);
		var accel1 = n1 * (dot1 * resistance);
		// 重力を追加
		accel0 += gravity;
		accel1 += gravity;
//Debug.Log(accel0 + " " + accel1);
		// 独立に積分
		var dt2 = deltaTime * deltaTime;
		var prevP0 = prevPosition0;
		var prevP1 = prevPosition1;
		prevPosition0 = p0;
		prevPosition1 = p1;
		p0 += p0 - prevP0 + (accel0 * dt2);
		p1 += p1 - prevP1 + (accel1 * dt2);
		// 拘束
		position = (p0 + p1) * 0.5f;
		var newForward = (p1 - p0).normalized;
		// 姿勢更新
		// forward -> newForwardに向くような、回転軸右ベクタの回転を作用させる
		var dq = Quaternion.FromToRotation(forward, newForward);
		orientation *= dq;
	}

	public void Draw(
		ParticleMesh mesh,
		float halfZSize,
		float halfXSize)
	{
		var forward = orientation * new Vector3(0f, 0f, -1f);
		forward *= halfZSize;
		var right = orientation * new Vector3(1f, 0f, 0f);
		right *= halfXSize;
		var normal = orientation * new Vector3(0f, 1f, 0f);
		mesh.AddRectangle(
			ref position,
			ref forward,
			ref right,
			ref normal,
			ref uv);
	}
	public Vector3 position;
	Vector3 prevPosition0;
	Vector3 prevPosition1;
	Quaternion orientation;
	Vector2 uv;
}