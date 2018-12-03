using UnityEngine;
using UnityEngine.UI;

// TODO: easeない
// TODO: anchorPos動くの対応してない
namespace AfterEffectsToUnity
{
	public static class AfterEffectsUtil
	{
		public const int Infinity = 0x7fffffff;
		private const float _setPeriodHideZ = -1e10f;
		private const float _setPeriodDeltaT = 0.0001f;
		// UI.Image/RawImage用。AEの値をつっこむ。スケールは%で、回転は度。
		public static void Set(
			Graphic graphic,
			float anchorX = 0f,
			float anchorY = 0f,
			float positionX = 0f,
			float positionY = 0f,
			float scaleX = 100f,
			float scaleY = 100f,
			float rotation = 0f,
			float opacity = 100f)
		{
			// OnValidateだとAwakeが終わっていなくて呼ばれないことがある
			if (graphic == null)
			{
				return;
			}
			var origColor = graphic.color;
			graphic.color = new Color(origColor.r, origColor.g, origColor.b, opacity * 0.01f);

			RectTransform transform = graphic.gameObject.GetComponent<RectTransform>();
			Set(
				transform,
				anchorX,
				anchorY,
				positionX,
				positionY,
				scaleX,
				scaleY,
				rotation);
		}

		public static void Set(
			CanvasGroup canvasGroup,
			float anchorX = 0f,
			float anchorY = 0f,
			float positionX = 0f,
			float positionY = 0f,
			float scaleX = 100f,
			float scaleY = 100f,
			float rotation = 0f,
			float opacity = 100f)
		{
			// OnValidateだとAwakeが終わっていなくて呼ばれないことがある
			if (canvasGroup == null)
			{
				return;
			}
			canvasGroup.alpha = canvasGroup.alpha * opacity * 0.01f;
			RectTransform transform = canvasGroup.gameObject.GetComponent<RectTransform>();
				Set(
				transform,
				anchorX,
				anchorY,
				positionX,
				positionY,
				scaleX,
				scaleY,
				rotation);
		}

		public static void Set(
			RectTransform transform,
			float anchorX = 0f,
			float anchorY = 0f,
			float positionX = 0f,
			float positionY = 0f,
			float scaleX = 100f,
			float scaleY = 100f,
			float rotation = 0f)
		{
			// OnValidateだとAwakeが終わっていなくて呼ばれないことがある
			if (transform == null)
			{
				return;
			}
			// 左上原点に変更
			transform.anchorMax = new Vector2(0f, 1f);
			transform.anchorMin = new Vector2(0f, 1f);
			// 基準点設定
			var size = transform.sizeDelta;
			float pivotX = anchorX / size.x;
			float pivotY = 1f - (anchorY / size.y);
			transform.pivot = new Vector2(pivotX, pivotY);
			transform.anchoredPosition = new Vector2(positionX, -positionY);
			transform.localScale = new Vector3(scaleX * 0.01f, scaleY * 0.01f, 1f);
			transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, -rotation));
		}

		// SpriteRenderer用。AEの値をつっこむ。スケールは%で、回転は度。UGUI用。
		public static void Set(
			SpriteRenderer renderer,
			float z,
			Vector2 parentSpritePivot,
			float anchorX,
			float anchorY,
			float positionX = 0f,
			float positionY = 0f,
			float scaleX = 100f,
			float scaleY = 100f,
			float rotation = 0f,
			float opacity = 100f)
		{
			// OnValidateだとAwakeが終わっていなくて呼ばれないことがある
			if (renderer == null)
			{
				return;
			}
			Sprite sprite = renderer.sprite;
			var pixelsPerUnit = sprite.pixelsPerUnit;
			float pixelToUnit = 1f / pixelsPerUnit;
			var pivot = sprite.pivot;

			Transform transform = renderer.gameObject.transform;
			transform.localPosition = new Vector3(
				(pivot.x - anchorX) * pixelToUnit,
				(-pivot.y + anchorY) * pixelToUnit,
				z);
			transform.localScale = Vector3.one;
			transform.localRotation = Quaternion.identity;

			Transform parent = transform.parent;
			parent.localPosition = new Vector3(
				(positionX - parentSpritePivot.x) * pixelToUnit,
				(-positionY + parentSpritePivot.y) * pixelToUnit,
				0f);
			parent.localScale = new Vector3(
				scaleX * 0.01f,
				scaleY * 0.01f,
				1f);
			parent.localRotation = Quaternion.Euler(new Vector3(0f, 0f, -rotation));

			var origColor = renderer.color;
			renderer.color = new Color(origColor.r, origColor.g, origColor.b, opacity * 0.01f);
		}

		/// ソートされた配列において、search以下の要素のうち最大のものの添字を返す。0番よりも小さければ-1を返す。
		public static int FindLargestLessEqual(short[] array, float search)
		{
			// 範囲外を先に処理。キーが一つしかないケース、キーが全て同値のケースもここで抜ける
			int end = array.Length - 1;
			if (search < (float)(array[0]))
			{
				return -1;
			}
			else if (search >= (float)(array[end]))
			{
				return end;
			}

			int begin = 0;
			Debug.Assert(begin < end);
			int middle;
			while ((end - begin) >= 2) // 平均(middle)がbeginより大きい間回す
			{
				middle = (begin + end) / 2;
				float pivot = (float)(array[middle]);
				if (search < pivot)
				{
					end = middle;
				}
				else // 等しければ後ろに送るので、この結果同要素が並んだ場合は最後のものを返す。
				{
					begin = middle;
				}
			}

			Debug.Assert(end == (begin + 1)); // begin==endのケースはない
			// array[begin] =< search < array[end]なのでbeginを返せば良い
			return begin;
		}

		/// ソートされた配列において、search以上の要素のうち最小のものの添字を返す。0番よりも小さければ-1を返す。
		// 実装メモ: 上の関数と対称なはずなので、0とLEngth-1、beginとend 不等号の向き、が全箇所で反転していればいい。
		public static int FindSmallestGreaterEqual(short[] array, float search)
		{
			// 範囲外を先に処理。キーが一つしかないケース、キーが全て同値のケースもここで抜ける
			int end = array.Length - 1;
			if (search <= (float)(array[0]))
			{
				return 0;
			}
			else if (search > (float)(array[end]))
			{
				return -1;
			}

			int begin = 0;
			Debug.Assert(begin < end);
			int middle;
			while ((end - begin) >= 2) // 平均(middle)がbeginより大きい間回す
			{
				middle = (begin + end) / 2;
				float pivot = (float)(array[middle]);
				if (search <= pivot) // middleも含む。等しい場合も前のキーが同値である可能性があり、その場合はそれを返したいので、後ろを捨てて続行する。
				{
					end = middle;
				}
				else
				{
					begin = middle;
				}
			}

			Debug.Assert(end == (begin + 1)); // begin==endのケースはない
			// array[begin] < search <= array[end]なのでendを返せば良い
			return end;
		}

		public static float Fmod(float x, float y)
		{
			int q = (int)(x / y);
			float r = x - ((float)q * y);
			return r;
		}
	}
}
