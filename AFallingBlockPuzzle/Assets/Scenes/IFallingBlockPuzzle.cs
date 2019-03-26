using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IFallingBlockPuzzle
{
	/// 落下間隔(秒)
	float fallInterval { get; set; }
	/// ゲームオーバーになったか否か
	bool isGameOver { get; }
	int activeShapeId{ get; }
	/// 直前のUpdateで消えた行があればそのyが入る
	IList<int> erasedYList { get; }
	/// すでに固定したマスが、元々どの形状だったかを返す。
	int GetShapeId(int x, int y);
	/// 空いていればtrueを返す
	bool IsVacant(int x, int y);
	/// ゲームを初期状態に戻す
	void Reset();
	void MoveLeft();
	void MoveRight();
	void RotateClockwise();
	void MoveDown();
	/// 一気に着地させる
	void Land();
	/// 毎フレーム1回呼ぶこと
	void Update(float deltaTime);
	/// 操作中のブロックのマスの座標を返す
	IEnumerable<Position> GetActiveCells();
}

// intの2次元ベクトル
public struct Position : System.IComparable<Position>, System.IEquatable<Position>
{
	public int x;
	public int y;

	public Position(int x, int y)
	{
		this.x = x;
		this.y = y;
	}
	public override string ToString()
	{
		return "(" + x + "," + y + ")";
	}
	public int CompareTo(Position other)
	{
		if (this.x != other.x)
		{
			return this.x - other.x;
		}
		else
		{
			return this.y - other.y;
		}
	}
	public bool Equals(Position other)
	{
		return this.CompareTo(other) == 0;
	}
	public override int GetHashCode()
	{
		return x | (y << 16);
	}
}