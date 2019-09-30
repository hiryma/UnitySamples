using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Konch : MonoBehaviour
{
    [SerializeField] Vector3 velocity;
	[SerializeField] float damping;
	[SerializeField] float gravity;

    public Vector3 Velocity
    {
        set
        {
            velocity = value;
        }
    }

    public Color Color
    {
        set
        {
            var spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
            spriteRenderer.color = value;
        }
    }

    void Start()
    {

    }

    void Update()
    {
		/*
				UnityEngine.Profiling.Profiler.BeginSample("GameObject.Find");
				var o = GameObject.Find("Konch" + Random.Range(0, 1000));
				UnityEngine.Profiling.Profiler.EndSample();
				if (o != null)
				{
					velocity += o.transform.position * 0.001f;
				}
		*/
		velocity -= velocity * damping * Time.deltaTime;
		velocity += (new Vector3(0f, 0f, 0f) - transform.position) * gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
    }
}
