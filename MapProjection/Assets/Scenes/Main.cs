using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
	[SerializeField]
	GameObject _prefab;
	[SerializeField]
	Camera _camera;

	void Start()
	{
		const int N = 9;
		for (int x = 0; x < N; x++)
		{
			for (int y = 0; y < N; y++)
			{
				for (int z = 0; z < N; z++)
				{
					var instance = Instantiate(_prefab, transform, false);
					instance.transform.localPosition = new Vector3(
						(x - (N * 0.5f)) * 4f,
						(y - (N * 0.5f)) * 4f,
						(z - (N * 0.5f)) * 4f);
				}
			}
		}
	}

	void Update()
	{
		_camera.transform.localRotation = Quaternion.Euler(Time.realtimeSinceStartup * 10f, Time.realtimeSinceStartup * 10f, 0f);
	}
}
