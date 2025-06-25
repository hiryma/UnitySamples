using Kayac;
using UnityEngine;

public class Projectile : MonoBehaviour
{
	[SerializeField] Rigidbody body;
	[SerializeField] float lifeTime = 30f;
	[SerializeField] float inertiaResistance = 0.1f;
	[SerializeField] float guideCalculationTimeStepFactor = 8f;
	[SerializeField] float guide = 1f;
	[SerializeField] int calcErrorMaxIterations = 128;

	public Vector3 Position => body.position;
	public Vector3 Velocity => body.velocity;
	public bool Alive => lifeTime > 0f;
	public float InertiaResistance => inertiaResistance;
	public float Mass => body.mass;
	public float Radius => transform.localScale.x * 0.5f; // assuming uniform scale

	public void ManualStart(
		Vector3 position, 
		Vector3 velocity, 
		Target target, 
		Vector3 targetOffset,
		int targetAccelRandSeed)
	{
		body.position = position;
		body.velocity = velocity;
		this.target = target;
		this.targetOffset = targetOffset;
		this.targetAccelRandSeed = targetAccelRandSeed;
	}

	public void ManualFixedUpdate(float deltaTime)
	{
		lifeTime -= deltaTime;
		// 慣性抵抗
		var v = body.velocity;
		var f = v * (-v.magnitude * inertiaResistance); // -k*|v|^2*v/|v| = -k*|v|*v 
		// 誘導計算
		//まず現状から誤差計算+飛翔時間計算
		float timeToClosest;
		bool converged;
		var e = ProjectileMath.CalculateError(
			body.position,
			v,
			target.transform.TransformPoint(targetOffset),
			target.Velocity,
			target.AccelAverage,
			target.AccelCovarianceCholesky,
			targetAccelRandSeed,
			Physics.gravity,
			body.mass,
			inertiaResistance,
			guideCalculationTimeStepFactor,
			calcErrorMaxIterations,
			out timeToClosest,
			out converged);
		Debug.Assert(converged, "Projectile: Error did not converge within max iterations.");
		// e = 0.5 * f/m * t^2 を解いてfを求める
		// f = 2 * e * m / t^2
		var guideF = 2f * e * body.mass / (timeToClosest * timeToClosest);
		// 誘導力の限界を加味
		if (guideF.magnitude > guide)
		{
			guideF = guideF.normalized * guide;
		}
		f += guideF;
		body.AddForce(f);
	}

    void OnCollisionEnter(Collision collision)
    {
		lifeTime = 0f;
    }

    // non public ----
	Target target;
	Vector3 targetOffset; // target.transformのローカル座標
	int targetAccelRandSeed;
}
