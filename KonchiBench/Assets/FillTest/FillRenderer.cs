using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FillRenderer : MonoBehaviour
{
	[SerializeField]
	MeshFilter _filter;
	[SerializeField]
	MeshRenderer _renderer;

	public void SetCount(int count)
	{
		_newCount = count;
	}
	public void SetMaterial(Material material)
	{
		_renderer.material = material;
	}
	int _currentCount;
	int _newCount;
	Mesh _mesh;
	Vector3 _quadVertices;

	public void ManualStart()
	{
		_mesh = new Mesh();
		_mesh.vertices = new Vector3[4]{
			new Vector3(-1f, -1f, 0f),
			new Vector3(-1f, 1f, 0f),
			new Vector3(1f, -1f, 0f),
			new Vector3(1f, 1f, 0f),
		};
		_mesh.uv = new Vector2[4]{
			new Vector2(0f, 0f),
			new Vector2(0f, 1f),
			new Vector2(1f, 0f),
			new Vector2(1f, 1f),
		};
		_mesh.triangles = new int[]{};
		_filter.mesh = _mesh;
	}

	public void ManualUpdate()
	{
		if (_newCount != _currentCount)
		{
			_currentCount = _newCount;
			var indices = new int[_currentCount * 6];
			var pos = 0;
			for (int i = 0; i < _currentCount; i++)
			{
				indices[pos + 0] = 0;
				indices[pos + 1] = 1;
				indices[pos + 2] = 2;
				indices[pos + 3] = 2;
				indices[pos + 4] = 1;
				indices[pos + 5] = 3;
				pos += 6;
			}
			_mesh.triangles = indices;
		}
	}
}
