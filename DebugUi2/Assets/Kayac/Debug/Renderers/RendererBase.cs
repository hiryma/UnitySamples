using System.Collections.Generic;
using UnityEngine;

namespace Kayac.Debug
{
    public abstract class RendererBase : System.IDisposable
    {
        public enum TextOverflow
        {
            Scale, // 箱に収めるようスケール
            Wrap,
        }
        public const float DefaultLineSpacingRatio = 0f;
        protected const int DefaultTriangleCapacity = 1024;
        const int InitialSubMeshCapacity = 16;
        readonly Shader textShader;
        readonly Shader texturedShader;
        readonly Mesh mesh;
        readonly MaterialPropertyBlock materialPropertyBlock;
        readonly Material textMaterial;
        readonly Material texturedMaterial;
        readonly int textureShaderPropertyId;
        protected Font font;
        protected int vertexCount;
        protected int capacity;
        protected Vector2 whiteUv;
        protected Vector3[] vertices;
        protected int indexCount;
        protected Vector2[] uv;
        protected Color32[] colors;
        protected int[] indices;
        protected List<Vector2> temporaryUv; // SetTriangles寸前に使う
        protected List<Vector3> temporaryVertices; // SetTriangles寸前に使う
        protected List<Color32> temporaryColors; // SetTriangles寸前に使う
        protected List<int> temporaryIndices; // SetTriangles寸前に使う
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

        public Color32 Color { get; set; }
        protected Texture FontTexture { get; private set; }

        protected RendererBase(
            DebugRendererAsset asset,
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
        }

        public void SetCapacity(int triangleCapacity)
        {
            capacity = triangleCapacity * 3;
            if (capacity >= 0xffff)
            {
                UnityEngine.Debug.LogWarning("triangleCapacity must be < 0xffff/3. clamped.");
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
                // 使用量が半分以下の場合、テンポラリにコピーしてから渡す
                if (vertexCount < (capacity / 2)) // 閾値は研究が必要だが、とりあえず。
                {
                    UnityEngine.Profiling.Profiler.BeginSample("DebugPrimitiveRenderer.UpdateMesh.FillTemporary");

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
                    UnityEngine.Profiling.Profiler.BeginSample("DebugPrimitiveRenderer.UpdateMesh.CopyAll");
                    mesh.vertices = vertices;
                    mesh.uv = uv;
                    mesh.colors32 = colors;
                    UnityEngine.Profiling.Profiler.EndSample();
                }
                mesh.subMeshCount = subMeshCount;

                Material[] materials = new Material[subMeshCount];
                for (int i = 0; i < subMeshCount; i++)
                {
                    materials[i] = subMeshes[i].material;
                }
                meshRenderer.sharedMaterials = materials;

                for (int i = 0; i < subMeshCount; i++)
                {
                    UnityEngine.Profiling.Profiler.BeginSample("DebugPrimitiveRenderer.UpdateMesh.FillIndices");
                    var subMesh = subMeshes[i];
                    temporaryIndices.Clear();
                    var tmpI = new System.ArraySegment<int>(indices, subMesh.indexStart, subMesh.indexCount);
                    temporaryIndices.AddRange(tmpI);
                    mesh.SetTriangles(temporaryIndices, i, true);
                    materialPropertyBlock.SetTexture(
                        textureShaderPropertyId,
                        subMesh.texture);
                    meshRenderer.SetPropertyBlock(materialPropertyBlock, i);
                    UnityEngine.Profiling.Profiler.EndSample();
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
            indices[indexCount + 0] = vertexCount + i0;
            indices[indexCount + 1] = vertexCount + i1;
            indices[indexCount + 2] = vertexCount + i2;
            indexCount += 3;
        }

        // 時計回り4頂点で三角形を2個生成
        protected void AddQuadIndices(int i0, int i1, int i2, int i3)
        {
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

            var p0 = new Vector3(p.x + x, p.y + y, 0f); // 左上
            var p1 = new Vector3(p0.x + w, p0.y, 0f); // 右上
            var p2 = new Vector3(p1.x, p0.y + h, 0f); // 右下
            var p3 = new Vector3(p0.x, p2.y, 0f); // 左下

            // 頂点は左上から時計回り
            vertices[vertexCount + 0] = p0;
            vertices[vertexCount + 1] = p1;
            vertices[vertexCount + 2] = p2;
            vertices[vertexCount + 3] = p3;

            uv[vertexCount + 0] = ch.uvTopLeft;
            uv[vertexCount + 1] = ch.uvTopRight;
            uv[vertexCount + 2] = ch.uvBottomRight;
            uv[vertexCount + 3] = ch.uvBottomLeft;

            for (int j = 0; j < 4; j++)
            {
                colors[vertexCount + j] = Color;
            }

            AddQuadIndices(0, 1, 2, 3);
            vertexCount += 4;
            return true;
        }

        protected void TransformVertices(int verticesBegin, ref Matrix4x4 matrix)
        {
            int vCount = vertexCount - verticesBegin;
            for (int i = 0; i < vCount; i++)
            {
                var v = vertices[verticesBegin + i];
                v = matrix.MultiplyPoint(v);
                vertices[verticesBegin + i] = v;
            }
        }

        protected void TransformVertices(int verticesBegin, float scale, ref Vector2 translation)
        {
            int vCount = vertexCount - verticesBegin;
            for (int i = 0; i < vCount; i++)
            {
                var v = vertices[verticesBegin + i];
                v.x *= scale;
                v.y *= scale;
                v.x += translation.x;
                v.y += translation.y;
                vertices[verticesBegin + i] = v;
            }
        }
    }
}
