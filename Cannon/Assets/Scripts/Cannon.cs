using System.Collections.Generic;
using Kayac;
using UnityEngine;

public class Cannon : MonoBehaviour
{
	[SerializeField] PidSettings movePidSettings;
	[SerializeField] PidSettings rotationPidSettings;
	[SerializeField] Rigidbody body;
	[SerializeField] Rigidbody barrelBody;
	[SerializeField] float fireInterval = 1f;
	[SerializeField] float speed = 10f;
	[SerializeField] float timeStepFactor = 1f;
	[SerializeField] float targetVelocitySmoothing = 1f;
	[SerializeField] float xSpeed = 1f;
	[SerializeField] float xRange = 20f;
	[SerializeField] int calcErrorMaxIterations = 128;
	[SerializeField] Transform muzzlePoint;
	[SerializeField] Projectile projectilePrefab;

	public void ManualStart(Main main)
	{
		this.main = main;
		fireTimer = fireInterval;
		projectiles = new List<Projectile>();
		initialPosition = body.position;
		goal = initialPosition;

		movePid = new PidController3(movePidSettings);
		rotationPid = new PidControllerRotation(rotationPidSettings, Quaternion.identity);
		currentDirectionGoal = barrelBody.rotation * Vector3.forward; // 銃口の向き
	}

	public void ManualFixedUpdate(float deltaTime, Target target, Transform projectilesParent)
	{
		// 移動
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

		var tv = target.Velocity; // ターゲットの速度
		// 目標の速度を平滑化
		smoothedTargetVelocity = Vector3.Lerp(
			smoothedTargetVelocity,
			tv,
			Mathf.Clamp01(deltaTime * targetVelocitySmoothing));

		UpdateBarrel(deltaTime, target);

		fireTimer -= deltaTime;
		if (fireTimer <= 0f)
		{
//ProjectileMath.D = 1;
			var error = CalcCurrentError(target);
ProjectileMath.D = 0;
//			if (error < 2f)
			{
				Fire(target, projectilesParent);
				fireTimer = fireInterval;
			}
		}

		var dst = 0;
		for (var i = 0; i < projectiles.Count; i++)
		{
			var projectile = projectiles[i];
			projectile.ManualFixedUpdate(deltaTime);
			projectiles[dst] = projectile;
			if (projectile.Alive)
			{
				dst++;
			}
			else
			{
				Destroy(projectile.gameObject);
			}
		}

		if (dst < projectiles.Count)
		{
			projectiles.RemoveRange(dst, projectiles.Count - dst);
		}
	}

	// non public ----
	Main main;
	float fireTimer = 0f;
	List<Projectile> projectiles;
	float steepestDescentAlpha = 1f;
	Vector3 goal;
	Vector3 initialPosition;
	PidController3 movePid;
	PidControllerRotation rotationPid;
	Vector3 currentDirectionGoal;
	Vector3 smoothedTargetVelocity;
	int targetAccelRandSeed;

	void UpdateBarrel(float deltaTime, Target target)
	{
		Quaternion goalRotation = barrelBody.transform.rotation; // 現在値
		var relativeVelocity = target.Velocity - body.velocity; // 相対速度
		var isStatic = (relativeVelocity.sqrMagnitude == 0f);
		if (isStatic)
		{
			if (projectilePrefab.InertiaResistance == 0f) // 相対速度ゼロで解析的に解ける
			{
				currentDirectionGoal = CalcBarrelRotationGoalStaticNoAirResistance(target);
			}
			else
			{
				currentDirectionGoal = RefineBarrelRotationGoalStatic(target, currentDirectionGoal);
			}
		}
		else
		{
			currentDirectionGoal = RefineBarrelRotationGoal(target, currentDirectionGoal);
		}

		var goalQ = Quaternion.LookRotation(currentDirectionGoal, Vector3.up);
		var torque = rotationPid.Update(barrelBody.rotation, goalQ, deltaTime);
		barrelBody.AddTorque(torque, ForceMode.Acceleration);
	}

	// 反復改良で解く。相対速度が非ゼロであるため、3次元最適化を行う
	Vector3 RefineBarrelRotationGoal(
		Target target,
		Vector3 goalDirection)
	{
		var p = muzzlePoint.position;
		var tp = target.Position; // ターゲット位置
		var tv = smoothedTargetVelocity; // 平滑化したターゲットの速度

		float timeToClosest;
		var minEv = CalculateError(
			goalDirection,
			target,
			out timeToClosest);
		var minError = minEv.magnitude;

		var succeeded = SteepestDescentOptimizer.Refine(
			(vDir) => 
			{
				float timeToClosest;
				var ev = CalculateError(
					vDir,
					target,
					out timeToClosest);
				return ev.magnitude;
			},
			goalDirection,
			steepestDescentAlpha,
			minError,
			2f,
			1e-8f,
			1e-5f,
			out goalDirection,
			out steepestDescentAlpha,
			out minError);
		goalDirection.Normalize();

		return goalDirection;
	}

	// 空気抵抗があるので反復改良で解く。ただし相対速度がゼロなので、角度の1次元最適化で済む
	Vector3 RefineBarrelRotationGoalStatic(
		Target target,
		Vector3 goalDirection)
	{
		var dir = goalDirection;
		var dirXzL = Mathf.Sqrt((dir.x * dir.x) + (dir.z * dir.z));
		var currentTheta = Mathf.Atan(dir.y / dirXzL);
		var p = muzzlePoint.position;
		var dp = target.Position - p; // ターゲット位置 - 銃口位置
		var dpXzL = Mathf.Sqrt((dp.x * dp.x) + (dp.z * dp.z));

		var minError = CalculateErrorStatic(
			currentTheta,
			dpXzL,
			dp.y);

		var bestTheta = currentTheta;
		SteepestDescentOptimizer.Refine(
			(theta) => CalculateErrorStatic(
				theta,
				dpXzL,
				dp.y),
			bestTheta,
			steepestDescentAlpha,
			minError,
			2f,
			1e-8f,
			1e-4f,
			out bestTheta,
			out steepestDescentAlpha,
			out minError);
//Debug.Log("\tRefined: " + bestTheta + " " + minError + " " + steepestDescentAlpha);
		
		Vector3 forwardGoal;
		var cosTheta = Mathf.Cos(bestTheta);
		forwardGoal.x = cosTheta * dp.x / dpXzL;
		forwardGoal.y = Mathf.Sin(bestTheta);
		forwardGoal.z = cosTheta * dp.z / dpXzL;

		return forwardGoal.normalized;
	}

	// 空気抵抗なし、相対速度ゼロ、の時のみ厳密に解ける
	Vector3 CalcBarrelRotationGoalStaticNoAirResistance(Target target)
	{
		Debug.Assert(Physics.gravity.x == 0f && Physics.gravity.z == 0f, "重力のx,z成分が0でないときは非対応");
		var p0 = muzzlePoint.position;
		var p1 = target.Position;
		var dx = p1.x - p0.x;
		var dy = p1.y - p0.y;
		var dz = p1.z - p0.z;
		var xzl = Mathf.Sqrt((dx * dx) + (dz * dz));
		var theta = ProjectileMath.CalculateElevationAngle(
			xzl,
			dy,
			speed,
			-Physics.gravity.y); // 下向きなら正の値を与える。x,zが非0の場合には非対応
		if (float.IsNaN(theta))
		{
			// 射程外。最も飛ぶ設定である45度にしておく
			theta = Mathf.PI / 4f; // 45度
		}

		Vector3 forwardGoal;
		var cosTheta = Mathf.Cos(theta);
		forwardGoal.x = cosTheta * dx / xzl;
		forwardGoal.y = Mathf.Sin(theta);
		forwardGoal.z = cosTheta * dz / xzl;
		return forwardGoal;
	}

	Vector3 CalculateError(Vector3 direction, Target target, out float timeToClosest)
	{
		bool converged;
		var ev = ProjectileMath.CalculateError(
			muzzlePoint.position,
			(direction.normalized * speed) + body.velocity,
			target.Position,
			smoothedTargetVelocity,
			target.AccelAverage,
			target.AccelCovarianceCholesky,
			targetAccelRandSeed,
			Physics.gravity, // 重力は下向きなので正の値
			projectilePrefab.Mass,
			projectilePrefab.InertiaResistance,
			timeStepFactor,
			calcErrorMaxIterations,
			out timeToClosest,
			out converged);
		Debug.Assert(converged, "Projectile: Error did not converge within max iterations.");
		return ev;
	}

	float CalculateErrorStatic(
		float theta,
		float horizontalDistance,
		float verticalDistance)
	{
		var e = ProjectileMath.CalculateErrorStatic(
			theta,
			horizontalDistance,
			verticalDistance,
			speed,
			-Physics.gravity.y, // 重力は下向きなので正の値
			projectilePrefab.Mass,
			projectilePrefab.InertiaResistance,
			timeStepFactor,
			calcErrorMaxIterations);
		return e;
	}

	float CalcCurrentError(Target target)
	{
		var vDir = barrelBody.rotation * Vector3.forward; // 銃口の向き
		var ev = CalculateError(vDir, target, out var timeToClosest);
		return ev.magnitude;
	}

	void Fire(Target target, Transform parent)
	{
		main.OnFire();
		var projectile = Instantiate(projectilePrefab, parent, false);
		var velocity = (muzzlePoint.forward * speed) + body.velocity;
		projectile.ManualStart(muzzlePoint.position, velocity, target, Vector3.zero, targetAccelRandSeed);
		targetAccelRandSeed++;
		projectiles.Add(projectile);
	}
}
