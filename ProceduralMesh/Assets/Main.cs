using UnityEngine;
using Kayac;

public class Main : MonoBehaviour
{
	[SerializeField] ProceduralMesh sphereCopy;
	[SerializeField] ProceduralMesh procedural;

	float div = 1;

	void OnGUI()
	{
		Vector3[] vertices;
		Vector3[] normals;
		int[] indices;
		GUI.Label(new Rect(0f, 0f, 100f, 50f), ((int)div).ToString());
		div = GUI.HorizontalSlider(new Rect(100f, 0f, 300f, 50f), div, 1f, 32f);
		if (procedural.MeshFilter.sharedMesh != null)
		{
			GUI.Label(new Rect(0f, 50f, 100f, 50f), "VertexCount: " + procedural.MeshFilter.sharedMesh.vertexCount);
		}
		if (GUI.Button(new Rect(0f, 100f, 100f, 50f), "GenSphere"))
		{
			MeshGenerator.GenerateSphere(out vertices, out normals, out indices, (int)div);
			procedural.MeshAsset.Set(
				vertices,
				normals,
				indices);
			procedural.LoadAsset();
		}
		if (GUI.Button(new Rect(100f, 100f, 100f, 50f), "GenTetrahedron"))
		{
			MeshGenerator.GenerateTetrahedron(out vertices, out normals, out indices);
			procedural.MeshAsset.Set(
				vertices,
				normals,
				indices);
			procedural.LoadAsset();
		}
	}
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			sphereCopy.SaveAsset();
		}
	}
}
