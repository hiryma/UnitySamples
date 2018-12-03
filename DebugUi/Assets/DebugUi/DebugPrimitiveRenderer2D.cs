using UnityEngine;

// 寸法指定は全て仮想ピクセル。
// 原点は画面左上ピクセルの左上角が(0,0)。画面右下ピクセルの右下角が(W, H)
// ピクセル中心は0.5ピクセルずれていることに注意。

// TODO: 負荷測ってない

namespace Kayac
{
	public class DebugPrimitiveRenderer2D : DebugPrimitiveRenderer
	{
		private float _toCoordScale;
		private float _toCoordScaleX;
		private float _toCoordScaleY;
		private float _toCoordOffsetX;
		private float _toCoordOffsetY;

		public float referenceScreenWidth{ get; private set; }
		public float referenceScreenHeight{ get; private set; }

		public DebugPrimitiveRenderer2D(
			Shader textShader,
			Shader texturedShader,
			Font font,
			Camera camera,
			int capacity = DefaultTriangleCapacity) : base(textShader, texturedShader, font, camera, capacity)
		{
			// 初期値
			if (camera.aspect >= 16f / 9f)
			{
				SetReferenceScreenHeight(640); // 仮
			}
			else
			{
				SetReferenceScreenWidth(1136);
			}
		}

		private void ToCoord(ref float x, ref float y)
		{
			x *= _toCoordScaleX;
			x += _toCoordOffsetX;
			y *= _toCoordScaleY;
			y += _toCoordOffsetY;
		}

		// 仮想解像度のうち、縦横どちらかを指定する。
		// 例えば1136x640で作り、横を一定にするならば、1136を指定する。
		// TODO: 縦横比が異なる端末に対応するにはこれでは不十分。配置に基準点を指定できないとダメ。
		public void SetReferenceScreenHeight(int rHeight)
		{
			referenceScreenHeight = (float)rHeight;
			int screenWidth = Screen.width;
			int screenHeight = Screen.height;
			referenceScreenWidth = referenceScreenHeight * (float)screenWidth / (float)screenHeight;
			InitializeTransform();
		}

		public void SetReferenceScreenWidth(int rWidth)
		{
			referenceScreenWidth = (float)rWidth;
			int screenWidth = Screen.width;
			int screenHeight = Screen.height;
			referenceScreenHeight = referenceScreenWidth * (float)screenHeight / (float)screenWidth;
			InitializeTransform();
		}

		private void InitializeTransform()
		{
			// カメラがなければ設定できない
			if (_camera == null)
			{
				return;
			}
			float cameraScreenHalfHeight = _camera.orthographicSize;
			float cameraScreenHalfWidth = cameraScreenHalfHeight * referenceScreenWidth / referenceScreenHeight;

			_toCoordScale = 2f * cameraScreenHalfHeight / referenceScreenHeight;
			_toCoordOffsetX = -cameraScreenHalfWidth;
			_toCoordOffsetY = cameraScreenHalfHeight;
			_toCoordScaleX = _toCoordScale;
			_toCoordScaleY = -_toCoordScale;
		}

		protected override void OnLateUpdate()
		{
			// カメラが差し直されるかもしれんので毎フレームやっておく
			InitializeTransform();
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

			ToCoord(ref startX, ref startY);
			ToCoord(ref endX, ref endY);
			ToCoord(ref controlPointX, ref controlPointY);
			width *= _toCoordScale;
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

			ToCoord(ref leftX, ref topY);
			width *= _toCoordScale;
			height *= _toCoordScale;
			lineWidth *= _toCoordScale;

			// 外側
			float x0o = leftX;
			float x1o = leftX + width;
			float y0o = topY;
			float y1o = topY - height;
			// 内側
			float x0i = x0o + lineWidth;
			float x1i = x1o - lineWidth;
			float y0i = y0o - lineWidth;
			float y1i = y1o + lineWidth;

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

			ToCoord(ref leftX, ref y);
			length *= _toCoordScale;
			lineWidth *= _toCoordScale;

			float halfWidth = lineWidth * 0.5f;
			float x0 = leftX;
			float x1 = leftX + length;
			float y0 = y + halfWidth;
			float y1 = y - halfWidth;
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

			ToCoord(ref x, ref topY);
			length *= _toCoordScale;
			lineWidth *= _toCoordScale;

			float halfWidth = lineWidth * 0.5f;
			float x0 = x - halfWidth;
			float x1 = x + halfWidth;
			float y0 = topY;
			float y1 = topY - length;
			AddRectangleInternal(x0, x1, y0, y1);
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

			ToCoord(ref x0, ref y0);
			ToCoord(ref x1, ref y1);
			lineWidth *= _toCoordScale;

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

			ToCoord(ref x0, ref y0);
			ToCoord(ref x1, ref y1);
			ToCoord(ref x2, ref y2);

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

			ToCoord(ref x0, ref y0);
			ToCoord(ref x1, ref y1);
			ToCoord(ref x2, ref y2);
			lineWidth *= _toCoordScale;

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

			ToCoord(ref leftX, ref topY);
			width *= _toCoordScale;
			height *= _toCoordScale;

			AddRectangleInternal(leftX, leftX + width, topY, topY - height);
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
				ToCoord(ref x, ref y);
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

			ToCoord(ref leftX, ref topY);
			width *= _toCoordScale;
			height *= _toCoordScale;

			// 頂点は左上から時計回り
			var right = leftX + width;
			var bottom = topY - height;
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

			ToCoord(ref centerX, ref centerY);
			radius *= _toCoordScale;

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

		public Vector2 MeasureText(string text, float fontSize, float lineHeight = DefaultLineHeight)
		{
			_font.RequestCharactersInTexture(text);
			int length = text.Length;
			float scale = (fontSize / _font.fontSize) * _toCoordScale;

			if (float.IsNaN(lineHeight))
			{
				lineHeight = (float)(_font.lineHeight) * scale;
			}

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
			maxLineWidth /= _toCoordScale;
			y /= _toCoordScale;
			return new Vector2(maxLineWidth, y);
		}

		// 最後がtrueだと縦書き内横書きのように90度回転させる。改行は無視される。
		public void AddText(
			string text,
			float fontSize,
			float leftX,
			float topY,
			float width,
			float height,
			Alignment alignment = Alignment.Left,
			bool rotateToVertical = false)
		{
			ToCoord(ref leftX, ref topY);
			width *= _toCoordScale;
			height *= _toCoordScale;
			int letterCount = text.Length;

			_font.RequestCharactersInTexture(text);
			SetTexture(fontTexture);

			// 各グリフ寸法に乗ずるスケールを計算する。
			float scale = (fontSize / _font.fontSize) * _toCoordScale;
			if (alignment == Alignment.Left)
			{
				if (rotateToVertical)
				{
					AddTextVerticalSingleLine(text, leftX, topY, width, height, scale, true);
				}
				else
				{
					AddTextHorizontalSingleLine(text, leftX, topY, width, height, scale, true);
				}
			}
/*
			if (alignment == Alignment.Center)
			{
				if (rotateToVertical)
				{
					AddTextCenterAlignVertical(text, leftX, topY, width, height, scale, autoLineBreak);
				}
				else
				{
					AddTextCenterAlignHorizontal(text, leftX, topY, width, height, scale, autoLineBreak);
				}
			}
*/
			else if (alignment == Alignment.Right)
			{
				if (rotateToVertical)
				{
					AddTextVerticalSingleLine(text, leftX, topY, width, height, scale, false);
				}
				else
				{
					AddTextHorizontalSingleLine(text, leftX, topY, width, height, scale, false);
				}
			}
			else
			{
				Debug.Assert(false);
			}
		}

		// 最後がtrueだと縦書き内横書きのように90度回転させる。改行も処理される。
		public void AddTextMultiLine(
			string text,
			float fontSize,
			float leftX,
			float topY,
			float width,
			float height,
			bool autoLineBreak = false,
			bool rotateToVertical = false,
			float lineHeight = DefaultLineHeight)
		{
			ToCoord(ref leftX, ref topY);
			width *= _toCoordScale;
			height *= _toCoordScale;
			lineHeight *= _toCoordScale;
			int letterCount = text.Length;

			_font.RequestCharactersInTexture(text);
			SetTexture(fontTexture);

			// 各グリフ寸法に乗ずるスケールを計算する。
			float scale = (fontSize / _font.fontSize) * _toCoordScale;
			if (rotateToVertical)
			{
				AddTextVerticalMultiLine(text, leftX, topY, width, height, scale, autoLineBreak, lineHeight);
			}
			else
			{
				AddTextHorizontalMultiLine(text, leftX, topY, width, height, scale, autoLineBreak, lineHeight);
			}
		}

		// 左上(0,0) 右下(w, h)の矩形に、左上もしくは右上から順に字を詰める。
		// 座標変換(Y逆転やオフセット)は上流で行うこと
		private struct LayoutState
		{
			private float _lineHeight;
			private float _scale;
			// xは文字の左端または右端
			public float x{ get; private set; }
			// yは行の上端
			public float y{ get; private set; }
			private float _width;
			private float _height;
			private bool _leftAlign;

			// lineHeightはNaNならデフォルト値
			public void Initialize(
				Font font,
				float width,
				float height,
				float scale,
				bool leftAlign,
				float lineHeight = float.NaN)
			{
				_width = width;
				_height = height;
				_scale = scale;
				if (float.IsNaN(lineHeight))
				{
					_lineHeight = (float)(font.lineHeight) * scale;
				}
				else
				{
					_lineHeight = lineHeight;
				}
				_leftAlign = leftAlign;
				x = (_leftAlign) ? 0f : _width;
				y = 0f;
			}

			// 次の行が範囲内ならtrueを返す
			public bool BreakLine()
			{
				x = (_leftAlign) ? 0f : _width;
				y += _lineHeight;
				return (y <= _height);
			}

			// 入れようとした文字が入ればtrueを返す
			public bool TryPut(ref CharacterInfo ch)
			{
				float tmpX = x;
				if (_leftAlign)
				{
					float x1 = tmpX + ((float)(ch.maxX) * _scale);
					return (x1 <= _width);
				}
				else
				{
					tmpX -= ((float)(ch.advance) * _scale);
					float x0 = tmpX + ((float)(ch.minX) * _scale);
					return (x0 >= 0f);
				}
			}

			public void Put(ref CharacterInfo ch)
			{
				float advance = (float)(ch.advance) * _scale;
				x += (_leftAlign) ? advance : -advance;
			}
		}

		// 座標は変換後。Yは上がプラス!
		private void AddTextHorizontalSingleLine(
			string text,
			float left,
			float top,
			float width,
			float height,
			float scale,
			bool leftAlign)
		{
			int letterCount = text.Length;
			LayoutState layout = new LayoutState();
			layout.Initialize(_font, width, height, scale, leftAlign);
			float ascent = (float)(_font.ascent) * scale;

			int i = (leftAlign) ? 0 : (letterCount - 1);
			int endI = (leftAlign) ? letterCount : -1;
			int di = (leftAlign) ? 1 : -1;
			while (i != endI)
			{
				CharacterInfo ch;
				char c = text[i];
				if (c == '\t')
				{
					c = ' ';
				}

				if (c == '\n')
				{
					break;
				}
				else if (_font.GetCharacterInfo(c, out ch) == true)
				{
					if (layout.TryPut(ref ch) == false)
					{
						break;
					}

					if (leftAlign == false)
					{
						layout.Put(ref ch);
					}

					if (AddChar(
						left + layout.x,
						top - layout.y - ascent,
						scale,
						ref ch) == false)
					{
						break;
					}

					if (leftAlign)
					{
						layout.Put(ref ch);
					}
				}
				i += di;
			}
		}

		// 座標は変換後。Yは上がプラス!
		private void AddTextHorizontalMultiLine(
			string text,
			float left,
			float top,
			float width,
			float height,
			float scale,
			bool autoLineBreak,
			float lineHeight)
		{
			int letterCount = text.Length;
			if (letterCount == 0)
			{
				return;
			}
			LayoutState layout = new LayoutState();
			layout.Initialize(_font, width, height, scale, true, lineHeight);
			float ascent = (float)(_font.ascent) * scale;

			for (int i = 0; i < letterCount; i++)
			{
				CharacterInfo ch;
				// 改行検出
				char c = text[i];
				if (c == '\t')
				{
					c = ' ';
				}

				if (c == '\n')
				{
					// 下にはみ出したら抜ける
					if (layout.BreakLine() == false)
					{
						break;
					}
				}
				else if (_font.GetCharacterInfo(c, out ch) == true)
				{
					// 行はみ出した
					if (layout.TryPut(ref ch) == false)
					{
						// 自動改行でないなら抜ける。改行してみてはみ出したら抜ける
						if (!autoLineBreak || (layout.BreakLine() == false))
						{
							break;
						}
					}

					if (AddChar(
						left + layout.x,
						top - layout.y - ascent,
						scale,
						ref ch) == false)
					{
						break;
					}
					layout.Put(ref ch);
				}
			}
		}

		// 座標は変換後。Yは上がプラス!
		private void AddTextVerticalSingleLine(
			string text,
			float left,
			float top,
			float width,
			float height,
			float scale,
			bool leftAlign)
		{
			int letterCount = text.Length;
			// x,yを置換してレイアウトさせる
			LayoutState layout = new LayoutState();
			layout.Initialize(_font, height, width, scale, leftAlign);
			float ascent = (float)(_font.ascent) * scale;

			int i = (leftAlign) ? 0 : (letterCount - 1);
			int endI = (leftAlign) ? letterCount : -1;
			int di = (leftAlign) ? 1 : -1;
			float right = left + width;
			while (i != endI)
			{
				CharacterInfo ch;
				char c = text[i];
				if (c == '\t')
				{
					c = ' ';
				}

				if (c == '\n')
				{
					break;
				}
				else if (_font.GetCharacterInfo(c, out ch) == true)
				{
					if (layout.TryPut(ref ch) == false)
					{
						break;
					}

					if (leftAlign == false)
					{
						layout.Put(ref ch);
					}

					// x,yを入れ換えている
					if (AddCharRotated(
						right - layout.y - ascent,
						top - layout.x,
						scale,
						ref ch) == false)
					{
						break;
					}

					if (leftAlign)
					{
						layout.Put(ref ch);
					}
				}
				i += di;
			}
		}

		// 座標は変換後。Yは上がプラス!
		private void AddTextVerticalMultiLine(
			string text,
			float left,
			float top,
			float width,
			float height,
			float scale,
			bool autoLineBreak,
			float lineHeight)
		{
			int letterCount = text.Length;
			if (letterCount == 0)
			{
				return;
			}
			// x,yを置換してレイアウトさせる
			LayoutState layout = new LayoutState();
			layout.Initialize(_font, height, width, scale, true, lineHeight);
			float ascent = (float)(_font.ascent) * scale;

			float right = left + width;
			for (int i = 0; i < letterCount; i++)
			{
				char c = text[i];
				if (c == '\t')
				{
					c = ' ';
				}

				CharacterInfo ch;
				// 改行検出
				if (c == '\n')
				{
					// 下にはみ出したので抜ける
					if (layout.BreakLine() == false)
					{
						break;
					}
				}
				else if (_font.GetCharacterInfo(c, out ch) == true)
				{
					// 行はみ出した
					if (layout.TryPut(ref ch) == false)
					{
						// 自動改行でないなら抜ける。改行してみてはみ出したら抜ける
						if (!autoLineBreak || (layout.BreakLine() == false))
						{
							break;
						}
					}

					// x,yを入れ換えている
					if (AddCharRotated(
						right - layout.y - ascent,
						top - layout.x,
						scale,
						ref ch) == false)
					{
						break;
					}
					layout.Put(ref ch);
				}
			}
		}

		private bool AddChar(float x, float y, float scale, ref CharacterInfo ch)
		{
			if (
			((_vertexCount + 4) > _capacity)
			|| ((_indexCount + 6) > _capacity))
			{
				return false;
			}
			float x0 = x + ((float)ch.minX * scale);
			float x1 = x + ((float)ch.maxX * scale);
			float y0 = y + ((float)ch.maxY * scale);
			float y1 = y + ((float)ch.minY * scale);

			// 頂点は左上から時計回り
			_vertices[_vertexCount + 0] = new Vector3(x0, y0, 0f);
			_vertices[_vertexCount + 1] = new Vector3(x1, y0, 0f);
			_vertices[_vertexCount + 2] = new Vector3(x1, y1, 0f);
			_vertices[_vertexCount + 3] = new Vector3(x0, y1, 0f);

			_uv[_vertexCount + 0] = ch.uvTopLeft;
			_uv[_vertexCount + 1] = ch.uvTopRight;
			_uv[_vertexCount + 2] = ch.uvBottomRight;
			_uv[_vertexCount + 3] = ch.uvBottomLeft;

			_colors[_vertexCount + 0] = color;
			_colors[_vertexCount + 1] = color;
			_colors[_vertexCount + 2] = color;
			_colors[_vertexCount + 3] = color;

			AddQuadIndices(0, 1, 2, 3);
			_vertexCount += 4;
			return true;
		}

		private bool AddCharRotated(float x, float y, float scale, ref CharacterInfo ch)
		{
			if (
			((_vertexCount + 4) > _capacity)
			|| ((_indexCount + 6) > _capacity))
			{
				return false;
			}
			float x0 = x + ((float)ch.minY * scale);
			float x1 = x + ((float)ch.maxY * scale);
			float y0 = y - ((float)ch.minX * scale);
			float y1 = y - ((float)ch.maxX * scale);

			// 頂点は左上から時計回り
			_vertices[_vertexCount + 0] = new Vector3(x0, y0, 0f);
			_vertices[_vertexCount + 1] = new Vector3(x1, y0, 0f);
			_vertices[_vertexCount + 2] = new Vector3(x1, y1, 0f);
			_vertices[_vertexCount + 3] = new Vector3(x0, y1, 0f);

			_uv[_vertexCount + 0] = ch.uvBottomLeft;
			_uv[_vertexCount + 1] = ch.uvTopLeft;
			_uv[_vertexCount + 2] = ch.uvTopRight;
			_uv[_vertexCount + 3] = ch.uvBottomRight;

			_colors[_vertexCount + 0] = color;
			_colors[_vertexCount + 1] = color;
			_colors[_vertexCount + 2] = color;
			_colors[_vertexCount + 3] = color;

			AddQuadIndices(0, 1, 2, 3);
			_vertexCount += 4;
			return true;
		}

		// 内部座標でもらっており、チェックもしない
		private void AddRectangleInternal(
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