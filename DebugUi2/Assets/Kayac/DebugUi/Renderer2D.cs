using UnityEngine;

// 寸法指定は全て仮想ピクセル。
// 原点は画面左上ピクセルの左上角が(0,0)。画面右下ピクセルの右下角が(W, H)
// ピクセル中心は0.5ピクセルずれていることに注意。

// TODO: 負荷測ってない

namespace Kayac.DebugUi
{
    public class Renderer2D : RendererBase
    {
        public Renderer2D(
            RendererAsset asset,
            MeshRenderer meshRenderer,
            MeshFilter meshFilter,
            int capacity = DefaultTriangleCapacity) : base(asset, meshRenderer, meshFilter, capacity)
        {
        }

        public bool AddQuadraticBezier(
            float startX,
            float startY,
            float endX,
            float endY,
            float controlPointX,
            float controlPointY,
            float width,
            int division)
        {
            if (
            ((vertexCount + ((division + 1) * 2)) > capacity)
            || ((indexCount + (division * 6)) > capacity))
            {
                return false;
            }
            SetTexture(FontTexture);

            float halfWidth = width * 0.5f;
            /*
			両端をp0(x0,y0), p1(x1,y1)とし、
			制御点を cp(xc,yc)とする2次ベジエ曲線を生成する。
			変数をtとして、

			(p1 - 2*cp + p0)t^2 + 2(cp - p0)t + p0
			となる。これを、at^2 + bt + cと書こう。
			微分は2at + b。これが接線になる。
			*/
            float ax = endX - (2f * controlPointX) + startX;
            float ay = endY - (2f * controlPointY) + startY;
            float bx = 2f * (controlPointX - startX);
            float by = 2f * (controlPointY - startY);
            float cx = startX;
            float cy = startY;

            float tScale = 1f / division;
            for (int i = 0; i <= division; i++)
            {
                // 中心点を算出する
                float t = (float)i * tScale;
                float x = (((ax * t) + bx) * t) + cx;
                float y = (((ay * t) + by) * t) + cy;
                // 接線=微分を算出
                float tx = (2f * ax * t) + bx;
                float ty = (2f * ay * t) + by;
                // 90度回転して法線を生成
                float nx = -ty;
                float ny = tx;
                // 法線を正規化
                float l = Mathf.Sqrt((nx * nx) + (ny * ny));
                nx *= halfWidth / l;
                ny *= halfWidth / l;
                // 2点を生成
                int i0 = vertexCount + (i * 2) + 0;
                int i1 = vertexCount + (i * 2) + 1;
                vertices[i0].position = new Vector2(x + nx, y + ny);
                vertices[i1].position = new Vector2(x - nx, y - ny);
                vertices[i0].color = Color;
                vertices[i1].color = Color;
                vertices[i0].uv = whiteUv;
                vertices[i1].uv = whiteUv;
            }
            // インデクス生成
            for (int i = 0; i < division; i++)
            {
                AddQuadIndices(0, 1, 3, 2);
                vertexCount += 2;
            }
            // 最終頂点
            vertexCount += 2;
            return true;
        }

        // 太さ分は内側に取られる。
        public bool AddRectangleFrame(
            float leftX,
            float topY,
            float width,
            float height,
            float lineWidth = 1f)
        {
            if (
            ((vertexCount + 8) > capacity)
            || ((indexCount + 24) > capacity))
            {
                return false;
            }
            SetTexture(FontTexture);

            // 外側
            float x0o = leftX;
            float x1o = leftX + width;
            float y0o = topY;
            float y1o = topY + height;
            // 内側
            float x0i = x0o + lineWidth;
            float x1i = x1o - lineWidth;
            float y0i = y0o + lineWidth;
            float y1i = y1o - lineWidth;

            // 頂点は外側の左上から時計回り、内側の左上から時計回り
            vertices[vertexCount + 0].position = new Vector2(x0o, y0o);
            vertices[vertexCount + 1].position = new Vector2(x1o, y0o);
            vertices[vertexCount + 2].position = new Vector2(x1o, y1o);
            vertices[vertexCount + 3].position = new Vector2(x0o, y1o);
            vertices[vertexCount + 4].position = new Vector2(x0i, y0i);
            vertices[vertexCount + 5].position = new Vector2(x1i, y0i);
            vertices[vertexCount + 6].position = new Vector2(x1i, y1i);
            vertices[vertexCount + 7].position = new Vector2(x0i, y1i);

            for (int i = 0; i < 8; i++)
            {
                vertices[vertexCount + i].uv = whiteUv;
                vertices[vertexCount + i].color = Color;
            }

            AddQuadIndices(0, 1, 5, 4);
            AddQuadIndices(5, 1, 2, 6);
            AddQuadIndices(7, 6, 2, 3);
            AddQuadIndices(0, 4, 7, 3);
            vertexCount += 8;
            return true;
        }

        public bool AddHorizontalLine(
            float leftX,
            float y,
            float length,
            float lineWidth)
        {
            if (
            ((vertexCount + 4) > capacity)
            || ((indexCount + 6) > capacity))
            {
                return false;
            }
            SetTexture(FontTexture);

            float halfWidth = lineWidth * 0.5f;
            float x0 = leftX;
            float x1 = leftX + length;
            float y0 = y + halfWidth;
            float y1 = y + halfWidth;
            AddRectangleInternal(x0, x1, y0, y1);
            return true;
        }

        public bool AddVerticalLine(
            float x,
            float topY,
            float length,
            float lineWidth)
        {
            if (
            ((vertexCount + 4) > capacity)
            || ((indexCount + 6) > capacity))
            {
                return false;
            }
            SetTexture(FontTexture);

            float halfWidth = lineWidth * 0.5f;
            float x0 = x - halfWidth;
            float x1 = x + halfWidth;
            float y0 = topY;
            float y1 = topY + length;
            AddRectangleInternal(x0, x1, y0, y1);
            return true;
        }

        // AddLineの直後に呼ばない限り動作は保証しない。
        public bool ContinueLine(
            float x,
            float y,
            float lineWidth)
        {
            if (
            ((vertexCount + 2) > capacity)
            || ((indexCount + 6) > capacity))
            {
                return false;
            }

            var prev0 = vertices[vertexCount - 2].position;
            var prev1 = vertices[vertexCount - 1].position;
            Vector2 prev;
            prev.x = (prev0.x + prev1.x) * 0.5f;
            prev.y = (prev0.y + prev1.y) * 0.5f;
            float tx = x - prev.x;
            float ty = y - prev.y;
            float nx = -ty;
            float ny = tx;
            float l = Mathf.Sqrt((nx * nx) + (ny * ny));
            nx *= lineWidth * 0.5f / l;
            ny *= lineWidth * 0.5f / l;

            vertices[vertexCount + 0].position = new Vector2(x - nx, y - ny);
            vertices[vertexCount + 1].position = new Vector2(x + nx, y + ny);
            for (int i = 0; i < 2; i++)
            {
                vertices[vertexCount + i].uv = whiteUv;
                vertices[vertexCount + i].color = Color;
            }
            AddQuadIndices(-2, 0, 1, -1);
            vertexCount += 2;
            return true;
        }

        public bool AddLine(
            float x0,
            float y0,
            float x1,
            float y1,
            float lineWidth)
        {
            if (
            ((vertexCount + 4) > capacity)
            || ((indexCount + 6) > capacity))
            {
                return false;
            }
            SetTexture(FontTexture);

            float tx = x1 - x0;
            float ty = y1 - y0;
            float nx = -ty;
            float ny = tx;
            float l = Mathf.Sqrt((nx * nx) + (ny * ny));
            nx *= lineWidth * 0.5f / l;
            ny *= lineWidth * 0.5f / l;

            vertices[vertexCount + 0].position = new Vector2(x0 - nx, y0 - ny);
            vertices[vertexCount + 1].position = new Vector2(x0 + nx, y0 + ny);
            vertices[vertexCount + 2].position = new Vector2(x1 - nx, y1 - ny);
            vertices[vertexCount + 3].position = new Vector2(x1 + nx, y1 + ny);

            for (int i = 0; i < 4; i++)
            {
                vertices[vertexCount + i].uv = whiteUv;
                vertices[vertexCount + i].color = Color;
            }
            AddQuadIndices(0, 2, 3, 1);
            vertexCount += 4;
            return true;
        }

        public bool AddTriangle(
            float x0,
            float y0,
            float x1,
            float y1,
            float x2,
            float y2)
        {
            if (
            ((vertexCount + 3) > capacity)
            || ((indexCount + 3) > capacity))
            {
                return false;
            }
            SetTexture(FontTexture);

            vertices[vertexCount + 0].position = new Vector3(x0, y0);
            vertices[vertexCount + 1].position = new Vector3(x1, y1);
            vertices[vertexCount + 2].position = new Vector3(x2, y2);

            for (int i = 0; i < 3; i++)
            {
                vertices[vertexCount + i].uv = whiteUv;
                vertices[vertexCount + i].color = Color;
            }
            AddTriangleIndices(0, 1, 2);
            vertexCount += 3;
            return true;
        }

        public bool AddTriangleFrame(
            float x0,
            float y0,
            float x1,
            float y1,
            float x2,
            float y2,
            float lineWidth = 1f)
        {
            if (
            ((vertexCount + 3) > capacity)
            || ((indexCount + 3) > capacity))
            {
                return false;
            }
            SetTexture(FontTexture);

            // 重心算出
            float gx = (x0 + x1 + x2) / 3f;
            float gy = (y0 + y1 + y2) / 3f;
            // 各頂点から重心方向へのベクタを生成
            float g0x = gx - x0;
            float g0y = gy - y0;
            float g1x = gx - x1;
            float g1y = gy - y1;
            float g2x = gx - x2;
            float g2y = gy - y2;
            // 正規化
            float g0l = Mathf.Sqrt((g0x * g0x) + (g0y * g0y));
            float g1l = Mathf.Sqrt((g1x * g1x) + (g1y * g1y));
            float g2l = Mathf.Sqrt((g2x * g2x) + (g2y * g2y));
            float s0 = lineWidth / g0l;
            float s1 = lineWidth / g1l;
            float s2 = lineWidth / g2l;
            g0x *= s0;
            g0y *= s0;
            g1x *= s1;
            g1y *= s1;
            g2x *= s2;
            g2y *= s2;
            // 内側頂点
            float x0i = x0 + g0x;
            float y0i = y0 + g0y;
            float x1i = x1 + g1x;
            float y1i = y1 + g1y;
            float x2i = x2 + g2x;
            float y2i = y2 + g2y;

            // 充填
            vertices[vertexCount + 0].position = new Vector2(x0, y0);
            vertices[vertexCount + 1].position = new Vector2(x1, y1);
            vertices[vertexCount + 2].position = new Vector2(x2, y2);
            vertices[vertexCount + 3].position = new Vector2(x0i, y0i);
            vertices[vertexCount + 4].position = new Vector2(x1i, y1i);
            vertices[vertexCount + 5].position = new Vector2(x2i, y2i);
            for (int i = 0; i < 6; i++)
            {
                vertices[vertexCount + i].uv = whiteUv;
                vertices[vertexCount + i].color = Color;
            }
            AddQuadIndices(0, 1, 4, 3);
            AddQuadIndices(1, 2, 5, 4);
            AddQuadIndices(2, 0, 3, 5);
            vertexCount += 6;
            return true;
        }

        public bool AddRectangle(
            float leftX,
            float topY,
            float width,
            float height)
        {
            if (
            ((vertexCount + 4) > capacity)
            || ((indexCount + 6) > capacity))
            {
                return false;
            }
            SetTexture(FontTexture);

            AddRectangleInternal(leftX, leftX + width, topY, topY + height);
            return true;
        }

        public bool AddSprite(
            float leftX,
            float topY,
            float width,
            float height,
            Sprite sprite)
        {
            var vertices = sprite.vertices;
            var indices = sprite.triangles;
            int vCount = vertices.Length;
            int iCount = indices.Length;
            if (
            ((vCount + vertexCount) > capacity)
            || ((iCount + indexCount) > capacity))
            {
                return false;
            }
            SetTexture(sprite.texture);

            float scaleX = width / sprite.rect.width;
            float scaleY = height / sprite.rect.height;
            var pivot = sprite.pivot;
            pivot.x *= scaleX;
            pivot.y *= scaleY;
            scaleX *= sprite.pixelsPerUnit;
            scaleY *= sprite.pixelsPerUnit;
            var uv = sprite.uv;
            for (int i = 0; i < vCount; i++)
            {
                float x = vertices[i].x * scaleX;
                float y = vertices[i].y * scaleY;
                x += pivot.x;
                y = pivot.y - y;
                x += leftX;
                y += topY;
                this.vertices[vertexCount + i].position = new Vector2(x, y);
                this.vertices[vertexCount + i].uv = uv[i];
                this.vertices[vertexCount + i].color = Color;
            }
            AddIndices(indices);
            vertexCount += vCount;
            return true;
        }

        public bool AddTexturedRectangle(
            float leftX,
            float topY,
            float width,
            float height,
            Texture texture)
        {
            if (
            ((vertexCount + 4) > capacity)
            || ((indexCount + 6) > capacity))
            {
                return false;
            }
            SetTexture(texture);

            // 頂点は左上から時計回り
            var right = leftX + width;
            var bottom = topY + height;
            vertices[vertexCount + 0].position = new Vector2(leftX, topY);
            vertices[vertexCount + 1].position = new Vector2(right, topY);
            vertices[vertexCount + 2].position = new Vector2(right, bottom);
            vertices[vertexCount + 3].position = new Vector2(leftX, bottom);

            vertices[vertexCount + 0].uv = new Vector2(0f, 1f);
            vertices[vertexCount + 1].uv = new Vector2(1f, 1f);
            vertices[vertexCount + 2].uv = new Vector2(1f, 0f);
            vertices[vertexCount + 3].uv = new Vector2(0f, 0f);

            for (int i = 0; i < 4; i++)
            {
                vertices[vertexCount + i].color = Color;
            }
            AddQuadIndices(0, 1, 2, 3);
            vertexCount += 4;
            return true;
        }

        public bool AddCircle(
            float centerX,
            float centerY,
            float radius,
            int divisionPer45Degree = 2)
        {
            // TODO: Sin,Cosを節約するために45度だけ作って反転、回転すべき
            int div = divisionPer45Degree * 8;
            int triangleCount = div;
            int vCount = triangleCount + 1;
            int iCount = triangleCount * 3;

            if (
            ((vCount + vertexCount) > capacity)
            || ((iCount + indexCount) > capacity))
            {
                return false;
            }
            SetTexture(FontTexture);

            float thetaStep = Mathf.PI * 2f / div;
            for (int i = 0; i < div; i++)
            {
                float theta = thetaStep * (float)i;
                float dx = Mathf.Sin(theta) * radius;
                float dy = Mathf.Cos(theta) * radius;
                vertices[vertexCount + i].position = new Vector2(
                    centerX + dx,
                    centerY + dy);
            }
            // 中心頂点は最後
            vertices[vertexCount + div].position = new Vector2(centerX, centerY);

            for (int i = 0; i < vCount; i++)
            {
                vertices[vertexCount + i].uv = whiteUv;
                vertices[vertexCount + i].color = Color;
            }

            for (int i = 0; i < (div - 1); i++)
            {
                AddTriangleIndices(div, i, i + 1);
            }
            AddTriangleIndices(div, div - 1, 0);
            vertexCount += vCount;
            return true;
        }

        public Vector2 MeasureText(string text, float fontSize, float lineSpacing = 0.2f)
        {
            font.RequestCharactersInTexture(text);
            int length = text.Length;
            float scale = (fontSize / font.fontSize);

            var lineHeight = fontSize * (1f + lineSpacing);
            float maxLineWidth = 0f;
            float x = 0f;
            float y = 0f;
            CharacterInfo ch;
            for (int i = 0; i < length; i++)
            {
                char c = text[i];
                if (c == '\n')
                {
                    maxLineWidth = Mathf.Max(maxLineWidth, x);
                    x = 0f;
                    y += lineHeight;
                }
                else if (font.GetCharacterInfo(c, out ch) == true)
                {
                    x += (float)(ch.advance) * scale;
                }
            }
            // 最後の行に何か入っていればlineHeight分プラス
            if (x > 0f)
            {
                maxLineWidth = Mathf.Max(maxLineWidth, x);
                y += lineHeight;
            }
            return new Vector2(maxLineWidth, y);
        }

        // fontsize, width, heightをすべて指定する場合は折り返し
        public int AddText(
            string text,
            float leftX,
            float topY,
            float fontSize,
            float boxWidth,
            float boxHeight,
            AlignX alignX = AlignX.Left,
            AlignY alignY = AlignY.Top,
            float lineSpacingRatio = DefaultLineSpacingRatio)
        {
            return AddText(
                text,
                leftX,
                topY,
                fontSize,
                boxWidth,
                boxHeight,
                alignX,
                alignY,
                TextOverflow.Wrap,
                lineSpacingRatio);
        }

        // fontsizeのみ指定する場合、オーバーフロー処理は行わない
        public int AddText(
            string text,
            float leftX,
            float topY,
            float fontSize,
            AlignX alignX = AlignX.Left,
            AlignY alignY = AlignY.Top,
            float lineSpacingRatio = DefaultLineSpacingRatio)
        {
            return AddText(
                text,
                leftX,
                topY,
                fontSize,
                float.MaxValue,
                float.MaxValue,
                alignX,
                alignY,
                TextOverflow.Wrap,
                lineSpacingRatio);
        }

        // 箱サイズのみ指定する場合、スケールで合わせる
        public int AddText(
            string text,
            float leftX,
            float topY,
            float boxWidth,
            float boxHeight,
            AlignX alignX = AlignX.Left,
            AlignY alignY = AlignY.Top,
            float lineSpacingRatio = DefaultLineSpacingRatio)
        {
            return AddText(
                text,
                leftX,
                topY,
                0f,
                boxWidth,
                boxHeight,
                alignX,
                alignY,
                TextOverflow.Scale,
                lineSpacingRatio);
        }

        public void AddText(
            string text,
            float deltaX,
            float deltaY,
            Vertex[] verticesCached,
            ushort[] indicesCached,
            int indexOffset)
        {
            font.RequestCharactersInTexture(text);
            SetTexture(FontTexture);

            AddCached(deltaX, deltaY, verticesCached, indicesCached, indexOffset);
        }

        public int AddText(
            string text,
            float leftX,
            float topY,
            float fontSize,
            float boxWidth,
            float boxHeight,
            AlignX alignX,
            AlignY alignY,
            TextOverflow overflow,
            float lineSpacingRatio)
        {
            int verticesBegin = vertexCount;

            float width, height;
            float scale = 0f;
            bool wrap;
            float normalizedBoxWidth, normalizedBoxHeight;
            if (overflow == TextOverflow.Wrap)
            {
                UnityEngine.Debug.Assert(fontSize > 0f);
                scale = fontSize / (float)font.fontSize;
                normalizedBoxWidth = boxWidth / scale;
                normalizedBoxHeight = boxHeight / scale;
                wrap = true;
            }
            else //if (overflow == TextOverflow.Scale)
            {
                normalizedBoxWidth = normalizedBoxHeight = float.MaxValue;
                wrap = false;
            }

            var drawnLines = AddTextNormalized(out width, out height, text, normalizedBoxWidth, normalizedBoxHeight, lineSpacingRatio, wrap);
            if (drawnLines <= 0) // 何も書いてないなら抜ける
            {
                return drawnLines;
            }

            if (overflow == TextOverflow.Scale)
            {
                var scaleX = boxWidth / width;
                var scaleY = boxHeight / height;
                scale = Mathf.Min(scaleX, scaleY);
            }

            // TODO: 以下の計算はまだ仮
            var offset = Vector2.zero;
            switch (alignX)
            {
                case AlignX.Center: offset.x = -width * 0.5f; break;
                case AlignX.Right: offset.x = -width; break;
            }
            switch (alignY)
            {
                case AlignY.Center: offset.y = -height * 0.5f; break;
                case AlignY.Bottom: offset.y = -height; break;
            }
            offset *= scale;
            offset.x += leftX;
            offset.y += topY;
            TransformVertices(verticesBegin, scale, ref offset);
            return drawnLines;
        }

        public float CalcLineHeight(float fontSize, float lineSpacingRatio = DefaultLineSpacingRatio)
        {
            var scale = fontSize / (float)font.fontSize;
            var ret = scale * (float)font.lineHeight * (1f + lineSpacingRatio);
            return ret;
        }


        // 内部座標でもらっており、チェックもしない
        void AddRectangleInternal(
            float left,
            float right,
            float top,
            float bottom)
        {
            // 頂点は左上から時計回り
            vertices[vertexCount + 0].position = new Vector2(left, top);
            vertices[vertexCount + 1].position = new Vector2(right, top);
            vertices[vertexCount + 2].position = new Vector2(right, bottom);
            vertices[vertexCount + 3].position = new Vector2(left, bottom);
            for (int i = 0; i < 4; i++)
            {
                vertices[vertexCount + i].uv = whiteUv;
                vertices[vertexCount + i].color = Color;
            }
            AddQuadIndices(0, 1, 2, 3);
            vertexCount += 4;
        }
    }
}