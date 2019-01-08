using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
	[SerializeField]
	SpriteRenderer _renderer;

	public void SetRotation(Quaternion q)
	{
		gameObject.transform.localRotation = q;
	}
}
