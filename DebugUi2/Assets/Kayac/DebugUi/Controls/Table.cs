using UnityEngine;
using System.Collections.Generic;

namespace Kayac.DebugUi
{
    public class Table : Control
    {
        public Color32 TextColor { get; set; }
        public int ColumnCount { get; private set; }
        public int RowCount { get; private set; }
        public Cells Cells { get; private set; }
        float[] _widths;
        float[] _heights;
        float _fontSize;

        public Table(
            float fontSize,
            IList<float> widths,
            int rowCount,
            float rowHeight) : base("Table")
        {
            Initialize(fontSize, widths, null, rowCount, rowHeight);
        }

        public Table(
            float fontSize,
            IList<float> widths,
            IList<float> heights) : base("Table")
        {
            Initialize(fontSize, widths, heights, 0, 0);
        }

        void Initialize(
            float fontSize,
            IList<float> widths,
            IList<float> heights,
            int rowCount,
            float rowHeight)
        {
            TextColor = new Color32(255, 255, 255, 255);
            _fontSize = fontSize;

            ColumnCount = widths.Count;
            _widths = new float[ColumnCount];

            float w = BorderWidth;
            for (int i = 0; i < ColumnCount; i++)
            {
                _widths[i] = widths[i];
                w += widths[i] + BorderWidth;
            }

            float h = BorderWidth;
            if (heights != null)
            {
                rowCount = heights.Count;
                _heights = new float[rowCount];
                for (int i = 0; i < rowCount; i++)
                {
                    _heights[i] = heights[i];
                    h += heights[i] + BorderWidth;
                }
            }
            else
            {
                _heights = new float[rowCount];
                for (int i = 0; i < rowCount; i++)
                {
                    _heights[i] = rowHeight;
                    h += rowHeight + BorderWidth;
                }
            }
            this.RowCount = rowCount;

            Cells = new Cells(rowCount, ColumnCount);

            SetSize(w, h);
            BackgroundEnabled = true;
            BorderEnabled = true;
        }

        public override void Draw(
            float offsetX,
            float offsetY,
            Renderer2D renderer)
        {
            // 罫線を描く
            renderer.Color = BorderColor;
            // まず縦
            int end = _widths.Length - 1; // 最後の右端線は不要
            float x = offsetX + LocalLeftX + BorderWidth;
            float topY = offsetY + LocalTopY;
            float halfBorderWidth = BorderWidth * 0.5f;
            for (int i = 0; i < end; i++)
            {
                x += _widths[i];
                x += halfBorderWidth; // 線の中心までずらす
                renderer.AddVerticalLine(
                    x,
                    topY,
                    Height,
                    BorderWidth);
                x += halfBorderWidth;
            }
            // 次に横
            end = _heights.Length - 1; // 最後の下端線は不要
            float y = offsetY + LocalTopY + BorderWidth;
            float leftX = offsetX + LocalLeftX;
            for (int i = 0; i < end; i++)
            {
                y += _heights[i];
                y += halfBorderWidth; // 線の中心までずらす
                renderer.AddHorizontalLine(
                    leftX,
                    y,
                    Width,
                    BorderWidth);
                y += halfBorderWidth;
            }

            y = offsetY + LocalTopY + BorderWidth;
            for (int rowIndex = 0; rowIndex < _heights.Length; rowIndex++)
            {
                float cellHeight = _heights[rowIndex];
                x = offsetX + LocalLeftX + BorderWidth;
                for (int colIndex = 0; colIndex < _widths.Length; colIndex++)
                {
                    float cellWidth = _widths[colIndex];
                    var cell = Cells[rowIndex, colIndex];
                    if (string.IsNullOrEmpty(cell.Text) == false)
                    {
                        renderer.Color = TextColor;
                        renderer.AddText(
                            cell.Text,
                            x,
                            y,
                            _fontSize);
                    }
                    x += cellWidth + BorderWidth;
                }
                y += cellHeight + BorderWidth;
            }
        }
    }

    public class Cells
    {
        public Cells(int rowCount, int colCount)
        {
            cells = new Cell[rowCount, colCount];
        }
        public Cell this[int row, int col]
        {
            get
            {
                return cells[row, col];
            }
            set
            {
                cells[row, col].Asign(ref value);
            }
        }
        readonly Cell[,] cells;
    }

    public struct Cell
    {
        enum Type
        {
            Int,
            Float,
            Bool,
            String,
        }
        public Cell(int intValue)
        {
            type = Type.Int;
            floatValue = 0f;
            boolValue = false;
            this.intValue = intValue;
            Text = null;
        }

        public Cell(float floatValue)
        {
            type = Type.Float;
            this.floatValue = floatValue;
            boolValue = false;
            intValue = 0;
            Text = null;
        }

        public Cell(string stringValue)
        {
            type = Type.String;
            floatValue = 0f;
            boolValue = false;
            intValue = 0;
            Text = stringValue;
        }

        public Cell(bool boolValue)
        {
            type = Type.Bool;
            floatValue = 0f;
            this.boolValue = boolValue;
            intValue = 0;
            Text = null;
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
            if (type != cell.type)
            {
                switch (cell.type)
                {
                    case Type.Int: Text = cell.intValue.ToString(); break;
                    case Type.Float: Text = cell.floatValue.ToString("N2"); break;
                    case Type.Bool: Text = cell.boolValue.ToString(); break;
                    case Type.String: Text = cell.Text; break;
                    default: UnityEngine.Debug.Assert(false); break;
                }
                type = cell.type;
            }
            // 型が等しい場合、値が等しい時のみ文字列化
            else
            {
                switch (cell.type)
                {
                    case Type.Int:
                        if (intValue != cell.intValue)
                        {
                            Text = cell.intValue.ToString();
                            intValue = cell.intValue;
                        }
                        break;
                    case Type.Float:
                        if (floatValue != cell.floatValue)
                        {
                            Text = cell.floatValue.ToString("N2");
                            floatValue = cell.floatValue;
                        }
                        break;
                    case Type.Bool:
                        if (boolValue != cell.boolValue)
                        {
                            Text = cell.boolValue.ToString();
                            boolValue = cell.boolValue;
                        }
                        break;
                    case Type.String:
                        Text = cell.Text;
                        break;
                    default: UnityEngine.Debug.Assert(false); break;
                }
            }
        }

        public string Text { get; private set; }
        Type type;
        float floatValue;
        int intValue;
        bool boolValue;
    }
}