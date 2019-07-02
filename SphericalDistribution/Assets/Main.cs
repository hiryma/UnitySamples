using UnityEngine;

public class Main : MonoBehaviour
{
	[SerializeField]
	GameObject pointPrefab;

	int selected;
	string[] buttonTexts;
	const int pointCount = 10000;
	GameObject[] points;
	float logPower;

	void Awake()
	{
		points = new GameObject[pointCount];
		for (int i = 0; i < pointCount; i++)
		{
			points[i] = Instantiate(pointPrefab, transform, false);
		}
		buttonTexts = new string[]{
			"WrongCircular",
			"Circular",
			"WrongSpherical",
			"Spherical",
			"HemiSpherical",
			"HemiSphericalCos",
			"HemiSphericalCosPowered"
		};
		UpdatePoints();
	}

	void OnGUI()
	{
		int newSelected = GUILayout.SelectionGrid(selected, buttonTexts, 1);
		if (newSelected != selected)
		{
			selected = newSelected;
			UpdatePoints();
		}
		float newLogPower = GUILayout.HorizontalSlider(logPower, -2f, 8f);
		GUILayout.Label("Power: " + Mathf.Pow(2f, newLogPower));
		if (newLogPower != logPower)
		{
			logPower = newLogPower;
			UpdatePoints();
		}
		if (GUILayout.RepeatButton("Update"))
		{
			UpdatePoints();
		}
	}

	void UpdatePoints()
	{
		foreach (var point in points)
		{
			float phi, theta, radius; // phiは縦角度。起点は赤道。thetaは横角度。
			radius = 1f;
			phi = theta = 0f;
			switch (selected)
			{
				case 0:
					GetWrongCircularDistribution(out radius, out theta);
					break;
				case 1:
					GetCircularDistribution(out radius, out theta);
					break;
				case 2:
					GetWrongSphericalDistribution(out phi, out theta);
					break;
				case 3:
					GetSphericalDistribution(out phi, out theta);
					break;
				case 4:
					GetHemisphericalDistribution(out phi, out theta);
					break;
				case 5:
					GetHemisphericalCosDistribution(out phi, out theta);
					break;
				case 6:
					GetHemisphericalCosPoweredDistribution(out phi, out theta, Mathf.Pow(2f, logPower));
					break;
			}
			if (selected <= 1)
			{
				point.transform.localPosition = new Vector3(
					radius * Mathf.Cos(theta),
					0f,
					radius * Mathf.Sin(theta));
			}
			else
			{
				point.transform.localPosition = new Vector3(
					Mathf.Cos(phi) * Mathf.Cos(theta),
					Mathf.Cos(phi) * Mathf.Sin(theta),
					Mathf.Sin(phi));
			}
		}
	}

	public static void GetWrongCircularDistribution(
		out float radius,
		out float theta)
	{
		radius = Random.Range(0f, 1f);
		theta = Random.Range(-Mathf.PI, Mathf.PI);
	}

	public static void GetCircularDistribution(
		out float radius,
		out float theta)
	{
		radius = Mathf.Sqrt(Random.Range(0f, 1f));
		theta = Random.Range(-Mathf.PI, Mathf.PI);
	}

	public static void GetWrongSphericalDistribution(
		out float phi,
		out float theta)
	{
		theta = Random.Range(-Mathf.PI, Mathf.PI);
		phi = Random.Range(-0.5f * Mathf.PI, 0.5f * Mathf.PI);
	}

	public static void GetSphericalDistribution(
		out float phi,
		out float theta)
	{
		theta = Random.Range(-Mathf.PI, Mathf.PI);
		var p = Random.Range(0f, 1f);
		phi = Mathf.Asin((2f * p) - 1f);
	}

	public static void GetHemisphericalDistribution(
		out float phi,
		out float theta)
	{
		theta = Random.Range(-Mathf.PI, Mathf.PI);
		var p = Random.Range(0f, 1f);
		phi = Mathf.Asin(p);
	}

	public static void GetHemisphericalCosDistribution(
		out float phi,
		out float theta)
	{
		theta = Random.Range(-Mathf.PI, Mathf.PI);
		var p = Random.Range(0f, 1f);
		phi = Mathf.Asin(Mathf.Sqrt(p));
	}

	public static void GetHemisphericalCosPoweredDistribution(
		out float phi,
		out float theta,
		float power)
	{
		theta = Random.Range(-Mathf.PI, Mathf.PI);
		var p = Random.Range(0f, 1f);
		var powered = Mathf.Pow(p, 1f / (power + 1f));
		phi = Mathf.Asin(powered);
	}
}
