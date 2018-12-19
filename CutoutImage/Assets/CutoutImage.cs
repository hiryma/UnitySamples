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
		private Sprite _sprite;
		public Sprite sprite
		{
			get
			{
				return _sprite;
			}
		}

		[SerializeField]
		private Vector2[] _overrideVertices;

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

		public void ReplaceSprite(Sprite sprite)
		{
			_sprite = sprite;
		}

		public override void SetNativeSize()
		{
			if (_sprite == null)
			{
				Debug.LogWarning("sprite刺さってないのでできない");
				return;
			}
			if ((rectTransform.anchorMin.x != rectTransform.anchorMax.x)
				|| (rectTransform.anchorMin.y != rectTransform.anchorMax.y))
			{
				Debug.LogWarning("自動伸縮あるとできない");
				return;
			}
			Debug.Assert(!_sprite.packed
				|| ((_sprite.packingMode == SpritePackingMode.Rectangle) && (_sprite.packingRotation == SpritePackingRotation.None)));
			var srcRect = (_sprite.packed) ? _sprite.textureRect : _sprite.rect;
			rectTransform.sizeDelta = new Vector2(srcRect.width, srcRect.height);
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			if (_sprite == null)
			{
				return;
			}
			vh.Clear();
			Color32 color32 = this.color;
			var dstSizeDelta = rectTransform.sizeDelta;
			var dstPivot = rectTransform.pivot;
			Debug.Assert(!_sprite.packed
				|| ((_sprite.packingMode == SpritePackingMode.Rectangle) && (_sprite.packingRotation == SpritePackingRotation.None)));
			var srcRect = (_sprite.packed) ? _sprite.textureRect : _sprite.rect;
			if ((_overrideVertices != null) && (_overrideVertices.Length >= 3))
			{
				var toUvOffsetX = dstPivot.x;
				var toUvOffsetY = dstPivot.y;
				Vector2 uv;
				if (_sprite.packed)
				{
					var spriteRect = _sprite.rect;
					var rcpAtlasWidth = 1f / _sprite.texture.width;
					var rcpAtlasHeight = 1f / _sprite.texture.height;
					for (int i = 0; i < _overrideVertices.Length; i++)
					{
						var pos = _overrideVertices[i];
						uv.x = (srcRect.xMin + spriteRect.width * (pos.x + toUvOffsetX)) * rcpAtlasWidth;
						uv.y = (srcRect.yMin + spriteRect.height * (pos.y + toUvOffsetY)) * rcpAtlasHeight;
						pos.x *= dstSizeDelta.x;
						pos.y *= dstSizeDelta.y;
						vh.AddVert(pos, color32, uv);
					}
				}
				else
				{
					for (int i = 0; i < _overrideVertices.Length; i++)
					{
						var pos = _overrideVertices[i];
						uv.x = pos.x;
						uv.y = pos.y;
						uv.x += toUvOffsetX;
						uv.y += toUvOffsetY;
						pos.x *= dstSizeDelta.x;
						pos.y *= dstSizeDelta.y;
						vh.AddVert(pos, color32, uv);
					}
				}
				for (int i = 2; i < _overrideVertices.Length; i++)
				{
					vh.AddTriangle(0, i - 1, i);
				}
			}
			else
			{
				var positions = _sprite.vertices;
				var uvs = _sprite.uv;
				var indices = _sprite.triangles;
				Debug.Assert(positions.Length == uvs.Length);
				var rcpSrcWidth = 1f / srcRect.width;
				var rcpSrcHeight = 1f / srcRect.height;
				var scaleX = _sprite.pixelsPerUnit * dstSizeDelta.x * rcpSrcWidth;
				var scaleY = _sprite.pixelsPerUnit * dstSizeDelta.y * rcpSrcHeight;
				var srcPivot = _sprite.pivot;
				srcPivot.x *= rcpSrcWidth; // [0,1]に正規化
				srcPivot.y *= rcpSrcHeight;
				var offsetX = (srcPivot.x - dstPivot.x) * dstSizeDelta.x;
				var offsetY = (srcPivot.y - dstPivot.y) * dstSizeDelta.y;
				for (int i = 0; i < positions.Length; i++)
				{
					var pos = positions[i];
					pos.x *= scaleX;
					pos.y *= scaleY;
					pos.x += offsetX;
					pos.y += offsetY;
					vh.AddVert(pos, color32, uvs[i]);
				}
				for (int i = 0; i < indices.Length; i += 3)
				{
					vh.AddTriangle(indices[i + 0], indices[i + 1], indices[i + 2]);
				}
			}
		}

#if UNITY_EDITOR
		private void OnDrawGizmosSelected()
		{
			if (_sprite == null)
			{
				return;
			}
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
