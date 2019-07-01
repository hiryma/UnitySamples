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
			"Wrong",
			"Spherical",
			"HemiSpherical",
			"HemiSphericalCos",
			"HemiSphericalCosPowered"
		};
		UpdatePoints();
	}

	void OnGUI()
	{
		int newSelected = GUILayout.SelectionGrid(selected, buttonTexts, buttonTexts.Length);
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
			float xAngle, yAngle;
			switch (selected)
			{
				case 0:
					GetWrongSphericalDistribution(out xAngle, out yAngle);
					break;
				case 1:
					GetSphericalDistribution(out xAngle, out yAngle);
					break;
				case 2:
					GetHemisphericalDistribution(out xAngle, out yAngle);
					break;
				case 3:
					GetHemisphericalCosDistribution(out xAngle, out yAngle);
					break;
				case 4:
					GetHemisphericalCosPoweredDistribution(out xAngle, out yAngle, Mathf.Pow(2f, logPower));
					break;
				default:
					xAngle = yAngle = 0f;
					break;
			}
			point.transform.localPosition = new Vector3(
				Mathf.Sin(xAngle) * Mathf.Cos(yAngle),
				Mathf.Sin(xAngle) * Mathf.Sin(yAngle),
				Mathf.Cos(xAngle));
		}
	}

	public static void GetWrongSphericalDistribution(
		out float xAngle,
		out float yAngle)
	{
		yAngle = Random.Range(-Mathf.PI, Mathf.PI);
		xAngle = Random.Range(0f, Mathf.PI);
	}

	public static void GetSphericalDistribution(
		out float xAngle,
		out float yAngle)
	{
		yAngle = Random.Range(-Mathf.PI, Mathf.PI);
		var r = Random.Range(0f, 1f);
		xAngle = Mathf.Acos(1f - (2f * r));
	}

	public static void GetHemisphericalDistribution(
		out float xAngle,
		out float yAngle)
	{
		yAngle = Random.Range(-Mathf.PI, Mathf.PI);
		var r = Random.Range(0f, 1f);
		xAngle = Mathf.Acos(r);
	}

	public static void GetHemisphericalCosDistribution(
		out float xAngle,
		out float yAngle)
	{
		yAngle = Random.Range(-Mathf.PI, Mathf.PI);
		var r = Random.Range(0f, 1f);
		xAngle = Mathf.Asin(Mathf.Sqrt(r));
	}

	public static void GetHemisphericalCosPoweredDistribution(
		out float xAngle,
		out float yAngle,
		float power)
	{
		yAngle = Random.Range(-Mathf.PI, Mathf.PI);
		var r = Random.Range(0f, 1f);
		var powered = Mathf.Pow(r, 1f / (power + 1f));
		xAngle = Mathf.Acos(powered);
	}
}
