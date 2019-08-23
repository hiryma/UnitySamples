using UnityEngine;
using Kayac;

public class Main : MonoBehaviour
{
	[SerializeField] Material material;
	[SerializeField] Camera staticCamera;
	GameObject sphereObject;
	Mesh mesh;
	float div = 0f;
	float displace = 0f;
	bool separateFaces;

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
		GUI.Label(new Rect(0f, 0f, 100f, 30f), ((int)div).ToString());
		div = GUI.HorizontalSlider(new Rect(100f, 0f, 180f, 30f), div, 0f, 10.5f);
		GUI.Label(new Rect(300f, 0f, 200f, 30f), "V: " + mesh.vertexCount);
		if (GUI.Button(new Rect(0f, 30f, 100f, 50f), "Sphere"))
		{
			if (MeshGenerator.GenerateSphere(mesh, (int)div, separateFaces))
			{
				mesh.name = string.Format("sphere_{0}", (int)div);
			}
		}

		if (GUI.Button(new Rect(0f, 80f, 100f, 50f), "Cylinder"))
		{
			if (MeshGenerator.GenerateCylinderSide(mesh, 1f, 0.5f, (int)div, separateFaces))
			{
				mesh.name = string.Format("cylinder_{0}", (int)div);
			}
		}

		if (GUI.Button(new Rect(0f, 130f, 100f, 50f), "Wall"))
		{
			var d = 2 << (int)div;
			var radMax = Mathf.PI * 2f * 4f;
			var positions = new Vector2[d + 1];
			for (int i = 0; i <= d; i++)
			{
				var t = (float)i / (float)d;
				var rad = radMax * t;
				positions[i] = new Vector2(
					Mathf.Cos(rad) * t * 0.5f,
					Mathf.Sin(rad) * t * 0.5f);
			}

			if (MeshGenerator.GenerateWall(mesh, positions, 0.1f, 0.05f, looped: false, separateFaces))
			{
				mesh.name = string.Format("wall_{0}", (int)div);
			}
		}

		if (GUI.Button(new Rect(0f, 180f, 100f, 50f), "Save"))
		{
			ObjFileWriter.Write("Assets/GeneratedMeshes", mesh, importImmediately: true);
		}

		staticCamera.enabled = GUI.Toggle(new Rect(0f, 230f, 200f, 30f),  staticCamera.enabled, "ShowStaticSample");
		separateFaces = GUI.Toggle(new Rect(0f, 260f, 200f, 30f),  separateFaces, "SeparateFaces");
	}
}
