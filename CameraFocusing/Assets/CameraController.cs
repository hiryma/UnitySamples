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
		// 各種傾き
		var ay1 = Mathf.Tan(Camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
		var ay0 = -ay1;
		var ax1 = ay1 * Camera.aspect;
		var ax0 = -ax1;
		// 最大最小を求めて不要な点を捨てる
		var y0Min = float.MaxValue;
		var y1Max = -float.MaxValue;
		var x0Min = float.MaxValue;
		var x1Max = -float.MaxValue;
		for (int i = 0; i < tmpPoints.Count; i++)
		{
			var p = tmpPoints[i];
			var by0 = p.y - (ay0 * p.z);
			var by1 = p.y - (ay1 * p.z);
			var bx0 = p.x - (ax0 * p.z);
			var bx1 = p.x - (ax1 * p.z);
			y0Min = Mathf.Min(y0Min, by0);
			y1Max = Mathf.Max(y1Max, by1);
			x0Min = Mathf.Min(x0Min, bx0);
			x1Max = Mathf.Max(x1Max, bx1);
		}
		// 求まった2点から位置を計算。Y軸で決めた点とX軸で決めた点が出来る。
		float zy = (y1Max - y0Min) / (ay0 - ay1);
		float y = y0Min + (ay0 * zy);
		float zx = (x1Max - x0Min) / (ax0 - ax1);
		float x = x0Min + (ax0 * zx);
		// より手前の方を選択。x,yはそのまま使う。
		var posInView = new Vector3(x, y, Mathf.Min(zy, zx));

		// ワールド座標に戻す
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
