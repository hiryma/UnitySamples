using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kayac
{
	public class RectTransformScaler : MonoBehaviour
	{
		// staticなデフォルト変数群を設定(起動/解像度設定直後に呼ぶ)
		public static void SetDefaults(
			float logicalWidth,
			float logicalHeight,
			float marginLeftNormalized,
			float marginRightNormalized,
			float marginTopNormalized,
			float marginBottomNormalized)
		{
			_defaultLogicalWidth = logicalWidth;
			_defaultLogicalHeight = logicalHeight;
			_defaultMarginLeft = marginLeftNormalized;
			_defaultMarginRight = marginRightNormalized;
			_defaultMarginTop = marginTopNormalized;
			_defaultMarginBottom = marginBottomNormalized;
		}
		private static float _defaultLogicalWidth = 1280f;
		private static float _defaultLogicalHeight = 720f;
		private static float _defaultMarginLeft = 0f;
		private static float _defaultMarginRight = 0f;
		private static float _defaultMarginTop = 0f;
		private static float _defaultMarginBottom = 0f;

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
				marginBottom = _defaultMarginBottom;
			}

			// 親を取ってくる
			var parentTransform = transform.parent as RectTransform;
			if (parentTransform == null)
			{
				return;
			}
			Debug.Assert(parentTransform != null);

			// safeAreaを削った仮想の親サイズを計算
			var parentRect = parentTransform.rect;
			var parentWidth = parentRect.width * (1f - marginLeft - marginRight);
			var parentHeight = parentRect.height * (1f - marginTop - marginBottom);
			transform.anchorMax = new Vector2(0.5f, 0.5f);
			transform.anchorMin = new Vector2(0.5f, 0.5f);
			transform.pivot = new Vector2(0.5f, 0.5f);
			transform.sizeDelta = new Vector2(_logicalWidth, _logicalHeight);

			// スケール計算
			float scale = CalcScale(_scaleMode, parentWidth, parentHeight, _logicalWidth, _logicalHeight);
			transform.localScale = new Vector3(scale, scale, 1f);

			// 位置計算
			var position = Vector2.zero;
			if (_verticalAnchor == VerticalAnchor.Center)
			{
				var centerY = (marginBottom + (1f - marginTop)) * 0.5f;
				position.y = parentRect.height * (centerY - 0.5f);
			}
			else if (_verticalAnchor == VerticalAnchor.Top)
			{
				position.y += (parentRect.height * 0.5f); // 上端へ移動
				position.y -= _logicalHeight * scale * 0.5f; // 自分の大きさの半分を移動
				position.y -= marginTop * parentRect.height; // マージン分ずらし
			}
			else if (_verticalAnchor == VerticalAnchor.Bottom)
			{
				position.y -= (parentRect.height * 0.5f); // 下端へ移動
				position.y += _logicalHeight * scale * 0.5f; // 自分の大きさの半分を移動
				position.y += marginBottom * parentRect.height; // マージン分ずらし
			}

			if (_horizontalAnchor == HorizontalAnchor.Center)
			{
				var centerX = (marginLeft + (1f - marginRight)) * 0.5f;
				position.x = parentRect.height * (centerX - 0.5f);
			}
			else if (_horizontalAnchor == HorizontalAnchor.Left)
			{
				position.x -= (parentRect.width * 0.5f); // 左端へ移動
				position.x += _logicalWidth * scale * 0.5f; // 自分の大きさの半分を移動
				position.x += marginLeft * parentRect.width; // マージン分ずらし
			}
			else if (_horizontalAnchor == HorizontalAnchor.Right)
			{
				position.x += (parentRect.width * 0.5f); // 右端へ移動
				position.x -= _logicalWidth * scale * 0.5f; // 自分の大きさの半分を移動
				position.x -= marginRight * parentRect.width; // マージン分ずらし
			}
			transform.anchoredPosition = position;
		}

#if UNITY_EDITOR
		// すごく遅くなりうることに注意。実行中に呼ぶな
		public static void ApplyRecursive(Transform transform)
		{
			var scaler = transform.gameObject.GetComponent<RectTransformScaler>();
			if (scaler != null)
			{
				scaler.Apply();
			}
			for (int i = 0; i < transform.childCount; i++)
			{
				var child = transform.GetChild(i);
				ApplyRecursive(child);
			}
		}

		[MenuItem("GameObject/RectTransformScaler", false, 20)]
		public static void ApplyRecursive()
		{
			var rootObject = Selection.activeGameObject;
			ApplyRecursive(rootObject.transform);
		}
#endif
	}
}
