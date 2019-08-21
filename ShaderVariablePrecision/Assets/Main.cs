using UnityEngine;

public class Main : MonoBehaviour
{
	[SerializeField] Material origMaterial;
	[SerializeField] UnityEngine.UI.RawImage image;
	float mantissaBits = 23f;
	float uvScaleLog = 0f;
	bool showDerivative;
	Material material;

	void Awake()
	{
		material = new Material(origMaterial);
		image.material = material;
	}

	void OnGUI()
	{
		GUI.Label(new Rect(0f, 0f, 100f, 50f), "mantissa: " + (int)mantissaBits);
		var newMantissaBits = GUI.HorizontalSlider(new Rect(100f, 0f, 300f, 50f), mantissaBits, 0f, 23.5f);
		var uvScale = Mathf.Pow(2f, uvScaleLog);
		GUI.Label(new Rect(0f, 50f, 100f, 50f), "uvScale: " + uvScale.ToString("F2"));
		var newUvScaleLog = GUI.HorizontalSlider(new Rect(100f, 50f, 300f, 50f), uvScaleLog, -4f, 8f);
		if (newMantissaBits != mantissaBits)
		{
			mantissaBits = newMantissaBits;
			material.SetFloat("_MantissaBits", mantissaBits);
		}
		if (newUvScaleLog != uvScaleLog)
		{
			uvScaleLog = newUvScaleLog;
			uvScale = Mathf.Pow(2f, uvScaleLog);
			material.SetFloat("_UvScale", uvScale);
		}
		var newShowDerivative = GUI.Toggle(new Rect(0f, 100f, 200f, 50), showDerivative, "Show ddx/ddy");
		if (newShowDerivative != showDerivative)
		{
			showDerivative = newShowDerivative;
			material.SetFloat("_ShowDerivative", showDerivative ? 1f : 0f);
		}
	}
}
