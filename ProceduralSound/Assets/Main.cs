using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
	[SerializeField]
	AudioSource audioSource;
	[SerializeField]
	Slider dampingSliderOn;
	[SerializeField]
	Slider dampingSliderOff;
	[SerializeField]
	Slider noteOffsetSlider;
	[SerializeField]
	Text onKeysText;

	float deltaTime;
	bool running;

	struct Oscillator
	{
		public void Attack(float strength)
		{
			var pulse = strength * Mathf.Sqrt(Stiffness);
			this.velocity += pulse;
		}
		public float Update(float deltaTime)
		{
			this.velocity -= this.velocity * Damping * deltaTime;
			this.velocity -= this.position * Stiffness * deltaTime;
			this.position += this.velocity * deltaTime;
			return this.position;
		}
		public float position;
		float velocity;
		public float Stiffness { set; private get; }
		public float Damping { set; private get; }
	}
	Oscillator[] oscillators;

	void Start()
	{
		this.deltaTime = 1f / (float)AudioSettings.outputSampleRate;
		this.running = true;
		this.oscillators = new Oscillator[88];
		var step = Mathf.Pow(2f, 1f / 12f);
		var f = 440f / 16f;
		for (int i = 0; i < this.oscillators.Length; i++)
		{
			var w = f * 2f * Mathf.PI;
			var stiffness = w * w;
			this.oscillators[i].Stiffness = stiffness;
			this.oscillators[i].Damping = 1f;
			f *= step;
		}
	}

	void Update()
	{
		var dampingOn = Mathf.Pow(10f, this.dampingSliderOn.value);
		var dampingOff = Mathf.Pow(10f, this.dampingSliderOff.value);
		int startNote = 39 + ((int)this.noteOffsetSlider.value * 12); //C4
		const float strength = 0.1f;
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
		var on = new bool[this.oscillators.Length];
		for (int i = 0; i < keys.Length; i++)
		{
			var note = startNote + i;
			if (Input.GetKeyDown(keys[i]))
			{
				Attack(note, strength);
			}
			if (Input.GetKey(keys[i]))
			{
				on[note] = true;
			}
			onKeys += "<color=" + (on[note] ? "#ff0000" : "ffffff") + ">";
			onKeys += noteNames[(startNote + i) % 12] + "</color> ";
		}
		this.onKeysText.text = onKeys;

		for (int i = 0; i < this.oscillators.Length; i++)
		{
			var d = on[i] ? dampingOn : dampingOff;
			this.oscillators[i].Damping = d;
		}
	}

	void Attack(int index, float strength)
	{
		this.oscillators[index].Attack(strength);
	}

	void OnAudioFilterRead(float[] data, int channels)
	{
		if (!this.running)
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
		for (int i = 0; i < this.oscillators.Length; i++)
		{
			v += this.oscillators[i].Update(this.deltaTime);
		}
		return v;
	}
}
