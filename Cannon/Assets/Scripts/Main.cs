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
