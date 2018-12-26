using UnityEngine;
using UnityEngine.UI;

// あくまでテスト/デバグ用。性能のことは全く考えていない。事実クッソ遅い。
public class Graph : MaskableGraphic
{
	[SerializeField]
	int _capacity;
	[SerializeField]
	float _xRange = 1f;
	[SerializeField]
	float _yMin = 0f;
	[SerializeField]
	float _yMax = 1f;

	struct Data : System.IComparable<Data>
	{
		public int CompareTo(Data other)
		{
			if (x < other.x)
			{
				return -1;
			}
			else if (x > other.x)
			{
				return 1;
			}
			else
			{
				return 0;
			}
		}
		public float x, y;
	}

	Data[] _data;
	int _count;
	float _xEnd;

	public void AddData(float x, float y)
	{
		if (_data == null)
		{
			_data = new Data[_capacity];
		}
		else if (_data.Length < _capacity) // 拡張処理
		{
			var newData = new Data[_capacity];
			for (int i = 0; i < _count; i++)
			{
				newData[i] = _data[i];
			}
			_data = newData;
		}
		Data data;
		data.x = x;
		data.y = y;
		if (_count < _capacity)
		{
			_data[_count] = data;
			_count++;
		}
		else
		{
			_data[0] = data;
		}
		System.Array.Sort(_data, 0, _count); // 超遅い
		SetAllDirty();
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
		for (int i = 0; i < _count; i++)
		{
			if ((_data[i].x < xBegin) || (_data[i].x > _xEnd))
			{
				continue;
			}
			var px = (_data[i].x - _xEnd + _xRange) * size.x / _xRange;
			var py = (_data[i].y - _yMin) * size.y / (_yMax - _yMin);
			vh.AddVert(new Vector3(px, py - 1f, 0f), this.color, Vector2.zero);
			vh.AddVert(new Vector3(px, py + 1f, 0f), this.color, Vector2.zero); // すごいテキトー
		}
		// インデクス追加
		for (int i = 4; i < (vh.currentVertCount - 2); i += 2)
		{
			vh.AddTriangle(i + 0, i + 1, i + 2);
			vh.AddTriangle(i + 1, i + 2, i + 3);
		}
	}
}