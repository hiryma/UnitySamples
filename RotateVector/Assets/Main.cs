using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
	[SerializeField] LineRenderer beamPrefab;
	[SerializeField] Camera mainCamera;

	List<LineRenderer> beams;
	float logN = 7f;
	const int beamCount = 32;
	float n;
	Vector3 beamAxis;
	Vector3 angularVelocity;

	void Start()
	{
		beams = new List<LineRenderer>();
		for (int i = 0; i < beamCount; i++)
		{
			beams.Add(Instantiate(beamPrefab, null, false));
		}
	}

	void Update()
	{
		float t = Time.time * 3;
		beamAxis.x = Mathf.Cos(t) * Mathf.Cos(t * 1.3f);
		beamAxis.y = Mathf.Cos(t) * Mathf.Sin(t * 1.3f);
		beamAxis.z = Mathf.Sin(t);
		UpdateBeams();
	}

	void UpdateBeams()
	{
		Vector3 upHint;
		if (Mathf.Abs(beamAxis.y) > 0.8f) // かなり上か下向きなら、替わりにxを使う
		{
			upHint = new Vector3(1f, 0f, 0f);
		}
		else
		{
			upHint = new Vector3(0f, 1f, 0f);
		}
		var right = Vector3.Cross(upHint, beamAxis);
		right.Normalize();
		var cannonPos = Vector3.zero;
		foreach (var beam in beams)
		{
			float phi, theta;
			GetHemisphericalCosPoweredDistribution(out phi, out theta, n);
			var v = RotateVector(beamAxis, right, phi);
			v = RotateVector(v, beamAxis, theta);
			v *= 1000f;
			beam.SetPosition(0, cannonPos);
			beam.SetPosition(1, cannonPos + v);
		}
	}

	void OnGUI()
	{
		GUILayout.Label("Distribution Pow: " + n);
		logN = GUI.HorizontalSlider(new Rect(0f, 30f, 200f, 40f), logN, 0f, 16f); // UnityバグっててGUILayout使うとまともに動かない
		n = Mathf.Pow(2f, logN);
	}

	static Vector3 RotateVector(
		Vector3 v,
		Vector3 axisNormalized, // 軸ベクトルは要正規化
		float radian)
	{
		// vを軸に射影して、回転円中心cを得る
		var c = ProjectVector(v, axisNormalized);
		var p = v - c;

		// p及びaと直交するベクタを得る
		var q = Vector3.Cross(axisNormalized, p);
		// a,pは直交しているから、|q|=|p|

		// 回転後のv'の終点V'は、V' = C + s*p + t*q と表せる。
		// ここで、s = cosθ t = sinθ
		var s = Mathf.Cos(radian);
		var t = Mathf.Sin(radian);
		return c + (p * s) + (q * t);
	}

	static Vector3 ProjectVector(
		Vector3 v,
		Vector3 axisNormalized)
	{
		return Vector3.Dot(v, axisNormalized) * axisNormalized;
	}

	static void GetHemisphericalCosPoweredDistribution(
		out float phi,
		out float theta,
		float power)
	{
		theta = Random.Range(-Mathf.PI, Mathf.PI);
		var r = Random.Range(0f, 1f);
		var powered = Mathf.Pow(r, 1f / (power + 1f));
		phi = Mathf.Acos(powered);
	}
}
