using UnityEngine;

public class Main : MonoBehaviour
{
	[SerializeField] Transform movingSphere;

	Kayac.FrameTimeWatcher frameTimeWatcher = new Kayac.FrameTimeWatcher();

	void Update()
	{
		frameTimeWatcher.Update();
		var t = Time.time * 1f;
		var p = new Vector3(
			4f * Mathf.Cos(t),
			0.5f,
			4f * Mathf.Sin(t));
		movingSphere.localPosition = p;
	}

	void OnGUI()
	{
		var ms = frameTimeWatcher.averageFrameTime / 1000f;
		GUILayout.Label(ms.ToString("F1"));
	}
}
