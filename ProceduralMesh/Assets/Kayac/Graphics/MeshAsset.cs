using UnityEngine;

[CreateAssetMenu(menuName = "Kayac/ProceduralMesh")]
public class MeshAsset : ScriptableObject
{
	[System.Serializable]
	struct SubMesh
	{
		public int[] indices;
		public MeshTopology topology;
	}
	[SerializeField] Vector3[] vertices;
	[SerializeField] Vector2[] uv;
	[SerializeField] Vector3[] normals;
	[SerializeField] Color32[] colors32;
	[SerializeField] SubMesh[] subMeshes;

	Mesh mesh;
	bool meshDirty = false;

	public void Set(
		Vector3[] vertices,
		Vector3[] normals,
		int[] indices)
	{
		this.vertices = vertices;
		this.normals = normals;
		subMeshes = new SubMesh[1];
		subMeshes[0].indices = indices;
		subMeshes[0].topology = MeshTopology.Triangles;
		meshDirty = true;
	}

	public void Set(Mesh mesh)
	{
		this.mesh = mesh; // もらったものすぐ使えるようにしておく
		meshDirty = false;
		vertices = mesh.vertices;
		uv = mesh.uv;
		normals = mesh.normals;
		colors32 = mesh.colors32;
		subMeshes = new SubMesh[mesh.subMeshCount];
		for (int i = 0; i < mesh.subMeshCount; i++)
		{
			subMeshes[i].indices = mesh.GetIndices(i);
			subMeshes[i].topology = mesh.GetTopology(i);
		}
	}

	public Mesh GenerateMesh()
	{
		// キャッシュあれば返す
		if (!meshDirty && (mesh != null))
		{
			return mesh;
		}
		mesh = new Mesh();
		meshDirty = false;
		mesh.vertices = vertices;
		if ((uv != null) && (uv.Length > 0))
		{
			Debug.Assert(vertices.Length == uv.Length);
			mesh.uv = uv;
		}
		if ((normals != null) && (normals.Length > 0))
		{
			Debug.Assert(vertices.Length == normals.Length);
			mesh.normals = normals;
		}
		if ((colors32 != null) && (colors32.Length > 0))
		{
			Debug.Assert(vertices.Length == colors32.Length);
			mesh.colors32 = colors32;
		}
		mesh.subMeshCount = subMeshes.Length;
		for (int i = 0; i < subMeshes.Length; i++)
		{
			mesh.SetIndices(subMeshes[i].indices, subMeshes[i].topology, i);
		}
		return mesh;
	}
}
