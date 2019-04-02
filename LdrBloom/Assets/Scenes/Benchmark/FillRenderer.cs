using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FillRenderer : MonoBehaviour
{
	[SerializeField]
	MeshFilter _filter;
	[SerializeField]
	MeshRenderer _renderer;

	public void SetCount(float count)
	{
		_newCount = count;
	}
	public void SetMaterial(Material material)
	{
		_renderer.material = material;
	}
	float _currentCount;
	float _newCount;
	Mesh _mesh;
	Vector3 _quadVertices;
	int[] _indices;
	Vector3[] _vertices;
	Vector2[] _uv;


	public void ManualStart()
	{
		_mesh = new Mesh();
		_vertices = new Vector3[6]{
			new Vector3(-1f, -1f, 0f),
			new Vector3(-1f, 1f, 0f),
			new Vector3(1f, -1f, 0f),
			new Vector3(1f, 1f, 0f),
			// 以下2つは動的に書き換える
			new Vector3(1f, -1f, 0f),
			new Vector3(1f, 1f, 0f),
		};
		_uv = new Vector2[6]{
			new Vector2(0f, 0f),
			new Vector2(0f, 1f),
			new Vector2(1f, 0f),
			new Vector2(1f, 1f),
			// 以下2つは動的に書き換える
			new Vector2(1f, 0f),
			new Vector2(1f, 1f),
		};
		_filter.mesh = _mesh;
	}

	public void ManualUpdate()
	{
		int newCountCeil = Mathf.CeilToInt(_newCount);
		// 基本小数部だが、ピッタリ整数だった場合は1にする
		float u1 = _newCount - (float)(newCountCeil - 1);
		var x1 = (u1 * 2f) - 1f;
		// 頂点を修正
		_vertices[4] = new Vector3(x1, -1f, 0f);
		_vertices[5] = new Vector3(x1, 1f, 0f);
		_mesh.vertices = _vertices;
		_uv[4] = new Vector2(u1, 0f);
		_uv[5] = new Vector2(u1, 1f);
		_mesh.uv = _uv;

		int currentCountCeil = Mathf.CeilToInt(_currentCount);
		if (newCountCeil != currentCountCeil) // 数が変わった時には配列を再確保
		{
			_indices = new int[newCountCeil * 6];
			var pos = 0;
			for (int i = 0; i < (newCountCeil - 1); i++)
			{
				_indices[pos + 0] = 0;
				_indices[pos + 1] = 1;
				_indices[pos + 2] = 2;
				_indices[pos + 3] = 2;
				_indices[pos + 4] = 1;
				_indices[pos + 5] = 3;
				pos += 6;
			}
			// 最後の矩形は2つ目の矩形頂点を参照
			_indices[pos + 0] = 0;
			_indices[pos + 1] = 1;
			_indices[pos + 2] = 4;
			_indices[pos + 3] = 4;
			_indices[pos + 4] = 1;
			_indices[pos + 5] = 5;
		}
		_mesh.triangles = _indices;
		_currentCount = _newCount;
	}
}
