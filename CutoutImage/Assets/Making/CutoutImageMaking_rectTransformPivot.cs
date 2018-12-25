using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CutoutImageMaking_rectTransformPivot : MaskableGraphic
{
	[SerializeField]
	Sprite _sprite;
	public override Texture mainTexture { get { return (_sprite != null) ? _sprite.texture : null; } }

	protected override void OnPopulateMesh(VertexHelper vh)
	{
		vh.Clear();
		if (_sprite == null){ return; }
		var spriteRect = _sprite.rect;
		var sizeDelta = rectTransform.sizeDelta;
		var scale = new Vector2(
			_sprite.pixelsPerUnit * sizeDelta.x / spriteRect.width,
			_sprite.pixelsPerUnit * sizeDelta.y / spriteRect.height);
		var offset = new Vector2(
			sizeDelta.x * (0.5f - rectTransform.pivot.x),
			sizeDelta.y * (0.5f - rectTransform.pivot.y));
		for (int i = 0; i < _sprite.vertices.Length; i++)
		{
			var v = new Vector2(
				_sprite.vertices[i].x * scale.x,
				_sprite.vertices[i].y * scale.y);
			v += offset;
			vh.AddVert(v, this.color, _sprite.uv[i]);
		}

		for (int i = 0; i < _sprite.triangles.Length; i += 3)
		{
			vh.AddTriangle(_sprite.triangles[i + 0], _sprite.triangles[i + 1], _sprite.triangles[i + 2]);
		}
	}

	public override void SetNativeSize()
	{
		if (_sprite == null)
		{
			return;
		}
		var rect = _sprite.rect;
		rectTransform.sizeDelta = new Vector2(
			rect.width,
			rect.height);
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(CutoutImageMaking_SetNativeSize), true)]
	public class Inspector : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			var self = (CutoutImageMaking_SetNativeSize)target;
			if (GUILayout.Button("Set Native Size"))
			{
				self.SetNativeSize();
			}
		}
	}
#endif
}