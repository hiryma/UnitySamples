using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kayac.DebugUi
{
	public class TextMeshCache
	{
		public TextMeshCache()
		{
			fontTextureVersion = int.MinValue;
		}

		public int FontTextureVersion { get => fontTextureVersion; }
		public void BeginSave(RendererBase renderer, float leftX, float topY)
		{
			this.fontTextureVersion = renderer.FontTextureVersion;
			this.leftXWhenSaved = leftX;
			this.topYWhenSaved = topY;
			this.vertexPositionWhenSaved = renderer.VertexCount;
			this.indexPositionWhenSaved = renderer.IndexCount;
		}

		public void EndSave(RendererBase renderer)
		{
			var vertexCount = renderer.VertexCount - vertexPositionWhenSaved;
			var indexCount = renderer.IndexCount - indexPositionWhenSaved;
			vertices = new RendererBase.Vertex[vertexCount];
			indices = new ushort[indexCount];
			renderer.GetVertices(vertices, vertexPositionWhenSaved, vertexCount);
			renderer.GetIndices(indices, indexPositionWhenSaved, indexCount);
		}

		public void Draw(Renderer2D renderer, string text, float leftX, float topY)
		{
			var vertexPosition = renderer.VertexCount;
			renderer.AddText(
				text,
				leftX - leftXWhenSaved,
				topY - topYWhenSaved,
				vertices,
				indices,
				vertexPosition - vertexPositionWhenSaved);
		}

		public void Invalidate()
		{
			fontTextureVersion = int.MinValue;
		}

		// non public ----
		RendererBase.Vertex[] vertices;
		ushort[] indices;
		int fontTextureVersion;
		int vertexPositionWhenSaved;
		int indexPositionWhenSaved;
		float leftXWhenSaved;
		float topYWhenSaved;
	}
}