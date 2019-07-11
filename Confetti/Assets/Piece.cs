using UnityEngine;
using Kayac;

struct Piece
{
	public void Init(
		ref Vector3 position,
		ref Vector3 axisNormalized,
		ref Vector3 velocity,
		float distance,
		int colorIndex)
	{
		uv.x = ((1f / 8f) * colorIndex) + (3f / 16f);
		uv.y = 0.5f;
		Vector3 halfAxis;
		Math.SetMul(out halfAxis, ref axisNormalized, distance * 0.5f);
		Math.SetAdd(out position0, ref position, ref halfAxis);
		Math.SetSub(out position1, ref position, ref halfAxis);
		velocity0 = velocity1 = velocity;
		center = position;
		halfDistance = distance * 0.5f;
		halfAxis = axisNormalized * halfDistance;
	}

	public void Update(float deltaTime, ref Vector3 accel, float resistance)
	{
		// TODO: ここがミソだから後で真面目に書く
		var dampingRatio = 1f - (resistance * deltaTime);
		Math.Mul(ref velocity0, dampingRatio);
		Math.Mul(ref velocity1, dampingRatio);
		// 独立に積分
		Math.Madd(ref velocity0, ref accel, deltaTime);
		Math.Madd(ref velocity1, ref accel, deltaTime);
		Math.Madd(ref position0, ref velocity0, deltaTime);
		Math.Madd(ref position1, ref velocity1, deltaTime);
		// 重心算出して距離拘束する
		Math.SetAdd(out center, ref position0, ref position1);
		Math.Mul(ref center, 0.5f);
		Math.SetSub(out halfAxis, ref position1, ref position0);
		Math.Normalize(ref halfAxis, halfDistance);
		Math.SetSub(out position0, ref center, ref halfAxis);
		Math.SetAdd(out position1, ref center, ref halfAxis);
	}

	public void Draw(ParticleMesh mesh, float halfWidth)
	{
		Vector3 rightAxis;
		Vector3 upHint = new Vector3(0f, 1f, 0f); // 最適化せよ
		Math.SetCross(out rightAxis, ref halfAxis, ref upHint);
		Math.Normalize(ref rightAxis, halfWidth);
		mesh.AddRectangle(
			ref center,
			ref halfAxis,
			ref rightAxis,
			ref uv);
	}

	Vector3 center;
	Vector3 halfAxis;
	Vector3 position0;
	Vector3 position1;
	Vector3 velocity0;
	Vector3 velocity1;
	Vector2 uv;
	float halfDistance;
}