using UnityEngine;

namespace Kayac
{
	public class ParticleMesh : DynamicMesh
	{
		public ParticleMesh(
			MeshRenderer meshRenderer,
			MeshFilter meshFilter,
			int capacity = defaultTriangleCapacity) : base(meshRenderer, meshFilter, capacity)
		{
		}

		public void AddRectangle(
			ref Vector3 center,
			ref Vector3 halfAxis0,
			ref Vector3 halfAxis1)
		{
			Vector3 tp0, tp1;
			Math.SetAdd(out tp0, ref center, ref halfAxis0);
			Math.SetSub(out tp1, ref center, ref halfAxis0);
			Math.SetSub(out vertices[vertexCount + 0], ref tp0, ref halfAxis1);
			Math.SetAdd(out vertices[vertexCount + 1], ref tp0, ref halfAxis1);
			Math.SetAdd(out vertices[vertexCount + 2], ref tp1, ref halfAxis1);
			Math.SetSub(out vertices[vertexCount + 3], ref tp1, ref halfAxis1);
			colors[vertexCount + 0] = color;
			colors[vertexCount + 1] = color;
			colors[vertexCount + 2] = color;
			colors[vertexCount + 3] = color;
			uv[vertexCount + 0] = Vector2.zero;
			uv[vertexCount + 1] = Vector2.zero;
			uv[vertexCount + 2] = Vector2.zero;
			uv[vertexCount + 3] = Vector2.zero;
			this.AddQuadIndices(0, 1, 2, 3);
			vertexCount += 4;
		}

		public void AddRectangleWholeTexture(
			ref Vector3 center,
			ref Vector3 halfAxis0,
			ref Vector3 halfAxis1)
		{
			Vector3 tp0, tp1;
			Math.SetAdd(out tp0, ref center, ref halfAxis0);
			Math.SetSub(out tp1, ref center, ref halfAxis0);
			Math.SetSub(out vertices[vertexCount + 0], ref tp0, ref halfAxis1);
			Math.SetAdd(out vertices[vertexCount + 1], ref tp0, ref halfAxis1);
			Math.SetAdd(out vertices[vertexCount + 2], ref tp1, ref halfAxis1);
			Math.SetSub(out vertices[vertexCount + 3], ref tp1, ref halfAxis1);
			colors[vertexCount + 0] = color;
			colors[vertexCount + 1] = color;
			colors[vertexCount + 2] = color;
			colors[vertexCount + 3] = color;
			uv[vertexCount + 0] = new Vector2(0f, 0f);
			uv[vertexCount + 1] = new Vector2(0f, 1f);
			uv[vertexCount + 2] = new Vector2(1f, 1f);
			uv[vertexCount + 3] = new Vector2(1f, 0f);
			this.AddQuadIndices(0, 1, 2, 3);
			vertexCount += 4;
		}

		public void AddRectangle(
			ref Vector3 center,
			ref Vector3 halfAxis0,
			ref Vector3 halfAxis1,
			ref Vector2 uvAll)
		{
			Vector3 tp0, tp1;
			Math.SetAdd(out tp0, ref center, ref halfAxis0);
			Math.SetSub(out tp1, ref center, ref halfAxis0);
			Math.SetSub(out vertices[vertexCount + 0], ref tp0, ref halfAxis1);
			Math.SetAdd(out vertices[vertexCount + 1], ref tp0, ref halfAxis1);
			Math.SetAdd(out vertices[vertexCount + 2], ref tp1, ref halfAxis1);
			Math.SetSub(out vertices[vertexCount + 3], ref tp1, ref halfAxis1);
			colors[vertexCount + 0] = color;
			colors[vertexCount + 1] = color;
			colors[vertexCount + 2] = color;
			colors[vertexCount + 3] = color;
			uv[vertexCount + 0] = uvAll;
			uv[vertexCount + 1] = uvAll;
			uv[vertexCount + 2] = uvAll;
			uv[vertexCount + 3] = uvAll;
			this.AddQuadIndices(0, 1, 2, 3);
			vertexCount += 4;
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