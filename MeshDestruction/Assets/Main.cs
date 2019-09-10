using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kayac;

public class Main : MonoBehaviour
{
	[SerializeField] MeshFilter meshFilter;

	void Start()
	{

	}

	void Update()
	{

	}

    void OnGUI()
    {
        if (GUILayout.Button("GenSphere"))
        {
            var mesh = new Mesh
            {
                name = "GeneratedSphere"
            };
            MeshGenerator.GenerateSphere(mesh, 2, false);
            meshFilter.sharedMesh = mesh;
        }
        
    }
}
