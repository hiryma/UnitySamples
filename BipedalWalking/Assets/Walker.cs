#define DEBUG
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kayac;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Walker : MonoBehaviour
{
	[System.Serializable]
	class Settings
	{
		[SerializeField] float hipsAngle = 32f;
		[SerializeField] float defaultHeightRatio = 0.98f;
		[SerializeField] float footPidLerp = 0.5f;
		[SerializeField] float landOffsetFactor = 1f;
		[SerializeField] float landOffsetMin = 2f;
		[SerializeField] float pivotScoreThreshold = 500f;
		[SerializeField] float pivotScoreThresholdMin = 500f;
		[SerializeField] float kdScaleFactorUpper = -0.2f;
		[SerializeField] float kdScaleFactorLower = -0.2f;
		[SerializeField] float kdScaleUpperLegMin = 0.25f;
		[SerializeField] float kdScaleLowerLegMin = 0f;
		[SerializeField] float speedSmoothing = 1f;
		[SerializeField] float legOriginHeightSmoothing = 1f;
		[SerializeField] float resetPivotOnPivotSwitchSpeedThreshold = 2f;
		[SerializeField] float resetNonPivotOnPivotSwitchSpeedThreshold = 1.5f;
		[SerializeField] PidSettings hipsPid;
		[SerializeField] PidSettings upperLegPid;
		[SerializeField] PidSettings lowerLegPid;

		public float HipsAngle => hipsAngle;
		public float DefaultHeightRatio => defaultHeightRatio;
		public float FootPidLerp => footPidLerp;
		public float LandOffsetFactor => landOffsetFactor;
		public float LandOffsetMin => landOffsetMin;
		public float PivotScoreThreshold => pivotScoreThreshold;
		public float PivotScoreThresholdMin => pivotScoreThresholdMin;
		public float KdScaleFactorUpper => kdScaleFactorUpper;
		public float KdScaleFactorLower => kdScaleFactorLower;
		public float KdScaleUpperLegMin => kdScaleUpperLegMin;
		public float KdScaleLowerLegMin => kdScaleLowerLegMin;
		public float SpeedSmoothing => speedSmoothing;
		public float ResetPivotOnPivotSwitchSpeedThreshold => resetPivotOnPivotSwitchSpeedThreshold;
		public float ResetNonPivotOnPivotSwitchSpeedThreshold => resetNonPivotOnPivotSwitchSpeedThreshold;
		public float LegOriginHeightSmoothing => legOriginHeightSmoothing;
		public PidSettings HipsPid => hipsPid;
		public PidSettings UpperLegPid => upperLegPid;
		public PidSettings LowerLegPid => lowerLegPid;
	}
	[SerializeField] Settings[] settingsList;
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
		hips.maxAngularVelocity = 100f;
		upperLegL.maxAngularVelocity = 100f;
		upperLegR.maxAngularVelocity = 100f;
		lowerLegL.maxAngularVelocity = 100f;
		lowerLegR.maxAngularVelocity = 100f;

		var w2l = Quaternion.Inverse(hips.rotation);

		// 仮で生成
		hipsPidSettings = new PidSettings(0f, 0f, 0f);
		upperLegPidSettings = new PidSettings(0f, 0f, 0f);
		lowerLegPidSettings = new PidSettings(0f, 0f, 0f);

		hipsPid = new PidControllerRotation(hipsPidSettings, w2l * hips.rotation);
		upperLegLPid = new PidController1(upperLegPidSettings);
		upperLegRPid = new PidController1(upperLegPidSettings);
		lowerLegLPid = new PidController1(lowerLegPidSettings);
		lowerLegRPid = new PidController1(lowerLegPidSettings);

		upperLegLength = (upperLegL.position - lowerLegL.position).magnitude;
		lowerLegLength = (lowerLegL.position - footL.position).magnitude;
prevFootGoalL = footL.position;
prevFootGoalR = footR.position;
		smoothedLegOriginHeight = upperLegLength + lowerLegLength; // 仮初期値
		lowerLegToFootL = lowerLegL.transform.InverseTransformPoint(footL.position);
		lowerLegToFootR = lowerLegR.transform.InverseTransformPoint(footR.position);
		upperToLowerLegL = upperLegL.transform.InverseTransformPoint(lowerLegL.position);
		upperToLowerLegR = upperLegR.transform.InverseTransformPoint(lowerLegR.position);

		upperLegBaseKdList = new float[settingsList.Length];
		lowerLegBaseKdList = new float[settingsList.Length];
		for (var i = 0; i < settingsList.Length; i++)
		{
			upperLegBaseKdList[i] = settingsList[i].UpperLegPid.kd;
			lowerLegBaseKdList[i] = settingsList[i].LowerLegPid.kd;
		}
	}

	public void OnLandDetectorCollisionStay(bool isEnter, Collision collision, Rigidbody body)
	{
		var owner = collision.gameObject.GetComponentInParent<Walker>();

		if (owner != this)
		{
			var contact = collision.contacts[0];
			var normal = contact.normal;
			if (body == lowerLegL)
			{
				landNormalL += normal;
				var legOrigin = upperLegL.position;
				var height = Vector3.Dot(legOrigin - contact.point, normal);
				smoothedLegOriginHeight = Mathf.Lerp(smoothedLegOriginHeight, height, Mathf.Clamp01(Time.fixedDeltaTime * currentSettings.LegOriginHeightSmoothing));
//Debug.Log("LandL " + normal + " -> " + landNormalL);
			}
			else if (body == lowerLegR)
			{
				landNormalR += normal;
				var legOrigin = upperLegR.position;
				var height = Vector3.Dot(legOrigin - contact.point, normal);
				smoothedLegOriginHeight = Mathf.Lerp(smoothedLegOriginHeight, height, Mathf.Clamp01(Time.fixedDeltaTime * currentSettings.LegOriginHeightSmoothing));
//Debug.Log("LandR " + normal + " -> " + landNormalR);	
			}

		}
	}

	public void ManualFixedUpdate(float deltaTime, float hipsAngleGoal, Vector3 forwardGoal, float pidFactor, float backCalculationTt) // yは無視される
	{
		if (hips == null) // 体幹が死んでるので抜ける
		{
			return;
		}

		// 設定を選ぶ
		var hipsAngleDiffMin = float.MaxValue;
		var settingsIndex = -1;
		for (var i = 0; i < settingsList.Length; i++)
		{
			var s = settingsList[i];
			var diff = Mathf.Abs(hipsAngleGoal - s.HipsAngle);
			if (diff < hipsAngleDiffMin)
			{
				hipsAngleDiffMin = diff;
				settingsIndex = i;
			}
		}

		var settings = settingsList[settingsIndex];
		currentSettings = settings;
		hipsPidSettings.CopyFrom(settings.HipsPid);
		upperLegPidSettings.CopyFrom(settings.UpperLegPid);
		lowerLegPidSettings.CopyFrom(settings.LowerLegPid);

		forwardGoal.y = 0f;
		var goalQ = Quaternion.LookRotation(forwardGoal, Vector3.up) * Quaternion.Euler(settings.HipsAngle, 0f, 0f);
		if (hips != null)
		{
			var torque = hipsPid.Update(hips.rotation, goalQ, deltaTime, pidFactor, backCalculationTt) * pidFactor;
			hips.AddTorque(torque);
		}
//Debug.Log(hips.rotation + " " + goalQ + " \t " + forwardGoal + " " + (hips.rotation * Vector3.forward) + " " + torque + " " + deltaTime + " " + hipsPid.PrevError + " " + hipsPid.ErrorSum);
		var hipsV = hips.velocity;
		var hipsDp = hipsV * deltaTime;

//		var fl = footL.position;
		Vector3 fl, fvl, ol;
		if (upperLegL != null)
		{
			ol = upperLegL.position;
			if (lowerLegL != null)
			{
				fl = lowerLegL.transform.TransformPoint(lowerLegToFootL);
				fvl = lowerLegL.GetPointVelocity(fl);
			}
			else
			{
				fl = upperLegL.transform.TransformPoint(upperToLowerLegL);
				fvl = upperLegL.GetPointVelocity(fl);
			}
		}
		else
		{
			ol = hips.position;
			fl = hips.position;
			fvl = hips.velocity;
		}

		Vector3 fr, fvr, or;
		if (upperLegR != null)
		{
			or = upperLegR.position;
			if (lowerLegR != null)
			{
				fr = lowerLegR.transform.TransformPoint(lowerLegToFootR);
				fvr = lowerLegR.GetPointVelocity(fr);
			}
			else
			{
				fr = upperLegR.transform.TransformPoint(upperToLowerLegR);
				fvr = upperLegR.GetPointVelocity(fr);
			}
		}
		else
		{
			or = hips.position;
			fr = hips.position;
			fvr = hips.velocity;
		}

		var hipsUp = hips.rotation * Vector3.up;
		var hipsRight = hips.rotation * Vector3.right;
		var hipsForward = hips.rotation * Vector3.forward;

		landNormalL = Vector3.Lerp(landNormalL, Vector3.up, settings.SpeedSmoothing * deltaTime);
		landNormalR = Vector3.Lerp(landNormalR, Vector3.up, settings.SpeedSmoothing * deltaTime);

		var groundNormal = (landNormalL + landNormalR).normalized;
		// 移動前方ベクタ = hipsForwardからgroundNormal成分を除いたもの
		var moveForward = hipsForward - (groundNormal * Vector3.Dot(hipsForward, groundNormal));
		moveForward.Normalize();

		var legLength = upperLegLength + lowerLegLength;
		// hipsを地面に射影した位置
		var effLegLength = legLength * settings.DefaultHeightRatio;
		var effLegGroundVector = groundNormal * effLegLength;
#if true
		var pivotL = (ol + hipsDp) - effLegGroundVector;// + (moveForward * Vector3.Dot(hips.position - ol, moveForward));
		var pivotR = (or + hipsDp) - effLegGroundVector;// + (moveForward * Vector3.Dot(hips.position - or, moveForward));
#else
		var pivotL = (ol + hipsDp) - effLegGroundVector + (hipsForward * Vector3.Dot(hips.position - ol, hipsForward));
		var pivotR = (or + hipsDp) - effLegGroundVector + (hipsForward * Vector3.Dot(hips.position - or, hipsForward));
#endif
		lastPivotL = pivotL;
		lastPivotR = pivotR;

		// 軸足スコアを算出する
#if true
		/*
		今、力を加えて軸足ポイント及び目標速度まで持っていくとする
		p' = pivot + v'*t 

		v0 = v + a0*t/2 ... f1
		v' = v0 + a1*t/2 ... f2
		p' = p + vt/2 + a0*(t/2)^2/2 + v0*t/2 + a1*(t/2)^2/2 ... f3

		a0,a1を求め、この大きさの和を軸足スコアとする

		f1をf2,f3に入れる

		v' = v + (a0 + a1)*t/2 ... f4
		p' = p + vt/2 + a0*(t/2)^2/2 + (v + a0*t/2)*t/2 + a1*(t/2)^2/2
		   = p + vt/2 + a0*t^2/8 + vt/2 + a0*t^2/4 + a1*t^2/8
		   = p + vt + 3a0*t^2/8 + a1*t^2/8 ... f5

		f4をa1について解く
		a1 = ((v' - v)*2 / t) - a0 ... f6
		f6をf5に入れる
		p' = p + vt + 3a0*t^2/8 + (((v' - v)*2 / t) - a0)*t^2/8
		   = p + vt + 3a0*t^2/8 + (v' - v)*t/4 - a0*t^2/8
		   = p + vt + (v' - v)*t/4 + a0*t^2/4
		   = p + (3/4)vt + (1/4)v't + a0*t^2/4 ... f7

		a0が求まる
		p' - p - 3vt/4 - v't/4 = a0*t^2/4
		4/(t^2)*(p' - p - 3vt/4 - v't/4) = a0

		*/
		var goalVelocity = moveForward * smoothedSpeed;
		var dpL = pivotL - fl;
		var dpR = pivotR - fr;
		var t = deltaTime;
		var a0l = (4f / (t * t) * dpL) - (((3f * fvl) + goalVelocity) / t);
		var a0r = (4f / (t * t) * dpR) - (((3f * fvr) + goalVelocity) / t);
		var v0l = fvl + (a0l * t / 2f);
		var v0r = fvr + (a0r * t / 2f);
		var al1 = (goalVelocity - v0l) * 2f / t;
		var ar1 = (goalVelocity - v0r) * 2f / t;

		var scoreL = a0l.magnitude + al1.magnitude;
		var scoreR = a0r.magnitude + ar1.magnitude;

//		Debug.Log("\t" + a0l + " " + a0r + " " + al1 + " " + ar1 + " \t " + scoreL + " " + scoreR);
#else 
		/*
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

//		Debug.Log("\t" + fvl + " " + fvr + " " + al + " " + ar);
#endif


#if true // 速度スコアバイアス
		var forwardSpeed = Vector3.Dot(hipsV, moveForward);
		var scoreBias = Mathf.Max(settings.PivotScoreThresholdMin, settings.PivotScoreThreshold * Mathf.Max(0f, forwardSpeed));
		if (prevPivotIsLeft) // 現在軸足である方を有利にする
		{
			scoreL -= scoreBias;
		}
		else
		{
			scoreR -= scoreBias;
		}
#endif

		var pivotIsLeft = false;
		if (scoreL < scoreR) // 左軸足
		{
			pivotIsLeft = true;
		}

		var resetPivotOnPivotSwitch = (smoothedSpeed < settings.ResetPivotOnPivotSwitchSpeedThreshold);
		var resetNonPivotOnPivotSwitch = (smoothedSpeed < settings.ResetNonPivotOnPivotSwitchSpeedThreshold);

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
Debug.LogError("Switch pivot land=" + landNormalL + " / " + landNormalR + " S: " + scoreL + " " + scoreR + " " + pivotIsLeft);
		}

		var downVector = hipsUp * (legLength * settings.DefaultHeightRatio);
		var basePosL = ol - downVector;
		var basePosR = or - downVector;

		var footUpBlend = Mathf.Sin(settings.HipsAngle * Mathf.Deg2Rad);
		Vector3 gfl, gfr;
		if (pivotIsLeft)
		{
			gfl = basePosL + landDelta;
			var mirrored = basePosR - (2f * moveForward * Vector3.Dot(moveForward, basePosR - or));
			gfr = Vector3.Lerp(mirrored, or, footUpBlend);
		}
		else // 右軸足
		{
			gfr = basePosR + landDelta;
			var mirrored = basePosL - (2f * moveForward * Vector3.Dot(moveForward, basePosL - ol));
			gfl = Vector3.Lerp(mirrored, ol, footUpBlend);
		}
		landDelta -= hipsForward * (Mathf.Max(settings.LandOffsetMin, forwardSpeed) * deltaTime * settings.LandOffsetFactor);
		prevPivotIsLeft = pivotIsLeft;

		// Kdの速度減衰
		var lerpT = Mathf.Clamp01(settings.SpeedSmoothing * deltaTime);
		smoothedSpeed = Mathf.Lerp(smoothedSpeed, forwardSpeed, lerpT);
		var kdScale = Mathf.Exp(smoothedSpeed * settings.KdScaleFactorUpper);
		kdScale = Mathf.Clamp01(kdScale);
		var upperLegKdScale = Mathf.Max(settings.KdScaleUpperLegMin, kdScale);
		kdScale = Mathf.Exp(smoothedSpeed * settings.KdScaleFactorLower);
		kdScale = Mathf.Clamp01(kdScale);
		var lowerLegKdScale = Mathf.Max(settings.KdScaleLowerLegMin, kdScale);
		upperLegPidSettings.kd = upperLegBaseKdList[settingsIndex] * upperLegKdScale;
		lowerLegPidSettings.kd = lowerLegBaseKdList[settingsIndex] * lowerLegKdScale;

		var kneeAnglePositiveL = true;
		var kneeAnglePositiveR = true; // 一旦固定

		// 現在角算出
		float upperLegLAngle, lowerLegLAngle, upperLegRAngle, lowerLegRAngle;
		CalcLegAngles(hips.rotation, ol, kneeAnglePositiveL, fl, upperLegLength, lowerLegLength, out upperLegLAngle, out lowerLegLAngle);
		CalcLegAngles(hips.rotation, or, kneeAnglePositiveR, fr, upperLegLength, lowerLegLength, out upperLegRAngle, out lowerLegRAngle);
		// 目標角算出
		var futureOl = ol;//+ hipsDp; // 股関節位置は予測位置で計算
		var futureOr = or;// + hipsDp;
		float upperLegLAngleG, lowerLegLAngleG, upperLegRAngleG, lowerLegRAngleG;

		var footPidLerp = settings.FootPidLerp;



#if false
		var idealLegOriginHeight = Mathf.Cos(settings.HipsAngle * Mathf.Deg2Rad) * (upperLegLength + lowerLegLength) * settings.DefaultHeightRatio;
		var ratio = Mathf.Clamp01(smoothedLegOriginHeight / idealLegOriginHeight);
		var lerp = Mathf.Clamp01(128f * Mathf.Pow(1f - ratio, 2f));
		footPidLerp = Mathf.Lerp(footPidLerp, 1f, lerp);
		if (lerp >= 1f)
		{
			Debug.LogError(footPidLerp + "\t" + smoothedLegOriginHeight + '\t' + idealLegOriginHeight + "\tR=" + ratio + "\tL=" + lerp);
		}
#endif

		var midGl = Vector3.Lerp(fl, gfl, footPidLerp);
		var midGr = Vector3.Lerp(fr, gfr, footPidLerp);

		// 緊急回避: 目標を股関節より上に置かない
		var dotL = Vector3.Dot(midGl - futureOl, groundNormal);
		if (dotL > 0f)
		{
			midGl -= groundNormal * dotL;
		}
		var dotR = Vector3.Dot(midGr - futureOr, groundNormal);
		if (dotR > 0f)
		{
			midGr -= groundNormal * dotR;
		}

//prevFootGoalL = midGl;
//prevFootGoalR = midGr;

//Debug.LogWarning("BEGIN GoalFoot CALC midGl=" + midGl + " midGr=" + midGr + " fol=" + futureOl + " for=" + futureOr + " dl=" + (futureOl - midGl).magnitude + " dr=" + (futureOr - midGr).magnitude);
		CalcLegAngles(hips.rotation, futureOl, kneeAnglePositiveL, midGl, upperLegLength, lowerLegLength, out upperLegLAngleG, out lowerLegLAngleG);
		CalcLegAngles(hips.rotation, futureOr, kneeAnglePositiveR, midGr, upperLegLength, lowerLegLength, out upperLegRAngleG, out lowerLegRAngleG);

//Debug.Log(lowerLegLAngle + " " + lowerLegRAngle + "\t " + upperLegLAngle + " " + upperLegRAngle + " score=" + scoreL + " " + scoreR);

		// 制御
		var torqueFactor = 1f / footPidLerp;
		var torqueUL = 0f;
		var torqueLL = 0f;
		var torqueUR = 0f;
		var torqueLR = 0f;
		if (upperLegL != null)
		{
			torqueUL = upperLegLPid.Update(upperLegLAngle, upperLegLAngleG, deltaTime, pidFactor, backCalculationTt) * torqueFactor * pidFactor;
			upperLegL.AddTorque(hips.transform.right * torqueUL);
		}

		if (lowerLegL != null)
		{
			torqueLL = lowerLegLPid.Update(lowerLegLAngle, lowerLegLAngleG, deltaTime, pidFactor, backCalculationTt) * torqueFactor * pidFactor;
			lowerLegL.AddTorque(upperLegL.transform.right * torqueLL);
		}

		if (upperLegR != null)
		{
			torqueUR = upperLegRPid.Update(upperLegRAngle, upperLegRAngleG, deltaTime, pidFactor, backCalculationTt) * torqueFactor * pidFactor;
			upperLegR.AddTorque(hips.transform.right * torqueUR);
		}

		if (lowerLegR != null)
		{
			torqueLR = lowerLegRPid.Update(lowerLegRAngle, lowerLegRAngleG, deltaTime, pidFactor, backCalculationTt) * torqueFactor * pidFactor;
			lowerLegR.AddTorque(upperLegL.transform.right * torqueLR);
		}
//Debug.Log("KdScale: " + kdScale + " " + upperLegKdScale + " spd=" + smoothedSpeed + " resetNP=" + resetNonPivotOnPivotSwitch + " resetP=" + resetPivotOnPivotSwitch + " pidLerp=" + footPidLerp +" footUpBlend=" + footUpBlend);

#if false && DEBUG // debug
		upperLegLAngle *= Mathf.Rad2Deg;
		lowerLegLAngle *= Mathf.Rad2Deg;
		upperLegRAngle *= Mathf.Rad2Deg;
		lowerLegRAngle *= Mathf.Rad2Deg;
		upperLegLAngleG *= Mathf.Rad2Deg;
		lowerLegLAngleG *= Mathf.Rad2Deg;
		upperLegRAngleG *= Mathf.Rad2Deg;
		lowerLegRAngleG *= Mathf.Rad2Deg;

		Debug.Log(
			"UpperL: " + upperLegLAngle + " -> " + upperLegLAngleG + " t=" + torqueUL + "\teu=" + upperLegL.transform.localRotation.eulerAngles + '\n' +
			"LowerL: " + lowerLegLAngle + " -> " + lowerLegLAngleG + " t=" + torqueLL + "\teu=" + lowerLegL.transform.localRotation.eulerAngles + '\n' +
			"UpperR: " + upperLegRAngle + " -> " + upperLegRAngleG + " t=" + torqueUR + "\teu=" + upperLegR.transform.localRotation.eulerAngles + '\n' +
			"LowerR: " + lowerLegRAngle + " -> " + lowerLegRAngleG + " t=" + torqueLR + "\teu=" + lowerLegR.transform.localRotation.eulerAngles);
#endif

#if DEBUG // debug
		if (goalSphereL != null)
		{
			goalSphereL.position = gfl;
		}

		if (goalSphereR != null)
		{
			goalSphereR.position = gfr;
		}

		if (tmpGoalSphereL != null)
		{
			tmpGoalSphereL.position = midGl;
			//tmpGoalSphereL.position = pivotL;
			//tmpGoalSphereL.position = basePosL;
		}

		if (tmpGoalSphereR != null)
		{
			tmpGoalSphereR.position = midGr;
			//tmpGoalSphereR.position = pivotR;
			//tmpGoalSphereR.position = basePosR;
		}
#else
		goalSphereL?.gameObject.SetActive(false);
		goalSphereR?.gameObject.SetActive(false);
		tmpGoalSphereL?.gameObject.SetActive(false);
		tmpGoalSphereR?.gameObject.SetActive(false);
#endif
	}

	// non public ----
	PidSettings hipsPidSettings;
	PidSettings upperLegPidSettings;
	PidSettings lowerLegPidSettings;
	PidControllerRotation hipsPid;
	PidController1 upperLegLPid;
	PidController1 lowerLegLPid;
	PidController1 upperLegRPid;
	PidController1 lowerLegRPid;
	bool prevPivotIsLeft = false;
	Vector3 landDelta;
	Vector3 landNormalL;
	Vector3 landNormalR;
	Vector3 prevFootGoalL;
	Vector3 prevFootGoalR;
	float smoothedSpeed; // 挙動変更用平滑化速度
	float smoothedLegOriginHeight;
	Settings currentSettings;
	// Kd調整
	float[] upperLegBaseKdList;
	float[] lowerLegBaseKdList;
	float upperLegLength;
	float lowerLegLength;
	Vector3 lowerLegToFootL;
	Vector3 lowerLegToFootR;
	Vector3 upperToLowerLegL;
	Vector3 upperToLowerLegR;

	// デバ用
	Vector3 lastPivotL;
	Vector3 lastPivotR;

	//[-180,180]
	float NormalizeAngle(float x)
	{
		var sign = (x < 0f) ? -1f : 1f; 
		x *= sign; // 正
		var q = (int)(x / 360f); 
		x -= q * 360f; // [0, 360]
		if (x < -180f) // [-180, 180]
		{
			x += 360f;
		}
		else if (x > 180f)
		{
			x -= 360f;
		}
		x *= sign;
		return x;
	}

	float NormalizeGoalAngle(float goal, float current)
	{
		const float PI2 = Mathf.PI * 2f;
		var d = goal - current;
		// 差を2piで割る
		var q = (int)(d / PI2);
		d -= PI2 * q;
		// [-PI,PI]に正規化
		if (d < -Mathf.PI)
		{
			d += PI2;
		}
		else if (d > Mathf.PI)
		{
			d -= PI2;
		}

		return current + d;
	}

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
//Debug.Log("\tFOC:" + o2fLen + "\t " + o2cLen + "\t " + f2cLen + " -> " + (angleFOC * Mathf.Rad2Deg) + " chest=" + chest + " footGoalPos=" + footGoalPos);
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
	[CustomEditor(typeof(Walker))]
	[CanEditMultipleObjects]
	public class Inspector : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			var self = target as Walker;

			if (self.hips != null)
			{
				EditorGUILayout.LabelField("HipsCenter", self.hips.position.ToString("F3"));
			}
			EditorGUILayout.LabelField("PivotL", self.lastPivotL.ToString("F3"));
			EditorGUILayout.LabelField("PivotR", self.lastPivotR.ToString("F3"));
			if (self.hips != null)
			{
				EditorGUILayout.LabelField("HipsZ+", (self.hips.rotation * Vector3.forward).ToString("F3"));
				EditorGUILayout.LabelField("HipsH", self.hips.position.y.ToString("F2"));
			}
			EditorGUILayout.LabelField("SmoothedSpeed", self.smoothedSpeed.ToString("F3"));
			EditorGUILayout.LabelField("landNormalL", self.landNormalL.ToString("F3"));
			EditorGUILayout.LabelField("landNormalR", self.landNormalR.ToString("F3"));
			EditorGUILayout.LabelField("LegOriginH", self.smoothedLegOriginHeight.ToString("F2"));
			if (self.upperLegPidSettings != null)
			{
				EditorGUILayout.LabelField("upperLegKd", self.upperLegPidSettings.kd.ToString("F3"));
			}

			if (self.lowerLegPidSettings != null)
			{
				EditorGUILayout.LabelField("lowerLegKd", self.lowerLegPidSettings.kd.ToString("F3"));
			}

			if (self.lowerLegLPid != null)
			{
				EditorGUILayout.LabelField("EL", self.lowerLegLPid.ErrorSum.ToString("F2"));
			}
		}
	}
#endif
}
