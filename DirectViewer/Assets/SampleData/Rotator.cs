using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    void Update()
    {
        var q = transform.localRotation;
        q = Quaternion.AngleAxis(
            1f,
            new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1, 1f))) * q;
        transform.localRotation = q;        
    }
}
