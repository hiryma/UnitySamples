using UnityEngine;

// 寸法指定はワールド座標
// TODO: 負荷測ってない
namespace Kayac
{
	public class DebugPrimitiveRenderer3D : DebugPrimitiveRenderer
	{
		Camera _camera;

		public DebugPrimitiveRenderer3D(
			Shader textShader,
			Shader texturedShader,
			Font font,
			Camera camera,
			MeshRenderer meshRenderer,
			MeshFilter meshFilter,
			int capacity = DefaultTriangleCapacity) : base(textShader, texturedShader, font, meshRenderer, meshFilter, capacity)
		{
			_camera = camera;
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

		// fontsize, width, heightをすべて指定する場合は折り返し
		public int AddText(
			string text,
			Vector3 origin,
			Vector3 rightVector,
			Vector3 downVector,
			float fontSize,
			float boxWidth,
			float boxHeight,
			AlignX alignX = AlignX.Left,
			AlignY alignY = AlignY.Top,
			float lineSpacingRatio = DefaultLineSpacingRatio)
		{
			return AddText(
				text,
				origin,
				rightVector,
				downVector,
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
			Vector3 origin,
			Vector3 rightVector,
			Vector3 downVector,
			float fontSize,
			AlignX alignX = AlignX.Left,
			AlignY alignY = AlignY.Top,
			float lineSpacingRatio = DefaultLineSpacingRatio)
		{
			return AddText(
				text,
				origin,
				rightVector,
				downVector,
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
			Vector3 origin,
			Vector3 rightVector,
			Vector3 downVector,
			float boxWidth,
			float boxHeight,
			AlignX alignX = AlignX.Left,
			AlignY alignY = AlignY.Top,
			float lineSpacingRatio = DefaultLineSpacingRatio)
		{
			return AddText(
				text,
				origin,
				rightVector,
				downVector,
				0f,
				boxWidth,
				boxHeight,
				alignX,
				alignY,
				TextOverflow.Scale,
				lineSpacingRatio);
		}

		// rightVectorとdownVectorが正規直交していないと平行四辺形に歪むので注意
		int AddText(
			string text,
			Vector3 origin,
			Vector3 rightVector,
			Vector3 downVector,
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
			var scale = 0f;
			bool wrap;
			float normalizedBoxWidth, normalizedBoxHeight;
			if (overflow == TextOverflow.Wrap)
			{
				Debug.Assert(fontSize > 0f);
				scale = fontSize / (float)_font.fontSize;
				normalizedBoxWidth = boxWidth * scale;
				normalizedBoxHeight = boxHeight * scale;
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

			var matrix = Matrix4x4.Translate(origin);
			var rotation = Matrix4x4.identity;
			rotation.m00 = rightVector.x;
			rotation.m10 = rightVector.y;
			rotation.m20 = rightVector.z;
			rotation.m01 = -downVector.x;
			rotation.m11 = -downVector.y;
			rotation.m21 = -downVector.z;
			var up = Vector3.Cross(downVector, rightVector);
			rotation.m02 = up.x;
			rotation.m12 = up.y;
			rotation.m22 = up.z;
			matrix *= rotation;

			matrix *= Matrix4x4.Scale(new Vector3(scale, -scale, 1f));
			// TODO: 以下の計算はまだ仮
			float offsetX = 0f;
			switch (alignX)
			{
				case AlignX.Center: offsetX = -width * 0.5f; break;
				case AlignX.Right: offsetX = -width; break;
			}
			float offsetY = 0f;
			switch (alignY)
			{
				case AlignY.Center: offsetY = -height * 0.5f; break;
				case AlignY.Bottom: offsetY = -height; break;
			}
			matrix *= Matrix4x4.Translate(new Vector3(offsetX, offsetY, 0f));
			TransformVertices(verticesBegin, ref matrix);
			return drawnLines;
		}

		public int AddBillboardText(
			string text,
			Vector3 center,
			float width,
			float height)
		{
			var transform = _camera.gameObject.transform;
			var forwardVector = center - transform.position;
			forwardVector.Normalize();
			var upVector = transform.up;
			// 右ベクタ、上ベクタを生成 axisX = cross(axisY, axisZ)
			Vector3 right = Vector3.Cross(upVector, forwardVector);
			right.Normalize();
			Vector3 down = Vector3.Cross(right, forwardVector);

			return AddText(
				text,
				center,
				right,
				down,
				width,
				height,
				AlignX.Center,
				AlignY.Center);
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

		public bool AddGrid3d(
			Vector3 p,
			Vector3 axis0,
			Vector3 axis1,
			Vector3 axis2,
			int div0,
			int div1,
			int div2,
			float lineWidth = 1f)
		{
			// 線の本数
			int lineCount = ((div0 + 1) * (div1 + 1))
				+ ((div1 + 1) * (div2 + 1))
				+ ((div2 + 1) * (div0 + 1));
			if (((_vertexCount + (lineCount * 4)) > _capacity)
				|| ((_indexCount + (lineCount * 6)) > _capacity))
			{
				return false;
			}
			SetTexture(fontTexture);
			var transform = _camera.gameObject.transform;
			var diff = p - transform.position;
			var normalVector0 = Vector3.Cross(diff, axis0);
			var normalVector1 = Vector3.Cross(diff, axis1);
			var normalVector2 = Vector3.Cross(diff, axis2);
			normalVector0.Normalize();
			normalVector1.Normalize();
			normalVector2.Normalize();
			normalVector0 *= lineWidth * 0.5f;
			normalVector1 *= lineWidth * 0.5f;
			normalVector2 *= lineWidth * 0.5f;

			for (int i = 0; i < (lineCount * 4); i++)
			{
				_colors[_vertexCount + i] = color;
				_uv[_vertexCount + i] = _whiteUv;
			}

			// axis0
			for (int i1 = 0; i1 <= div1; i1++)
			{
				var a = p + (axis1 * ((float)i1 / (float)div1));
				for (int i2 = 0; i2 <= div2; i2++)
				{
					var b = a + (axis2 * ((float)i2 / (float)div2));
					var c = b + axis0;
					var p0 = b - normalVector0;
					var p1 = b + normalVector0;
					var p2 = c - normalVector0;
					var p3 = c + normalVector0;
					_vertices[_vertexCount + 0] = p0;
					_vertices[_vertexCount + 1] = p1;
					_vertices[_vertexCount + 2] = p2;
					_vertices[_vertexCount + 3] = p3;
					AddQuadIndices(0, 1, 3, 2);
					_vertexCount += 4;
				}
			}
			// axis1
			for (int i0 = 0; i0 <= div0; i0++)
			{
				var a = p + (axis0 * ((float)i0 / (float)div0));
				for (int i2 = 0; i2 <= div2; i2++)
				{
					var b = a + (axis2 * ((float)i2 / (float)div2));
					var c = b + axis1;
					var p0 = b - normalVector1;
					var p1 = b + normalVector1;
					var p2 = c - normalVector1;
					var p3 = c + normalVector1;

					_vertices[_vertexCount + 0] = p0;
					_vertices[_vertexCount + 1] = p1;
					_vertices[_vertexCount + 2] = p2;
					_vertices[_vertexCount + 3] = p3;
					AddQuadIndices(0, 1, 3, 2);
					_vertexCount += 4;
				}
			}
			// axis2
			for (int i0 = 0; i0 <= div0; i0++)
			{
				var a = p + (axis0 * ((float)i0 / (float)div0));
				for (int i1 = 0; i1 <= div1; i1++)
				{
					var b = a + (axis1 * ((float)i1 / (float)div1));
					var c = b + axis2;
					var p0 = b - normalVector2;
					var p1 = b + normalVector2;
					var p2 = c - normalVector2;
					var p3 = c + normalVector2;

					_vertices[_vertexCount + 0] = p0;
					_vertices[_vertexCount + 1] = p1;
					_vertices[_vertexCount + 2] = p2;
					_vertices[_vertexCount + 3] = p3;
					AddQuadIndices(0, 1, 3, 2);
					_vertexCount += 4;
				}
			}
			return true;
		}

		public bool AddGrid2d(
			Vector3 p,
			Vector3 axis0,
			Vector3 axis1,
			int div0,
			int div1,
			float lineWidth = 1f)
		{
			// 線の本数
			int lineCount = ((div0 + 1) * (div1 + 1));
			if (((_vertexCount + (lineCount * 4)) > _capacity)
				|| ((_indexCount + (lineCount * 6)) > _capacity))
			{
				return false;
			}
			SetTexture(fontTexture);
			var transform = _camera.gameObject.transform;
			var diff = p - transform.position;
			var normalVector0 = axis0;
			var normalVector1 = axis1;
			normalVector0.Normalize();
			normalVector1.Normalize();
			normalVector0 *= lineWidth * 0.5f;
			normalVector1 *= lineWidth * 0.5f;

			for (int i = 0; i < (lineCount * 4); i++)
			{
				_colors[_vertexCount + i] = color;
				_uv[_vertexCount + i] = _whiteUv;
			}

			for (int i = 0; i <= div0; i++)
			{
				var a = p + (axis0 * ((float)i / (float)div0));
				var b = a + axis1;
				var p0 = a - normalVector0;
				var p1 = a + normalVector0;
				var p2 = b - normalVector0;
				var p3 = b + normalVector0;
				_vertices[_vertexCount + 0] = p0;
				_vertices[_vertexCount + 1] = p1;
				_vertices[_vertexCount + 2] = p2;
				_vertices[_vertexCount + 3] = p3;
				AddQuadIndices(0, 1, 3, 2);
				_vertexCount += 4;
			}
			for (int i = 0; i <= div1; i++)
			{
				var a = p + (axis1 * ((float)i / (float)div1));
				var b = a + axis0;
				var p0 = a - normalVector1;
				var p1 = a + normalVector1;
				var p2 = b - normalVector1;
				var p3 = b + normalVector1;
				_vertices[_vertexCount + 0] = p0;
				_vertices[_vertexCount + 1] = p1;
				_vertices[_vertexCount + 2] = p2;
				_vertices[_vertexCount + 3] = p3;
				AddQuadIndices(0, 1, 3, 2);
				_vertexCount += 4;
			}
			return true;
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