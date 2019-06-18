using UnityEngine;

public class Main : MonoBehaviour
{
	[SerializeField]
	Light mainLight;

	void Update()
	{
		mainLight.transform.localRotation = Quaternion.Euler(
			Time.time * 10f,
			Time.time * 20f,
			0f);
	}
}
