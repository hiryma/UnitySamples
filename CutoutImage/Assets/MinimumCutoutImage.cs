using UnityEngine;
using UnityEngine.UI;

public class MinimumCutoutImage : MaskableGraphic
{
	[SerializeField]
	Sprite _sprite;
	public override Texture mainTexture { get { return (_sprite != null) ? _sprite.texture : null; } }

	protected override void OnPopulateMesh(VertexHelper vh)
	{
		vh.Clear();
		if (_sprite == null){ return; }
		for (int i = 0; i < _sprite.vertices.Length; i++)
		{
			vh.AddVert(_sprite.vertices[i] * _sprite.pixelsPerUnit, this.color, _sprite.uv[i]);
		}

		for (int i = 0; i < _sprite.triangles.Length; i += 3)
		{
			vh.AddTriangle(_sprite.triangles[i + 0], _sprite.triangles[i + 1], _sprite.triangles[i + 2]);
		}
	}
}