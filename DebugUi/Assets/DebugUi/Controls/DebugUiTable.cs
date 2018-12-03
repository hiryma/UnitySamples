using UnityEngine;
using System.Collections.Generic;

namespace Kayac
{
	public class DebugUiTable : DebugUiControl
	{
		public class Cells
		{
			public Cells(int rowCount, int colCount)
			{
				_cells = new Cell[rowCount, colCount];
			}
			public Cell this[int row, int col]
			{
				get
				{
					return _cells[row, col];
				}
				set
				{
					_cells[row, col].Asign(ref value);
				}
			}
			private Cell[,] _cells;
		}

		public struct Cell
		{
			private enum Type
			{
				Int,
				Float,
				Bool,
				String,
			}
			public Cell(int intValue)
			{
				_type = Type.Int;
				_float = 0f;
				_bool = false;
				_int = intValue;
				text = null;
			}

			public Cell(float floatValue)
			{
				_type = Type.Float;
				_float = floatValue;
				_bool = false;
				_int = 0;
				text = null;
			}

			public Cell(string stringValue)
			{
				_type = Type.String;
				_float = 0f;
				_bool = false;
				_int = 0;
				text = stringValue;
			}

			public Cell(bool boolValue)
			{
				_type = Type.Bool;
				_float = 0f;
				_bool = boolValue;
				_int = 0;
				text = null;
			}

			public static implicit operator Cell(int intValue)
			{
				return new Cell(intValue);
			}

			public static implicit operator Cell(float floatValue)
			{
				return new Cell(floatValue);
			}

			public static implicit operator Cell(bool boolValue)
			{
				return new Cell(boolValue);
			}

			public static implicit operator Cell(string stringValue)
			{
				return new Cell(stringValue);
			}

			public void Asign(ref Cell cell)
			{
				// 型が異なる場合、文字列化
				if (_type != cell._type)
				{
					switch (cell._type)
					{
						case Type.Int: text = cell._int.ToString(); break;
						case Type.Float: text = cell._float.ToString("N2"); break;
						case Type.Bool: text = cell._bool.ToString(); break;
						case Type.String: text = cell.text; break;
						default: Debug.Assert(false); break;
					}
					_type = cell._type;
				}
				// 型が等しい場合、値が等しい時のみ文字列化
				else
				{
					switch (cell._type)
					{
						case Type.Int:
							if (_int != cell._int)
							{
								text = cell._int.ToString();
								_int = cell._int;
							}
							break;
						case Type.Float:
							if (_float != cell._float)
							{
								text = cell._float.ToString("N2");
								_float = cell._float;
							}
							break;
						case Type.Bool:
							if (_bool != cell._bool)
							{
								text = cell._bool.ToString();
								_bool = cell._bool;
							}
							break;
						case Type.String:
							text = cell.text;
							break;
						default: Debug.Assert(false); break;
					}
				}
			}

			public string text{ get; private set; }
			private Type _type;
			private float _float;
			private int _int;
			private bool _bool;
		}

		public Color32 textColor{ get; set; }
		public int columnCount{ get; private set; }
		public int rowCount{ get; private set; }
		public Cells cells{ get; private set; }
		private float[] _widths;
		private float[] _heights;
		private float _fontSize;

		public DebugUiTable(
			float fontSize,
			IList<float> widths,
			int rowCount,
			float rowHeight) : base("Table")
		{
			Initialize(fontSize, widths, null, rowCount, rowHeight);
		}

		public DebugUiTable(
			float fontSize,
			IList<float> widths,
			IList<float> heights) : base("Table")
		{
			Initialize(fontSize, widths, heights, 0, 0);
		}

		private void Initialize(
			float fontSize,
			IList<float> widths,
			IList<float> heights,
			int rowCount,
			float rowHeight)
		{
			textColor = new Color32(255, 255, 255, 255);
			_fontSize = fontSize;

			columnCount = widths.Count;
			_widths = new float[columnCount];

			float w = borderWidth;
			for (int i = 0; i < columnCount; i++)
			{
				_widths[i] = widths[i];
				w += widths[i] + borderWidth;
			}

			float h = borderWidth;
			if (heights != null)
			{
				rowCount = heights.Count;
				_heights = new float[rowCount];
				for (int i = 0; i < rowCount; i++)
				{
					_heights[i] = heights[i];
					h += heights[i] + borderWidth;
				}
			}
			else
			{
				_heights = new float[rowCount];
				for (int i = 0; i < rowCount; i++)
				{
					_heights[i] = rowHeight;
					h += rowHeight + borderWidth;
				}
			}
			this.rowCount = rowCount;

			cells = new Cells(rowCount, columnCount);

			SetSize(w, h);
			backgroundEnabled = true;
			borderEnabled = true;
		}

		public override void Draw(
			float offsetX,
			float offsetY,
			DebugPrimitiveRenderer2D renderer)
		{
			// 罫線を描く
			renderer.color = borderColor;
			// まず縦
			int end = _widths.Length - 1; // 最後の右端線は不要
			float x = offsetX + localLeftX + borderWidth;
			float topY = offsetY + localTopY;
			float halfBorderWidth = borderWidth * 0.5f;
			for (int i = 0; i < end; i++)
			{
				x += _widths[i];
				x += halfBorderWidth; // 線の中心までずらす
				renderer.AddVerticalLine(
					x,
					topY,
					height,
					borderWidth);
				x += halfBorderWidth;
			}
			// 次に横
			end = _heights.Length - 1; // 最後の下端線は不要
			float y = offsetY + localTopY + borderWidth;
			float leftX = offsetX + localLeftX;
			for (int i = 0; i < end; i++)
			{
				y += _heights[i];
				y += halfBorderWidth; // 線の中心までずらす
				renderer.AddHorizontalLine(
					leftX,
					y,
					width,
					borderWidth);
				y += halfBorderWidth;
			}

			y = offsetY + localTopY + borderWidth;
			for (int rowIndex = 0; rowIndex < _heights.Length; rowIndex++)
			{
				float cellHeight = _heights[rowIndex];
				x = offsetX + localLeftX + borderWidth;
				for (int colIndex = 0; colIndex < _widths.Length; colIndex++)
				{
					float cellWidth = _widths[colIndex];
					var cell = cells[rowIndex, colIndex];
					if (string.IsNullOrEmpty(cell.text) == false)
					{
						DrawTextMultiLine(
							renderer,
							cell.text,
							textColor,
							_fontSize,
							x,
							y,
							cellWidth,
							cellHeight,
							true);
					}
					x += cellWidth + borderWidth;
				}
				y += cellHeight + borderWidth;
			}
		}
	}
}