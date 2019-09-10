using UnityEngine;
using System.Collections.Generic;

namespace Kayac
{
	public class ParticleMesh : MonoBehaviour
	{
		[SerializeField] MeshFilter meshFilter;
		[SerializeField] Vector3 boundingCenter = Vector3.zero;
		[SerializeField] Vector3 boundingSize = new Vector3(1000f, 1000f, 1000f);

		Mesh mesh;
		List<Vector3> vertices;
		List<Vector3> normals;
		List<Vector2> uvs;
		List<int> indices;

		void Start()
		{
			Debug.Assert(meshFilter != null);
			mesh = new Mesh();
			mesh.name = "ParticleMesh";
			meshFilter.sharedMesh = mesh;
			vertices = new List<Vector3>();
			normals = new List<Vector3>();
			uvs = new List<Vector2>();
			indices = new List<int>();
		}

		public void UpdateMesh()
		{
			mesh.Clear();
			mesh.SetVertices(vertices);
			mesh.SetNormals(normals);
			mesh.SetUVs(0, uvs);
			mesh.SetTriangles(indices, 0, calculateBounds: false);
			mesh.bounds = new Bounds(boundingCenter, boundingSize);
			vertices.Clear();
			normals.Clear();
			uvs.Clear();
			indices.Clear();
		}

		public void AddRectangle(
			ref Vector3 center,
			ref Vector3 halfAxis0,
			ref Vector3 halfAxis1,
			ref Vector3 normal,
			ref Vector2 uvAll)
		{
			AddQuadIndices(0, 1, 3, 2);
			var p0 = center - halfAxis0;
			var p1 = center + halfAxis0;
			vertices.Add(p0 - halfAxis1);
			vertices.Add(p0 + halfAxis1);
			vertices.Add(p1 - halfAxis1);
			vertices.Add(p1 + halfAxis1);
			normals.Add(normal);
			normals.Add(normal);
			normals.Add(normal);
			normals.Add(normal);
			uvs.Add(uvAll);
			uvs.Add(uvAll);
			uvs.Add(uvAll);
			uvs.Add(uvAll);
		}

		void AddQuadIndices(int i0, int i1, int i2, int i3)
		{
			var vOffset = vertices.Count;
			indices.Add(vOffset + i0);
			indices.Add(vOffset + i1);
			indices.Add(vOffset + i2);
			indices.Add(vOffset + i2);
			indices.Add(vOffset + i3);
			indices.Add(vOffset + i0);
		}
	}
}