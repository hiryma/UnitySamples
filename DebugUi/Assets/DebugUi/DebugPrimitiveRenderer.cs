using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace Kayac
{
	// MonoBehaviourが所有者として必要。LateUpdateを呼ばないと描画されない。
	public abstract class DebugPrimitiveRenderer : System.IDisposable
	{
		public enum Alignment
		{
			Left,
			Center,
			Right,
		}
		public const float DefaultLineHeight = float.NaN;
		protected const int DefaultTriangleCapacity = 1024;
		private const int InitialSubMeshCapacity = 16;

		private Shader _textShader;
		private Shader _texturedShader;
		private Mesh _mesh;
		private MaterialPropertyBlock _materialPropertyBlock;
		private Material _textMaterial;
		private Material _texturedMaterial;
		private int _textureShaderPropertyId;
		private CommandBuffer _commandBuffer;
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
		protected Camera _camera;
		private class SubMesh
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
		private List<SubMesh> _subMeshes;
		// 毎フレーム0にリセットする。
		private int _subMeshCount;
		private Texture _texture;

		public Color32 color { get; set; }

		protected Texture fontTexture
		{
			get
			{
				return _font.material.mainTexture;
			}
		}

		public DebugPrimitiveRenderer(
			Shader textShader,
			Shader texturedShader,
			Font font,
			Camera camera,
			int capacity = DefaultTriangleCapacity)
		{
			_textShader = textShader;
			_texturedShader = texturedShader;
			_font = font;
			_camera = camera;
			color = new Color32(255, 255, 255, 255);

			_mesh = new Mesh();
			_mesh.MarkDynamic();

			_textMaterial = new Material(_textShader);
			_texturedMaterial = new Material(_texturedShader);
			_materialPropertyBlock = new MaterialPropertyBlock();
			_commandBuffer = new CommandBuffer();
			_camera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, _commandBuffer);

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
			_camera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, _commandBuffer);
		}

		protected virtual void OnLateUpdate()
		{
		}

		// コマンドバッファを生成する
		public void LateUpdate()
		{
			OnLateUpdate();
			// ■だけは常に入れておく。他は文字描画要求の都度投げる
			_font.RequestCharactersInTexture("■");
			// 描画キック
			_mesh.Clear();

			if (_subMeshCount > 0)
			{
				_subMeshes[_subMeshCount - 1].FixIndexCount(_indexCount);

				// 使用量が半分以下の場合、テンポラリにコピーしてから渡す
				if (_vertexCount < (_capacity / 2)) // 閾値は研究が必要だが、とりあえず。
				{
					_temporaryVertices.Clear();
					_temporaryUv.Clear();
					_temporaryColors.Clear();
					for (int i = 0; i < _vertexCount; i++)
					{
						_temporaryVertices.Add(_vertices[i]);
						_temporaryUv.Add(_uv[i]);
						_temporaryColors.Add(_colors[i]);
					}
					_mesh.SetVertices(_temporaryVertices);
					_mesh.SetUVs(0, _temporaryUv);
					_mesh.SetColors(_temporaryColors);
				}
				else // 半分以上使っている場合、そのまま渡す。
				{
					_mesh.vertices = _vertices;
					_mesh.uv = _uv;
					_mesh.colors32 = _colors;
				}
				_mesh.subMeshCount = _subMeshCount;
				var matrix = Matrix4x4.identity;
				_commandBuffer.Clear();
				for (int i = 0; i < _subMeshCount; i++)
				{
					var subMesh = _subMeshes[i];
					_temporaryIndices.Clear();
					for (int srcIndex = subMesh.indexStart; srcIndex < subMesh.indexCount; srcIndex++)
					{
						_temporaryIndices.Add(_indices[srcIndex]);
					}
					_mesh.SetTriangles(_temporaryIndices, i, true);
					_materialPropertyBlock.SetTexture(
						_textureShaderPropertyId,
						subMesh.texture);
					_commandBuffer.DrawMesh(
						_mesh,
						matrix,
						subMesh.material,
						i,
						-1,
						_materialPropertyBlock);
				}
			}

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
		private void OnFontTextureRebuilt(Font font)
		{
			if (font == _font)
			{
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

		private void AddSubMesh(Texture texture, int minimumIndexCount = 0)
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

		// 時計回り4頂点で三角形を2個生成
		protected void AddQuadIndices(int i0, int i1, int i2, int i3)
		{
			AddTriangleIndices(i0, i1, i2);
			AddTriangleIndices(i2, i3, i0);
		}
	}
}
