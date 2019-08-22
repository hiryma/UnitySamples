﻿using UnityEngine;
using Kayac;

public class Main : MonoBehaviour
{
	[SerializeField] Material material;
	GameObject sphereObject;
	Mesh mesh;
	float div = 0f;
	float displace = 0f;

	void Start()
	{
		mesh = new Mesh();
		sphereObject = new GameObject("Sphere");
		var renderer = sphereObject.AddComponent<MeshRenderer>();
		renderer.sharedMaterial = material;
		var filter = sphereObject.AddComponent<MeshFilter>();
		filter.sharedMesh = mesh;
	}

	void Update()
	{
		sphereObject.transform.localRotation = Quaternion.Euler(
			Time.time * 20f,
			Time.time * 30f,
			Time.time * 40f);
	}

	void OnGUI()
	{
		GUI.Label(new Rect(0f, 0f, 100f, 50f), ((int)div).ToString());
		div = GUI.HorizontalSlider(new Rect(100f, 0f, 300f, 50f), div, 0f, 10.5f);
		GUI.Label(new Rect(0f, 50f, 200f, 50f), "VertexCount: " + mesh.vertexCount);
		if (GUI.Button(new Rect(0f, 100f, 100f, 50f), "Sphere"))
		{
			if (MeshGenerator.GenerateSphere(mesh, (int)div))
			{
				mesh.name = string.Format("sphere_{0}", (int)div);
				ObjFileWriter.Write("Assets/GeneratedMeshes", mesh, importImmediately: true);
			}
		}

		if (GUI.Button(new Rect(100f, 100f, 100f, 50f), "Cylinder"))
		{
			if (MeshGenerator.GenerateCylinderSide(mesh, 1f, 0.5f, (int)div))
			{
				mesh.name = string.Format("cylinder_{0}", (int)div);
				ObjFileWriter.Write("Assets/GeneratedMeshes", mesh, importImmediately: true);
			}
		}
	}
}
