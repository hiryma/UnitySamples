using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
	[SerializeField] RectTransform canvasTransform;
	[SerializeField] AudioSource seSource;
	[SerializeField] AudioSource bgmSource;

	[SerializeField] AudioHighPassFilter hiPass;
	[SerializeField] AudioLowPassFilter loPass;

	[SerializeField] AudioChorusFilter chorus;
	[SerializeField] AudioDistortionFilter distortion;
	[SerializeField] AudioEchoFilter echo;
	[SerializeField] AudioReverbFilter reverb;
	float volume = -6f; // db
	float pitch = 1f;
	bool loop;
	bool bgm;
	bool chorusEnabled;
	bool distortionEnabled;
	bool echoEnabled;
	bool reverbEnabled;
	float loPassHz = 22000f;
	float hiPassHz = 10f;

	// レイアウト支援用
	float x;
	float y;
	float lineHeight = 30f;
	Image[] images;
	int sampleCount = 256;

	void Awake()
	{
		chorus.enabled = false;
		distortion.enabled = false;
		echo.enabled = false;
		reverb.enabled = false;
		loPass.cutoffFrequency = loPassHz;
		hiPass.cutoffFrequency = hiPassHz;
		images = new Image[sampleCount];
		var canvasW = canvasTransform.sizeDelta.x;
		for (int i = 0; i < sampleCount; i++)
		{
			var go = new GameObject("SpectrumBar" + i);
			var im = go.AddComponent<Image>();
			im.rectTransform.anchorMin = Vector2.zero;
			im.rectTransform.anchorMax = Vector2.zero;
			im.rectTransform.pivot = Vector2.zero;
			im.color = new Color(0.7f, 0.7f, 0.2f, 0.5f);
			im.rectTransform.anchoredPosition = new Vector2(
				canvasW * i / sampleCount,
				0f);
			images[i] = im;
			go.transform.SetParent(canvasTransform, false);
		}
	}
	Rect MakeRect(float width)
	{
		var ret = new Rect(x, y, width, lineHeight);
		x += width;
		return ret;
	}

	void Break()
	{
		x = 0f;
		y += lineHeight;
	}

	void OnGUI()
	{
		x = y = 0f;
		GUI.Label(MakeRect(200f), "Volume: " + volume);
		var newVolume = GUI.HorizontalSlider(MakeRect(200f), volume, -60f, 0f);
		if (newVolume != volume)
		{
			volume = newVolume;
			var linear = Mathf.Pow(10f, volume / 20f);
			seSource.volume = linear;
			bgmSource.volume = linear;
		}
		Break();

		GUI.Label(MakeRect(200f), "Pitch: " + pitch);
		var newPitch = GUI.HorizontalSlider(MakeRect(200f), pitch, 0f, 4f);
		if (newPitch != pitch)
		{
			pitch = newPitch;
			seSource.pitch = pitch;
			bgmSource.pitch = pitch;
		}
		Break();

		GUI.Label(MakeRect(200f), "LoPassHz: " + loPassHz);
		var loPassHzLog = LogHz(loPassHz);
		var newLoPassHzLog = GUI.HorizontalSlider(MakeRect(200f), loPassHzLog, 0f, 1f);
		if (newLoPassHzLog != loPassHzLog)
		{
			loPassHz = ExpHz(newLoPassHzLog);
			loPass.cutoffFrequency = loPassHz;
		}
		Break();

		GUI.Label(MakeRect(200f), "HiPassHz: " + hiPassHz);
		var hiPassHzLog = LogHz(hiPassHz);
		var newHiPassHzLog = GUI.HorizontalSlider(MakeRect(200f), hiPassHzLog, 0f, 1f);
		if (newHiPassHzLog != hiPassHzLog)
		{
			hiPassHz = ExpHz(newHiPassHzLog);
			hiPass.cutoffFrequency = hiPassHz;
		}
		Break();

		FilterToggle(ref chorusEnabled, "Chorus", chorus);
		FilterToggle(ref distortionEnabled, "Distortion", distortion);
		FilterToggle(ref echoEnabled, "Echo", echo);
		FilterToggle(ref reverbEnabled, "Reverb", reverb);
		Break();

		var newLoop = GUI.Toggle(MakeRect(100f), loop, "Loop");
		if (newLoop != loop)
		{
			loop = newLoop;
			seSource.loop = loop;
		}

		var newBgm = GUI.Toggle(MakeRect(100f), bgm, "Bgm");
		if (newBgm != bgm)
		{
			bgm = newBgm;
			if (bgm)
			{
				bgmSource.Play();
			}
			else
			{
				bgmSource.Stop();
			}
		}

		if (GUI.Button(MakeRect(100f), "Play"))
		{
			seSource.Play();
		}
	}

	void Update()
	{
		var spectrum = new float[sampleCount];
		AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.Rectangular);

		float max = -float.MaxValue;
		float min = -max;
		float barW = canvasTransform.sizeDelta.x / sampleCount;
		float barHMax = canvasTransform.sizeDelta.y * 0.5f;
		float minDb = -80f;
		for (int i = 0; i < spectrum.Length; i++)
		{
			float db = minDb;
			if (spectrum[i] > 0)
			{
				db = Mathf.Log10(spectrum[i]) * 20f;
				if (db < minDb)
				{
					db = minDb;
				}
			}
			max = Mathf.Max(max, db);
			min = Mathf.Min(min, db);
			var h = barHMax * (-minDb + db) / -minDb;
			images[i].rectTransform.sizeDelta = new Vector2(
				barW,
				h);
		}
	}

	void FilterToggle(ref bool enabled, string label, Behaviour filter)
	{
		var newEnabled = GUI.Toggle(MakeRect(100f), enabled, label);
		if (newEnabled != enabled)
		{
			enabled = newEnabled;
			filter.enabled = enabled;
		}
	}

	// 10hzで0、22000hzで1になるような対数変換を返す
	float LogHz(float hz)
	{
		return (Mathf.Log(hz) - Mathf.Log(10f)) / (Mathf.Log(22000f) - Mathf.Log(10f));
	}

	// 上の逆変換
	// logHz = (log(hz) - log(10)) / (log(22000) - log(10))
	// logHz * (log(22000) - log(10)) = log(hz) - log(10)
	// log(hz) = (logHz * (log(22000) - log(10))) + log(10)
	// expして
	// hz = Exp((logHz * (log(22000) - log(10))) + log(10))
	//    = Exp(logHz * (log(22000) - log(10))) * 10
	float ExpHz(float logHz)
	{
		var t = Mathf.Log(22000f) - Mathf.Log(10f);
		return Mathf.Exp(logHz * t) * 10f;
	}
}
