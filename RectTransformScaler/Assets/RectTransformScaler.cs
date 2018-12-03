using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kayac
{
	public class RectTransformScaler : MonoBehaviour
	{
		// staticなデフォルト変数群
		public static void SetDefaults(
			float logicalWidth,
			float logicalHeight,
			float safeAreaLeftNormalized,
			float safeAreaTopNormalized,
			float safeAreaWidthNormalized,
			float safeAreaHeightNormalized)
		{
			_defaultLogicalWidth = logicalWidth;
			_defaultLogicalHeight = logicalHeight;
			_defaultMarginLeft = safeAreaLeftNormalized;
			_defaultMarginTop = safeAreaTopNormalized;
			_defaultMarginRight = 1f - safeAreaWidthNormalized - safeAreaLeftNormalized;
			_defaultMarginBottom = 1f - safeAreaHeightNormalized- safeAreaTopNormalized;
		}
		private static float _defaultLogicalWidth = 1280f;
		private static float _defaultLogicalHeight = 720f;
		private static float _defaultMarginLeft = 0f;
		private static float _defaultMarginRight = 0f;
		private static float _defaultMarginTop = 0f;
		private static float _defaultMarginBottom = 0f;

		public struct ScreenDimensions
		{
			public float width;
			public float height;
			public float marginLeft;
			public float marginRight;
			public float marginTop;
			public float marginBottom;
		}

		public enum ScaleMode
		{
			Horizontal,
			Vertical,
			ToMin, // 指定の矩形(_logicalWidth x _logicalHeight)が完全に収まることを保証する→切れてはいけないもの向け
			ToMax, // 指定の矩形(_logicalWidth x _logicalHeight)に完全に含まれることを保証する→隙間が見えてはいけない物向け。背景等。
		}

		public enum HorizontalAnchor
		{
			Center,
			Left,
			Right,
		}
		public enum VerticalAnchor
		{
			Center,
			Top,
			Bottom,
		}

		[SerializeField]
		private ScaleMode _scaleMode;
		[SerializeField]
		private HorizontalAnchor _horizontalAnchor;
		[SerializeField]
		private VerticalAnchor _verticalAnchor;
		[SerializeField]
		private float _logicalWidth = _defaultLogicalWidth;
		[SerializeField]
		private float _logicalHeight = _defaultLogicalHeight;
		[SerializeField]
		private bool _useSafeArea;

		public void Start()
		{
			Apply();
		}

		private static float CalcScale(
			ScaleMode mode,
			float parentWidth,
			float parentHeight,
			float logicalWidth,
			float logicalHeight)
		{
			// スケール計算
			float scale = 1f;
			if (mode == ScaleMode.Horizontal)
			{
				scale = parentWidth / logicalWidth;
			}
			else if (mode == ScaleMode.Vertical)
			{
				scale = parentHeight / logicalHeight;
			}
			else if (mode == ScaleMode.ToMin)
			{
				var scaleV = parentHeight / logicalHeight;
				var scaleH = parentWidth / logicalWidth;
				scale = Mathf.Min(scaleV, scaleH);
			}
			else if (mode == ScaleMode.ToMax)
			{
				var scaleV = parentHeight / logicalHeight;
				var scaleH = parentWidth / logicalWidth;
				scale = Mathf.Max(scaleV, scaleH);
			}
			return scale;
		}

		public void Apply()
		{
			// 自分のtransform
			var transform = gameObject.GetComponent<RectTransform>();
			Debug.Assert(transform != null);
			if (transform == null)
			{
				return;
			}
			// safeAreaからpivot,anchorMin,anchorMaxを計算する
			float marginLeft = 0f;
			float marginRight = 0f;
			float marginTop = 0f;
			float marginBottom = 0f;
			if (_useSafeArea)
			{
				marginLeft = _defaultMarginLeft;
				marginRight = _defaultMarginRight;
				marginTop = _defaultMarginTop;
				marginBottom = _defaultMarginRight;
			}

			// 親を取ってくる
			var parentTransform = transform.parent as RectTransform;
			if (parentTransform == null)
			{
				return;
			}
			Debug.Assert(parentTransform != null);

			// safeAreaを削った仮想のの親サイズを計算
			var parentRect = parentTransform.rect;
			var parentWidth = parentRect.width * (1f - marginLeft - marginRight);
			var parentHeight = parentRect.height * (1f - marginTop - marginBottom);
			transform.anchorMax = new Vector2(0.5f, 0.5f);
			transform.anchorMin = new Vector2(0.5f, 0.5f);
			transform.sizeDelta = new Vector2(_logicalWidth, _logicalHeight);

			var centerX = (marginLeft + (1f - marginRight)) * 0.5f;
			var centerY = (marginBottom + (1f - marginTop)) * 0.5f;
			transform.pivot = new Vector2(1f - centerX, 1f - centerY); // pivotは反転するために1から引いて設定する(自分の中でまだ腑に落ちてない)

			// スケール計算
			float scale = CalcScale(_scaleMode, parentWidth, parentHeight, _logicalWidth, _logicalHeight);
			transform.localScale = new Vector3(scale, scale, 1f);

			// アンカー考慮した位置計算
			var position = Vector2.zero;
			var anchorOffsetY = (parentHeight - (_logicalHeight * scale)) * 0.5f;
			if (_verticalAnchor == VerticalAnchor.Top)
			{
				position.y = anchorOffsetY;
			}
			else if (_verticalAnchor == VerticalAnchor.Bottom)
			{
				position.y = -anchorOffsetY;
			}
			var anchorOffsetX = (parentWidth - (_logicalWidth * scale)) * 0.5f;
			if (_horizontalAnchor == HorizontalAnchor.Left)
			{
				position.x = -anchorOffsetX;
			}
			else if (_horizontalAnchor == HorizontalAnchor.Right)
			{
				position.x = anchorOffsetX;
			}
			transform.anchoredPosition = position;
		}
#if UNITY_EDITOR
		private void OnValidate()
		{
			Apply();
		}
#endif
	}
}
