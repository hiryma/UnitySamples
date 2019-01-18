using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// あくまでテスト/デバグ用。性能のことは全く考えていない。事実クッソ遅い。
public class Graph : MaskableGraphic
{
	[SerializeField]
	int _capacity;
	[SerializeField]
	int _seriesCount;
	[SerializeField]
	float _xRange = 1f;
	[SerializeField]
	float _yMin = 0f;
	[SerializeField]
	float _yMax = 1f;
	[SerializeField]
	Color[] _seriesColors;

	struct Data
	{
		// Dataが直接IComparableを実装すると、Array.SortがムチャクチャGC Allocする。
		public class Comparer : IComparer<Data>
		{
			public int Compare(Data a, Data b)
			{
				if (a.x < b.x)
				{
					return -1;
				}
				else if (a.x > b.x)
				{
					return 1;
				}
				else
				{
					return 0;
				}
			}
		}
		public float x, y;
	}

	protected override void Awake()
	{
		base.Awake();
		_counts = new int[_seriesCount];
		_data = new Data[_seriesCount][];
		for (int i = 0; i < _seriesCount; i++)
		{
			_data[i] = new Data[_capacity];
		}
	}

	Data[][] _data;
	IComparer<Data> _comparer = new Data.Comparer();
	int[] _counts;
	float _xEnd;

	public void AddData(float x, float y, int seriesIndex)
	{
		// Unityクラッシュガード
		if (Mathf.Abs(y) > 1e10f)
		{
			return;
		}
		Data data;
		data.x = x;
		data.y = y;
		if (_counts[seriesIndex] < _capacity)
		{
			_data[seriesIndex][_counts[seriesIndex]] = data;
			_counts[seriesIndex]++;
		}
		else
		{
			_data[seriesIndex][0] = data;
		}
		System.Array.Sort(_data[seriesIndex], 0, _counts[seriesIndex], _comparer); // 超遅い
		SetAllDirty();
	}

	public void SetYRange(float min, float max)
	{
		_yMin = min;
		_yMax = max;
	}
	public void SetXEnd(float x)
	{
		_xEnd = x;
		SetAllDirty();
	}

	protected override void OnPopulateMesh(VertexHelper vh)
	{
		vh.Clear();
		// 背景足す
		var size = rectTransform.sizeDelta;
		vh.AddVert(new Vector3(0f, 0f, 0f), new Color(0f, 0f, 0f, 0.5f), Vector2.zero);
		vh.AddVert(new Vector3(0f, size.y, 0f), new Color(0f, 0f, 0f, 0.5f), Vector2.zero);
		vh.AddVert(new Vector3(size.x, 0f, 0f), new Color(0f, 0f, 0f, 0.5f), Vector2.zero);
		vh.AddVert(new Vector3(size.x, size.y, 0f), new Color(0f, 0f, 0f, 0.5f), Vector2.zero);
		vh.AddTriangle(0, 1, 2);
		vh.AddTriangle(1, 2, 3);
		// 頂点追加
		// [yMin, yMax] -> [0, height]
		// [_xBegin, _xBegin+_xRange] -> [0, width]
		// に射影する変換は、
		// x' = (x - _xBegin) * width / _xRange
		// y' = (y - yMin) * height / (yMax - yMin)
		float xBegin = _xEnd - _xRange;
		var vertexStart = 4;
		for (int series = 0; series < _seriesCount; series++)
		{
			for (int i = 0; i < _counts[series]; i++)
			{
				if ((_data[series][i].x < xBegin) || (_data[series][i].x > _xEnd))
				{
					continue;
				}
				var px = (_data[series][i].x - xBegin) * size.x / _xRange;
				var py = (_data[series][i].y - _yMin) * size.y / (_yMax - _yMin);
				vh.AddVert(new Vector3(px, py - 1f, 0f), _seriesColors[series], Vector2.zero);
				vh.AddVert(new Vector3(px, py + 1f, 0f), _seriesColors[series], Vector2.zero); // すごいテキトー
			}
			// インデクス追加
			for (int i = vertexStart; i < (vh.currentVertCount - 2); i += 2)
			{
				vh.AddTriangle(i + 0, i + 1, i + 2);
				vh.AddTriangle(i + 1, i + 2, i + 3);
			}
			vertexStart = vh.currentVertCount;
		}
	}
}