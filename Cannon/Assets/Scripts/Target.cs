using Kayac;
using UnityEngine;

public class Target : MonoBehaviour
{
	[SerializeField] PidSettings movePidSettings;
	[SerializeField] Rigidbody body;
	[SerializeField] float accel = 1f;
	[SerializeField] float velocitySmoothing = 1f;

	public Vector3 Position => body.position;
	public Vector3 Velocity => body.velocity;
	public Vector3 VelocityAverage => velocityAverage;
	public Matrix3x3 VelocityCovarianceCholesky => velocityCovarianceCholesky;

	public void ManualStart(Main main)
	{
		this.main = main;
		initialPosition = body.position;
		movePid = new PidController3(movePidSettings);
	}

	public void ManualFixedUpdate(float deltaTime)
	{
		var a2 = Random.insideUnitCircle * accel;
		var p = body.position;
		var f = movePid.Update(p, initialPosition, deltaTime);
		f.x += a2.x;
		f.z += a2.y;
		body.AddForce(f, ForceMode.Acceleration);
		// 速度を計算して蓄積
		var v = body.velocity;
		var k = 1f - Mathf.Clamp01(deltaTime * velocitySmoothing);
		velocitySum *= k;
		velocity2ndSum.m00 *= k;
		velocity2ndSum.m01 *= k;
		velocity2ndSum.m02 *= k;
		velocity2ndSum.m11 *= k;
		velocity2ndSum.m12 *= k;
		velocity2ndSum.m22 *= k;
		weightSum *= k;

		velocitySum += v;
		velocity2ndSum.m00 += v.x * v.x;
		velocity2ndSum.m01 += v.x * v.y;
		velocity2ndSum.m02 += v.x * v.z;
		velocity2ndSum.m11 += v.y * v.y;
		velocity2ndSum.m12 += v.y * v.z;
		velocity2ndSum.m22 += v.z * v.z;
		weightSum += 1f;

		velocityAverage = velocitySum / weightSum;
		Matrix3x3 velocityCovariance;
		velocityCovariance.m00 = (velocity2ndSum.m00 / weightSum) - (velocityAverage.x * velocityAverage.x);
		velocityCovariance.m01 = (velocity2ndSum.m01 / weightSum) - (velocityAverage.x * velocityAverage.y);
		velocityCovariance.m02 = (velocity2ndSum.m02 / weightSum) - (velocityAverage.x * velocityAverage.z);
		velocityCovariance.m11 = (velocity2ndSum.m11 / weightSum) - (velocityAverage.y * velocityAverage.y);
		velocityCovariance.m12 = (velocity2ndSum.m12 / weightSum) - (velocityAverage.y * velocityAverage.z);
		velocityCovariance.m22 = (velocity2ndSum.m22 / weightSum) - (velocityAverage.z * velocityAverage.z);
		velocityCovariance.m10 = velocityCovariance.m01;
		velocityCovariance.m20 = velocityCovariance.m02;
		velocityCovariance.m21 = velocityCovariance.m12;

		velocityCovariance.DecomposeToCholesky(out velocityCovarianceCholesky, 0.001f);
Debug.Log(velocityAverage + "\t" + velocityCovarianceCholesky.m00);
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
	Matrix3x3 velocity2ndSum; // 対称なので、m00,m01,m02,m11,m12,m22のみ使う
	Vector3 velocitySum;
	Vector3 velocityAverage;
	Matrix3x3 velocityCovarianceCholesky;
	float weightSum;
}
