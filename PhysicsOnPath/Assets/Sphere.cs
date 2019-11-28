using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sphere : MonoBehaviour
{
    [SerializeField] float avoidance;
    [SerializeField] Rigidbody myRigidbody;
    public Vector3 Force { get; set; }
    public Rigidbody Rigidbody { get { return myRigidbody; } }

    public void OnCollisionStay(Collision collision)
    {
        foreach (var contact in collision.contacts)
        {
            var n = contact.normal;
            n.y = 0f;
            n.Normalize();
            Force += n * avoidance;
        }
    }
}
