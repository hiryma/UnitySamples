using Kayac;
using UnityEngine;

public class Target : MonoBehaviour
{
	[SerializeField] PidSettings movePidSettings;
	[SerializeField] Rigidbody body;
	[SerializeField] float xSpeed = 1f;
	[SerializeField] float xRange = 20f;
	[SerializeField] float accelSmoothing = 1f;

	public Vector3 Position => body.position;
	public Vector3 Velocity => body.velocity;
	public Rigidbody Body => body;
	public Vector3 AccelAverage => accelAverage;
	public Matrix3x3 AccelCovarianceCholesky => accelCovarianceCholesky;

	public void ManualStart(Main main)
	{
		this.main = main;
		initialPosition = body.position;
		goal = initialPosition;
		movePid = new PidController3(movePidSettings);
	}

	public void ManualFixedUpdate(float deltaTime)
	{
		if (xSpeed != 0f)
		{
			goal.x += xSpeed * deltaTime;
			if (goal.x < -xRange || goal.x > xRange)
			{
				xSpeed = -xSpeed;
				goal.x = Mathf.Clamp(goal.x, -xRange, xRange);
			}
		}
		var p = body.position;
		var f = movePid.Update(p, goal, deltaTime);
		body.AddForce(f, ForceMode.Acceleration);
		// 加速度を計算して蓄積
		var v = body.velocity;
		var a = (v - prevVelocity) / deltaTime;
		prevVelocity = v;
		var k = 1f - Mathf.Clamp01(deltaTime * accelSmoothing);
		accelSum *= k;
		accelCovarianceSum.m00 *= k;
		accelCovarianceSum.m01 *= k;
		accelCovarianceSum.m02 *= k;
		accelCovarianceSum.m11 *= k;
		accelCovarianceSum.m12 *= k;
		accelCovarianceSum.m22 *= k;
		weightSum *= k;

		accelSum += a;
		accelCovarianceSum.m00 += a.x * a.x;
		accelCovarianceSum.m01 += a.x * a.y;
		accelCovarianceSum.m02 += a.x * a.z;
		accelCovarianceSum.m11 += a.y * a.y;
		accelCovarianceSum.m12 += a.y * a.z;
		accelCovarianceSum.m22 += a.z * a.z;
		weightSum += 1f;

		accelAverage = accelSum / weightSum;
		Matrix3x3 accelCovariance;
		accelCovariance.m00 = accelCovarianceSum.m00 / weightSum;
		accelCovariance.m01 = accelCovarianceSum.m01 / weightSum;
		accelCovariance.m02 = accelCovarianceSum.m02 / weightSum;
		accelCovariance.m11 = accelCovarianceSum.m11 / weightSum;
		accelCovariance.m12 = accelCovarianceSum.m12 / weightSum;
		accelCovariance.m22 = accelCovarianceSum.m22 / weightSum;
		accelCovariance.m10 = accelCovariance.m01;
		accelCovariance.m20 = accelCovariance.m02;
		accelCovariance.m21 = accelCovariance.m12;

		accelCovariance.DecomposeToCholesky(out accelCovarianceCholesky, 0.001f);
	}

	void OnCollisionEnter(Collision collision)
	{
		var projectile = collision.gameObject.GetComponent<Projectile>();
		if (projectile != null)
		{
			main.OnHit();
		}	
	}

	// non public ----
	Main main;
	Vector3 goal;
	Vector3 initialPosition;
	PidController3 movePid;
	Matrix3x3 accelCovarianceSum; // 対称なので、m00,m01,m02,m11,m12,m22のみ使う
	Vector3 accelSum;
	Vector3 accelAverage;
	Matrix3x3 accelCovarianceCholesky;
	Vector3 prevVelocity;
	float weightSum;
}
