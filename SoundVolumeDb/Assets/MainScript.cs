using UnityEngine;
using UnityEngine.UI;

public class MainScript : MonoBehaviour
{
	[SerializeField]
	float dbMin = -80f;
	[SerializeField]
	Slider linearSlider;
	[SerializeField]
	Slider dbSlider;
	[SerializeField]
	Slider sqDbSlider;
	[SerializeField]
	Toggle bgmToggle;
	[SerializeField]
	Button seButton;
	[SerializeField]
	Text linearVolumeText;
	[SerializeField]
	Text dbVolumeText;
	[SerializeField]
	AudioSource bgmSource;
	[SerializeField]
	AudioSource seSource;

	void Start()
	{
		bgmToggle.onValueChanged.AddListener(on =>
		{
			if (on)
			{
				bgmSource.Play();
			}
			else
			{
				bgmSource.Stop();
			}
		});
		seButton.onClick.AddListener(() =>
		{
			seSource.Play();
		});
		linearSlider.onValueChanged.AddListener(pos =>
		{
			float linear = pos; // そのまま01
			float db = ToDecibel(linear, dbMin);
			dbSlider.SetValueWithoutNotify(DecibelToDbPos(db, dbMin));
			sqDbSlider.SetValueWithoutNotify(dbSlider.value * dbSlider.value); // 二乗
			UpdateText(db);
		});
		dbSlider.onValueChanged.AddListener(pos =>
		{
			float db = DbPosToDecibel(pos, dbMin);
			float linear = FromDecibel(db);
			linearSlider.SetValueWithoutNotify(linear);
			sqDbSlider.SetValueWithoutNotify(pos * pos); // 二乗
			UpdateText(db);
		});
		sqDbSlider.onValueChanged.AddListener(pos =>
		{
			float dbPos = Mathf.Sqrt(pos); // 二乗されているので戻す
			float db = DbPosToDecibel(dbPos, dbMin);
			float linear = FromDecibel(db);
			linearSlider.SetValueWithoutNotify(linear);
			dbSlider.SetValueWithoutNotify(dbPos);
			UpdateText(db);
		});
		UpdateText(DbPosToDecibel(dbSlider.value, dbMin));
	}

	// デシベルスライダーの[0,1]位置→デシベル
	static float DbPosToDecibel(float dbPos, float dbMin)
	{
		float db = (dbPos * (0f - dbMin)) + dbMin;
		return db;
	}

	static float DecibelToDbPos(float db, float dbMin)
	{
		float dbPos = (db - dbMin) / (0f - dbMin);
		return dbPos;
	}

	void UpdateText(float db)
	{
		var linear = FromDecibel(db);
		bgmSource.volume = linear;
		seSource.volume = linear;
		linearVolumeText.text = "Linear: " + linear.ToString("F2");
		dbVolumeText.text = "db: " + db.ToString("F0");
	}

	static float ToDecibel(float linear, float dbMin)
	{
		var db = dbMin;
		if (linear > 0f)
		{
			db = 20f * Mathf.Log10(linear);
			db = Mathf.Max(db, dbMin);
		}
		return db;
	}

	static float FromDecibel(float db)
	{
		var linear = Mathf.Pow(10f, db / 20f);
		return linear;
	}
}
