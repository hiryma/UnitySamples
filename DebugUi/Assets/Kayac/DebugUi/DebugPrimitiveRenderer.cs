using System.Collections.Generic;
using UnityEngine;

namespace Kayac
{
	public abstract class DebugPrimitiveRenderer : System.IDisposable
	{
		public enum AlignX
		{
			Left,
			Center,
			Right,
		}
		public enum AlignY
		{
			Top,
			Center,
			Bottom,
		}
		public enum TextOverflow
		{
			Scale, // 箱に収めるようスケール
			Wrap,
		}
		public const float DefaultLineSpacingRatio = 0f;
		protected const int DefaultTriangleCapacity = 1024;
		const int InitialSubMeshCapacity = 16;

		Shader _textShader;
		Shader _texturedShader;
		Mesh _mesh;
		MaterialPropertyBlock _materialPropertyBlock;
		Material _textMaterial;
		Material _texturedMaterial;
		int _textureShaderPropertyId;
		protected Font _font;
		protected int _vertexCount;
		protected int _capacity;
		protected Vector2 _whiteUv;
		protected Vector3[] _vertices;
		protected int _indexCount;
		protected Vector2[] _uv;
		protected Color32[] _colors;
		protected int[] _indices;
		protected List<Vector2> _temporaryUv; // SetTriangles寸前に使う
		protected List<Vector3> _temporaryVertices; // SetTriangles寸前に使う
		protected List<Color32> _temporaryColors; // SetTriangles寸前に使う
		protected List<int> _temporaryIndices; // SetTriangles寸前に使う
		class SubMesh
		{
			public void FixIndexCount(int indexPosition)
			{
				indexCount = indexPosition - indexStart;
			}

			public Material material;
			public Texture texture;
			public int indexStart;
			public int indexCount;
		}
		List<SubMesh> _subMeshes;
		// 毎フレーム0にリセットする。
		int _subMeshCount;
		Texture _texture;
		MeshFilter _meshFilter;
		MeshRenderer _meshRenderer;

		public Color32 color { get; set; }
		protected Texture fontTexture { get; private set; }

		public DebugPrimitiveRenderer(
			Shader textShader,
			Shader texturedShader,
			Font font,
			MeshRenderer meshRenderer,
			MeshFilter meshFilter,
			int capacity = DefaultTriangleCapacity)
		{
			_textShader = textShader;
			_texturedShader = texturedShader;
			_font = font;
			_meshRenderer = meshRenderer;
			_meshFilter = meshFilter;
			color = new Color32(255, 255, 255, 255);

			_mesh = new Mesh();
			_mesh.MarkDynamic();
			_meshFilter.mesh = _mesh;

			_textMaterial = new Material(_textShader);
			_texturedMaterial = new Material(_texturedShader);
			_materialPropertyBlock = new MaterialPropertyBlock();

			_textureShaderPropertyId = Shader.PropertyToID("_MainTex");

			Font.textureRebuilt += OnFontTextureRebuilt;
			_font.RequestCharactersInTexture("■");

			SetCapacity(capacity);

			// 初回は手動
			OnFontTextureRebuilt(_font);
		}

		public void SetCapacity(int triangleCapacity)
		{
			_capacity = triangleCapacity * 3;
			if (_capacity >= 0xffff)
			{
				Debug.LogWarning("triangleCapacity must be < 0xffff/3. clamped.");
				_capacity = 0xffff;
			}
			_vertices = new Vector3[_capacity];
			_uv = new Vector2[_capacity];
			_colors = new Color32[_capacity];
			_indices = new int[_capacity];
			_temporaryVertices = new List<Vector3>(_capacity); // SetTriangles寸前に使う
			_temporaryColors = new List<Color32>(_capacity); // SetTriangles寸前に使う
			_temporaryUv = new List<Vector2>(_capacity); // SetTriangles寸前に使う
			_temporaryIndices = new List<int>(_capacity); // SetTriangles寸前に使う
			_vertexCount = 0;
			_indexCount = 0; // すぐ足すことになる
			_subMeshes = new List<SubMesh>();
			_subMeshes.Capacity = InitialSubMeshCapacity;
		}

		public void Dispose()
		{
			Font.textureRebuilt -= OnFontTextureRebuilt;
		}

		// 描画キックを行う
		public void UpdateMesh()
		{
			// ■だけは常に入れておく。他は文字描画要求の都度投げる
			_font.RequestCharactersInTexture("■");
			// 描画キック
			_mesh.Clear();

			Material[] materials = null;
			if (_subMeshCount > 0)
			{
				_subMeshes[_subMeshCount - 1].FixIndexCount(_indexCount);
				// 使用量が半分以下の場合、テンポラリにコピーしてから渡す
				if (_vertexCount < (_capacity / 2)) // 閾値は研究が必要だが、とりあえず。
				{
					UnityEngine.Profiling.Profiler.BeginSample("DebugPrimitiveRenderer.UpdateMesh.FillTemporary");

					_temporaryVertices.Clear();
					_temporaryUv.Clear();
					_temporaryColors.Clear();

					var tmpV = new System.ArraySegment<Vector3>(_vertices, 0, _vertexCount);
					var tmpUv = new System.ArraySegment<Vector2>(_uv, 0, _vertexCount);
					var tmpC = new System.ArraySegment<Color32>(_colors, 0, _vertexCount);

					_temporaryVertices.AddRange(tmpV);
					_temporaryUv.AddRange(tmpUv);
					_temporaryColors.AddRange(tmpC);

					_mesh.SetVertices(_temporaryVertices);
					_mesh.SetUVs(0, _temporaryUv);
					_mesh.SetColors(_temporaryColors);

					UnityEngine.Profiling.Profiler.EndSample();
				}
				else // 半分以上使っている場合、そのまま渡す。
				{
					UnityEngine.Profiling.Profiler.BeginSample("DebugPrimitiveRenderer.UpdateMesh.CopyAll");
					_mesh.vertices = _vertices;
					_mesh.uv = _uv;
					_mesh.colors32 = _colors;
					UnityEngine.Profiling.Profiler.EndSample();
				}
				_mesh.subMeshCount = _subMeshCount;

				materials = new Material[_subMeshCount];
				for (int i = 0; i < _subMeshCount; i++)
				{
					materials[i] = _subMeshes[i].material;
				}
				_meshRenderer.sharedMaterials = materials;

				var matrix = Matrix4x4.identity;
				for (int i = 0; i < _subMeshCount; i++)
				{
					UnityEngine.Profiling.Profiler.BeginSample("DebugPrimitiveRenderer.UpdateMesh.FillIndices");
					var subMesh = _subMeshes[i];
					_temporaryIndices.Clear();
					var tmpI = new System.ArraySegment<int>(_indices, subMesh.indexStart, subMesh.indexCount);
					_temporaryIndices.AddRange(tmpI);
					_mesh.SetTriangles(_temporaryIndices, i, true);
					_materialPropertyBlock.SetTexture(
						_textureShaderPropertyId,
						subMesh.texture);
					_meshRenderer.SetPropertyBlock(_materialPropertyBlock, i);
					UnityEngine.Profiling.Profiler.EndSample();
				}
			}
			_meshFilter.sharedMesh = _mesh;
			_vertexCount = 0;
			_indexCount = 0;
			_texture = null;
			// 毎フレーム白にリセット
			color = new Color32(255, 255, 255, 255);
			_subMeshCount = 0;

			// どうもおかしいので毎フレーム取ってみる。
			CharacterInfo ch;
			_font.GetCharacterInfo('■', out ch);
			_whiteUv = ch.uvTopLeft;
			_whiteUv += ch.uvTopRight;
			_whiteUv += ch.uvBottomLeft;
			_whiteUv += ch.uvBottomRight;
			_whiteUv *= 0.25f;
		}

		// ■の中心のUVを取り直す
		void OnFontTextureRebuilt(Font font)
		{
			if (font == _font)
			{
				this.fontTexture = font.material.mainTexture; //　テクスチャ別物になってる可能性があるので刺しなおし
				CharacterInfo ch;
				_font.GetCharacterInfo('■', out ch);
				_whiteUv = ch.uvTopLeft;
				_whiteUv += ch.uvTopRight;
				_whiteUv += ch.uvBottomLeft;
				_whiteUv += ch.uvBottomRight;
				_whiteUv *= 0.25f;
			}
		}

		public void SetTexture(Texture texture)
		{
			if (_texture != texture)
			{
				// ここまででSubMeshを終わらせる
				AddSubMesh(texture);
				_texture = texture;
			}
		}

		void AddSubMesh(Texture texture, int minimumIndexCount = 0)
		{
			// 現インデクス数を記録
			if (_subMeshCount > 0)
			{
				_subMeshes[_subMeshCount - 1].FixIndexCount(_indexCount);
			}

			SubMesh subMesh = null;
			// 足りていれば使う。ただしインデクスは作り直す。TODO: もっとマシにできる。何フレームか経ったら使い回す、ということはできるはず。
			if (_subMeshCount < _subMeshes.Count)
			{
				subMesh = _subMeshes[_subMeshCount];
			}
			// 足りなければ足す
			else
			{
				subMesh = new SubMesh();
				subMesh.indexStart = _indexCount;
				_subMeshes.Add(subMesh);
			}

			// フォントテクスチャならテキストシェーダが差さったマテリアルを選択
			if (texture == _font.material.mainTexture)
			{
				subMesh.material = _textMaterial;
			}
			else
			{
				subMesh.material = _texturedMaterial;
			}
			subMesh.texture = texture;
			_subMeshCount++;
			_indexCount = 0;
		}

		// 時計回りの相対頂点番号を3つ設定して三角形を生成
		protected void AddTriangleIndices(int i0, int i1, int i2)
		{
			_indices[_indexCount + 0] = _vertexCount + i0;
			_indices[_indexCount + 1] = _vertexCount + i1;
			_indices[_indexCount + 2] = _vertexCount + i2;
			_indexCount += 3;
		}

		// 時計回り4頂点で三角形を2個生成
		protected void AddQuadIndices(int i0, int i1, int i2, int i3)
		{
			_indices[_indexCount + 0] = _vertexCount + i0;
			_indices[_indexCount + 1] = _vertexCount + i1;
			_indices[_indexCount + 2] = _vertexCount + i2;

			_indices[_indexCount + 3] = _vertexCount + i2;
			_indices[_indexCount + 4] = _vertexCount + i3;
			_indices[_indexCount + 5] = _vertexCount + i0;
			_indexCount += 6;
		}

		protected void AddIndices(IList<ushort> src)
		{
			var count = src.Count;
			for (int i = 0; i < count; i++)
			{
				_indices[_indexCount + i] = _vertexCount + src[i];
			}
			_indexCount += count;
		}

		protected void AddIndices(IList<int> src)
		{
			var count = src.Count;
			for (int i = 0; i < count; i++)
			{
				_indices[_indexCount + i] = _vertexCount + src[i];
			}
			_indexCount += count;
		}

		// 書き込み行数を返す
		protected int AddTextNormalized(
			out float widthOut,
			out float heightOut,
			string text,
			float boxWidth,
			float boxHeight,
			float lineSpacingRatio,
			bool wrap)
		{
			UnityEngine.Profiling.Profiler.BeginSample("DebugPrimitiveRenderer.AddTextNormalized");
			int letterCount = text.Length;
			_font.RequestCharactersInTexture(text);
			SetTexture(fontTexture);
			var verticesBegin = _vertexCount;

			widthOut = heightOut = 0f;
			// 高さが不足して一行も入らないケースに対処
			var lineHeight = (float)_font.lineHeight;
			if (lineHeight > boxHeight)
			{
				return 0;
			}
			heightOut = lineHeight;
			// 二行目以降行間込みにする
			lineHeight *= (1f + lineSpacingRatio);
			// まず原点開始、z=0,xyがfont内整数座標、という状態で頂点を生成してしまう。
			var pos = 0;
			var lines = 1;
			var p = Vector2.zero;
			p.y += _font.ascent;
			var waitNewLine = false;
			while (pos < letterCount)
			{
				CharacterInfo ch;
				var c = text[pos];
				if (c == '\n')
				{
					waitNewLine = false;
					p.y += lineHeight;
					p.x = 0f;
					// 縦はみ出しは即時終了
					if ((heightOut + lineHeight) > boxHeight)
					{
						break;
					}
					heightOut += lineHeight;
					lines++;
				}
				else if (!waitNewLine && _font.GetCharacterInfo(c, out ch))
				{
					if ((p.x + ch.advance) > boxWidth) // 横にはみ出した
					{
						if (wrap) // 折り返し
						{
							p.y += lineHeight;
							p.x = 0f;
							// 縦はみ出しは即時終了
							if ((heightOut + lineHeight) > boxHeight)
							{
								break;
							}
							heightOut += lineHeight;
							lines++;
						}
						else // 次の改行まで捨てる
						{
							waitNewLine = true;
							break;
						}
					}

					if (!AddCharNormalized(ref p, ref ch))
					{
						break;
					}
					p.x += ch.advance;
					widthOut = Mathf.Max(p.x, widthOut);
				}
				pos++;
			}
			UnityEngine.Profiling.Profiler.EndSample();
			return lines;
		}

		bool AddCharNormalized(
			ref Vector2 p, // 原点(Xは左端、Yは上端+Font.ascent)
			ref CharacterInfo ch)
		{
			if (((_vertexCount + 4) > _capacity) || ((_indexCount + 6) > _capacity))
			{
				return false;
			}
			float x = (float)(ch.minX);
			float y = (float)(-ch.maxY);
			float w = (float)(ch.maxX - ch.minX);
			float h = (float)(ch.maxY - ch.minY);

			var p0 = new Vector3(p.x + x, p.y + y, 0f); // 左上
			var p1 = new Vector3(p0.x + w, p0.y, 0f); // 右上
			var p2 = new Vector3(p1.x, p0.y + h, 0f); // 右下
			var p3 = new Vector3(p0.x, p2.y, 0f); // 左下

			// 頂点は左上から時計回り
			_vertices[_vertexCount + 0] = p0;
			_vertices[_vertexCount + 1] = p1;
			_vertices[_vertexCount + 2] = p2;
			_vertices[_vertexCount + 3] = p3;

			_uv[_vertexCount + 0] = ch.uvTopLeft;
			_uv[_vertexCount + 1] = ch.uvTopRight;
			_uv[_vertexCount + 2] = ch.uvBottomRight;
			_uv[_vertexCount + 3] = ch.uvBottomLeft;

			for (int j = 0; j < 4; j++)
			{
				_colors[_vertexCount + j] = color;
			}

			AddQuadIndices(0, 1, 2, 3);
			_vertexCount += 4;
			return true;
		}

		protected void TransformVertices(int verticesBegin, ref Matrix4x4 matrix)
		{
			int vertexCount = _vertexCount - verticesBegin;
			for (int i = 0; i < vertexCount; i++)
			{
				var v = _vertices[verticesBegin + i];
				v = matrix.MultiplyPoint(v);
				_vertices[verticesBegin + i] = v;
			}
		}

		protected void TransformVertices(int verticesBegin, float scale, ref Vector2 translation)
		{
			int vertexCount = _vertexCount - verticesBegin;
			for (int i = 0; i < vertexCount; i++)
			{
				var v = _vertices[verticesBegin + i];
				v.x *= scale;
				v.y *= scale;
				v.x += translation.x;
				v.y += translation.y;
				_vertices[verticesBegin + i] = v;
			}
		}



		// 以下未実装。箱だけ作っておく。数字の文字列化に伴うGC Alloc抹殺のため
		public static void ToStringAsCharArray(out int writeCount, char[] dst, int dstPos, int value, int precision)
		{
			writeCount = 0;
		}

		public static void ToStringAsCharArray(out int writeCount, char[] dst, int dstPos, float value, int precision)
		{
			writeCount = 0;
		}
	}
}
