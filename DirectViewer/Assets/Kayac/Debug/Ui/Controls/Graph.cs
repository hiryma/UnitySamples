using System.Collections.Generic;
using UnityEngine;

namespace Kayac.Debug.Ui
{
    //　毎フレーム全データセットする前提で、時間で流れていくグラフ
    public class Graph : Control
    {
        struct Series
        {
            public List<Data> data;
            public int dataBegin;
            public Color32 color;
        }

        struct Data
        {
            public Data(float time, float value)
            {
                this.time = time;
                this.value = value;
            }
            public float time;
            public float value;
        }

        readonly float duration;
        readonly List<Series> seriesList;
        float yCenter;
        float yCenterGoal;
        float yWidth;
        float yWidthGoal;

        public Graph(
            float duration,
            float width,
            float height) : base("Graph")
        {
            this.duration = duration;
            BackgroundEnabled = true;
            BorderEnabled = true;

            SetSize(width, height);
            seriesList = new List<Series>();
            yCenter = yCenterGoal = 0f;
            yWidth = yWidthGoal = 1f;
        }

        public int AddSeries(Color32 color)
        {
            Series series;
            series.color = color;
            series.data = new List<Data>();
            series.dataBegin = 0;
            seriesList.Add(series);
            return seriesList.Count - 1;
        }

        public void AddData(int seriesIndex, float v)
        {
            UnityEngine.Debug.Assert(seriesIndex < seriesList.Count, "call AddSeries(). invalid seriesIndex.");
            var series = seriesList[seriesIndex];
            var now = Time.time;
            series.data.Add(new Data(now, v));
        }

        float CalcXScale()
        {
            float netWidth = Width - (2f * BorderWidth);
            float xScale = netWidth / duration;
            return xScale;
        }

        public override void Update(float deltaTime)
        {
            // 幅と中心の調整
            yWidth += (yWidthGoal - yWidth) * deltaTime * 4f;
            yCenter += (yCenterGoal - yCenter) * deltaTime * 4f;
        }

        public override void Draw(
            float offsetX,
            float offsetY,
            Renderer2D renderer)
        {
            UnityEngine.Profiling.Profiler.BeginSample("DebugUiGraph.Draw");
            var now = Time.time;
            var startTime = now - duration;
            // 単純な折れ線だけとりあえず用意
            float netHeight = Height - (2f * BorderWidth);
            float xScale = CalcXScale();
            float xOffset = offsetX + LocalLeftX + BorderWidth;
            float yScale = -netHeight / yWidth;
            float yOffset = offsetY + LocalTopY + Height - BorderWidth - (netHeight * 0.5f);
            float yMin = float.MaxValue;
            float yMax = -yMin;

            for (int seriesIndex = 0; seriesIndex < seriesList.Count; seriesIndex++)
            {
                var series = seriesList[seriesIndex];
                int dst = 0;
                int dataCount = series.data.Count;
                renderer.Color = series.color;
                // 最初のデータを打ち込むところまでまず回す
                UnityEngine.Profiling.Profiler.BeginSample("DebugUiGraph.Draw FirstLine");
                int dataIndex = 0;
                while (dataIndex < (dataCount - 1))
                {
                    series.data[dst] = series.data[dataIndex];
                    var d0 = series.data[dataIndex];
                    var d1 = series.data[dataIndex + 1];
                    dataIndex++;
                    if ((d0.time >= startTime) && (d1.time >= startTime)) // どちらか範囲内なら描画
                    {
                        var x0 = ((d0.time - startTime) * xScale) + xOffset;
                        var x1 = ((d1.time - startTime) * xScale) + xOffset;
                        var y0 = ((d0.value - yCenter) * yScale) + yOffset;
                        var y1 = ((d1.value - yCenter) * yScale) + yOffset;
                        renderer.AddLine(x0, y0, x1, y1, 1f);
                        dst++;
                        yMin = Mathf.Min(yMin, d0.value);
                        yMax = Mathf.Max(yMax, d0.value);
                        break;
                    }
                }
                UnityEngine.Profiling.Profiler.EndSample();

                // 続き描画
                UnityEngine.Profiling.Profiler.BeginSample("DebugUiGraph.Draw FollowingLines");
                while (dataIndex < (dataCount - 1))
                {
                    series.data[dst] = series.data[dataIndex];
                    var d0 = series.data[dataIndex];
                    var d1 = series.data[dataIndex + 1];
                    dataIndex++;
                    var x1 = ((d1.time - startTime) * xScale) + xOffset;
                    var y1 = ((d1.value - yCenter) * yScale) + yOffset;
                    renderer.ContinueLine(x1, y1, 1f);
                    dst++;
                    yMin = Mathf.Min(yMin, d0.value);
                    yMax = Mathf.Max(yMax, d0.value);
                }
                UnityEngine.Profiling.Profiler.EndSample();

                if (dataCount > 0)
                {
                    var last = series.data[dataCount - 1];
                    series.data[dst] = last;
                    dst++;
                    series.data.RemoveRange(dst, dataCount - dst);
                    yMin = Mathf.Min(yMin, last.value);
                    yMax = Mathf.Max(yMax, last.value);
                }
            }

            if (yMin != float.MaxValue) // データがある
            {
                yCenterGoal = (yMin + yMax) * 0.5f;
                if (yMin != yMax) // 最低2種以上値がある
                {
                    yWidthGoal = (yMax - yMin);
                }
            }
            yMin = yCenter - (yWidth * 0.5f);
            yMax = yCenter + (yWidth * 0.5f);

            renderer.Color = new Color32(255, 255, 255, 255);
            renderer.AddText(
                yMax.ToString("F3"),
                offsetX + LocalLeftX + BorderWidth,
                offsetY + LocalTopY + BorderWidth,
                10f);
            renderer.AddText(
                yMin.ToString("F3"),
                offsetX + LocalLeftX + BorderWidth,
                offsetY + LocalTopY + BorderWidth + netHeight - 10f,
                10f);
            UnityEngine.Profiling.Profiler.EndSample();
        }
    }
}
