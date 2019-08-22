using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kayac
{
	public class SpectrumViewer : Graphic
	{
		[SerializeField] float sampleCount = 8192;
		[SerializeField] int binCount = 512;
		[SerializeField] int graphCenterFrequency = 1000;
		[SerializeField] int graphMinFrequency = 20;
		[SerializeField] int graphMaxFrequency = 20000;
		[SerializeField] int graphMaxDb = 12;
		[SerializeField] int graphMinDb = -96;
		float[] spectrum;
		float[] bins;
		// Start is called before the first frame update
		protected override void Start()
		{
			int actualSampleCount = 1;
			while (actualSampleCount < sampleCount)
			{
				actualSampleCount *= 2;
			}
			actualSampleCount = Mathf.Clamp(actualSampleCount, 64, 8192);
			spectrum = new float[actualSampleCount];

			bins = new float[binCount];
		}

		int toPow2(int x)
		{
			int l = 0;
			int t = x;
			while (t > 1)
			{
				t /= 2;
				l++;
			}
			t = 1 << l;
			if (t != x)
			{
				t *= 2;
			}
			return t;
		}

		protected override void OnPopulateMesh(VertexHelper vertexHelper)
		{
			vertexHelper.Clear();
			AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.Hanning);
			var outputF = AudioSettings.outputSampleRate;
			// 全binを初期化
			for (int i = 0; i < bins.Length; i++)
			{
				bins[i] = 0f;
			}

			var logMaxF = Mathf.Log(graphMaxFrequency); // 上のbinの周波数のlog
			var logMinF = Mathf.Log(graphMinFrequency);
			var logRange = logMaxF - logMinF;
			if (logRange <= 0f)
			{
				logRange = 8f;
			}
			// まず周波数分類
			for (int i = 0; i < spectrum.Length; i++)
			{
				var f = outputF * 0.5f * (float)i / (float)spectrum.Length;
				if (f == 0f)
				{
					f = float.Epsilon;
				}
				// 対数を取ってどのビンに入るか確定
				float binValue = (float)bins.Length * (Mathf.Log(f) - logMinF) / logRange;
				int binIndex = Mathf.FloorToInt(binValue);
				if ((binIndex >= 0) && (binIndex < bins.Length))
				{
					// そのビンにデータを加算
					bins[binIndex] += spectrum[i];
				}
			}
			// 背景描画
			float rectH = rectTransform.rect.height;
			float rectW = rectTransform.rect.width;
			var color = new Color(0f, 0f, 0f, 0.5f);
			int vertexCount = 0;
			vertexCount = Draw(0f, 0f, rectW, rectH, color, vertexHelper, vertexCount);

			// 横線描画。6dbごとに描画する。
			float maxDb = graphMaxDb;
			float minDb = graphMinDb;
			float dbRange = maxDb - minDb;
			if (dbRange <= 0f)
			{
				dbRange = 96f;
			}
			float hPerDb = rectH / dbRange;
			float yBase = rectH * -minDb / dbRange;
			color = new Color(0x4a / 255f, 0x68 / 255f, 0x74 / 255f, 0.25f);
			float y = yBase + (6f * hPerDb);
			while (y < rectH)
			{
				vertexCount = Draw(0f, y - 1f, rectW, y + 1f, color, vertexHelper, vertexCount);
				y += 6f * hPerDb;
			}
			y = yBase - (6f * hPerDb);
			while (y > 0f)
			{
				vertexCount = Draw(0f, y - 1f, rectW, y + 1f, color, vertexHelper, vertexCount);
				y -= 6f * hPerDb;
			}
			// 0dbラインは濃く
			color.a = 1f;
			vertexCount = Draw(0f, yBase - 1f, rectW, yBase + 1f, color, vertexHelper, vertexCount);

			// 縦線描画。
			float xBase = rectW * (Mathf.Log(graphCenterFrequency) - logMinF) / logRange;
			float wOctave = rectW * Mathf.Log(2f) / logRange;
			color.a = 0.25f;
			float x = xBase + wOctave;
			while (x < rectW)
			{
				vertexCount = Draw(x - 1f, 0f, x + 1f, rectH, color, vertexHelper, vertexCount);
				x += wOctave;
			}
			x = xBase - wOctave;
			while (x >= 0f)
			{
				vertexCount = Draw(x - 1f, 0f, x + 1f, rectH, color, vertexHelper, vertexCount);
				x -= wOctave;
			}
			// 中央周波数は濃く
			color.a = 1f;
			vertexCount = Draw(xBase - 1f, 0, xBase + 1f, rectH, color, vertexHelper, vertexCount);

			// ビンごとに描画
			float prevY = 0f;
			float barW = rectTransform.rect.width / bins.Length;
			color = new Color(0x80 / 255f, 0x19 / 255f, 0x2d / 255f, 1f);
			for (int i = 0; i < bins.Length; i++)
			{
				var db = minDb;
				// 平均取る
				var v = bins[i];
				if (v > 0)
				{
					db = Mathf.Log10(v) * 20f;
					if (db < minDb)
					{
						db = minDb;
					}
				}
				y = yBase * (db - minDb) / -minDb;
				if (v == 0f) // 0なら前の値につなげる
				{
					y = prevY;
				}
				var x0 = barW * (float)i;
				var x1 = (barW * (float)(i + 1));
				vertexCount = DrawLine(x0, prevY, x1, y, color, vertexHelper, vertexCount);
				prevY = y;
			}
		}

		int DrawLine(float x0, float y0, float x1, float y1, Color color, VertexHelper vertexHelper, int vertexCount)
		{
			var ox = -rectTransform.pivot.x * rectTransform.rect.width;
			var oy = -rectTransform.pivot.y * rectTransform.rect.height;
			x0 += ox;
			x1 += ox;
			y0 += oy;
			y1 += oy;
			var nx = -(y1 - y0);
			var ny = x1 - x0;
			var l = Mathf.Sqrt((nx * nx) + (ny * ny));
			nx /= l;
			ny /= l;
			vertexHelper.AddVert(new Vector3(x0 - nx, y0 - ny, 0f), color, Vector2.zero);
			vertexHelper.AddVert(new Vector3(x0 + nx, y0 + ny, 0f), color, Vector2.zero);
			vertexHelper.AddVert(new Vector3(x1 + nx, y1 + ny, 0f), color, Vector2.zero);
			vertexHelper.AddVert(new Vector3(x1 - nx, y1 - ny, 0f), color, Vector2.zero);
			vertexHelper.AddTriangle(
				vertexCount + 0,
				vertexCount + 1,
				vertexCount + 2);
			vertexHelper.AddTriangle(
				vertexCount + 2,
				vertexCount + 3,
				vertexCount + 0);
			return vertexCount + 4;
		}

		int Draw(float x0, float y0, float x1, float y1, Color color, VertexHelper vertexHelper, int vertexCount)
		{
			var ox = -rectTransform.pivot.x * rectTransform.rect.width;
			var oy = -rectTransform.pivot.y * rectTransform.rect.height;
			x0 += ox;
			x1 += ox;
			y0 += oy;
			y1 += oy;
			vertexHelper.AddVert(new Vector3(x0, y0, 0f), color, Vector2.zero);
			vertexHelper.AddVert(new Vector3(x0, y1, 0f), color, Vector2.zero);
			vertexHelper.AddVert(new Vector3(x1, y1, 0f), color, Vector2.zero);
			vertexHelper.AddVert(new Vector3(x1, y0, 0f), color, Vector2.zero);
			vertexHelper.AddTriangle(
				vertexCount + 0,
				vertexCount + 1,
				vertexCount + 2);
			vertexHelper.AddTriangle(
				vertexCount + 2,
				vertexCount + 3,
				vertexCount + 0);
			return vertexCount + 4;
		}

		void Update()
		{
			SetVerticesDirty(); // 毎フレームひたすらDirtyにする
		}
	}
}