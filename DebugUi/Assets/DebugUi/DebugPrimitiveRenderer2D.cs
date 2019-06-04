using UnityEngine;

// 寸法指定は全て仮想ピクセル。
// 原点は画面左上ピクセルの左上角が(0,0)。画面右下ピクセルの右下角が(W, H)
// ピクセル中心は0.5ピクセルずれていることに注意。

// TODO: 負荷測ってない

namespace Kayac
{
	public class DebugPrimitiveRenderer2D : DebugPrimitiveRenderer
	{
		public DebugPrimitiveRenderer2D(
			Shader textShader,
			Shader texturedShader,
			Font font,
			MeshRenderer meshRenderer,
			MeshFilter meshFilter,
			int capacity = DefaultTriangleCapacity) : base(textShader, texturedShader, font, meshRenderer, meshFilter, capacity)
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
			((_vertexCount + ((division + 1) * 2)) > _capacity)
			|| ((_indexCount + (division * 6)) > _capacity))
			{
				return false;
			}
			SetTexture(fontTexture);

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
				int i0 = _vertexCount + (i * 2) + 0;
				int i1 = _vertexCount + (i * 2) + 1;
				_vertices[i0] = new Vector3(x + nx, y + ny, 0f);
				_vertices[i1] = new Vector3(x - nx, y - ny, 0f);
				_colors[i0] = color;
				_colors[i1] = color;
				_uv[i0] = _whiteUv;
				_uv[i1] = _whiteUv;
			}
			// インデクス生成
			for (int i = 0; i < division; i++)
			{
				AddQuadIndices(0, 1, 3, 2);
				_vertexCount += 2;
			}
			// 最終頂点
			_vertexCount += 2;
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
			((_vertexCount + 8) > _capacity)
			|| ((_indexCount + 24) > _capacity))
			{
				return false;
			}
			SetTexture(fontTexture);

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
			_vertices[_vertexCount + 0] = new Vector3(x0o, y0o, 0f);
			_vertices[_vertexCount + 1] = new Vector3(x1o, y0o, 0f);
			_vertices[_vertexCount + 2] = new Vector3(x1o, y1o, 0f);
			_vertices[_vertexCount + 3] = new Vector3(x0o, y1o, 0f);
			_vertices[_vertexCount + 4] = new Vector3(x0i, y0i, 0f);
			_vertices[_vertexCount + 5] = new Vector3(x1i, y0i, 0f);
			_vertices[_vertexCount + 6] = new Vector3(x1i, y1i, 0f);
			_vertices[_vertexCount + 7] = new Vector3(x0i, y1i, 0f);

			for (int i = 0; i < 8; i++)
			{
				_uv[_vertexCount + i] = _whiteUv;
				_colors[_vertexCount + i] = color;
			}

			AddQuadIndices(0, 1, 5, 4);
			AddQuadIndices(5, 1, 2, 6);
			AddQuadIndices(7, 6, 2, 3);
			AddQuadIndices(0, 4, 7, 3);
			_vertexCount += 8;
			return true;
		}

		public bool AddHorizontalLine(
			float leftX,
			float y,
			float length,
			float lineWidth)
		{
			if (
			((_vertexCount + 4) > _capacity)
			|| ((_indexCount + 6) > _capacity))
			{
				return false;
			}
			SetTexture(fontTexture);

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
			((_vertexCount + 4) > _capacity)
			|| ((_indexCount + 6) > _capacity))
			{
				return false;
			}
			SetTexture(fontTexture);

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
			((_vertexCount + 2) > _capacity)
			|| ((_indexCount + 6) > _capacity))
			{
				return false;
			}

			var prev0 = _vertices[_vertexCount - 2];
			var prev1 = _vertices[_vertexCount - 1];
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

			_vertices[_vertexCount + 0] = new Vector3(x - nx, y - ny, 0f);
			_vertices[_vertexCount + 1] = new Vector3(x + nx, y + ny, 0f);
			for (int i = 0; i < 2; i++)
			{
				_uv[_vertexCount + i] = _whiteUv;
				_colors[_vertexCount + i] = color;
			}
			this.AddQuadIndices(-2, 0, 1, -1);
			_vertexCount += 2;
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
			((_vertexCount + 4) > _capacity)
			|| ((_indexCount + 6) > _capacity))
			{
				return false;
			}
			SetTexture(fontTexture);

			float tx = x1 - x0;
			float ty = y1 - y0;
			float nx = -ty;
			float ny = tx;
			float l = Mathf.Sqrt((nx * nx) + (ny * ny));
			nx *= lineWidth * 0.5f / l;
			ny *= lineWidth * 0.5f / l;

			_vertices[_vertexCount + 0] = new Vector3(x0 - nx, y0 - ny, 0f);
			_vertices[_vertexCount + 1] = new Vector3(x0 + nx, y0 + ny, 0f);
			_vertices[_vertexCount + 2] = new Vector3(x1 - nx, y1 - ny, 0f);
			_vertices[_vertexCount + 3] = new Vector3(x1 + nx, y1 + ny, 0f);

			for (int i = 0; i < 4; i++)
			{
				_uv[_vertexCount + i] = _whiteUv;
				_colors[_vertexCount + i] = color;
			}
			this.AddQuadIndices(0, 2, 3, 1);
			_vertexCount += 4;
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
			((_vertexCount + 3) > _capacity)
			|| ((_indexCount + 3) > _capacity))
			{
				return false;
			}
			SetTexture(fontTexture);

			_vertices[_vertexCount + 0] = new Vector3(x0, y0, 0f);
			_vertices[_vertexCount + 1] = new Vector3(x1, y1, 0f);
			_vertices[_vertexCount + 2] = new Vector3(x2, y2, 0f);

			for (int i = 0; i < 3; i++)
			{
				_uv[_vertexCount + i] = _whiteUv;
				_colors[_vertexCount + i] = color;
			}
			AddTriangleIndices(0, 1, 2);
			_vertexCount += 3;
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
			((_vertexCount + 3) > _capacity)
			|| ((_indexCount + 3) > _capacity))
			{
				return false;
			}
			SetTexture(fontTexture);

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
			_vertices[_vertexCount + 0] = new Vector3(x0, y0, 0f);
			_vertices[_vertexCount + 1] = new Vector3(x1, y1, 0f);
			_vertices[_vertexCount + 2] = new Vector3(x2, y2, 0f);
			_vertices[_vertexCount + 3] = new Vector3(x0i, y0i, 0f);
			_vertices[_vertexCount + 4] = new Vector3(x1i, y1i, 0f);
			_vertices[_vertexCount + 5] = new Vector3(x2i, y2i, 0f);
			for (int i = 0; i < 6; i++)
			{
				_uv[_vertexCount + i] = _whiteUv;
				_colors[_vertexCount + i] = color;
			}
			AddQuadIndices(0, 1, 4, 3);
			AddQuadIndices(1, 2, 5, 4);
			AddQuadIndices(2, 0, 3, 5);
			_vertexCount += 6;
			return true;
		}

		public bool AddRectangle(
			float leftX,
			float topY,
			float width,
			float height)
		{
			if (
			((_vertexCount + 4) > _capacity)
			|| ((_indexCount + 6) > _capacity))
			{
				return false;
			}
			SetTexture(fontTexture);

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
			int vertexCount = vertices.Length;
			int indexCount = indices.Length;
			if (
			((_vertexCount + vertexCount) > _capacity)
			|| ((_indexCount + indexCount) > _capacity))
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
			for (int i = 0; i < vertexCount; i++)
			{
				float x = vertices[i].x * scaleX;
				float y = vertices[i].y * scaleY;
				x += pivot.x;
				y = pivot.y - y;
				x += leftX;
				y += topY;
				_vertices[_vertexCount + i] = new Vector3(x, y, 0f);
				_uv[_vertexCount + i] = uv[i];
				_colors[_vertexCount + i] = color;
			}

			AddIndices(indices);
			_vertexCount += vertexCount;
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
			((_vertexCount + 4) > _capacity)
			|| ((_indexCount + 6) > _capacity))
			{
				return false;
			}
			SetTexture(texture);

			// 頂点は左上から時計回り
			var right = leftX + width;
			var bottom = topY + height;
			_vertices[_vertexCount + 0] = new Vector3(leftX, topY, 0f);
			_vertices[_vertexCount + 1] = new Vector3(right, topY, 0f);
			_vertices[_vertexCount + 2] = new Vector3(right, bottom, 0f);
			_vertices[_vertexCount + 3] = new Vector3(leftX, bottom, 0f);

			_uv[_vertexCount + 0] = new Vector2(0f, 1f);
			_uv[_vertexCount + 1] = new Vector2(1f, 1f);
			_uv[_vertexCount + 2] = new Vector2(1f, 0f);
			_uv[_vertexCount + 3] = new Vector2(0f, 0f);

			for (int i = 0; i < 4; i++)
			{
				_colors[_vertexCount + i] = color;
			}
			AddQuadIndices(0, 1, 2, 3);
			_vertexCount += 4;
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
			int vertexCount = triangleCount + 1;
			int indexCount = triangleCount * 3;

			if (
			((_vertexCount + vertexCount) > _capacity)
			|| ((_indexCount + indexCount) > _capacity))
			{
				return false;
			}
			SetTexture(fontTexture);

			float thetaStep = Mathf.PI * 2f / div;
			for (int i = 0; i < div; i++)
			{
				float theta = thetaStep * (float)i;
				float dx = Mathf.Sin(theta) * radius;
				float dy = Mathf.Cos(theta) * radius;
				_vertices[_vertexCount + i] = new Vector3(
					centerX + dx,
					centerY + dy,
					0f);
			}
			// 中心頂点は最後
			_vertices[_vertexCount + div] = new Vector3(centerX, centerY, 0f);

			for (int i = 0; i < vertexCount; i++)
			{
				_uv[_vertexCount + i] = _whiteUv;
				_colors[_vertexCount + i] = color;
			}

			for (int i = 0; i < (div - 1); i++)
			{
				AddTriangleIndices(div, i, i + 1);
			}
			AddTriangleIndices(div, div - 1, 0);
			_vertexCount += vertexCount;
			return true;
		}

		public Vector2 MeasureText(string text, float fontSize, float lineSpacing = 0.2f)
		{
			_font.RequestCharactersInTexture(text);
			int length = text.Length;
			float scale = (fontSize / _font.fontSize);

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
				else if (_font.GetCharacterInfo(c, out ch) == true)
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
			int verticesBegin = _vertexCount;

			float width, height;
			float scale = 0f;
			bool wrap;
			float normalizedBoxWidth, normalizedBoxHeight;
			if (overflow == TextOverflow.Wrap)
			{
				Debug.Assert(fontSize > 0f);
				scale = fontSize / (float)_font.fontSize;
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
			var scale = fontSize / (float)_font.fontSize;
			var ret = scale * (float)_font.lineHeight * (1f + lineSpacingRatio);
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
			_vertices[_vertexCount + 0] = new Vector3(left, top, 0f);
			_vertices[_vertexCount + 1] = new Vector3(right, top, 0f);
			_vertices[_vertexCount + 2] = new Vector3(right, bottom, 0f);
			_vertices[_vertexCount + 3] = new Vector3(left, bottom, 0f);
			for (int i = 0; i < 4; i++)
			{
				_uv[_vertexCount + i] = _whiteUv;
				_colors[_vertexCount + i] = color;
			}
			AddQuadIndices(0, 1, 2, 3);
			_vertexCount += 4;
		}
	}
}