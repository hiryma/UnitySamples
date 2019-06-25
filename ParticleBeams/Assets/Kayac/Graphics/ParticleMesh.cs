using UnityEngine;

namespace Kayac
{
	public class ParticleMesh : DynamicMesh
	{
		public ParticleMesh(
			Shader shader,
			MeshRenderer meshRenderer,
			MeshFilter meshFilter,
			int capacity = defaultTriangleCapacity) : base(shader, meshRenderer, meshFilter, capacity)
		{
		}

		public new void SetTexture(Texture texture)
		{
			base.SetTexture(texture);
		}

		public void AddTriangle(
			ref Vector3 p0,
			ref Vector3 p1,
			ref Vector3 p2,
			ref Vector2 uv0,
			ref Vector2 uv1,
			ref Vector2 uv2)
		{
			vertices[vertexCount + 0] = p0;
			vertices[vertexCount + 1] = p1;
			vertices[vertexCount + 2] = p2;
			colors[vertexCount + 0] = color;
			colors[vertexCount + 1] = color;
			colors[vertexCount + 2] = color;
			uv[vertexCount + 0] = uv0;
			uv[vertexCount + 1] = uv1;
			uv[vertexCount + 2] = uv2;
			AddTriangleIndices(0, 1, 2);
			vertexCount += 3;
		}
	}
}