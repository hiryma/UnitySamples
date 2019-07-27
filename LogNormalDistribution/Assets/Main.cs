using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
	[SerializeField] RectTransform canvasTransform;
	[SerializeField] int binCount = 256;
	[SerializeField] int sampleCount = 65536;
	[SerializeField] float[] graphCuts;

	delegate float Func();
	Image[] images;
	string text;
	int mode = 2;
	readonly string[] modeNames = new string[] { "Uniform", "Normal", "LogNormal" };
	bool useBoxMuller;

	void Start()
	{
		images = new Image[binCount];
		var interval = canvasTransform.sizeDelta.x / binCount;
		for (int i = 0; i < images.Length; i++)
		{
			var go = new GameObject(i.ToString());
			images[i] = go.AddComponent<Image>();
			images[i].rectTransform.anchorMin = Vector2.zero;
			images[i].rectTransform.anchorMax = Vector2.zero;
			images[i].rectTransform.pivot = Vector2.zero;
			images[i].rectTransform.anchoredPosition = new Vector2(i * interval, 0f);
			images[i].rectTransform.sizeDelta = new Vector2(interval, 0);
			go.transform.SetParent(canvasTransform, false);
		}
		UpdateHistogram(2);
	}

	void OnGUI()
	{
		GUILayout.Label(text);
		var newMode = GUILayout.SelectionGrid(mode, modeNames, 3);
		if (newMode != mode)
		{
			UpdateHistogram(newMode);
			mode = newMode;
		}
		useBoxMuller = GUILayout.Toggle(useBoxMuller, "Box-Muller");
	}

	void UpdateHistogram(int mode)
	{
		float[] samples = null;
		switch (mode)
		{
			case 0: samples = Sample(sampleCount, Uniform); break;
			case 1: samples = Sample(sampleCount, Normal); break;
			case 2: samples = Sample(sampleCount, LogNormal); break;
		}
		if (samples != null)
		{
			DrawHistogram(samples, graphCuts[mode]);
		}
	}

	void DrawHistogram(float[] samples, float graphCut)
	{
		// 値をソートしてしまう
		System.Array.Sort(samples);
		// 中央値
		float median = samples[samples.Length / 2];
		// 平均算出
		var avg = 0f;
		foreach (var item in samples)
		{
			avg += item;
		}
		avg /= samples.Length;
		// 最小最大
		var min = samples[0];
		var max = samples[samples.Length - 1];

		// 上をいささか捨てる
		var graphMax = samples[(int)((samples.Length -1) * (1f - graphCut))];

		// 分類
		int[] bins = new int[images.Length];
		float binWidth = (graphMax - min) / images.Length;
		foreach (var item in samples)
		{
			if (item < graphMax)
			{
				int index = (int)((item - min) / binWidth);
				index = Mathf.Clamp(index, 0, bins.Length - 1);
				bins[index]++;
			}
		}

		// binの最大値を求める
		int binMax = 0;
		foreach (var bin in bins)
		{
			binMax = Mathf.Max(binMax, bin);
		}
		float binHeightScale = canvasTransform.sizeDelta.y * 0.75f / binMax;

		// imageのサイズに反映
		float barWidth = canvasTransform.sizeDelta.x / images.Length;
		for (var i = 0; i < images.Length; i++)
		{
			var h = binHeightScale * bins[i];
			images[i].rectTransform.sizeDelta = new Vector2(barWidth, h);
		}
		// 情報表示
		text = string.Format("avg: {0} med:{1} min:{2} max:{3}", avg, median, min, max);
	}

	static float[] Sample(int n, Func func)
	{
		var ret = new float[n];
		for (int i = 0; i < n; i++)
		{
			ret[i] = func();
		}
		return ret;
	}

	float Uniform()
	{
		return Random.value;
	}

	float Normal()
	{
		if (useBoxMuller)
		{
			return GetNormalDistributionBoxMuller();
		}
		else
		{
			return GetNormalDistributionApprox();
		}
	}

	float LogNormal()
	{
		var t = Normal();
		return Mathf.Pow(2f, t);
	}

	float GetNormalDistributionApprox()
	{
		var ret = 0f;
		for (int i = 0; i < 12; i++)
		{
			ret += Random.value;
		}
		return ret - 6f;
	}

	float normalDistributionCosine = float.MaxValue;  // MaxValueが入ってる時は計算が必要。
	float GetNormalDistributionBoxMuller()
	{
		float ret;
		if (normalDistributionCosine != float.MaxValue) // 前に作った奴が残っていれば返す
		{
			ret = normalDistributionCosine;
			normalDistributionCosine = float.MaxValue;
		}
		else
		{
			var x = Random.value;
			var y = Random.value;
			var t0 = Mathf.Sqrt(-2f * Mathf.Log(x));
			var t1 = Mathf.PI * 2f * y;
			normalDistributionCosine = t0 * Mathf.Cos(t1);
			ret = t0 * Mathf.Sin(t1);
		}
		return ret;
	}
}
