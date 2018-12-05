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
		// 画面のマージンを設定する左右は画面幅を1とする大きさ、上下は画面高さを1とする大きさで指定。
		public static void SetNormalizedMargin(
			float left,
			float right,
			float top,
			float bottom)
		{
			Debug.Assert((left + right) < 1f, "左右マージンの和が1を越えている。ピクセルで指定してない?画面幅で割ってね!");
			Debug.Assert((top + bottom) < 1f, "上下マージンの和が1を越えている。ピクセルで指定してない?画面高さで割ってね!");
			_marginLeft = left;
			_marginRight = right;
			_marginTop = top;
			_marginBottom = bottom;
		}
		private static float _marginLeft;
		private static float _marginRight;
		private static float _marginTop;
		private static float _marginBottom;

		public enum ScaleMode
		{
			Horizontal,
			Vertical,
			Min, // RectTransform矩形が画面の中に完全に収まることを保証する→切れてはいけないもの向け
			Max, // 画面がRectTransform矩形の中に完全に含まれることを保証する→隙間が見えてはいけない物向け。背景等。
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
		private bool _useMargin;

		public void Start()
		{
			Apply();
		}

		private static float CalcScale(
			ScaleMode mode,
			float parentWidth,
			float parentHeight,
			float width,
			float height)
		{
			// スケール計算
			float scale = 1f;
			if (mode == ScaleMode.Horizontal)
			{
				scale = parentWidth / width;
			}
			else if (mode == ScaleMode.Vertical)
			{
				scale = parentHeight / height;
			}
			else if (mode == ScaleMode.Min)
			{
				var scaleV = parentHeight / height;
				var scaleH = parentWidth / width;
				scale = Mathf.Min(scaleV, scaleH);
			}
			else if (mode == ScaleMode.Max)
			{
				var scaleV = parentHeight / height;
				var scaleH = parentWidth / width;
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
			if (_useMargin)
			{
				marginLeft = _marginLeft;
				marginRight = _marginRight;
				marginTop = _marginTop;
				marginBottom = _marginBottom;
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
			var size = transform.sizeDelta;

			// スケール計算
			float scale = CalcScale(_scaleMode, parentWidth, parentHeight, size.x, size.y);
			transform.localScale = new Vector3(scale, scale, 1f);

			// 位置計算
			var halfX = (size.x * scale * 0.5f);
			var halfY = (size.y * scale * 0.5f);
			var left = (parentRect.width * (marginLeft - 0.5f)) + halfX;
			var right = (parentRect.width * (0.5f - marginRight)) - halfX;
			var top = (parentRect.height * (0.5f - marginTop)) - halfY;
			var bottom = (parentRect.height * (marginBottom - 0.5f)) + halfY;
			var blendX = 0.5f;
			var blendY = 0.5f;
			// もしUIをスライダーにして、「左10%の場所」みたいなのを可能にした場合は下をちょちょいと書き換えればいい
			switch (_horizontalAnchor)
			{
				case HorizontalAnchor.Center: blendX = 0.5f; break;
				case HorizontalAnchor.Left: blendX = 0f; break;
				case HorizontalAnchor.Right: blendX = 1f; break;
			}
			switch (_verticalAnchor)
			{
				case VerticalAnchor.Center: blendY = 0.5f; break;
				case VerticalAnchor.Top: blendY = 0f; break;
				case VerticalAnchor.Bottom: blendY = 1f; break;
			}
			Vector2 position;
			position.x = Mathf.Lerp(left, right, blendX);
			position.y = Mathf.Lerp(top, bottom, blendY);
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

		[CustomEditor (typeof(RectTransformScaler))]
		public class RectTransformScalerInspector : Editor
		{
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				var self = (RectTransformScaler)target;
				if (GUILayout.Button("ここだけApply"))
				{
					self.Apply();
				}
				if (GUILayout.Button("再帰的Apply"))
				{
					RectTransformScaler.ApplyRecursive(self.transform);
				}
			}
		}
#endif
	}
}
