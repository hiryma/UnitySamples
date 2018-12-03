using UnityEngine;

// 寸法指定はワールド座標
// TODO: 負荷測ってない
namespace Kayac
{
	public class DebugPrimitiveRenderer3D : DebugPrimitiveRenderer
	{
		public DebugPrimitiveRenderer3D(
			Shader textShader,
			Shader texturedShader,
			Font font,
			Camera camera,
			int capacity = DefaultTriangleCapacity) : base(textShader, texturedShader, font, camera, capacity)
		{
		}

		public bool AddQuadraticBezier(
			Vector3 start,
			Vector3 end,
			Vector3 controlPoint,
			float width = 1,
			bool arrowHead = false,
			int division = 16)
		{
			int vertexCount = ((division + 1) * 2);
			int indexCount = division * 6;
			if (arrowHead)
			{
				vertexCount += 3;
				indexCount += 3;
			}
			if (
			((_vertexCount + vertexCount) > _capacity)
			|| ((_indexCount + indexCount) > _capacity))
			{
				return false;
			}
			SetTexture(fontTexture);

			/*
			両端をp0(x0,y0), p1(x1,y1)とし、
			制御点を cp(xc,yc)とする2次ベジエ曲線を生成する。
			変数をtとして、

			(p1 - 2*cp + p0)t^2 + 2(cp - p0)t + p0
			となる。これを、at^2 + bt + cと書こう。
			微分は2at + b。これが接線になる。
			*/
			Vector3 a, b, c;
			MakeQuadBezier(out a, out b, out c, ref start, ref end, ref controlPoint);

			Vector3 forwardVector = _camera.gameObject.transform.forward;

			float tScale = 1f / division;
			float halfWidth = width * 0.5f;
			for (int i = 0; i <= division; i++)
			{
				// 中心点を算出する
				float t = (float)i * tScale;
				Vector3 p = EvaluateQuad(ref a, ref b, ref c, t);
				// 接線=微分を算出
				Vector3 tangent = a;
				tangent *= t * 2f;
				tangent += b;
				// 90度回転して法線を生成
				// TODO: tangentとforwardVectorが平行の場合の処理がない
				Vector3 normal = Vector3.Cross(forwardVector, tangent);
				float l = normal.magnitude;
				normal *= halfWidth / l;
				// 2点を生成
				int i0 = _vertexCount + (i * 2) + 0;
				int i1 = _vertexCount + (i * 2) + 1;
				_vertices[i0] = p + normal;
				_vertices[i1] = p - normal;
				// 鏃は太さ0に収束
				if (arrowHead && (i == division))
				{
					_vertices[i0] = _vertices[i1] = end;
				}
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
			// 矢印
			if (arrowHead)
			{
				_vertices[_vertexCount + 0] = end;
				// 終端における接線を計算
				Vector3 tangent = a;
				tangent *= 2f;
				tangent += b;
				// 90度回転して法線を生成
				Vector3 normal = Vector3.Cross(forwardVector, tangent);
				float l = normal.magnitude;
				// 鏃の幅は太さの倍としてみる
				normal *= 4f * width / l;
				// 2点を生成
				int i0 = _vertexCount + 1;
				int i1 = _vertexCount + 2;
				// 接線正規化
				tangent *= (width * 8f) / tangent.magnitude;
				Vector3 arrowHeadRoot = end - tangent;
				_vertices[i0] = arrowHeadRoot + normal;
				_vertices[i1] = arrowHeadRoot - normal;
				for (int i = 0; i < 3; i++)
				{
					_colors[_vertexCount + i] = color;
					_uv[_vertexCount + i] = _whiteUv;
				}
				AddTriangleIndices(0, 1, 2);
				_vertexCount += 3;
			}
			return true;
		}

		// center中心で、ワールド座標で幅width、高さheightの板を出す
		public bool AddBillboard(
			Vector3 center,
			float width,
			float height)
		{
			var transform = _camera.gameObject.transform;
			var forwardVector = transform.forward;
			var upVector = transform.up;
			// TODO: 右ベクタ、上ベクタは前計算可能
			// 右ベクタ、上ベクタを生成 axisX = cross(axisY, axisZ)
			Vector3 right = Vector3.Cross(upVector, forwardVector);
			right.Normalize();
			// ビルボード空間の上ベクタを計算
			Vector3 up = Vector3.Cross(forwardVector, right);
			// スケール
			right *= width;
			up *= height;
			// 左下点を生成
			Vector3 p = center - (right * 0.5f) - (up * 0.5f);

			return AddParallelogram(p, right, up);
		}

		// rightVectorとdownVectorが直交していないと平行四辺形に歪むので注意
		public bool AddText(
			Vector3 topLeft,
			Vector3 rightVector,
			Vector3 downVector,
			float fontSize,
			string text)
		{
			int letterCount = text.Length;
			int vertexCount = letterCount * 4;
			int indexCount = letterCount * 6;
			if (
			((_vertexCount + vertexCount) > _capacity)
			|| ((_indexCount + indexCount) > _capacity))
			{
				return false;
			}
			_font.RequestCharactersInTexture(text);
			SetTexture(fontTexture);

			rightVector.Normalize();
			downVector.Normalize();

			// フォント内整数座標→ワールド座標のスケール
			float scale = fontSize / (float)(_font.fontSize);
			Vector3 p = topLeft + (downVector * (float)(_font.ascent) * scale);
			Vector3 upVector = -downVector;

			for (int i = 0; i < letterCount; i++)
			{
				CharacterInfo ch;
				if (_font.GetCharacterInfo(text[i], out ch) == true)
				{
					AddChar(p, rightVector, upVector, scale, ref ch);
					p += rightVector * (ch.advance * scale);
				}
			}
			return true;
		}

		public bool AddBillboardText(
			Vector3 center,
			float width,
			float height,
			string text)
		{
			int letterCount = text.Length;
			int vertexCount = letterCount * 4;
			int indexCount = letterCount * 6;
			if (
			((_vertexCount + vertexCount) > _capacity)
			|| ((_indexCount + indexCount) > _capacity))
			{
				return false;
			}
			_font.RequestCharactersInTexture(text);
			SetTexture(fontTexture);
			// TODO: 文字列測定しない。全文字等サイズ正方形として考える。
			var transform = _camera.gameObject.transform;
			var forwardVector = transform.forward;
			var upVector = transform.up;

			// TODO: 改行対応
			float charWidth = width / letterCount;
			float fontSize = (charWidth < height) ? charWidth : height;
			// フォント内整数座標→ワールド座標のスケール
			float scale = fontSize / (float)(_font.fontSize);

			// TODO: 右ベクタ、上ベクタは前計算可能
			// 右ベクタ、上ベクタを生成 axisX = cross(axisY, axisZ)
			Vector3 right = Vector3.Cross(upVector, forwardVector);
			right.Normalize();
			// ビルボード空間の上ベクタを計算
			Vector3 up = Vector3.Cross(forwardVector, right);

			// レイアウト開始点を計算
			Vector3 p = center - (right * (width * 0.5f)) - (up * (height * 0.5f));

			for (int i = 0; i < letterCount; i++)
			{
				CharacterInfo ch;
				if (_font.GetCharacterInfo(text[i], out ch) == true)
				{
					AddChar(p, right, up, scale, ref ch);
					p += right * (ch.advance * scale);
				}
			}
			return true;
		}

		private void AddChar(
			Vector3 p, // 左下点
			Vector3 right, //右単位ベクタ
			Vector3 up, //上単位ベクタ
			float scale,
			ref CharacterInfo ch)
		{
			float x = (float)(ch.minX) * scale;
			float y = (float)(ch.maxY) * scale;
			float w = (float)(ch.maxX - ch.minX) * scale;
			float h = (float)(ch.minY - ch.maxY) * scale;

			Vector3 p0 = p + (right * x) + (up * y);
			Vector3 p1 = p0 + (right * w);
			Vector3 p2 = p1 + (up * h);
			Vector3 p3 = p0 + (up * h);

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
		}

		public bool AddBillbordFrame(
			Vector3 center,
			float width,
			float height,
			float lineWidth = 1f)
		{
			var transform = _camera.gameObject.transform;
			var forwardVector = transform.forward;
			var upVector = transform.up;
			// TODO: 右ベクタは前計算可能
			// 右ベクタ、上ベクタを生成 axisX = cross(axisY, axisZ)
			Vector3 right = Vector3.Cross(upVector, forwardVector);
			right.Normalize();
			// ビルボード空間の上ベクタを計算
			Vector3 up = Vector3.Cross(forwardVector, right);
			// スケール
			right *= width;
			up *= height;
			// 左上点を生成
			Vector3 p = center - (right * 0.5f) + (up * 0.5f);

			return AddParallelogramFrame(p, right, up, lineWidth);
		}

		// 三角形描く
		public bool AddTriangle(
			Vector3 p0,
			Vector3 p1,
			Vector3 p2)
		{
			if (
			((_vertexCount + 3) > _capacity)
			|| ((_indexCount + 3) > _capacity))
			{
				return false;
			}
			SetTexture(fontTexture);

			_vertices[_vertexCount + 0] = p0;
			_vertices[_vertexCount + 1] = p1;
			_vertices[_vertexCount + 2] = p2;
			_colors[_vertexCount + 0] = color;
			_colors[_vertexCount + 1] = color;
			_colors[_vertexCount + 2] = color;
			_uv[_vertexCount + 0] = _whiteUv;
			_uv[_vertexCount + 1] = _whiteUv;
			_uv[_vertexCount + 2] = _whiteUv;
			AddTriangleIndices(0, 1, 2);
			_vertexCount += 3;
			return true;
		}

		// 平行四辺形を描画する。p, p+v0, p+v1, p+v0+v1の4点。
		public bool AddParallelogram(
			Vector3 p,
			Vector3 v0,
			Vector3 v1)
		{
			if (
			((_vertexCount + 4) > _capacity)
			|| ((_indexCount + 6) > _capacity))
			{
				return false;
			}
			SetTexture(fontTexture);

			// 時計回り
			_vertices[_vertexCount + 0] = p;
			_vertices[_vertexCount + 1] = p + v0;
			_vertices[_vertexCount + 2] = _vertices[_vertexCount + 1] + v1;
			_vertices[_vertexCount + 3] = p + v1;

			for (int i = 0; i < 4; i++)
			{
				_colors[_vertexCount + i] = color;
				_uv[_vertexCount + i] = _whiteUv;
			}
			AddQuadIndices(0, 1, 2, 3);
			_vertexCount += 4;
			return true;
		}

		// 平行六面体を描画する。p, p+v0, p+v1, p+v2, p+v0+v1, p+v0+v2, p+v1+v2, p+v0+v1+v2の8点
		public bool AddParallelepipedFrame(
			Vector3 p,
			Vector3 v0,
			Vector3 v1,
			Vector3 v2,
			float lineWidth)
		{
			if (
			((_vertexCount + (8 * 6)) > _capacity)
			|| ((_indexCount + (12 * 6)) > _capacity))
			{
				return false;
			}
			// 6回ParallelogramFrameを呼ぶ
			AddParallelogramFrame(p, v0, v1, lineWidth);
			AddParallelogramFrame(p, v0, v2, lineWidth);
			AddParallelogramFrame(p, v1, v2, lineWidth);
			var p123 = p + v0 + v1 + v2;
			AddParallelogramFrame(p123, -v0, -v1, lineWidth);
			AddParallelogramFrame(p123, -v0, -v2, lineWidth);
			AddParallelogramFrame(p123, -v1, -v2, lineWidth);
			return true;
		}

		// 平行四辺形を描画する。p, p+v0, p+v1, p+v0+v1の4点。
		// 線はこの内側
		public bool AddParallelogramFrame(
			Vector3 p,
			Vector3 v0,
			Vector3 v1,
			float lineWidth)
		{
			if (
			((_vertexCount + 8) > _capacity)
			|| ((_indexCount + 12) > _capacity))
			{
				return false;
			}
			SetTexture(fontTexture);

			// 時計回り
			_vertices[_vertexCount + 0] = p;
			_vertices[_vertexCount + 1] = p + v0;
			_vertices[_vertexCount + 2] = _vertices[_vertexCount + 1] + v1;
			_vertices[_vertexCount + 3] = p + v1;
			// 正規化して長さを調整したv0, v1を生成
			Vector3 v0n = v0 * (lineWidth / v0.magnitude);
			Vector3 v1n = v1 * (lineWidth / v1.magnitude);
			_vertices[_vertexCount + 4] = p + v0n + v1n;
			_vertices[_vertexCount + 5] = _vertices[_vertexCount + 1] - v0n + v1n;
			_vertices[_vertexCount + 6] = _vertices[_vertexCount + 2] - v0n - v1n;
			_vertices[_vertexCount + 7] = _vertices[_vertexCount + 3] + v0n - v1n;

			for (int i = 0; i < 8; i++)
			{
				_colors[_vertexCount + i] = color;
				_uv[_vertexCount + i] = _whiteUv;
			}
			AddQuadIndices(0, 1, 5, 4);
			AddQuadIndices(5, 1, 2, 6);
			AddQuadIndices(7, 6, 2, 3);
			AddQuadIndices(0, 4, 7, 3);
			_vertexCount += 8;
			return true;
		}

		static void MakeQuadBezier(out Vector3 a2, out Vector3 a1, out Vector3 a0, ref Vector3 begin, ref Vector3 end, ref Vector3 controlPoint)
		{
			a2 = end - (controlPoint * 2f) + begin;
			a1 = (controlPoint - begin) * 2f;
			a0 = begin;
		}

		static Vector3 EvaluateQuad(ref Vector3 a2, ref Vector3 a1, ref Vector3 a0, float t)
		{
			Vector3 r = a2;
			r *= t;
			r += a1;
			r *= t;
			r += a0;
			return r;
		}
	}
}