using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ColorReductionUtil
{
	// 実験用1Bit化。残念ながらガンマの影響で明るくなる。
	public static Color32 To1111(Color32 x)
	{
		var r = ((x.r * 2) + 255) / 510;
		var g = ((x.g * 2) + 255) / 510;
		var b = ((x.b * 2) + 255) / 510;
		var a = ((x.a * 2) + 255) / 510;
		r = (r >= 1) ? 255 : 0;
		g = (g >= 1) ? 255 : 0;
		b = (b >= 1) ? 255 : 0;
		a = (a >= 1) ? 255 : 0;
		return new Color32((byte)r, (byte)g, (byte)b, (byte)a);
	}

	public static Color32 To4444(Color32 x)
	{
		var r = ((x.r * 30) + 255) / 510;
		var g = ((x.g * 30) + 255) / 510;
		var b = ((x.b * 30) + 255) / 510;
		var a = ((x.a * 30) + 255) / 510;
		// 上位ビットを下位にコピー
		r = (r << 4) | r;
		g = (g << 4) | g;
		b = (b << 4) | b;
		a = (a << 4) | a;
		return new Color32((byte)r, (byte)g, (byte)b, (byte)a);
	}

	public static Color32 To5650(Color32 x)
	{
		var r = ((x.r * 62) + 255) / 510; //5bit
		var g = ((x.g * 126) + 255) / 510; //6bit
		var b = ((x.b * 62) + 255) / 510; //5bit
		// RBは3bitシフトして、上3bitを下位にコピー(=2bit右シフトして2桁消す)
		r = (r << 3) | (r >> 2);
		b = (b << 3) | (b >> 2);
		// Gは2bitシフトして、上2bitを下位にコピー(=4bit右シフトして4桁消す)
		g = (g << 2) | (g >> 4);
		return new Color32((byte)r, (byte)g, (byte)b, 0xff);
	}

	struct ColorInt{
		public void AddDiff(ref Color32 c0, ref Color32 c1)
		{
			r += c0.r - c1.r;
			g += c0.g - c1.g;
			b += c0.b - c1.b;
			a += c0.a - c1.a;
		}
		public void SetMulDiv(ref ColorInt c, int mul, int div)
		{
			r = (c.r * mul) / div;
			g = (c.g * mul) / div;
			b = (c.b * mul) / div;
			a = (c.a * mul) / div;
		}
		public void Sub(ref ColorInt c)
		{
			r -= c.r;
			g -= c.g;
			b -= c.b;
			a -= c.a;
		}

		public int r,g,b,a;
	}

	// https://en.wikipedia.org/wiki/Floyd%E2%80%93Steinberg_dithering
	public static void FloydSteinberg(
		Color32[] pixels,
		System.Func<Color32, Color32> encode,
		int width,
		int height)
	{
		ColorInt e, e7, e5, e3, e1;
		e = e7 = e5 = e3 = e1 = new ColorInt();
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				var c0 = pixels[(y * width) + x];
				var c1 = encode(c0);
				pixels[(y * width) + x] = c1;
				e.AddDiff(ref c0, ref c1);
				e7.SetMulDiv(ref e, 7, 16);
				e5.SetMulDiv(ref e, 5, 16);
				e3.SetMulDiv(ref e, 3, 16);
				e1.SetMulDiv(ref e, 1, 16);
				if ((x + 1) < width)
				{
					AddColor(ref pixels[(y * width) + (x + 1)], ref e7);
					if ((y + 1) < height)
					{
						AddColor(ref pixels[((y + 1) * width) + (x + 1)], ref e1);
					}
				}
				if ((y + 1) < height)
				{
					if ((x - 1) >= 0)
					{
						AddColor(ref pixels[((y + 1) * width) + (x - 1)], ref e3);
					}
					AddColor(ref pixels[((y + 1) * width) + x], ref e5);
				}
				e.Sub(ref e7);
				e.Sub(ref e5);
				e.Sub(ref e3);
				e.Sub(ref e1);
			}
		}
	}

	static void AddColor(ref Color32 c, ref ColorInt t)
	{
		c.r = (byte)Mathf.Clamp(c.r + t.r, 0, 255);
		c.g = (byte)Mathf.Clamp(c.g + t.g, 0, 255);
		c.b = (byte)Mathf.Clamp(c.b + t.b, 0, 255);
		c.a = (byte)Mathf.Clamp(c.a + t.a, 0, 255);
	}
}
