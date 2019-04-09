using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AFallingBlockPuzzle : IFallingBlockPuzzle
{
	// public
	public float fallInterval { get; set; }
	public bool isGameOver { get{ return GetCell(_startX, _startY) != CellVacant; } }
	public int activeShapeId { get{ return _shapeId; } }
	public IList<int> erasedYList{ get; private set; }

	public AFallingBlockPuzzle(
		int width,
		int height,
		int[] blockSizes,
		bool cornerAllowed,
		bool loopX)
	{
		_blockSizes = new int[blockSizes.Length];
		int maxBlockSize = 0;
		for (int i = 0; i < blockSizes.Length; i++)
		{
			_blockSizes[i] = blockSizes[i];
			if (maxBlockSize < blockSizes[i])
			{
				maxBlockSize = blockSizes[i];
			}
		}
		this.erasedYList = new List<int>();
		_width = width;
		_height = height;
		_extendedHeight = height + ((maxBlockSize + 1) / 2); // include bottom and upper dummy lines
		_startY = height - 1;
		_startX = _width / 2;
		_loopX = loopX;
		_cells = new int[_width * _extendedHeight];
		_tmpShape = new Shape(maxBlockSize);
		_shapes = new List<Shape>();
		_random = new System.Random();
		var generator = new ShapeGenerator(maxBlockSize);
		generator.Generate(_shapes, _blockSizes, cornerAllowed);
		Reset();
	}

	public int GetShapeId(int x, int y)
	{
		return GetCell(x, y);
	}

	public bool IsVacant(int x, int y)
	{
		return GetCell(x, y) == CellVacant;
	}

	public void Reset()
	{
		// fill vacant cells
		for (int y = 0; y < _extendedHeight; y++)
		{
			FillVacantLine(y);
		}
		this.fallInterval = float.MaxValue; // デフォルトだと永遠に落ちない
		GenerateBlock();
	}

	public void MoveLeft()
	{
		_dx = -1;
	}

	public void MoveRight()
	{
		_dx = 1;
	}

	public void RotateClockwise()
	{
		_dRotation = 1;
	}

	public void MoveDown()
	{
		_dy = -1;
	}

	public void Land()
	{
		_dy = -_height;
	}

	public void Update(float deltaTime)
	{
		this.erasedYList.Clear();
		if (this.isGameOver)
		{
			return;
		}
		// 横移動及び回転判定
		_x += _dx;
		_rotation += _dRotation;
		TransformShape(_tmpShape, _shapes[_shapeId], _x, _y, _rotation);
		if (CheckOverlap(_tmpShape)) // 重なるならこの移動を棄却
		{
Debug.Log("Hit in Update : " + _x + " + " + _dx);
			_x -= _dx;
			_rotation -= _dRotation;
		}
		// 縦移動量決定
		// 縦移動は当たれば着地を発生させる
		_fallTimer += deltaTime;
		int fallCount = (int)(_fallTimer / this.fallInterval); // 同時に2マス以上落ちることもありうる
		_dy -= fallCount;
		_fallTimer -= (float)fallCount * this.fallInterval;

		bool landed = false;
		// 落下ループ
		while (_dy != 0)
		{
			_y += -1;
			_dy -= -1;
			TransformShape(_tmpShape, _shapes[_shapeId], _x, _y, _rotation);
			if (CheckOverlap(_tmpShape)) // 重なるならこの移動を棄却
			{
				_y -= -1;
				Stack();
				landed = true;
				break;
			}
		}

		// 削除
		if (landed)
		{
			TryErase();
		}
		_dx = _dy = _dRotation = 0;
		// ループ処理
		if (_loopX)
		{
			if (_x < 0)
			{
				_x += _width;
			}
			else if (_x >= _width)
			{
				_x -= _width;
			}
		}
	}

	public IEnumerable<Position> GetActiveCells()
	{
		TransformShape(_tmpShape, _shapes[_shapeId], _x, _y, _rotation);
		for (int i = 0; i < _tmpShape.positions.Count; i++)
		{
			var position = _tmpShape.positions[i];
			if ((position.y >= 0) && (position.y <= _startY) && (position.x >= 0) && (position.x < _width))
			{
				yield return position;
			}
		}
	}

	// ----- 以下private ---------------------
	class Shape : System.IComparable<Shape>, System.IEquatable<Shape>
	{
		public Shape(int capacity)
		{
			this.positions = new List<Position>(capacity);
		}
		public void Translate(int x, int y)
		{
			for (int i = 0; i < this.positions.Count; i++)
			{
				var p = this.positions[i];
				p.x += x;
				p.y += y;
				this.positions[i] = p;
			}
		}
		public void Rotate90()
		{
			for (int i = 0; i < this.positions.Count; i++)
			{
				var p = this.positions[i];
				this.positions[i] = new Position(-p.y, p.x);
			}
		}
		public void Rotate180()
		{
			for (int i = 0; i < this.positions.Count; i++)
			{
				var p = this.positions[i];
				p.x = -p.x;
				p.y = -p.y;
				this.positions[i] = p;
			}
		}
		public override string ToString()
		{
			var ret = "[Shape] ";
			for (int i = 0; i < this.positions.Count; i++)
			{
				var p = this.positions[i];
				ret += "(" + p.x + "," + p.y + ") ";
			}
			return ret;
		}

		public void CopyFrom(Shape from)
		{
			this.positions.Clear();
			for (int i = 0; i < from.positions.Count; i++)
			{
				this.positions.Add(from.positions[i]);
			}
		}
		public int CompareTo(Shape other)
		{
			if (this.positions.Count != other.positions.Count)
			{
				return this.positions.Count - other.positions.Count;
			}
			for (int i = 0; i < this.positions.Count; i++)
			{
				var cmp = this.positions[i].CompareTo(other.positions[i]);
				if (cmp != 0)
				{
					return cmp;
				}
			}
			return 0;
		}
		public bool Equals(Shape other)
		{
			return this.CompareTo(other) == 0;
		}
		public override int GetHashCode()
		{
			int ret = 0;
			for (int i = 0; i < positions.Count; i++)
			{
				ret ^= positions[i].GetHashCode();
			}
			return ret;
		}
		public List<Position> positions;
	}
	const int CellVacant = -1; // vacant cell
	int[] _cells;
	int _extendedHeight;
	int _width;
	int _height;
	int[] _blockSizes;
	int _startX;
	int _startY;
	int _x; // moving block center X
	int _y; // moving block center Y
	int _rotation; // moving block rotation. degree = _rotation*90
	int _shapeId;
	int _dx;
	int _dy;
	int _dRotation;
	float _fallTimer;
	List<Shape> _shapes;
	Shape _tmpShape; // transformed shape
	System.Random _random;
	bool _loopX;

	void GenerateBlock()
	{
		_x = _startX;
		_y = _startY;
		_rotation = 0;
		_shapeId = _random.Next(0, _shapes.Count);
	}

	void TryErase()
	{
		int dstLine = 0;
		for (int y = 0; y < _extendedHeight; y++)
		{
			CopyLine(dstLine, y);
			if (!IsLineFilled(y))
			{
				dstLine++;
			}
			else
			{
				this.erasedYList.Add(y);
			}
		}
		for (int y = dstLine; y < _extendedHeight; y++)
		{
			FillVacantLine(y);
		}
	}

	void FillVacantLine(int y)
	{
		for (int x = 0; x < _width; x++)
		{
			SetCell(x, y, CellVacant);
		}
	}

	void CopyLine(int toY, int fromY)
	{
		for (int x = 0; x < _width; x++)
		{
			SetCell(x, toY, GetCell(x, fromY));
		}
	}

	bool IsLineFilled(int y)
	{
		for (int x = 0; x < _width; x++)
		{
			if (GetCell(x, y) == CellVacant)
			{
				return false;
			}
		}
		return true;
	}

	int GetCell(int x, int y)
	{
		return _cells[(y * _width) + x];
	}

	void SetCell(int x, int y, int type)
	{
		_cells[(y * _width) + x] = type;
	}

	void Stack()
	{
		TransformShape(_tmpShape, _shapes[_shapeId], _x, _y, _rotation);
		for (int i = 0; i < _tmpShape.positions.Count; i++)
		{
			var p = _tmpShape.positions[i];
			SetCell(p.x, p.y, _shapeId);
		}

		// ゲームオーバーでなければ次を出す
		if (!this.isGameOver)
		{
			GenerateBlock();
		}
	}

	bool CheckOverlap(Shape shape)
	{
		for (int i = 0; i < _tmpShape.positions.Count; i++)
		{
			var position = _tmpShape.positions[i];
			if (CheckOverlap(position))
			{
				return true;
			}
		}
		return false;
	}

	bool CheckOverlap(Position position)
	{
		if (!_loopX) // 横方向ループしない場合、X範囲外なら当たり
		{
			if ((position.x < 0)
				|| (position.x >= _width))
			{
				return true;
			}
		}

		// Y範囲外なら当たり
		if ((position.y < 0)
			|| (position.y >= _extendedHeight)) // 範囲外には無限に壁があるものと考えるので「当たり」
		{
			return true;
		}
		return (GetCell(position.x, position.y) != CellVacant); // 空きでなければ当たり
	}

	void TransformShape(Shape shapeOut, Shape shape, int x, int y, int rotation)
	{
		shapeOut.CopyFrom(shape);
		rotation %= 4;
		for (int i = 0; i < rotation; i++)
		{
			shapeOut.Rotate90();
		}
		shapeOut.Translate(x, y);
		if (_loopX)
		{
			for (int i = 0; i < shapeOut.positions.Count; i++)
			{
				var p = shapeOut.positions[i];
				if (p.x < 0)
				{
					p.x += _width;
				}
				else if (p.x >= _width)
				{
					p.x -= _width;
				}
				shapeOut.positions[i] = p;
			}
		}
	}

	// 形状リストを生成するクラス。ゲームをいじるに際して、ここから下に触る必要はない。
	class ShapeGenerator
	{
		HashSet<Shape> _shapes;
		Shape[] _tmpShapes; // Normalizeで使う
		List<Position> _tmpPositions; // GetAddPositionsで使う
		Shape[] _shapeStack; // AddPositionToShapeRecursiveで使う
		int _shapeStackPos;

		public ShapeGenerator(int maxBlockSize)
		{
			_shapes = new HashSet<Shape>();
			_tmpShapes = new Shape[4];
			for (int i = 0; i < _tmpShapes.Length; i++)
			{
				_tmpShapes[i] = new Shape(maxBlockSize);
			}
			_tmpPositions = new List<Position>();
			_shapeStack = new Shape[maxBlockSize];
			for (int i = 0; i < _shapeStack.Length; i++)
			{
				_shapeStack[i] = new Shape(maxBlockSize);
			}
		}

		public void Generate(List<Shape> shapesOut, int[] blockSizes, bool cornerAllowed)
		{
			int maxBlockSize = 0;
			for (int i = 0; i < blockSizes.Length; i++)
			{
				maxBlockSize = System.Math.Max(maxBlockSize, blockSizes[i]);
			}
			_shapes.Clear();
			var t0 = System.DateTime.Now;

			_shapeStack[_shapeStackPos].positions.Add(new Position(0, 0));
			_shapeStackPos++;
			AddPositionToShapeRecursive(maxBlockSize, cornerAllowed);

			// リストにコピー
			shapesOut.Clear();
			foreach (var shape in _shapes)
			{
				for (int i = 0; i < blockSizes.Length; i++)
				{
					if (shape.positions.Count == blockSizes[i])
					{
						shapesOut.Add(shape);
						break;
					}
				}
			}
			var t1 = System.DateTime.Now;
			Debug.Log("ShapeGenerator.Generate() takes " + (t1 - t0).TotalMilliseconds + " msecs. " + shapesOut.Count + " shapes generated.");
		}

		void AddPositionToShapeRecursive(
			int blockSize,
			bool cornerAllowed)
		{
			var shape = _shapeStack[_shapeStackPos - 1];
			NormalizeShape(shape);
			if (!_shapes.Contains(shape))
			{
				var copied = new Shape(blockSize);
				copied.CopyFrom(shape);
				_shapes.Add(copied); // 途中経過も入れておく
				if (shape.positions.Count < blockSize) // まだであれば再帰
				{
					var addPositions = GetAddPositions(shape, cornerAllowed);
					foreach (var addPosition in addPositions)
					{
						var shapeCopy = _shapeStack[_shapeStackPos];
						_shapeStackPos++;
						shapeCopy.CopyFrom(shape);
						shapeCopy.positions.Add(addPosition);
						AddPositionToShapeRecursive(blockSize, cornerAllowed);
						_shapeStackPos--;
					}
				}
			}
		}

		HashSet<Position> GetAddPositions(Shape shape, bool cornerAllowed)
		{
			// まず衝突とか無視してリストに候補を全部つっこむ
			_tmpPositions.Clear();
			for (int i = 0; i < shape.positions.Count; i++)
			{
				var position = shape.positions[i];
				_tmpPositions.Add(new Position(position.x - 1, position.y));
				_tmpPositions.Add(new Position(position.x + 1, position.y));
				_tmpPositions.Add(new Position(position.x, position.y - 1));
				_tmpPositions.Add(new Position(position.x, position.y + 1));
				if (cornerAllowed)
				{
					_tmpPositions.Add(new Position(position.x - 1, position.y - 1));
					_tmpPositions.Add(new Position(position.x - 1, position.y + 1));
					_tmpPositions.Add(new Position(position.x + 1, position.y - 1));
					_tmpPositions.Add(new Position(position.x + 1, position.y + 1));
				}
			}

			var addPositions = new HashSet<Position>();
			for (int i = 0; i < _tmpPositions.Count; i++)
			{
				if (!addPositions.Contains(_tmpPositions[i]))
				{
					addPositions.Add(_tmpPositions[i]);
				}
			}

			// 自分ブロックは除く
			for (int i = 0; i < shape.positions.Count; i++)
			{
				addPositions.Remove(shape.positions[i]);
			}
			return addPositions;
		}

		/*
		初期配置として都合が良い形で、かつ、比較に都合が良いように頂点をソートして並べる。
		1.縦長か横長かを識別し、縦長なら90度回す。
		2.中心を計算し、重心が0からずれていれば移動させる
		3.頂点をソートする(x、yの順)
		*/
		void NormalizeShape(Shape shape)
		{
			// まずX,Yの幅を計算する
			int minX, maxX, minY, maxY;
			minX = minY = int.MaxValue;
			maxX = maxY = int.MinValue;
			for (int i = 0; i < shape.positions.Count; i++)
			{
				var p = shape.positions[i];
				minX = System.Math.Min(minX, p.x);
				maxX = System.Math.Max(maxX, p.x);
				minY = System.Math.Min(minY, p.y);
				maxY = System.Math.Max(maxY, p.y);
			}
			var width = maxX - minX;
			var height = maxY - minY;
			var tmpShapeCount = 0;
			if (width > height) // すでに横長であれば、0度と180度を加える
			{
				_tmpShapes[0].CopyFrom(shape);
				_tmpShapes[1].CopyFrom(_tmpShapes[0]);
				_tmpShapes[1].Rotate180();
				tmpShapeCount = 2;
			}
			else if (height > width) // 縦長であれば、90度と270度を加える
			{
				_tmpShapes[0].CopyFrom(shape);
				_tmpShapes[0].Rotate90();
				_tmpShapes[1].CopyFrom(_tmpShapes[0]);
				_tmpShapes[1].Rotate180();
				tmpShapeCount = 2;
			}
			else // 縦幅横幅が等しければ、4回転全て入れる
			{
				_tmpShapes[0].CopyFrom(shape);
				_tmpShapes[1].CopyFrom(_tmpShapes[0]);
				_tmpShapes[1].Rotate90();
				_tmpShapes[2].CopyFrom(_tmpShapes[1]);
				_tmpShapes[2].Rotate90();
				_tmpShapes[3].CopyFrom(_tmpShapes[2]);
				_tmpShapes[3].Rotate90();
				tmpShapeCount = 4;
			}

			// リスト内全ての位置ずらしを行う
			for (int shapeIndex = 0; shapeIndex < tmpShapeCount; shapeIndex++)
			{
				minX = minY = int.MaxValue;
				maxX = maxY = int.MinValue;
				for (int positionIndex = 0; positionIndex < shape.positions.Count; positionIndex++)
				{
					var p = _tmpShapes[shapeIndex].positions[positionIndex];
					minX = System.Math.Min(minX, p.x);
					maxX = System.Math.Max(maxX, p.x);
					minY = System.Math.Min(minY, p.y);
					maxY = System.Math.Max(maxY, p.y);
				}
				// x,yの最小値が-幅/2になるように移動する
				int dx = -((maxX - minX) / 2) - minX;
				int dy = -((maxY - minY) / 2) - minY;
				_tmpShapes[shapeIndex].Translate(dx, dy);
				_tmpShapes[shapeIndex].positions.Sort((a, b) => a.CompareTo(b)); // 頂点をソートする
			}
			// ソート順最小のものを選ぶ
			var minIndex = 0;
			for (int i = 1; i < tmpShapeCount; i++)
			{
				if (_tmpShapes[i].CompareTo(_tmpShapes[minIndex]) < 0)
				{
					minIndex = i;
				}
			}
			shape.CopyFrom(_tmpShapes[minIndex]);
		}
	}
}
