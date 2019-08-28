using UnityEngine;
using Kayac;

public class Main : MonoBehaviour
{
	[SerializeField] Material material;
	[SerializeField] Camera staticCamera;
	[SerializeField] Camera dynamicCamera;
	GameObject sphereObject;
	Mesh mesh;
	float div = 0f;
	float displace = 0f;
	bool separateFaces;

	void Start()
	{
		mesh = new Mesh();
		sphereObject = new GameObject("Sphere");
		sphereObject.transform.SetParent(transform, false);
		var renderer = sphereObject.AddComponent<MeshRenderer>();
		renderer.sharedMaterial = material;
		var collider = sphereObject.AddComponent<SphereCollider>();
		var filter = sphereObject.AddComponent<MeshFilter>();
		filter.sharedMesh = mesh;
	}

	void OnGUI()
	{
		float y = 0f;
		GUI.Label(new Rect(0f, y, 100f, 30f), ((int)div).ToString());
		div = GUI.HorizontalSlider(new Rect(100f, y, 180f, 30f), div, 0f, 10.5f);
		GUI.Label(new Rect(300f, y, 200f, 30f), "V: " + mesh.vertexCount);
		y += 30f;
		if (GUI.Button(new Rect(0f, y, 100f, 50f), "Sphere"))
		{
			if (MeshGenerator.GenerateSphere(mesh, (int)div, separateFaces))
			{
				mesh.name = string.Format("sphere_{0}", (int)div);
			}
		}
		y += 50f;

		if (GUI.Button(new Rect(0f, y, 100f, 50f), "Cylinder"))
		{
			if (MeshGenerator.GenerateCylinderSide(mesh, 1f, 0.5f, (int)div, separateFaces))
			{
				mesh.name = string.Format("cylinder_{0}", (int)div);
			}
		}
		y += 50f;

		if (GUI.Button(new Rect(0f, y, 100f, 50f), "Wall"))
		{
			var d = 2 << (int)div;
			var radMax = Mathf.PI * 2f * 4f;
			var positions = new Vector2[d + 1];
			for (int i = 0; i <= d; i++)
			{
				var t = 1f - (float)i / (float)d;
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
		y += 50f;

		if (GUI.Button(new Rect(0f, y, 100f, 50f), "ConvexPolygon"))
		{
			int d = 3 << (int)div;
			var positions = new Vector2[d];
			for (int i = 0; i < positions.Length; i++)
			{
				var rad = 2f * Mathf.PI * (float)i / (float)positions.Length;
				positions[i] = new Vector2(
					Mathf.Sin(rad),
					Mathf.Cos(rad)) * 0.5f;
			}
			if (MeshGenerator.GenerateConvexPolygon(mesh, positions, 0.1f, separateFaces))
			{
				mesh.name = string.Format("wall_{0}", (int)div);
			}
		}
		y += 50f;

#if UNITY_EDITOR
		if (GUI.Button(new Rect(0f, y, 100f, 50f), "Save"))
		{
			ObjFileWriter.Write("Assets/GeneratedMeshes", mesh, importImmediately: true);
		}
		y += 50f;
#endif
		staticCamera.enabled = GUI.Toggle(new Rect(0f, y, 200f, 30f),  staticCamera.enabled, "ShowStaticSample");
		y += 30f;
		separateFaces = GUI.Toggle(new Rect(0f, y, 200f, 30f),  separateFaces, "SeparateFaces");
	}
}
