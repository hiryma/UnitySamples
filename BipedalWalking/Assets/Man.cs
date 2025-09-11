using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kayac;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Man : MonoBehaviour
{
	[System.Serializable]
	class Settings
	{
		[SerializeField] public float hipsAngle;
		[SerializeField] public float defaultHeightRatio = 0.9f;
		[SerializeField] public float footPidLerp = 0.5f;
		[SerializeField] public float landOffsetFactor = 1f;
		[SerializeField] public float landOffsetMin = 0f;
		[SerializeField] public float pivotScoreThreshold = 0f;
		[SerializeField] public float pivotScoreThresholdMin = 0f;
		[SerializeField] public float kdScaleFactor = 0f;
		[SerializeField] public float kdScaleUpperLegMin = 1f;
		[SerializeField] public float kdScaleLowerLegMin = 1f;
		[SerializeField] public float speedSmoothing = 1f;
		[SerializeField] public float resetPivotOnPivotSwitchSpeedThreshold = 1f;
		[SerializeField] public float resetNonPivotOnPivotSwitchSpeedThreshold = 2f;
		[SerializeField] public PidSettings hipsPid;
		[SerializeField] public PidSettings upperLegPid;
		[SerializeField] public PidSettings lowerLegPid;
	}
	[SerializeField] Settings settings;
	[SerializeField] Rigidbody hips;
	[SerializeField] Rigidbody upperLegL;
	[SerializeField] Rigidbody upperLegR;
	[SerializeField] Rigidbody lowerLegL;
	[SerializeField] Rigidbody lowerLegR;
	[SerializeField] Transform footL;
	[SerializeField] Transform footR;

	[SerializeField] Transform goalSphereL;
	[SerializeField] Transform goalSphereR;
	[SerializeField] Transform tmpGoalSphereL;
	[SerializeField] Transform tmpGoalSphereR;

	public Vector3 Position => hips.position;
	public Vector3 Velocity => hips.velocity;

	public void ManualStart()
	{
		hips.maxAngularVelocity = 1000f;
		upperLegL.maxAngularVelocity = 1000f;
		upperLegR.maxAngularVelocity = 1000f;
		lowerLegL.maxAngularVelocity = 1000f;
		lowerLegR.maxAngularVelocity = 1000f;

		var w2l = Quaternion.Inverse(hips.rotation);
		hipsPid = new PidControllerRotation(settings.hipsPid, w2l);
		upperLegLPid = new PidController1(settings.upperLegPid);
		lowerLegLPid = new PidController1(settings.lowerLegPid);
		upperLegRPid = new PidController1(settings.upperLegPid);
		lowerLegRPid = new PidController1(settings.lowerLegPid);

		upperLegLength = (upperLegL.position - lowerLegL.position).magnitude;
		lowerLegLength = (lowerLegL.position - footL.position).magnitude;
		upperLegBaseKd = settings.upperLegPid.kd;
		lowerLegBaseKd = settings.lowerLegPid.kd;
	}

	public void OnLandDetectorCollisionStay(bool isEnter, Collision collision, Rigidbody body)
	{
		if (body == lowerLegL)
		{
			landL = true;
		}
		else if (body == lowerLegR)
		{
			landR = true;
		}
//if (isEnter)
{
//Debug.Log("LAND " + isEnter + " " + collision.gameObject.name + " " + body.velocity + " " + collision.contacts[0].point.ToString("F3"));
}
	}

	public void ManualFixedUpdate(float deltaTime)
	{
//Debug.LogWarning("----");
		var goalQ = Quaternion.Euler(settings.hipsAngle, 0, 0);
		var torque = hipsPid.Update(hips.rotation, goalQ, deltaTime);
		hips.AddTorque(torque, ForceMode.Acceleration);
		var hipsV = hips.velocity;
		var hipsDp = hipsV * deltaTime;

		var fl = footL.position;
		var fr = footR.position;
		var fvl = lowerLegL.GetPointVelocity(fl);
		var fvr = lowerLegR.GetPointVelocity(fr);
//Debug.Log("FootV: " + fvl + " " + fvr + " " + lowerLegL.velocity + " " + lowerLegR.velocity + " land=" + landing);
		var ol = upperLegL.position;
		var or = upperLegR.position;

		var hipsUp = hips.rotation * Vector3.up;
		var hipsRight = hips.rotation * Vector3.right;
		var hipsForward = hips.rotation * Vector3.forward;

		var groundNormal = Vector3.Cross(smoothedVelocity, hipsRight);
Debug.Log("Normal : " + groundNormal.ToString("F3"));


		var forward = hipsForward;
		forward.y = 0f;
		forward.Normalize();

		var legLength = upperLegLength + lowerLegLength;
		// hipsを地面に射影した位置
//		var pivotL = (ol + hipsDp) - (Vector3.up * legLength * settings.defaultHeightRatio);
//		var pivotR = (or + hipsDp) - (Vector3.up * legLength * settings.defaultHeightRatio);
		var pivotL = (ol + hipsDp) - (Vector3.up * legLength * settings.defaultHeightRatio) + hipsForward * Vector3.Dot(hips.position - ol, hipsForward);
		var pivotR = (or + hipsDp) - (Vector3.up * legLength * settings.defaultHeightRatio) + hipsForward * Vector3.Dot(hips.position - or, hipsForward);
//		var pivotL = (ol + hipsDp) - (Vector3.up * legLength * settings.defaultHeightRatio) + forward * Vector3.Dot(hips.position - ol, forward);
//		var pivotR = (or + hipsDp) - (Vector3.up * legLength * settings.defaultHeightRatio) + forward * Vector3.Dot(hips.position - or, forward);
		lastPivotL = pivotL;
		lastPivotR = pivotR;
		/* 軸足スコアを算出する
		今一定の力を加えて軸足ポイントまで持っていくとする
		pg = foot + (footV * t) + 0.5 * (a + g) * t^2
		今tを固定として、aを求め、aの大きさが軸足スコアである
		pg - foot - (footV * t) = (a + g) * t^2 / 2
		a = ((pg - foot - (footV * t)) * 2 / t^2) - g
		*/

		var t = deltaTime;
		var dpL = pivotL - fl;
		var dpR = pivotR - fr;
		var al = ((dpL - (fvl * t)) * 2f / (t * t)) - Physics.gravity;
		var ar = ((dpR - (fvr * t)) * 2f / (t * t)) - Physics.gravity;
		var scoreL = al.magnitude;
		var scoreR = ar.magnitude;
if (Mathf.Min(scoreL, scoreR) > 18000f)
{
//Debug.LogError("コケてる!");
}
//Debug.Log(scoreL + " " + scoreR + " " + pivotL + " " + pivotR + " \tA: " + al + " " + ar + " \tV:" + fvl + " " + fvr);
		var forwardSpeed = Vector3.Dot(hipsV, hipsForward);
		var scoreBias = Mathf.Max(settings.pivotScoreThresholdMin, settings.pivotScoreThreshold * Mathf.Max(0f, forwardSpeed));
		if (prevPivotIsLeft) // 現在軸足である方を有利にする
		{
			scoreL -= scoreBias;
		}
		else
		{
			scoreR -= scoreBias;
		}

		var pivotIsLeft = false;
		if (scoreL < scoreR) // 左軸足
		{
			pivotIsLeft = true;
		}

		var resetPivotOnPivotSwitch = (smoothedSpeed < settings.resetPivotOnPivotSwitchSpeedThreshold);
		var resetNonPivotOnPivotSwitch = (smoothedSpeed < settings.resetNonPivotOnPivotSwitchSpeedThreshold);

		if (pivotIsLeft != prevPivotIsLeft)
		{
			landDelta = Vector3.zero;
			if (resetNonPivotOnPivotSwitch)
			{
				if (pivotIsLeft) // 非軸足をリセット
				{
					upperLegRPid.Reset();
					lowerLegRPid.Reset();
				}
				else
				{
					upperLegLPid.Reset();
					lowerLegLPid.Reset();
				}
			}

			if (resetPivotOnPivotSwitch)
			{
				if (pivotIsLeft) // 非軸足をリセット
				{
					upperLegLPid.Reset();
					lowerLegLPid.Reset();
				}
				else
				{
					upperLegRPid.Reset();
					lowerLegRPid.Reset();
				}
			}
Debug.LogError("Switch pivot land=" + landL + "/" + landR + " S: " + scoreL + " " + scoreR + " " + pivotIsLeft);
		}

		var downVector = (hipsUp * legLength * settings.defaultHeightRatio);
		var basePosL = ol - downVector;
		var basePosR = or - downVector;

		var footUpBlend = Mathf.Sin(settings.hipsAngle * Mathf.Deg2Rad);
		Vector3 footGl, footGr;
		if (pivotIsLeft)
		{
			footGl = basePosL + landDelta;
			var mirrored = basePosR - (2f * forward * Vector3.Dot(forward, basePosR - or));
			footGr = Vector3.Lerp(mirrored, or, footUpBlend);
//footGr = mirrored + (Vector3.up * legLength * footUpBlend);
		}
		else // 右軸足
		{
			footGr = basePosR + landDelta;
			var mirrored = basePosL - (2f * forward * Vector3.Dot(forward, basePosL - ol));
			footGl = Vector3.Lerp(mirrored, ol, footUpBlend);
//footGl = mirrored + (Vector3.up * legLength * footUpBlend);
		}

		landDelta -= hipsForward * (Mathf.Max(settings.landOffsetMin, forwardSpeed) * deltaTime * settings.landOffsetFactor);
//landDelta -= hipsForward * (Mathf.Max(settings.landOffsetMin, smoothedSpeed) * deltaTime * settings.landOffsetFactor);
		prevPivotIsLeft = pivotIsLeft;
//Debug.Log(footGl + " " + footGr + " blend:" + footUpBlend + " pivot=" + pivotIsLeft + " ld=" + landDelta);

		// Kdの速度減衰
		var lerpT = Mathf.Clamp01(settings.speedSmoothing * deltaTime);
		smoothedVelocity = Vector3.Lerp(smoothedVelocity, hipsV, lerpT);
		smoothedSpeed = Mathf.Lerp(smoothedSpeed, forwardSpeed, lerpT);
		var kdScale = Mathf.Clamp01(1f + (smoothedSpeed * settings.kdScaleFactor));
		var upperLegKdScale = Mathf.Max(settings.kdScaleUpperLegMin, kdScale);
		var lowerLegKdScale = Mathf.Max(settings.kdScaleLowerLegMin, kdScale);
		settings.upperLegPid.kd = upperLegBaseKd * upperLegKdScale;
		settings.lowerLegPid.kd = lowerLegBaseKd * lowerLegKdScale;


		// 膝角度の符号を決定
/*
		var o2fl = fl - ol;
		var o2fr = fr - or;
		var o2fPlaneNormalL = Vector3.Cross(o2fl, hipsRight);
		var o2fPlaneNormalR = Vector3.Cross(o2fr, hipsRight);
		var o2kl = lowerLegL.position - ol;
		var o2kr = lowerLegR.position - or;
		var dotL = Vector3.Dot(o2kl, o2fPlaneNormalL);
		var dotR = Vector3.Dot(o2kr, o2fPlaneNormalR);
		var kneeAnglePositiveL = (dotL > 0f);
		var kneeAnglePositiveR = (dotR > 0f);
*/
var kneeAnglePositiveL = true;
var kneeAnglePositiveR = true; // 一旦固定
	 	

//Debug.Log("P: " + fl + " -> " + footGl + " " + fr + " -> " + footGr);
//Debug.LogWarning("BEGIN FOOT CALC");
//Debug.Log("KneeAngleSign " + kneeAnglePositiveL + " " + kneeAnglePositiveR + " dot " + dotL + " " + dotR);
		// 現在角算出
		float upperLegLAngle, lowerLegLAngle, upperLegRAngle, lowerLegRAngle;
		CalcLegAngles(hips.rotation, ol, kneeAnglePositiveL, fl, upperLegLength, lowerLegLength, out upperLegLAngle, out lowerLegLAngle);
		CalcLegAngles(hips.rotation, or, kneeAnglePositiveR, fr, upperLegLength, lowerLegLength, out upperLegRAngle, out lowerLegRAngle);

		// 目標角算出
		var foL = upperLegL.position; // 股関節位置は予測位置で計算
		var foR = upperLegR.position;
		float upperLegLAngleG, lowerLegLAngleG, upperLegRAngleG, lowerLegRAngleG;

//		var footPidLerp = Mathf.Max(settings.footPidLerpScaleMin, 1f - (smoothedSpeed * settings.footPidLerpScaleFactor));
		var footPidLerp = 1f - Mathf.Sin(settings.hipsAngle * Mathf.Deg2Rad);
footPidLerp = settings.footPidLerp;
		var midGl = Vector3.Lerp(fl, footGl, footPidLerp);
		var midGr = Vector3.Lerp(fr, footGr, footPidLerp);
		CalcLegAngles(hips.rotation, foL, kneeAnglePositiveL, midGl, upperLegLength, lowerLegLength, out upperLegLAngleG, out lowerLegLAngleG);
		CalcLegAngles(hips.rotation, foR, kneeAnglePositiveR, midGr, upperLegLength, lowerLegLength, out upperLegRAngleG, out lowerLegRAngleG);

goalSphereL.position = footGl;
goalSphereR.position = footGr;
tmpGoalSphereL.position = midGl;
tmpGoalSphereR.position = midGr;
//tmpGoalSphereL.position = pivotL;
//tmpGoalSphereR.position = pivotR;
tmpGoalSphereL.position = basePosL;
tmpGoalSphereR.position = basePosR;

//Debug.Log(lowerLegLAngle + " " + lowerLegRAngle + "\t " + upperLegLAngle + " " + upperLegRAngle + " score=" + scoreL + " " + scoreR);

		// 制御
		var torqueFactor = 1f / footPidLerp;
		var torqueUL = upperLegLPid.Update(upperLegLAngle, upperLegLAngleG, deltaTime) * torqueFactor;
		var torqueLL = lowerLegLPid.Update(lowerLegLAngle, lowerLegLAngleG, deltaTime) * torqueFactor;
		var torqueUR = upperLegRPid.Update(upperLegRAngle, upperLegRAngleG, deltaTime) * torqueFactor;
		var torqueLR = lowerLegRPid.Update(lowerLegRAngle, lowerLegRAngleG, deltaTime) * torqueFactor;
		upperLegL.AddTorque(upperLegL.transform.right * torqueUL, ForceMode.Acceleration);
		lowerLegL.AddTorque(lowerLegL.transform.right * torqueLL, ForceMode.Acceleration);
		upperLegR.AddTorque(upperLegR.transform.right * torqueUR, ForceMode.Acceleration);
		lowerLegR.AddTorque(lowerLegR.transform.right * torqueLR, ForceMode.Acceleration);
Debug.Log("KdScale: " + kdScale + " " + upperLegKdScale + " spd=" + smoothedSpeed + " resetNP=" + resetNonPivotOnPivotSwitch + " resetP=" + resetPivotOnPivotSwitch + " pidLerp=" + footPidLerp +" footUpBlend=" + footUpBlend);

		landL = landR = false;

#if true
upperLegLAngle *= Mathf.Rad2Deg;
lowerLegLAngle *= Mathf.Rad2Deg;
upperLegRAngle *= Mathf.Rad2Deg;
lowerLegRAngle *= Mathf.Rad2Deg;
upperLegLAngleG *= Mathf.Rad2Deg;
lowerLegLAngleG *= Mathf.Rad2Deg;
upperLegRAngleG *= Mathf.Rad2Deg;
lowerLegRAngleG *= Mathf.Rad2Deg;

Debug.Log(
	upperLegLAngle + " -> " + upperLegLAngleG + " t=" + torqueUL + '\n' +
	lowerLegLAngle + " -> " + lowerLegLAngleG + " t=" + torqueLL + '\n' +
	upperLegRAngle + " -> " + upperLegRAngleG + " t=" + torqueUR + '\n' +
	lowerLegRAngle + " -> " + lowerLegRAngleG + " t=" + torqueLR);
#endif
	}

	// non public ----
	PidControllerRotation hipsPid;
	PidController1 upperLegLPid;
	PidController1 lowerLegLPid;
	PidController1 upperLegRPid;
	PidController1 lowerLegRPid;
	bool prevPivotIsLeft = false;
	Vector3 landDelta;
	bool landL;
	bool landR;
	// 挙動変更用平滑化速度
	float smoothedSpeed;
	Vector3 smoothedVelocity;
	// Kd調整
	float upperLegBaseKd;
	float lowerLegBaseKd;
	float upperLegLength;
	float lowerLegLength;

	// デバ用
	Vector3 lastPivotL;
	Vector3 lastPivotR;

	void CalcLegAngles(
		Quaternion hipsRotation,
		Vector3 upperLegOrigin,
		bool kneeAnglePositive,
		Vector3 footGoalPos, 
		float upperLegLength,
		float lowerLegLength,
		out float upperLegAngle, 
		out float lowerLegAngle)
	{
		var kneeAngleSign = (kneeAnglePositive) ? 1f : -1f;
		// 膝角度(足-膝-股関節)
		var o2f = footGoalPos - upperLegOrigin;
		var o2fLen = o2f.magnitude;
		var angleFKO = CalcCenterAngle(upperLegLength, lowerLegLength, o2fLen);
//Debug.Log("\tFKO: " + upperLegLength + " " + lowerLegLength + " " + o2fLen + " -> " + (angleFKO * Mathf.Rad2Deg));
		lowerLegAngle = (Mathf.PI - angleFKO) * kneeAngleSign;

		// 股関節A(足-股関節-膝)
		var angleFOK = CalcCenterAngle(o2fLen, upperLegLength, lowerLegLength);
//Debug.Log("\tFOK: " + o2fLen + " " + upperLegLength + " " + lowerLegLength + " -> " + (angleFOK * Mathf.Rad2Deg));
		// 股関節角度の符号は膝と同一
		angleFOK *= kneeAngleSign;

		// 股関節B(足-股関節-胸)
		// まず背骨にもう一点取って胸とする。大腿原点からY方向に1m足したものにしよう
		var hipsUp = hipsRotation * Vector3.up;
		var chest = upperLegOrigin + (hipsUp * 1f);
		var o2cLen = (chest - upperLegOrigin).magnitude;
		var f2cLen = (chest - footGoalPos).magnitude;
		var angleFOC = CalcCenterAngle(o2fLen, o2cLen, f2cLen);
//Debug.Log("\tFOC:" + o2fLen + " " + o2cLen + " " + f2cLen + " -> " + (angleFOC * Mathf.Rad2Deg));
		// 足が、体幹前面より前にあれば角は+、後ろにあれば-とする
		var hipsForward = hipsRotation * Vector3.forward;
		var dot = Vector3.Dot(o2f, hipsForward);
		upperLegAngle = ((dot > 0f) ? (angleFOC - Mathf.PI) : (Mathf.PI - angleFOC)) - angleFOK;
//Debug.Log("\tFinal: " + (lowerLegAngle * Mathf.Rad2Deg) + " " + (upperLegAngle * Mathf.Rad2Deg) + " kneeSign=" + kneeAngleSign + " dot=" + dot);
	}

	float CalcCenterAngle(float l0c, float l1c, float l01) // 0-cの距離、1-cの距離、0-1の距離から、0-c-1の角度を求める
	{
		var cos = ((l0c * l0c) + (l1c * l1c) - (l01 * l01)) / (2f * l0c * l1c);
if (cos < -1f || cos > 1f)
{
//Debug.LogWarning("acos out of range: " + (Mathf.Abs(cos) - 1f) + " " + l0c + " " + l1c + " " + l01);
}
		cos = Mathf.Clamp(cos, -1f, 1f); // 演算誤差対策
		var angle = Mathf.Acos(cos);
		return angle;
	}
#if UNITY_EDITOR
	[CustomEditor(typeof(Man))]
	public class Inspector : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			var self = target as Man;

			EditorGUILayout.LabelField("HipsCenter", self.hips.position.ToString("F3"));
			EditorGUILayout.LabelField("PivotL", self.lastPivotL.ToString("F3"));
			EditorGUILayout.LabelField("PivotR", self.lastPivotR.ToString("F3"));
			EditorGUILayout.LabelField("HipsZ+", (self.hips.rotation * Vector3.forward).ToString("F3"));
			EditorGUILayout.LabelField("LandL", self.landL.ToString());
			EditorGUILayout.LabelField("LandR", self.landR.ToString());
			EditorGUILayout.LabelField("SmoothedSpeed", self.smoothedSpeed.ToString("F3"));
			EditorGUILayout.LabelField("SmoothedVelocity", self.smoothedVelocity.ToString("F3"));
		}
	}
#endif
}
