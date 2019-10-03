using UnityEngine;
struct ConfettiPiece
{
	public void Init(
		Vector3 position,
        Vector3 velocity,
        float deltaTime,
        Quaternion orientation, //Z軸方向に
		float halfZSize,
        float normalBendRatio)
    {
        this.normalBendRatio = normalBendRatio;
        this.orientation = orientation;
		this.position = position;
		var forward = orientation * new Vector3(0f, 0f, 1f);
		var t = forward * halfZSize;
        var dp = velocity * deltaTime;
		prevPosition0 = position - t;
		prevPosition1 = position + t;
        prevPosition0 -= dp;
        prevPosition1 -= dp;
    }

    public void Update(
		float deltaTime,
		ref Vector3 wind,
		ref Vector3 gravity,
		float resistance,
		float halfZSize)
	{
		var forward = orientation * new Vector3(0f, 0f, 1f);
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
        if (dot0 < 0)
        {
            n0 = -n0;
            dot0 = -dot0;
        }
        if (dot1 < 0)
        {
            n1 = -n1;
            dot1 = -dot1;
        }
        var accel0 = n0 * (dot0 * resistance);
		var accel1 = n1 * (dot1 * resistance);
        // 重力を追加
        accel0 += gravity;
		accel1 += gravity;
        // 独立に積分
        var dt2 = deltaTime * deltaTime;
        var dp0 = p0 - prevPosition0 + (accel0 * dt2);
        var dp1 = p1 - prevPosition1 + (accel1 * dt2);
        var nextP0 = p0 + dp0;
        var nextP1 = p1 + dp1;
        prevPosition0 = p0;
        prevPosition1 = p1;
        p0 = nextP0;
        p1 = nextP1;
		// 拘束
		var newForward = (p1 - p0).normalized;
		position = (p0 + p1) * 0.5f;
		// 姿勢更新
		// forward -> newForwardに向くような、回転軸右ベクタの回転を作用させる
		var dq = Quaternion.FromToRotation(forward, newForward);
        orientation = dq * orientation; // dqは時間的に後の回転だから、ベクタから遠い方、つまり前から乗算
        orientation.Normalize();
    }

    public void GetTransform(
        ref Matrix4x4 matrix,
		float ZSize,
		float XSize)
	{
        matrix.SetTRS(position, orientation, new Vector3(XSize, 1f, ZSize));
	}
	public Vector3 position;
	Vector3 prevPosition0;
	Vector3 prevPosition1;
	Quaternion orientation;
    float normalBendRatio;
}