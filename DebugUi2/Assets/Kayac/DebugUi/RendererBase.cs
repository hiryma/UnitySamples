using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace Kayac.DebugUi
{
    public abstract class RendererBase : System.IDisposable
    {
        [StructLayout(LayoutKind.Explicit)] // これ機種依存しない?大丈夫?
        public struct Vertex
        {
            [FieldOffset(0)] public Vector2 position;
            [FieldOffset(8)] public Color32 color;
            [FieldOffset(12)] public Vector2 uv;
        }

        public enum TextOverflow
        {
            Scale, // 箱に収めるようスケール
            Wrap,
        }
        public const float DefaultLineSpacingRatio = 0f;
        public Color32 Color { get; set; }
        public int VertexCount { get => vertexCount; }
        public int IndexCount { get => indexCount; }
        public int FontTextureVersion { get; private set; }

        public void GetVertices(Vertex[] dst, int startIndex, int count)
        {
            System.Array.Copy(vertices, startIndex, dst, 0, count);
        }

        public void GetIndices(ushort[] dst, int startIndex, int count)
        {
            System.Array.Copy(indices, startIndex, dst, 0, count);
        }

        public void SetCapacity(int triangleCapacity)
        {
            capacity = triangleCapacity * 3;
            if (capacity >= 0xffff)
            {
                UnityEngine.Debug.LogWarning("triangleCapacity must be < 0xffff/3. clamped.");
                capacity = 0xffff;
            }
            vertices = new Vertex[capacity];
            indices = new ushort[capacity];
            vertexCount = 0;
            indexCount = 0; // すぐ足すことになる
            subMeshes = new List<SubMesh>
            {
                Capacity = InitialSubMeshCapacity
            };
        }

        public void Dispose()
        {
            Font.textureRebuilt -= OnFontTextureRebuilt;
        }

        // 描画キックを行う
        public void UpdateMesh()
        {
            // ■だけは常に入れておく。他は文字描画要求の都度投げる
            font.RequestCharactersInTexture(whiteString);
            // 描画キック
            mesh.Clear();

            if (subMeshCount > 0)
            {
                subMeshes[subMeshCount - 1].FixIndexCount(indexCount);

                mesh.SetVertexBufferParams(vertexCount, vertexDescriptors);
                mesh.SetVertexBufferData<Vertex>(
                    vertices,
                    0,
                    0,
                    vertexCount, 
                    0,
                    MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers);
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
                mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt16);
    			mesh.SetIndexBufferData(
                    indices,
                    0, 
                    0, 
                    indexCount,
                    MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers);
                
                var subMeshDesc = new SubMeshDescriptor();
                subMeshDesc.baseVertex = 0;
                subMeshDesc.bounds = new Bounds(Vector3.zero, Vector3.one * 1e10f);
                subMeshDesc.firstVertex = 0;
                subMeshDesc.topology = MeshTopology.Triangles;
                subMeshDesc.vertexCount = vertexCount;
                for (int i = 0; i < subMeshCount; i++)
                {
                    var subMesh = subMeshes[i];
                    subMeshDesc.indexStart = subMesh.indexStart;
                    subMeshDesc.indexCount = subMesh.indexCount;
                    mesh.SetSubMesh(
                        i, 
                        subMeshDesc,
                    MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers);
                    materialPropertyBlock.SetTexture(
                        textureShaderPropertyId,
                        subMesh.texture);
                    meshRenderer.SetPropertyBlock(materialPropertyBlock, i);
                }
            }

            meshFilter.sharedMesh = mesh;
            vertexCount = 0;
            indexCount = 0;
            texture = null;
            // 毎フレーム白にリセット
            Color = new Color32(255, 255, 255, 255);
            subMeshCount = 0;

            // どうもおかしいので毎フレーム取ってみる。
            CharacterInfo ch;
            font.GetCharacterInfo(whiteChar, out ch);
            whiteUv = ch.uvTopLeft;
            whiteUv += ch.uvTopRight;
            whiteUv += ch.uvBottomLeft;
            whiteUv += ch.uvBottomRight;
            whiteUv *= 0.25f;
        }

        // protected ----
        protected Font font;
        protected int vertexCount;
        protected int capacity;
        protected Vector2 whiteUv;
    	protected Vertex[] vertices;
        protected ushort[] indices;
		protected int indexCount;
        protected const int DefaultTriangleCapacity = 1024;
        public Texture FontTexture { get; private set; }

        protected RendererBase(
            RendererAsset asset,
            MeshRenderer meshRenderer,
            MeshFilter meshFilter,
            int capacity = DefaultTriangleCapacity)
        {
            textShader = asset.TextShader;
            texturedShader = asset.TexturedShader;
            font = asset.Font;
            this.meshRenderer = meshRenderer;
            this.meshFilter = meshFilter;
            Color = new Color32(255, 255, 255, 255);

            mesh = new Mesh();
            mesh.MarkDynamic();

            this.meshFilter.mesh = mesh;

            textMaterial = new Material(textShader);
            texturedMaterial = new Material(texturedShader);
            materialPropertyBlock = new MaterialPropertyBlock();

            textureShaderPropertyId = Shader.PropertyToID("_MainTex");

            Font.textureRebuilt += OnFontTextureRebuilt;
            font.RequestCharactersInTexture(whiteString);

            SetCapacity(capacity);

            // 初回は手動
            OnFontTextureRebuilt(font);

            vertexDescriptors = new VertexAttributeDescriptor[3];
            vertexDescriptors[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 2);
            vertexDescriptors[1] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4);
            vertexDescriptors[2] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2);
        }

        // private ----
        const int InitialSubMeshCapacity = 16;
        readonly Shader textShader;
        readonly Shader texturedShader;
        readonly Mesh mesh;
        readonly MaterialPropertyBlock materialPropertyBlock;
        readonly Material textMaterial;
        readonly Material texturedMaterial;
        readonly int textureShaderPropertyId;

#if UNITY_WEBGL
        const char whiteChar = '.';
#else
		const char whiteChar = '■';
#endif
        readonly string whiteString = new string(whiteChar, 1);

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

        List<SubMesh> subMeshes;
        // 毎フレーム0にリセットする。
        int subMeshCount;
        Texture texture;
        readonly MeshFilter meshFilter;
        readonly MeshRenderer meshRenderer;
        VertexAttributeDescriptor[] vertexDescriptors;
        Material[] materials; // サイズ変わる度に作り直す

        // ■の中心のUVを取り直す
        void OnFontTextureRebuilt(Font font)
        {
            if (font == this.font)
            {
                FontTexture = font.material.mainTexture; //　テクスチャ別物になってる可能性があるので刺しなおし
                CharacterInfo ch;
                this.font.GetCharacterInfo(whiteChar, out ch);
                whiteUv = ch.uvTopLeft;
                whiteUv += ch.uvTopRight;
                whiteUv += ch.uvBottomLeft;
                whiteUv += ch.uvBottomRight;
                whiteUv *= 0.25f;
                FontTextureVersion++;
            }
        }

        public void SetTexture(Texture texture)
        {
            if (this.texture != texture)
            {
                // ここまででSubMeshを終わらせる
                AddSubMesh(texture);
                this.texture = texture;
            }
        }

        void AddSubMesh(Texture texture, int minimumIndexCount = 0)
        {
            // 現インデクス数を記録
            if (subMeshCount > 0)
            {
                subMeshes[subMeshCount - 1].FixIndexCount(indexCount);
            }

            SubMesh subMesh;
            // 足りていれば使う。ただしインデクスは作り直す。TODO: もっとマシにできる。何フレームか経ったら使い回す、ということはできるはず。
            if (subMeshCount < subMeshes.Count)
            {
                subMesh = subMeshes[subMeshCount];
            }
            // 足りなければ足す
            else
            {
                subMesh = new SubMesh
                {
                    indexStart = indexCount
                };
                subMeshes.Add(subMesh);
            }

            // フォントテクスチャならテキストシェーダが差さったマテリアルを選択
            if (texture == font.material.mainTexture)
            {
                subMesh.material = textMaterial;
            }
            else
            {
                subMesh.material = texturedMaterial;
            }
            subMesh.texture = texture;
            subMeshCount++;
            indexCount = 0;
        }

        // 時計回りの相対頂点番号を3つ設定して三角形を生成
        protected void AddTriangleIndices(int i0, int i1, int i2)
        {
            indices[indexCount + 0] = (ushort)(vertexCount + i0);
            indices[indexCount + 1] = (ushort)(vertexCount + i1);
            indices[indexCount + 2] = (ushort)(vertexCount + i2);
            indexCount += 3;
        }

        // 時計回り4頂点で三角形を2個生成
        protected void AddQuadIndices(int i0, int i1, int i2, int i3)
        {
            indices[indexCount + 0] = (ushort)(vertexCount + i0);
            indices[indexCount + 1] = (ushort)(vertexCount + i1);
            indices[indexCount + 2] = (ushort)(vertexCount + i2);

            indices[indexCount + 3] = indices[indexCount + 2];
            indices[indexCount + 4] = (ushort)(vertexCount + i3);
            indices[indexCount + 5] = indices[indexCount + 0];
            indexCount += 6;
        }

        protected void AddIndices(IList<ushort> src)
        {
            var count = src.Count;
            for (int i = 0; i < count; i++)
            {
                indices[indexCount + i] = (ushort)(vertexCount + src[i]);
            }
            indexCount += count;
        }

        protected void AddIndices(IList<int> src)
        {
            var count = src.Count;
            for (int i = 0; i < count; i++)
            {
                indices[indexCount + i] = (ushort)(vertexCount + src[i]);
            }
            indexCount += count;
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
            UnityEngine.Profiling.Profiler.BeginSample("Kayac.DebugUi.RendererBase.AddTextNormalized");
            int letterCount = text.Length;
            font.RequestCharactersInTexture(text);
            SetTexture(FontTexture);

            widthOut = heightOut = 0f;
            // 高さが不足して一行も入らないケースに対処
            var lineHeight = (float)font.lineHeight;
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
            p.y += font.ascent;
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
                else if (!waitNewLine && font.GetCharacterInfo(c, out ch))
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

//Debug.Log(c + " " + ch.uvTopLeft.ToString("F3") + " " + ch.uvBottomRight.ToString("F3"));
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
            if (((vertexCount + 4) > capacity) || ((indexCount + 6) > capacity))
            {
                return false;
            }
            float x = (float)(ch.minX);
            float y = (float)(-ch.maxY);
            float w = (float)(ch.maxX - ch.minX);
            float h = (float)(ch.maxY - ch.minY);

            var p0 = new Vector2(p.x + x, p.y + y); // 左上
            var p1 = new Vector2(p0.x + w, p0.y); // 右上
            var p2 = new Vector2(p1.x, p0.y + h); // 右下
            var p3 = new Vector2(p0.x, p2.y); // 左下

            // 頂点は左上から時計回り
            vertices[vertexCount + 0].position = p0;
            vertices[vertexCount + 1].position = p1;
            vertices[vertexCount + 2].position = p2;
            vertices[vertexCount + 3].position = p3;

            vertices[vertexCount + 0].uv = ch.uvTopLeft;
            vertices[vertexCount + 1].uv = ch.uvTopRight;
            vertices[vertexCount + 2].uv = ch.uvBottomRight;
            vertices[vertexCount + 3].uv = ch.uvBottomLeft;
            for (int j = 0; j < 4; j++)
            {
                vertices[vertexCount + j].color = Color;
            }

            AddQuadIndices(0, 1, 2, 3);
            vertexCount += 4;
            return true;
        }

        protected void AddCached(
            float deltaX,
            float deltaY,
            Vertex[] verticesCached,
            ushort[] indicesCached,
            int indexOffset)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Kayac.DebugUi.RendererBase.AddCached");
            if ((vertexCount + verticesCached.Length) < vertices.Length)
            {
                if ((indexCount + indicesCached.Length) < indices.Length)
                {
                    var verticesBegin = vertexCount;
                    System.Array.Copy(verticesCached, 0, vertices, vertexCount, verticesCached.Length);
                    vertexCount += verticesCached.Length;

                    for (var i = 0; i < indicesCached.Length; i++)
                    {
                        indices[indexCount + i] = (ushort)(indicesCached[i] + indexOffset);
                    }
                    indexCount += indicesCached.Length;

                    var offset = new Vector2(deltaX, deltaY);
                    OffsetVertices(verticesBegin, ref offset);
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }

        protected void TransformVertices(int verticesBegin, float scale, ref Vector2 translation)
        {
            int vCount = vertexCount - verticesBegin;
            for (int i = 0; i < vCount; i++)
            {
                var v = vertices[verticesBegin + i].position;
                v.x *= scale;
                v.y *= scale;
                v.x += translation.x;
                v.y += translation.y;
                vertices[verticesBegin + i].position = v;
            }
        }

        protected void OffsetVertices(int verticesBegin, ref Vector2 translation)
        {
            int vCount = vertexCount - verticesBegin;
            for (int i = 0; i < vCount; i++)
            {
                var v = vertices[verticesBegin + i].position;
                v.x += translation.x;
                v.y += translation.y;
                vertices[verticesBegin + i].position = v;
            }
        }
    }
}
