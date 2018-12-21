using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kayac
{
	// 上流のコンポーネントのscaleのzに0が入ってると動作がおかしくなる(原因不明)
	public class CutoutImage : MaskableGraphic
	{
		[SerializeField]
		Sprite _sprite;
		public Sprite sprite
		{
			get
			{
				return _sprite;
			}
		}

		[SerializeField]
		Vector2[] _overrideVertices;

		public Vector2[] overrideVertices
		{
			get { return _overrideVertices; }
		}

		public override Texture mainTexture
		{
			get
			{
				return (_sprite != null) ? _sprite.texture : null;
			}
		}

		public override void SetNativeSize()
		{
			if (_sprite == null)
			{
				Debug.LogError("sprite刺さってないのでできない");
				return;
			}
			var rect = _sprite.rect;
			rectTransform.sizeDelta = new Vector2(
				rect.width,
				rect.height);
		}

		static void MeasureRect(out Vector2 min, out Vector2 max, Sprite sprite)
		{
			min = new Vector2(float.MaxValue, float.MaxValue);
			max = -min;
			for (int i = 0; i < sprite.vertices.Length;i++)
			{
				var v = sprite.vertices[i];
				min.x = Mathf.Min(min.x, v.x);
				max.x = Mathf.Max(max.x, v.x);
				min.y = Mathf.Min(min.y, v.y);
				max.y = Mathf.Max(max.y, v.y);
			}
		}

		static void SelectIndependentVertices(out int i0, out int i1, out int i2, Vector2[] vertices)
		{
			// 0,1番は固定で使う。2番は外積が最大になるものを選ぶ
			i0 = 0;
			i1 = 1;
			i2 = 2;
			float maxAbsCross = 0f;
			var v0 = vertices[i0];
			var v1 = vertices[i1];
			for (int i = 2; i < vertices.Length;i++)
			{
				var v = vertices[i];
				var d0 = v0 - v;
				var d1 = v1 - v;
				float absCross = Mathf.Abs((d0.x * d1.y) - (d0.y * d0.x));
				if (absCross > maxAbsCross)
				{
					maxAbsCross = absCross;
					i2 = i;
				}
			}
		}

		static void MeasureTextureRect(out Vector2 min, out Vector2 max, Sprite sprite)
		{
			min = new Vector2(float.MaxValue, float.MaxValue);
			max = -min;
			for (int i = 0; i < sprite.vertices.Length;i++)
			{
				var v = sprite.uv[i];
				min.x = Mathf.Min(min.x, v.x);
				max.x = Mathf.Max(max.x, v.x);
				min.y = Mathf.Min(min.y, v.y);
				max.y = Mathf.Max(max.y, v.y);
			}
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();
			Color32 color32 = this.color;
			var rectSizeDelta = rectTransform.sizeDelta;
			var rectPivot = rectTransform.pivot;
			Vector2 uvMin, uvMax;
			float atlasWidth, atlasHeight;
			Vector2 spritePivot;
			float spriteWidth, spriteHeight;
			Vector2[] positions;
			Vector2[] uvs;
			ushort[] indices;
			float pixelsPerUnitX, pixelsPerUnitY;
			if (_sprite != null)
			{
				MeasureTextureRect(out uvMin, out uvMax, _sprite);
				atlasWidth = _sprite.texture.width;
				atlasHeight = _sprite.texture.height;
				var spriteRect = _sprite.rect;
				spriteWidth = spriteRect.width;
				spriteHeight = spriteRect.height;
				if ((_overrideVertices != null) && (_overrideVertices.Length >= 3))
				{
					positions = _overrideVertices;
					uvs = new Vector2[_overrideVertices.Length];
					var uSize = spriteWidth / atlasWidth;
					var vSize = spriteHeight / atlasHeight;
#if true
					// UVと位置の関係を求めるために、位置にある行列を乗ずるとアトラス内のUVになる、
					// というような「ある行列」を求める。
					// 3頂点の位置p0,p1,p2及びUVt0,t1,t2を横に並べ、行列P及びTを作る。
					// 「ある行列」をXとする。すると、T=XP
					// 右からPの逆行列P^を乗ずれば、TP^=XPP^=X
					// 要素ごとに書けば以下のようになる
					// |t0x t1x t2x|   |a00 a01 a02| |p0x p1x p2x|
					// |t0y t1y t2y| = |a10 a11 a12| |p0y p1y p2y|
					// |1     1   1|   |0     0   1| |  1   1   1|
					Matrix33 t;
					t.m00 = _sprite.uv[0].x;
					t.m10 = _sprite.uv[0].y;
					t.m01 = _sprite.uv[1].x;
					t.m11 = _sprite.uv[1].y;
					t.m02 = _sprite.uv[2].x;
					t.m12 = _sprite.uv[2].y;
					t.m20 = t.m21 = t.m22 = 1f;
					Matrix33 p;
					p.m00 = _sprite.vertices[0].x;
					p.m10 = _sprite.vertices[0].y;
					p.m01 = _sprite.vertices[1].x;
					p.m11 = _sprite.vertices[1].y;
					p.m02 = _sprite.vertices[2].x;
					p.m12 = _sprite.vertices[2].y;
					p.m20 = p.m21 = p.m22 = 1f;
					Matrix33 positionToUv = new Matrix33();
					if (!positionToUv.SetInverse(ref p))
					{
						return;
					}
					positionToUv.SetMul(ref t, ref positionToUv);
					float toSpriteVertexScaleX = spriteWidth / _sprite.pixelsPerUnit;
					float toSpriteVertexScaleY = spriteHeight / _sprite.pixelsPerUnit;
					float toSpriteVertexOffsetX = -_sprite.pivot.x / spriteWidth;
					float toSpriteVertexOffsetY = -_sprite.pivot.y / spriteHeight;
					for (int i = 0; i < uvs.Length; i++)
					{
						var position = _overrideVertices[i];
						position.x += toSpriteVertexOffsetX;
						position.y += toSpriteVertexOffsetY;
						position.x *= toSpriteVertexScaleX;
						position.y *= toSpriteVertexScaleY;
						positionToUv.Multiply(ref uvs[i], ref position);
					}
#else

					// 第一頂点からuvのoffsetを求める
					Vector2 p0 = _sprite.vertices[0];
					p0 *= _sprite.pixelsPerUnit;
					p0 += _sprite.pivot;
					Vector2 uv0 = _sprite.uv[0];
					uv0.x *= atlasWidth;
					uv0.y *= atlasHeight;
					var uOffset = (uv0.x - p0.x) / atlasWidth;
					var vOffset = (uv0.y - p0.y) / atlasHeight;
Debug.LogWarning(_sprite.name + " " + uOffset + " " + vOffset);
					for (int i = 0; i < uvs.Length; i++)
					{
						uvs[i].x = (_overrideVertices[i].x * uSize) + uOffset;
						uvs[i].y = (_overrideVertices[i].y * vSize) + vOffset;
Debug.LogWarning("\t" + i +  " " + uvs[i]);
					}
#endif
					indices = null;
					pixelsPerUnitX = rectSizeDelta.x;
					pixelsPerUnitY = rectSizeDelta.y;
					spritePivot = new Vector2(0f, 0f);
				}
				else
				{
					spritePivot = _sprite.pivot;
					spritePivot.x /= spriteWidth;
					spritePivot.y /= spriteHeight; // [0,1]に正規化
					positions = _sprite.vertices;
					uvs = _sprite.uv;
					Debug.Assert(positions.Length == uvs.Length);
					indices = _sprite.triangles;
					pixelsPerUnitX = pixelsPerUnitY = _sprite.pixelsPerUnit;
				}
			}
			else
			{
				uvMin = uvMax = Vector2.zero;
				atlasWidth = atlasHeight = spriteWidth = spriteHeight = 1f;
				spritePivot = new Vector2(0.5f, 0.5f);
				positions = _overrideVertices;
				uvs = _overrideVertices;
				indices = null;
				pixelsPerUnitX = pixelsPerUnitY = 1f;
			}
 			var scaleX = pixelsPerUnitX * rectSizeDelta.x / spriteWidth;
			var scaleY = pixelsPerUnitY * rectSizeDelta.y / spriteHeight;
			Vector2 offset;
			offset.x = (spritePivot.x - rectPivot.x) * rectSizeDelta.x;
			offset.y = (spritePivot.y - rectPivot.y) * rectSizeDelta.y;
			for (int i = 0; i < positions.Length; i++)
			{
				var pos = positions[i];
				pos.x *= scaleX;
				pos.y *= scaleY;
				pos += offset;
//Debug.LogWarning(i + " " + pos + " " + uvs[i]);
				vh.AddVert(pos, color32, uvs[i]);
			}

			if (indices != null)
			{
				for (int i = 0; i < indices.Length; i += 3)
				{
					vh.AddTriangle(indices[i + 0], indices[i + 1], indices[i + 2]);
				}
			}
			else
			{
				for (int i = 2; i < _overrideVertices.Length; i++)
				{
					vh.AddTriangle(0, i - 1, i);
				}
			}
		}

		struct Matrix33
		{
			public float m00, m01, m02;
			public float m10, m11, m12;
			public float m20, m21, m22;

			public void Dump()
			{
				string t = "| " + m00 + "\t" + m01 + "\t" + m02 + "|\n";
				t += "| " + m10 + "\t" + m11 + "\t" + m12 + "|\n";
				t += "| " + m20 + "\t" + m21 + "\t" + m22 + "|";
				Debug.LogWarning(t);
			}
			public void Multiply(ref Vector2 to, ref Vector2 from)
			{
				float tx = (m00 * from.x) + (m01 * from.y) + m02;
				to.y = (m10 * from.x) + (m11 * from.y) + m12;
				to.x = tx;
			}

			public void SetMul(ref Matrix33 a, ref Matrix33 b)
			{
				var t00 = (a.m00 * b.m00) + (a.m01 * b.m10) + (a.m02 * b.m20);
				var t01 = (a.m00 * b.m01) + (a.m01 * b.m11) + (a.m02 * b.m21);
				var t02 = (a.m00 * b.m02) + (a.m01 * b.m12) + (a.m02 * b.m22);

				var t10 = (a.m10 * b.m00) + (a.m11 * b.m10) + (a.m12 * b.m20);
				var t11 = (a.m10 * b.m01) + (a.m11 * b.m11) + (a.m12 * b.m21);
				var t12 = (a.m10 * b.m02) + (a.m11 * b.m12) + (a.m12 * b.m22);

				var t20 = (a.m20 * b.m00) + (a.m21 * b.m10) + (a.m22 * b.m20);
				var t21 = (a.m20 * b.m01) + (a.m21 * b.m11) + (a.m22 * b.m21);
				var t22 = (a.m20 * b.m02) + (a.m21 * b.m12) + (a.m22 * b.m22);

				m00 = t00;
				m01 = t01;
				m02 = t02;
				m10 = t10;
				m11 = t11;
				m12 = t12;
				m20 = t20;
				m21 = t21;
				m22 = t22;
			}

			public bool SetInverse(ref Matrix33 a)
			{
				/*
				|m00 m01 m02|
				|m10 m11 m12|
				|m20 m21 m22|
				の逆行列。
				*/
				var t00 = (a.m11 * a.m22) - (a.m12 * a.m21);
				var t01 = (a.m12 * a.m20) - (a.m22 * a.m10);
				var t02 = (a.m10 * a.m21) - (a.m11 * a.m20);
				var t10 = (a.m21 * a.m02) - (a.m22 * a.m01);
				var t11 = (a.m22 * a.m00) - (a.m20 * a.m02);
				var t12 = (a.m20 * a.m01) - (a.m21 * a.m00);

				var t20 = (a.m01 * a.m12) - (a.m02 * a.m11);
				var t21 = (a.m02 * a.m10) - (a.m00 * a.m12);
				var t22 = (a.m00 * a.m11) - (a.m01 * a.m10);

				var det = a.m00 * t00;
				det += a.m01 * t01;
				det += a.m02 * t02;
				if (det == 0f)
				{
					return false;
				}

				var rcpDet = 1f / det;

				m00 = t00 * rcpDet;
				m01 = t10 * rcpDet;
				m02 = t20 * rcpDet;
				m10 = t01 * rcpDet;
				m11 = t11 * rcpDet;
				m12 = t21 * rcpDet;
				m20 = t02 * rcpDet;
				m21 = t12 * rcpDet;
				m22 = t22 * rcpDet;
				return true;
			}
		}

#if UNITY_EDITOR
/*
		private void OnDrawGizmosSelected()
		{
			var ap = rectTransform.anchoredPosition;
			var t = rectTransform.localToWorldMatrix;
			Gizmos.color = new Color(0f, 1f, 0f, 1f);
			if ((_overrideVertices != null) && (_overrideVertices.Length >= 3))
			{
				var dstSizeDelta = rectTransform.sizeDelta;
				var v = _overrideVertices[_overrideVertices.Length - 1];
				v.x *= dstSizeDelta.x;
				v.y *= dstSizeDelta.y;
				var prev = t.MultiplyPoint3x4(v);
				for (int i = 0; i < _overrideVertices.Length; i++)
				{
					v = _overrideVertices[i];
					v.x *= dstSizeDelta.x;
					v.y *= dstSizeDelta.y;
					var current = t.MultiplyPoint3x4(v);
					Gizmos.DrawLine(prev, current);
					prev = current;
				}
			}
			else
			{
				Debug.Assert(!_sprite.packed
					|| ((_sprite.packingMode == SpritePackingMode.Rectangle) && (_sprite.packingRotation == SpritePackingRotation.None)));
				var srcRect = (_sprite.packed) ? _sprite.textureRect : _sprite.rect;
				var rcpSrcWidth = 1f / srcRect.width;
				var rcpSrcHeight = 1f / srcRect.height;
				var dstSizeDelta = rectTransform.sizeDelta;
				var dstPivot = rectTransform.pivot;
				var scaleX = _sprite.pixelsPerUnit * dstSizeDelta.x * rcpSrcWidth;
				var scaleY = _sprite.pixelsPerUnit * dstSizeDelta.y * rcpSrcHeight;
				var srcPivot = _sprite.pivot;
				srcPivot.x *= rcpSrcWidth; // [0,1]に正規化
				srcPivot.y *= rcpSrcHeight;
				var offsetX = (srcPivot.x - dstPivot.x) * dstSizeDelta.x;
				var offsetY = (srcPivot.y - dstPivot.y) * dstSizeDelta.y;
				var positions = _sprite.vertices;
				var indices = _sprite.triangles;
				// TODO: 座標変換3倍通しててマジ重いが、変換済み頂点列前もって作るとメモリ汚すのでやってない。3倍くらいならいいかなあ。
				for (int i = 0; i < indices.Length; i += 3)
				{
					var p0 = positions[indices[i + 0]];
					var p1 = positions[indices[i + 1]];
					var p2 = positions[indices[i + 2]];
					p0.x *= scaleX;
					p1.x *= scaleX;
					p2.x *= scaleX;
					p0.y *= scaleY;
					p1.y *= scaleY;
					p2.y *= scaleY;
					p0.x += offsetX;
					p1.x += offsetX;
					p2.x += offsetX;
					p0.y += offsetY;
					p1.y += offsetY;
					p2.y += offsetY;
					p0 = t.MultiplyPoint3x4(p0);
					p1 = t.MultiplyPoint3x4(p1);
					p2 = t.MultiplyPoint3x4(p2);
					Gizmos.DrawLine(p0, p1);
					Gizmos.DrawLine(p1, p2);
					Gizmos.DrawLine(p2, p0);
				}
			}
		}
*/
		[CustomEditor(typeof(CutoutImage), true)]
		public class ShavedImageInspector : Editor
		{
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();
				var self = (CutoutImage)target;

				if (GUILayout.Button("Set Native Size"))
				{
					self.SetNativeSize();
				}
			}

			private void OnSceneGUI()
			{
				Tools.current = Tool.None;

				var component = target as CutoutImage;
				var rectTransform = component.rectTransform;
				var overrideVertices = component._overrideVertices;

				var t = rectTransform.localToWorldMatrix;
				var inverse = rectTransform.worldToLocalMatrix;
				if ((overrideVertices != null) && (overrideVertices.Length >= 3))
				{
					var dstSizeDelta = rectTransform.sizeDelta;
					var v = overrideVertices[overrideVertices.Length - 1];
					v.x *= dstSizeDelta.x;
					v.y *= dstSizeDelta.y;
					for (int i = 0; i < overrideVertices.Length; i++)
					{
						v = overrideVertices[i];
						v.x *= dstSizeDelta.x;
						v.y *= dstSizeDelta.y;
						var current = t.MultiplyPoint3x4(v);
						PositionHandle(ref overrideVertices[i], ref inverse, ref current, ref dstSizeDelta);
					}

					component.SetVerticesDirty();
				}
			}

			void PositionHandle(ref Vector2 targetPoint, ref Matrix4x4 inverse, ref Vector3 position, ref Vector2 sizeDelta)
			{
				// TODO: rotationも考慮していい感じにする
				var handleSize = HandleUtility.GetHandleSize(position) * 0.2f;
				var newWorldPosition = Handles.FreeMoveHandle(position, Quaternion.identity, handleSize, new Vector3(1f, 1f, 0f), Handles.CircleHandleCap);
				var newPosition = inverse.MultiplyPoint3x4(newWorldPosition);
				newPosition.x /= sizeDelta.x;
				newPosition.y /= sizeDelta.y;
				if (Mathf.Abs(newPosition.x - targetPoint.x) > 1e-5f)
				{
					targetPoint.x = newPosition.x;
				}
				if (Mathf.Abs(newPosition.y - targetPoint.y) > 1e-5f)
				{
					targetPoint.y = newPosition.y;
				}
			}
		}
#endif
	}
}
