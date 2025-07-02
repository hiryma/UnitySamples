using Kayac;
using UnityEngine;

public class Main : MonoBehaviour
{
	[SerializeField] UnityEngine.UI.Text text;
	[SerializeField] Cannon cannon;
	[SerializeField] Target target;
	[SerializeField] Transform projectilesParent;

	// public ----
	public void OnHit()
	{
		hitCount++;
		UpdateText();
	}

	public void OnFire()
	{
		fireCount++;
		UpdateText();
	}

	// monoBehaviour Events ----
	void Start()
	{
#if UNITY_EDITOR
var gen = new StandardNormalDistributionGenerator(0);

var s1 = 0f;
var s2 = 0f;
var min = float.MaxValue;
var max = float.MinValue;
for (var i = 0; i < 10000; i++)
{
	var v = gen.Sample();
	s1 += v;
	s2 += v * v;
	if (v < min) min = v;
	if (v > max) max = v;
}
var mean = s1 / 10000f;
var variance = s2 / 10000f - mean * mean;
Debug.LogFormat("mean = {0}, variance = {1}", mean, variance);
Debug.LogFormat("min = {0}, max = {1}", min, max);

#endif
		cannon.ManualStart(this);
		target.ManualStart(this);
	}

	void FixedUpdate()
	{
		var dt = Time.fixedDeltaTime;
		cannon.ManualFixedUpdate(dt, target, projectilesParent);
		target.ManualFixedUpdate(dt);
	}

	// non public ----
	int hitCount = 0;
	int fireCount = 0;

	void UpdateText()
	{
		text.text = string.Format("{0} / {1}", hitCount, fireCount);
	}
}
