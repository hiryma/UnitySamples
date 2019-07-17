using System.Collections.Generic;
using UnityEngine;

namespace Kayac
{
	public class DynamicMesh
	{
		protected const int defaultTriangleCapacity = 1024;
		const int initialSubMeshCapacity = 16;

		Mesh mesh;
		Material material;
		protected int vertexCount;
		protected int capacity;
		protected Vector3[] vertices;
		protected int indexCount;
		protected Vector2[] uv;
		protected Color32[] colors;
		protected int[] indices;
		protected List<Vector2> temporaryUv; // SetTriangles寸前に使う
		protected List<Vector3> temporaryVertices; // SetTriangles寸前に使う
		protected List<Color32> temporaryColors; // SetTriangles寸前に使う
		protected List<int> temporaryIndices; // SetTriangles寸前に使う
		class SubMesh
		{
			public void FixIndexCount(int indexPosition)
			{
				indexCount = indexPosition - indexStart;
			}

			public Material material;
			public int indexStart;
			public int indexCount;
		}
		List<SubMesh> subMeshes;
		// 毎フレーム0にリセットする。
		int subMeshCount;
		MeshFilter meshFilter;
		MeshRenderer meshRenderer;
		Material[] materials;

		public Color32 color { get; set; }
		protected Texture fontTexture { get; private set; }

		public DynamicMesh(
			MeshRenderer meshRenderer,
			MeshFilter meshFilter,
			int capacity = defaultTriangleCapacity)
		{
			this.meshRenderer = meshRenderer;
			this.meshFilter = meshFilter;
			color = new Color32(255, 255, 255, 255);

			mesh = new Mesh();
			mesh.MarkDynamic();
			meshFilter.mesh = mesh;
			SetCapacity(capacity);
		}

		public void SetCapacity(int triangleCapacity)
		{
			capacity = triangleCapacity * 3;
			if (capacity >= 0xffff)
			{
				Debug.LogWarning("triangleCapacity must be < 0xffff/3. clamped.");
				capacity = 0xffff;
			}
			vertices = new Vector3[capacity];
			uv = new Vector2[capacity];
			colors = new Color32[capacity];
			indices = new int[capacity];
			temporaryVertices = new List<Vector3>(capacity); // SetTriangles寸前に使う
			temporaryColors = new List<Color32>(capacity); // SetTriangles寸前に使う
			temporaryUv = new List<Vector2>(capacity); // SetTriangles寸前に使う
			temporaryIndices = new List<int>(capacity); // SetTriangles寸前に使う
			vertexCount = 0;
			indexCount = 0; // すぐ足すことになる
			subMeshes = new List<SubMesh>();
			subMeshes.Capacity = initialSubMeshCapacity;
		}

		// 描画キックを行う
		public void Update()
		{
			mesh.Clear();
			if (subMeshCount > 0)
			{
				FillSubMeshes();
			}
			mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f); // 負荷削減
			meshFilter.sharedMesh = mesh;
			vertexCount = 0;
			indexCount = 0;
			material = null;
			// 毎フレーム白にリセット
			color = new Color32(255, 255, 255, 255);
			subMeshCount = 0;
		}

		void FillSubMeshes()
		{
			subMeshes[subMeshCount - 1].FixIndexCount(indexCount);
			// 使用量が半分以下の場合、テンポラリにコピーしてから渡す
			if (vertexCount < (capacity / 2)) // 閾値は研究が必要だが、とりあえず。
			{
				UnityEngine.Profiling.Profiler.BeginSample("DynamicMesh.Update.FillTemporary");

				temporaryVertices.Clear();
				temporaryUv.Clear();
				temporaryColors.Clear();

				var tmpV = new System.ArraySegment<Vector3>(vertices, 0, vertexCount);
				var tmpUv = new System.ArraySegment<Vector2>(uv, 0, vertexCount);
				var tmpC = new System.ArraySegment<Color32>(colors, 0, vertexCount);

				temporaryVertices.AddRange(tmpV);
				temporaryUv.AddRange(tmpUv);
				temporaryColors.AddRange(tmpC);

				mesh.SetVertices(temporaryVertices);
				mesh.SetUVs(0, temporaryUv);
				mesh.SetColors(temporaryColors);

				UnityEngine.Profiling.Profiler.EndSample();
			}
			else // 半分以上使っている場合、そのまま渡す。
			{
				UnityEngine.Profiling.Profiler.BeginSample("DynamicMesh.Update.CopyAll");
				mesh.vertices = vertices;
				mesh.uv = uv;
				mesh.colors32 = colors;
				UnityEngine.Profiling.Profiler.EndSample();
			}
			mesh.subMeshCount = subMeshCount;

			if ((materials == null) || (materials.Length != subMeshCount))
			{
				materials = new Material[subMeshCount];
			}
			for (int i = 0; i < subMeshCount; i++)
			{
				materials[i] = subMeshes[i].material;
			}
			meshRenderer.sharedMaterials = materials;

			var matrix = Matrix4x4.identity;
			for (int i = 0; i < subMeshCount; i++)
			{
				UnityEngine.Profiling.Profiler.BeginSample("DynamicMesh.Update.FillIndices");
				var subMesh = subMeshes[i];
				temporaryIndices.Clear();
				var tmpI = new System.ArraySegment<int>(indices, subMesh.indexStart, subMesh.indexCount);
				temporaryIndices.AddRange(tmpI);
				mesh.SetTriangles(temporaryIndices, i, calculateBounds: false);
				UnityEngine.Profiling.Profiler.EndSample();
			}
		}

		public void SetMaterial(Material material)
		{
			if (this.material != material)
			{
				// ここまででSubMeshを終わらせる
				AddSubMesh(material);
				this.material = material;
			}
		}

		void AddSubMesh(Material material, int minimumIndexCount = 0)
		{
			// 現インデクス数を記録
			if (subMeshCount > 0)
			{
				subMeshes[subMeshCount - 1].FixIndexCount(indexCount);
			}

			SubMesh subMesh = null;
			// 足りていれば使う。ただしインデクスは作り直す。TODO: もっとマシにできる。何フレームか経ったら使い回す、ということはできるはず。
			if (subMeshCount < subMeshes.Count)
			{
				subMesh = subMeshes[subMeshCount];
			}
			// 足りなければ足す
			else
			{
				subMesh = new SubMesh();
				subMesh.indexStart = indexCount;
				subMeshes.Add(subMesh);
			}

			subMesh.material = material;
			subMeshCount++;
			indexCount = 0;
		}

		// 時計回りの相対頂点番号を3つ設定して三角形を生成
		protected void AddTriangleIndices(int i0, int i1, int i2)
		{
			Debug.Assert((indexCount + i2) <= 0xffff);
			indices[indexCount + 0] = vertexCount + i0;
			indices[indexCount + 1] = vertexCount + i1;
			indices[indexCount + 2] = vertexCount + i2;
			indexCount += 3;
		}

		// 時計回り4頂点で三角形を2個生成
		protected void AddQuadIndices(int i0, int i1, int i2, int i3)
		{
			Debug.Assert((indexCount + i3) <= 0xffff);
			indices[indexCount + 0] = vertexCount + i0;
			indices[indexCount + 1] = vertexCount + i1;
			indices[indexCount + 2] = vertexCount + i2;

			indices[indexCount + 3] = vertexCount + i2;
			indices[indexCount + 4] = vertexCount + i3;
			indices[indexCount + 5] = vertexCount + i0;
			indexCount += 6;
		}

		protected void AddIndices(IList<ushort> src)
		{
			var count = src.Count;
			for (int i = 0; i < count; i++)
			{
				indices[indexCount + i] = vertexCount + src[i];
			}
			indexCount += count;
		}

		protected void AddIndices(IList<int> src)
		{
			var count = src.Count;
			for (int i = 0; i < count; i++)
			{
				indices[indexCount + i] = vertexCount + src[i];
			}
			indexCount += count;
		}
	}
}
