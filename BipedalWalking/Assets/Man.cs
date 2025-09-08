using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kayac;

public class Man : MonoBehaviour
{
	[System.Serializable]
	class Settings
	{
		[SerializeField] public float hipsAngle;
		[SerializeField] public float defaultHeightRatio = 0.9f;
		[SerializeField] public float upFootHeight = 0.3f;
		[SerializeField] public float upFootForward = 0.3f;
		[SerializeField] public float footPidLerp = 0.5f;
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
	public float VelocityZ => hips.velocity.z;

	public void ManualStart()
	{
		var A = CalcCenterAngle(2f, 2f, 1f);

		hips.maxAngularVelocity = 100f;
		upperLegL.maxAngularVelocity = 100f;
		upperLegR.maxAngularVelocity = 100f;
		lowerLegL.maxAngularVelocity = 100f;
		lowerLegR.maxAngularVelocity = 100f;

		var w2l = Quaternion.Inverse(hips.rotation);
		hipsPid = new PidControllerRotation(settings.hipsPid, w2l);
		upperLegLPid = new PidController1(settings.upperLegPid);
		lowerLegLPid = new PidController1(settings.lowerLegPid);
		upperLegRPid = new PidController1(settings.upperLegPid);
		lowerLegRPid = new PidController1(settings.lowerLegPid);

		upperLegLength = (upperLegL.position - lowerLegL.position).magnitude;
		lowerLegLength = (lowerLegL.position - footL.position).magnitude;
	}

	public void ManualFixedUpdate(float deltaTime)
	{
Debug.LogWarning("----");
		var goalQ = Quaternion.Euler(settings.hipsAngle, 0, 0);
		var torque = hipsPid.Update(hips.rotation, goalQ, deltaTime);
		hips.AddTorque(torque, ForceMode.Acceleration);
		var hipsV = hips.velocity;
		var hipsDp = hipsV * deltaTime;

		var fl = footL.position;
		var fr = footR.position;
		var fvl = lowerLegL.GetPointVelocity(fl);
		var fvr = lowerLegR.GetPointVelocity(fr);
		var ol = upperLegL.position;
		var or = upperLegR.position;

		var hipsUp = hips.rotation * Vector3.up;
		var hipsForward = hips.rotation * Vector3.forward;

		var legLength = upperLegLength + lowerLegLength;
		var basePosL = ol - (hipsUp * legLength * settings.defaultHeightRatio);// + hipsDp;
		var basePosR = or - (hipsUp * legLength * settings.defaultHeightRatio);// + hipsDp;
		// hipsを地面に射影した位置
		var pivotPosL = (ol + hipsDp) - (Vector3.up * legLength * settings.defaultHeightRatio) + hipsForward * Vector3.Dot(hips.position - ol, hipsForward);
		var pivotPosR = (or + hipsDp) - (Vector3.up * legLength * settings.defaultHeightRatio) + hipsForward * Vector3.Dot(hips.position - or, hipsForward);
		// 軸足スコアを算出する
		// 今一定の力を加えて軸足ポイントまで持っていくとする
		// pg = foot + (footV * t) + 0.5 * a * t^2
		// 今tを固定として、aを求め、aの大きさが軸足スコアである
		var t = deltaTime;
		var dpL = pivotPosL - fl;
		var dpR = pivotPosR - fr;
		var al = (dpL - (fvl * t)) * 2f / (t * t);
		var ar = (dpR - (fvr * t)) * 2f / (t * t);
		var scoreL = al.magnitude;
		var scoreR = ar.magnitude;
//Debug.Log(scoreL + " " + scoreR + " " + pivotPosL + " " + pivotPosR + " \t " + fl + " " + fr + " \t " + dpL + " " + dpR + " \t " + fvl + " " + fvr);

		Vector3 footGl, footGr;
		var pivotIsLeft = false;
		if (scoreL < scoreR) // 左軸足
		{
			pivotIsLeft = true;
		}

		if (pivotIsLeft != prevPivotIsLeft)
		{
			landDelta = Vector3.zero;
		}

		var hipsRight = hips.rotation * Vector3.right;
		var forward = Vector3.Cross(hipsRight, Vector3.up);
		forward.Normalize();
Debug.Log("Forward " + forward);
//Debug.Log(landDelta);

//		var upFootHeight = Mathf.Sin(settings.hipsAngle * Mathf.Deg2Rad) * 1.5f;
//		var upFootForward = Mathf.Sin(settings.hipsAngle * Mathf.Deg2Rad) * 1.5f;
//Debug.Log("upFoot: " + upFootHeight + " " + upFootForward);
		if (pivotIsLeft)
//if (true)
		{
			footGl = basePosL - landDelta;
			var mirrored = basePosR - (2f * forward * Vector3.Dot(forward, basePosR - or));
			footGr = (or + mirrored) * 0.5f;
//			footGr = basePosR + (hipsUp * upFootHeight) + (hipsForward * upFootForward);
		}
		else // 右軸足
		{
			footGr = basePosR - landDelta;
			var mirrored = basePosL - (2f * forward * Vector3.Dot(forward, basePosL - ol));
			footGl = (ol + mirrored) * 0.5f;
//			footGl = basePosL + (hipsUp * upFootHeight) + (hipsForward * upFootForward);
		}

Debug.Log(footGl + " " + footGr);
		landDelta += hipsForward * (Mathf.Max(0.2f, Vector3.Dot(hipsForward, hipsV)) * deltaTime);
		prevPivotIsLeft = pivotIsLeft;

		// 膝角度の符号を決定
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
kneeAnglePositiveL = kneeAnglePositiveR = true; // 一旦固定
	 	

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
		var midGl = Vector3.Lerp(fl, footGl, settings.footPidLerp);
		var midGr = Vector3.Lerp(fr, footGr, settings.footPidLerp);
		CalcLegAngles(hips.rotation, foL, kneeAnglePositiveL, midGl, upperLegLength, lowerLegLength, out upperLegLAngleG, out lowerLegLAngleG);
		CalcLegAngles(hips.rotation, foR, kneeAnglePositiveR, midGr, upperLegLength, lowerLegLength, out upperLegRAngleG, out lowerLegRAngleG);

goalSphereL.position = footGl;
goalSphereR.position = footGr;
tmpGoalSphereL.position = midGl;
tmpGoalSphereR.position = midGr;

//Debug.Log(lowerLegLAngle + " " + lowerLegRAngle + "\t " + upperLegLAngle + " " + upperLegRAngle + " score=" + scoreL + " " + scoreR);

		// 制御
		var torqueFactor = 1f / settings.footPidLerp;
		var torqueUL = upperLegLPid.Update(upperLegLAngle, upperLegLAngleG, deltaTime) * torqueFactor;
		var torqueLL = lowerLegLPid.Update(lowerLegLAngle, lowerLegLAngleG, deltaTime) * torqueFactor;
		var torqueUR = upperLegRPid.Update(upperLegRAngle, upperLegRAngleG, deltaTime) * torqueFactor;
		var torqueLR = lowerLegRPid.Update(lowerLegRAngle, lowerLegRAngleG, deltaTime) * torqueFactor;
		upperLegL.AddTorque(upperLegL.transform.right * torqueUL, ForceMode.Acceleration);
		lowerLegL.AddTorque(lowerLegL.transform.right * torqueLL, ForceMode.Acceleration);
		upperLegR.AddTorque(upperLegR.transform.right * torqueUR, ForceMode.Acceleration);
		lowerLegR.AddTorque(lowerLegR.transform.right * torqueLR, ForceMode.Acceleration);

upperLegLAngle *= Mathf.Rad2Deg;
lowerLegLAngle *= Mathf.Rad2Deg;
upperLegRAngle *= Mathf.Rad2Deg;
lowerLegRAngle *= Mathf.Rad2Deg;
upperLegLAngleG *= Mathf.Rad2Deg;
lowerLegLAngleG *= Mathf.Rad2Deg;
upperLegRAngleG *= Mathf.Rad2Deg;
lowerLegRAngleG *= Mathf.Rad2Deg;
/*
Debug.Log(
	upperLegLAngle + " -> " + upperLegLAngleG + " t=" + torqueUL + '\n' +
	lowerLegLAngle + " -> " + lowerLegLAngleG + " t=" + torqueLL + '\n' +
	upperLegRAngle + " -> " + upperLegRAngleG + " t=" + torqueUR + '\n' +
	lowerLegRAngle + " -> " + lowerLegRAngleG + " t=" + torqueLR);
*/
//Debug.Log(upperLegLPid.ErrorSum + " " + lowerLegLPid.ErrorSum);
	}

	// non public ----
	PidControllerRotation hipsPid;
	PidController1 upperLegLPid;
	PidController1 lowerLegLPid;
	PidController1 upperLegRPid;
	PidController1 lowerLegRPid;
	float upperLegLength;
	float lowerLegLength;
	bool prevPivotIsLeft = false;
	Vector3 landDelta;

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
}
