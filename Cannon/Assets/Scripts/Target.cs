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
		var k = 1f - Mathf.Clamp01(deltaTime * accelSmoothing);
		accelSum *= k;
		accel2ndSum.m00 *= k;
		accel2ndSum.m01 *= k;
		accel2ndSum.m02 *= k;
		accel2ndSum.m11 *= k;
		accel2ndSum.m12 *= k;
		accel2ndSum.m22 *= k;
		weightSum *= k;

		accelSum += a;
		accel2ndSum.m00 += a.x * a.x;
		accel2ndSum.m01 += a.x * a.y;
		accel2ndSum.m02 += a.x * a.z;
		accel2ndSum.m11 += a.y * a.y;
		accel2ndSum.m12 += a.y * a.z;
		accel2ndSum.m22 += a.z * a.z;
		weightSum += 1f;

		accelAverage = accelSum / weightSum;
		Matrix3x3 accelCovariance;
		accelCovariance.m00 = (accel2ndSum.m00 / weightSum) - (accelAverage.x * accelAverage.x);
		accelCovariance.m01 = (accel2ndSum.m01 / weightSum) - (accelAverage.x * accelAverage.y);
		accelCovariance.m02 = (accel2ndSum.m02 / weightSum) - (accelAverage.x * accelAverage.z);
		accelCovariance.m11 = (accel2ndSum.m11 / weightSum) - (accelAverage.y * accelAverage.y);
		accelCovariance.m12 = (accel2ndSum.m12 / weightSum) - (accelAverage.y * accelAverage.z);
		accelCovariance.m22 = (accel2ndSum.m22 / weightSum) - (accelAverage.z * accelAverage.z);
		accelCovariance.m10 = accelCovariance.m01;
		accelCovariance.m20 = accelCovariance.m02;
		accelCovariance.m21 = accelCovariance.m12;

		accelCovariance.DecomposeToCholesky(out accelCovarianceCholesky, 0.001f);
//Debug.Log(accelAverage);
//Debug.Log(accelAverage + "\t" + weightSum + " " + a + "\n" + accelCovarianceCholesky);
Debug.Log(accelCovariance.m00 + " Sw=" + weightSum + " a.x=" + a.x + " " + accelCovarianceCholesky.m00 + " Ax=" + accelAverage.x + " vx=" + v.x + " pvx=" + prevVelocity.x);
		//Debug.Log(accelCovariance.m01 + " " + accelCovarianceCholesky.m01);
		//Debug.Log(accelCovariance.m02 + " " + accelCovarianceCholesky.m02);
		//Debug.Log(accelCovariance.m11 + " " + accelCovarianceCholesky.m11);
		//Debug.Log(accelCovariance.m12 + " " + accelCovarianceCholesky.m12);
		//Debug.Log(accelCovariance.m22 + " " + accelCovarianceCholesky.m22);
		prevVelocity = v;
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
	Matrix3x3 accel2ndSum; // 対称なので、m00,m01,m02,m11,m12,m22のみ使う
	Vector3 accelSum;
	Vector3 accelAverage;
	Matrix3x3 accelCovarianceCholesky;
	Vector3 prevVelocity;
	float weightSum;
}
