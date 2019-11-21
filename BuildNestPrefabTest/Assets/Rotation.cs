using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotation : MonoBehaviour
{
    Quaternion angularVelocity;
    void Start()
    {
        angularVelocity.x = Random.Range(-1f, 1f);
        angularVelocity.y = Random.Range(-1f, 1f);
        angularVelocity.z = Random.Range(-1f, 1f);
        angularVelocity.w = 0f;
    }

    public void Update()
    {
        var q = transform.localRotation;
        var dq = q * angularVelocity;
        var s = 0.5f * Time.deltaTime;
        q.x += dq.x * s;
        q.y += dq.y * s;
        q.z += dq.z * s;
        q.w += dq.w * s;
        transform.localRotation = q;
    }
}
