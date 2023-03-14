using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
	[SerializeField] AudioSource audioSource;
	[SerializeField] Slider dampingSliderOn;
	[SerializeField] Slider dampingSliderOff;
	[SerializeField] Slider noteOffsetSlider;
	[SerializeField] Text onKeysText;

	float deltaTime;
	bool running;

	struct Oscillator
	{
		public void Attack(float strength)
		{
			var pulse = strength * Mathf.Sqrt(Stiffness);
			velocity += pulse;
		}
		public float Update(float deltaTime)
		{
			velocity -= velocity * Damping * deltaTime;
			velocity -= position * Stiffness * deltaTime;
			position += velocity * deltaTime;
			return position;
		}
		public float position;
		float velocity;
		public float Stiffness { set; private get; }
		public float Damping { set; private get; }
	}
	Oscillator[] oscillators;

	void Start()
	{
		deltaTime = 1f / (float)AudioSettings.outputSampleRate;
		running = true;
		oscillators = new Oscillator[88];
		TuneEqually();
	}

	void TuneEqually()
	{
		var step = Mathf.Pow(2f, 1f / 12f);
		var f = 440f / 16f;
		for (int i = 0; i < oscillators.Length; i++)
		{
			var w = f * 2f * Mathf.PI;
			var stiffness = w * w;
			oscillators[i].Stiffness = stiffness;
			oscillators[i].Damping = 1f;
			f *= step;
		}
	}

	void Tune(int baseNote)
	{
		var baseF = (440f / 16f) * Mathf.Pow(2f, baseNote / 12f);
		Debug.Log("Tune: " + baseNote + "F=" + baseF);
		var mul = new float[12]
		{
			1f, // base
			15f / 14f, //0.988832221402009
//			16f / 15f, // 0.993246650961839
//			17f / 16f, // 0.997141735867572
//			18f / 17f, // 1.000604033561557
			9f / 8f,
			7f / 6f,
			5f / 4f,
			4f / 3f,
			7f / 5f,
			3f / 2f,
			8f / 5f,
			5f / 3f,
			7f / 4f,
			15f / 8f,
		};

		for (int i = 0; i < oscillators.Length; i++)
		{
			var d = i - baseNote;
			var octave = 0;
			while (d < 0)
			{
				d += 12;
				octave--;
			}
			octave += d / 12;
			d = d % 12;

			var f = baseF * Mathf.Pow(2f, octave);
			f *= mul[d];
			var w = f * 2f * Mathf.PI;
			var stiffness = w * w;
			oscillators[i].Stiffness = stiffness;
			oscillators[i].Damping = 1f;
		}
	}

	void Update()
	{
		var dampingOn = Mathf.Pow(10f, dampingSliderOn.value);
		var dampingOff = Mathf.Pow(10f, dampingSliderOff.value);
		int startNote = 39 + ((int)noteOffsetSlider.value * 12); //C4
		const float strength = 0.05f;
		var keys = new KeyCode[]
		{
			KeyCode.A,
			KeyCode.W,
			KeyCode.S,
			KeyCode.E,
			KeyCode.D,
			KeyCode.F,
			KeyCode.T,
			KeyCode.G,
			KeyCode.Y,
			KeyCode.H,
			KeyCode.U,
			KeyCode.J,
			KeyCode.K,
			KeyCode.O,
			KeyCode.L,
			KeyCode.P
		};
		var noteNames = new string[]
		{
			"A",
			"B",
			"H",
			"C",
			"Cis",
			"D",
			"Dis",
			"E",
			"F",
			"Fis",
			"G",
			"Gis",
		};
		var onKeys = "";
		var on = new bool[oscillators.Length];
		var shift = 0;
		var tuned = false;
		if (Input.GetKey(KeyCode.LeftShift))
		{
			shift = -12;
		}
		if (Input.GetKey(KeyCode.RightShift))
		{
			shift = 12;
		}
		for (int i = 0; i < keys.Length; i++)
		{
			var note = startNote + i + shift;
			if (Input.GetKeyDown(keys[i]))
			{
				Attack(note, strength);
			}
			if (Input.GetKey(keys[i]))
			{
				on[note] = true;
				if (Input.GetKeyDown(KeyCode.Return))
				{
					Tune(note);
					tuned = true;
				}
			}
			onKeys += "<color=" + (on[note] ? "#ff0000" : "ffffff") + ">";
			onKeys += noteNames[(startNote + i) % 12] + "</color> ";
		}
		onKeysText.text = onKeys;

		if (!tuned && Input.GetKeyDown(KeyCode.Return))
		{
			TuneEqually();
		}

		for (int i = 0; i < oscillators.Length; i++)
		{
			var d = on[i] ? dampingOn : dampingOff;
			oscillators[i].Damping = d;
		}
	}

	void Attack(int index, float strength)
	{
		Debug.Log(index);
		oscillators[index].Attack(strength);
	}

	uint x = 1;
	uint X(){
		x ^= x >>  13;
		x ^= x << 17;
		x ^= x >> 5;
		return x;
	}

	void OnAudioFilterRead(float[] data, int channels)
	{
		if (!running)
		{
			return;
		}

		int sampleCount = data.Length / channels;
		for (int i = 0; i < sampleCount; i++)
		{
			float v = UpdateOsccilators();
			for (int c = 0; c < channels; c++)
			{
				data[(i * channels) + c] = v;
			}
		}
	}

	float UpdateOsccilators()
	{
		var v = 0f;
		for (int i = 0; i < oscillators.Length; i++)
		{
			v += oscillators[i].Update(deltaTime);
		}
		return v;
	}
}
