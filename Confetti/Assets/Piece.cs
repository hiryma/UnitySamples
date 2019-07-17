using UnityEngine;
using Kayac;

struct Piece
{
	public void Init(
		ref Vector3 position,
		ref Vector3 forwardAxis,
		ref Vector3 velocity,
		int colorIndex)
	{
		uv.x = ((1f / 8f) * colorIndex) + (3f / 16f); // 8x1テクスチャに8色入れてあるのでそのUVを渡して色替えをしている
		uv.y = 0.5f;
		this.forwardAxis = forwardAxis;
		this.position = position;
		this.velocity0 = velocity1 = velocity;
		var upHint = new Vector3(0f, 1f, 0f);
		rightAxis = Vector3.Cross(forwardAxis, new Vector3(0f, 1f, 0f));
		rightAxis.Normalize();
	}

#if true
	public void Update(
		float deltaTime,
		ref Vector3 wind,
		float gravity,
		float resistance,
		float halfZSize,
		float halfXSize,
		float normalBendRatio)
	{
		// 相対的な風ベクトルを出す
		var relativeWind0 = wind - velocity0;
		var relativeWind1 = wind - velocity1;
		// 頂点ごとの法線を生成
		var upAxis = Vector3.Cross(forwardAxis, rightAxis);
		// 曲げる
		var n0 = upAxis + (forwardAxis * normalBendRatio);
		var n1 = upAxis - (forwardAxis * normalBendRatio);
		// 正規化。n1はn0と同じ長さなので長さを計算して使い回す
		n0.Normalize();
		n1.Normalize();
		// 風ベクトルの法線方向成分を、加速度とする
		var dot0 = Vector3.Dot(n0, relativeWind0);
		var dot1 = Vector3.Dot(n1, relativeWind1);
		var accel0 = n0 * (dot0 * resistance);
		var accel1 = n1 * (dot1 * resistance);
		// 重力を追加
		accel0.y -= gravity;
		accel1.y -= gravity;
		// 独立に積分
		velocity0 += accel0 * deltaTime;
		velocity1 += accel1 * deltaTime;
		var position0 = position - (forwardAxis * halfZSize);
		var position1 = position + (forwardAxis * halfZSize);
		position0 += velocity0 * deltaTime;
		position1 += velocity1 * deltaTime;
		// 重心を新しい位置、軸を更新する
		position = (position0 + position1) * 0.5f;
		forwardAxis = position1 - position0;
		forwardAxis.Normalize();
	}

	public void Draw(
		ParticleMesh mesh,
		float halfZSize,
		float halfXSize)
	{
		var halfForward = forwardAxis * halfZSize;
		var halfRight = rightAxis * halfXSize;
		mesh.AddRectangleWholeTexture(
			ref position,
			ref halfForward,
			ref halfRight);
	}
#else

	public void Update(
		float deltaTime,
		ref Vector3 wind,
		float gravity,
		float resistance,
		float halfZSize,
		float halfXSize,
		float normalBendRatio)
	{
		// 相対的な風ベクトルを出す
		Vector3 relativeWind0, relativeWind1;
		Math.SetSub(out relativeWind0, ref wind, ref velocity0);
		Math.SetSub(out relativeWind1, ref wind, ref velocity1);
		// 右軸
		var upHint = new Vector3(0f, 1f, 0f);
		Math.SetCross(out rightAxis, ref forwardAxis, ref upHint);
		Math.Normalize(ref rightAxis);
		// 頂点ごとの法線を生成
		Vector3 upAxis, n0, n1;
		Math.SetCross(out upAxis, ref forwardAxis, ref rightAxis);
		// 曲げる
		Math.SetMadd(out n0, ref upAxis, ref forwardAxis, normalBendRatio);
		Math.SetMsub(out n1, ref upAxis, ref forwardAxis, normalBendRatio);
Debug.Log(position + " " + forwardAxis + " " + rightAxis + " " + upAxis + " " + n0 + " " + n1);
		// 正規化。n1はn0と同じ長さなので長さを計算して使い回す
		float rcpL = 1f / Math.GetLength(ref n0);
		Math.Mul(ref n0, rcpL);
		Math.Mul(ref n1, rcpL);
		// 風ベクトルの法線方向成分を、加速度とする
		var dot0 = Math.Dot(ref n0, ref relativeWind0);
		var dot1 = Math.Dot(ref n1, ref relativeWind1);
		Vector3 accel0, accel1;
		Math.SetMul(out accel0, ref n0, dot0 * resistance);
		Math.SetMul(out accel1, ref n1, dot1 * resistance);
		// 重力を追加
		accel0.y -= gravity;
		accel1.y -= gravity;
Debug.Log(position + " " + forwardAxis + " " + rightAxis + " " + upAxis + " " + accel0 + " " + accel1);
		// 独立に積分
		Math.Madd(ref velocity0, ref accel0, deltaTime);
		Math.Madd(ref velocity1, ref accel1, deltaTime);
		Vector3 position0, position1;
		Vector3 halfForward;
		Math.SetMul(out halfForward, ref forwardAxis, halfZSize);
		Math.SetSub(out position0, ref position, ref halfForward);
		Math.SetAdd(out position1, ref position, ref halfForward);
		Math.Madd(ref position0, ref velocity0, deltaTime);
		Math.Madd(ref position1, ref velocity1, deltaTime);
		// 重心を新しい位置、軸を更新する
		Math.SetAdd(out position, ref position0, ref position1);
		Math.Mul(ref position, 0.5f);
		Math.SetSub(out forwardAxis, ref position1, ref position0);
		Math.Normalize(ref forwardAxis);
Debug.Log("\t" + position + " " + forwardAxis + " " + rightAxis + " " + upAxis + " " + n0 + " " + n1);
	}

	public void Draw(
		ParticleMesh mesh,
		float halfZSize,
		float halfXSize)
	{
		Vector3 halfForward, halfRight;
		Math.SetMul(out halfForward, ref forwardAxis, halfZSize);
		Math.SetMul(out halfRight, ref rightAxis, halfXSize);
		mesh.AddRectangle(
			ref position,
			ref halfForward,
			ref halfRight,
			ref uv);
	}
#endif

	public Vector3 position;
	Vector3 forwardAxis; // 頂点0から1へ向かうベクトル。正規化
	Vector3 rightAxis; // 頂点0から1へ向かうベクトル。正規化
	Vector3 velocity0;
	Vector3 velocity1;
	Vector2 uv;
}