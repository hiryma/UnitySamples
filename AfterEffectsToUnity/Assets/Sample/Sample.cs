// project name : AfterEffectsToUnitySample.aep

// generated from composition Sample

// [object hierarchy]
// root
// 	layer1_人_png
// 	layer2_鳥_png
// 	layer5_背景ルート
// 		layer3_背景_png
// 		layer4_背景_png

using UnityEngine;
using UnityEngine.UI;
using AfterEffectsToUnity;

namespace Ae2UnitySample
{
	public class Sample : AfterEffectsAnimation
	{
		private static AfterEffectsResource _resource;
		[SerializeField]
		private Sprite _layer1_人_png_sprite;
		[SerializeField]
		private Sprite _layer2_鳥_png_sprite;
		[SerializeField]
		private Sprite _layer3_背景_png_sprite;
		[SerializeField]
		private Sprite _layer4_背景_png_sprite;

		[Header("以下はBuildHierarchyで自動で入る")]
		[SerializeField]
		private Graphic _layer1_人_png;
		[SerializeField]
		private Graphic _layer2_鳥_png;
		[SerializeField]
		private Graphic _layer3_背景_png;
		[SerializeField]
		private Graphic _layer4_背景_png;
		[SerializeField]
		private RectTransform _layer5_背景ルート;

		protected override void SetFirstFrame()
		{
			AfterEffectsUtil.Set(
				_layer1_人_png,
				32f,
				50f,
				52f,
				276f,
				100f,
				100f,
				20f,
				100f);
			AfterEffectsUtil.Set(
				_layer2_鳥_png,
				50f,
				50f,
				660f,
				-20f,
				10f,
				10f,
				-18f,
				100f);
			AfterEffectsUtil.Set(
				_layer3_背景_png,
				320f,
				200f,
				320f,
				200f,
				100f,
				100f,
				0f,
				100f);
			AfterEffectsUtil.Set(
				_layer4_背景_png,
				320f,
				200f,
				960f,
				200f,
				100f,
				100f,
				0f,
				100f);
			AfterEffectsUtil.Set(
				_layer5_背景ルート,
				320f,
				200f,
				320f,
				200f,
				100f,
				100f,
				0f);
		}

		public static void CreateResource()
		{
			_resource = new AfterEffectsResource(30f);
			_resource
				// layer1_人_png
				.AddPosition(
					"layer1_人_png_position",
					new int[] { 0, 90, 107, 117, 131, 147, 162, 169, 179, 299 },
					new float[] { 52f, 242f, 282f, 308f, 366f, 438f, 474f, 508f, 532f, 678f },
					new float[] { 276f, 294f, 188f, 158f, 148f, 206f, 278f, 246f, 290f, 270f })
				.AddRotation(
					"layer1_人_png_rotation",
					new int[] { 0, 90, 108, 131, 169, 178 },
					new float[] { 20f, 0f, -43f, -12f, 0f, 13f })
				// layer2_鳥_png
				.AddPosition(
					"layer2_鳥_png_position",
					new int[] { 0, 54, 90, 120, 139, 209, 299 },
					new float[] { 660f, 489.861051392424f, 412f, 349f, 205.589212215198f, 115f, 23f },
					new float[] { -20f, 71.5331734089565f, 109f, 120f, 103.796352593026f, 141f, 273f })
				.AddScale(
					"layer2_鳥_png_scale",
					new int[] { 0, 54, 90, 119, 139, 209, 299 },
					new float[] { 10f, 30f, 60f, 100f, 50f, 30f, 10f })
				.AddRotation(
					"layer2_鳥_png_rotation",
					new int[] { 120, 299 },
					new float[] { -18f, 3600f })
				// layer5_背景ルート
				.AddPosition(
					"layer5_背景ルート_position",
					new int[] { 0, 299 },
					new float[] { 320f, -320f },
					new float[] { 200f, 200f })
				.AddCut("", 0, 300);
		}

		protected override void InitializeInstance(AfterEffectsInstance instance)
		{
			var layer1_人_png_transform = _layer1_人_png.gameObject.GetComponent<RectTransform>();
			var layer2_鳥_png_transform = _layer2_鳥_png.gameObject.GetComponent<RectTransform>();

			instance
				// layer1_人_png
				.BindPosition(layer1_人_png_transform, "layer1_人_png_position")
				.BindRotation(layer1_人_png_transform, "layer1_人_png_rotation")
				// layer2_鳥_png
				.BindPosition(layer2_鳥_png_transform, "layer2_鳥_png_position")
				.BindScale(layer2_鳥_png_transform, "layer2_鳥_png_scale")
				.BindRotation(layer2_鳥_png_transform, "layer2_鳥_png_rotation")
				// layer5_背景ルート
				.BindPosition(_layer5_背景ルート, "layer5_背景ルート_position")
			;
		}

		public static void DestroyResource()
		{
			_resource = null;
		}

		protected override AfterEffectsResource GetResource()
		{
			if (_resource == null)
			{
				CreateResource();
			}
			return _resource;
		}

#if UNITY_EDITOR
		protected override void BuildHierarchy()
		{
			gameObject.name = this.GetType().Name;
			var transform = gameObject.GetComponent<RectTransform>();
			transform.sizeDelta = new Vector2(640f, 400f);
			GameObject layerObj;
			Image image;

			layerObj = new GameObject("layer5_背景ルート", typeof(RectTransform));
			_layer5_背景ルート = layerObj.GetComponent<RectTransform>();
			layerObj.transform.SetParent(gameObject.transform, false);
			layerObj.GetComponent<RectTransform>().sizeDelta = new Vector2(640, 400);

			layerObj = new GameObject("layer4_背景_png", typeof(Image));
			image = layerObj.GetComponent<Image>();
			image.sprite = _layer4_背景_png_sprite;
			image.raycastTarget = false;
			_layer4_背景_png = image;
			layerObj.transform.SetParent(_layer5_背景ルート.transform, false);
			layerObj.GetComponent<RectTransform>().sizeDelta = new Vector2(640, 400);

			layerObj = new GameObject("layer3_背景_png", typeof(Image));
			image = layerObj.GetComponent<Image>();
			image.sprite = _layer3_背景_png_sprite;
			image.raycastTarget = false;
			_layer3_背景_png = image;
			layerObj.transform.SetParent(_layer5_背景ルート.transform, false);
			layerObj.GetComponent<RectTransform>().sizeDelta = new Vector2(640, 400);

			layerObj = new GameObject("layer2_鳥_png", typeof(Image));
			image = layerObj.GetComponent<Image>();
			image.sprite = _layer2_鳥_png_sprite;
			image.raycastTarget = false;
			_layer2_鳥_png = image;
			layerObj.transform.SetParent(gameObject.transform, false);
			layerObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);

			layerObj = new GameObject("layer1_人_png", typeof(Image));
			image = layerObj.GetComponent<Image>();
			image.sprite = _layer1_人_png_sprite;
			image.raycastTarget = false;
			_layer1_人_png = image;
			layerObj.transform.SetParent(gameObject.transform, false);
			layerObj.GetComponent<RectTransform>().sizeDelta = new Vector2(64, 100);

		}
#endif
	}
}

