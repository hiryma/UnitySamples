using UnityEngine;
using System.Collections.Generic;

public class CameraController
{
	Vector3 position;
	Vector3 target; // ロールしないのでzはいらない
	public Vector3 PositionGoal{ get; set; }
	public Vector3 TargetGoal{ get; set; }
	public float Stiffness { get; set; }
	public Camera Camera { get; private set; }
	List<Vector3> tmpPoints;

	public CameraController(Camera camera)
	{
		tmpPoints = new List<Vector3>();
		Camera = camera;
		position = PositionGoal = camera.transform.position;
		target = TargetGoal = position + camera.transform.forward;
		Stiffness = 1f;
	}

	public void FitByMove(IList<Vector3> points)
	{
		tmpPoints.Clear();
		// 全点をビュー空間に移動
		var toView = Camera.transform.worldToLocalMatrix;
		for (int i = 0; i < points.Count; i++)
		{
			tmpPoints.Add(toView.MultiplyPoint3x4(points[i]));
		}
		// X-,X+,Y-,Y+の各平面と平面距離を求める。
		// それぞれビュー座標で直交するので容易に求まる。
		// Y+平面 tan(θ_y/2) = y/1
		float tanHalfFovY = Mathf.Tan(Camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
		float ay0 = -tanHalfFovY;
		float ay1 = -ay0;
		float ax0 = -tanHalfFovY * Camera.aspect;
		float ax1 = -ax0;
		// y=ay0*z, y=ay1*z, x=ax0*z, x=ax1*z
		// の4本の式が立つが、これを平面の式に直す。
		// y-ay0*z=by0, y-ay1*z=by1, x-ax0*z=bx0, x-ax1*z=bx1
		// by0,by1,bx0,bx1の最小最大を計算する。
		var by0Min = float.MaxValue;
		var by1Max = -float.MaxValue;
		var bx0Min = float.MaxValue;
		var bx1Max = -float.MaxValue;

		for (int i = 0; i < tmpPoints.Count; i++)
		{
			var p = tmpPoints[i];
			var by0 = p.y - (ay0 * p.z);
			var by1 = p.y - (ay1 * p.z);
			var bx0 = p.x - (ax0 * p.z);
			var bx1 = p.x - (ax1 * p.z);
			by0Min = Mathf.Min(by0Min, by0);
			by1Max = Mathf.Max(by1Max, by1);
			bx0Min = Mathf.Min(bx0Min, bx0);
			bx1Max = Mathf.Max(bx1Max, bx1);
		}
		// X,Y別に交点を算出する。
		// y-ay0*z=by0 とy-ay1*z=by1の交点は、連立方程式を解けば良く、いきなりyが消せ、
		// by0+ay0*z=by1+ay1*z
		// (ay0-ay1)z = by1-by0
		// z = (by1-by0)/(ay0-ay1)
		float zy = (by1Max - by0Min) / (ay0 - ay1);
		float y = by0Min + (ay0 * zy);
		float zx = (bx1Max - bx0Min) / (ax0 - ax1);
		float x = bx0Min + (ax0 * zx);
		var posInView = new Vector3(x, y, Mathf.Min(zy, zx));

		// ワールドに戻す
		PositionGoal = Camera.transform.localToWorldMatrix.MultiplyPoint3x4(posInView);

		var forward = Camera.transform.forward; // 前ベクトル
		var d = (TargetGoal - PositionGoal).magnitude;
		TargetGoal = PositionGoal + (forward.normalized * d);
	}

	public void ManualUpdate(float deltaTime)
	{
		position += (PositionGoal - position) * deltaTime * Stiffness;
		target += (TargetGoal - target) * deltaTime * Stiffness;
		Camera.transform.position = position;
		Camera.transform.LookAt(target);
	}
}
