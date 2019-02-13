using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class YuvImageImporter : AssetPostprocessor
{
	const string PathPattern = "/CompressToYuv/";
	const string PathPatternAlpha = "/CompressToYuvAlpha/";
	// BT.601
	const float Wr = 0.299f;
	const float Wb = 0.114f;
	// UVを[-0.5,0.5]にしたいので0.5で良い。
	const float UMax = 0.5f; // BT.601では0.436f
	const float VMax = 0.5f; // BT.601では0.615f

	void OnPreprocessTexture()
	{
//TestYuv();
		var importer = this.assetImporter as TextureImporter;
		var path = this.assetPath;
		if (IsGenerated(path))
		{
			importer.alphaIsTransparency = false; // 勝手にいじるな
			// SetPlatformTextureSettingsした後に使い回して大丈夫かわからなので全部別インスタンスでやる
			var settings = new TextureImporterPlatformSettings();
			settings.format = TextureImporterFormat.Alpha8;
			settings.overridden = true;
			settings.maxTextureSize = importer.maxTextureSize;
			SetPlatformSettingForIndex(importer, "Standalone", settings);
			SetPlatformSettingForIndex(importer, "Android", settings);
			SetPlatformSettingForIndex(importer, "iPhone", settings);
			SetPlatformSettingForIndex(importer, "WebGL", settings);
		}
		else if (path.Contains(PathPattern) || path.Contains(PathPatternAlpha))
		{
			importer.alphaIsTransparency = false; // 勝手にいじられるのを避ける
			importer.isReadable = true; // 読めないと何もできない
			importer.textureCompression = TextureImporterCompression.Uncompressed;
		}
	}

	void SetPlatformSettingForIndex(TextureImporter importer, string name, TextureImporterPlatformSettings original)
	{
		var settings = new TextureImporterPlatformSettings();
		original.CopyTo(settings);
		settings.name = name;
		importer.SetPlatformTextureSettings(settings);
	}

	bool IsGenerated(string path)
	{
		if (path.Contains("_yuv_y.png")
			|| path.Contains("_yuv_uv.png")
			|| path.Contains("_yuv_a.png"))
		{
			return true;
		}
		return false;
	}

	void OnPostprocessTexture(Texture2D texture)
	{
		var path = this.assetPath;
		if (IsGenerated(path))
		{
			return;
		}
		else if (path.Contains(PathPattern))
		{
			CompressToYuv(texture, path);
		}
		else if (path.Contains(PathPatternAlpha))
		{
			CompressToYuvAlpha(texture, path);
		}
	}

	static void TestYuv()
	{
		var t0 = Time.realtimeSinceStartup;
		int e = 0;
		for (int r = 0; r < 256; r++)
		{
			for (int g = 0; g < 256; g++)
			{
				for (int b = 0; b < 256; b++)
				{
					var rgb = new Color32((byte)r, (byte)g, (byte)b, 127);
					var yuv = rgb;
					ToYuv(ref yuv);
					var rgb2 = yuv;
					FromYuv(ref rgb2);
					var te = Mathf.Abs(rgb.r - rgb2.r) + Mathf.Abs(rgb.g - rgb2.g) + Mathf.Abs(rgb.b - rgb2.b);
					if (te > e)
					{
						e = te;
						Debug.Log("E: " + e + " " + rgb + " -> " + yuv + " -> " + rgb2);
					}
				}
			}
		}
		var t1 = Time.realtimeSinceStartup;
		Debug.Log("TestYuv time: " + (t1 - t0) + "MaxError: " + e);
	}

	// https://en.wikipedia.org/wiki/YUV
	static void ToYuv(ref Color32 pixel) // RGBA値をYUVA値で置き換えるので注意
	{
		// BT.601 constants
		const float yr = Wr;
		const float yb = Wb;
		// 以下は便利なので導出しておく定数
		const float yg = 1f - yr - yb;
		const float uScale = UMax / (1f - yb);
		const float vScale = VMax / (1f - yr);

		float y = (yr * pixel.r) + (yg * pixel.g) + (yb * pixel.b);
		float u = (pixel.b - y) * uScale; // [-127.5, 127.5]
		float v = (pixel.r - y) * vScale; // [-127.5, 127.5]
		u += 255f / 2f; // [0, 255]
		v += 255f / 2f; // [0, 255]
		// 0.5は四捨五入
		pixel.r = (byte)(y + 0.5f);
		pixel.g = (byte)(u + 0.5f);
		pixel.b = (byte)(v + 0.5f);
	}

	// デバグ用 & シェーダに移植するための種としてここに用意
	static void FromYuv(ref Color32 pixel)
	{
		const float yr = Wr;
		const float yb = Wb;
		// 以下は便利なので導出しておく定数
		const float yg = 1f - yr - yb;
		const float uScale = UMax / (1f - yb);
		const float vScale = VMax / (1f - yr);
		/*
		y = (yr * r) + (yg * g) + (yb * b)
		u = ku * (b - y) + 127.5
		v = kv * (r - y) + 127.5

		の逆変換。下2本をbとrについて解くと、
		b = ((u - 127.5) / ku) + y
		r = ((v - 127.5) / kv) + y

		g = (y - (yr * r) - (yb * b)) / yg
		*/
		float y = pixel.r;
		float u = pixel.g;
		float v = pixel.b;
		float b = ((u - 127.5f) / uScale) + y;
		float r = ((v - 127.5f) / vScale) + y;
		float g = (y - (yr * r) - (yb * b)) / yg;

		pixel.r = (byte)Mathf.Clamp(r + 0.5f, 0f, 255f);
		pixel.g = (byte)Mathf.Clamp(g + 0.5f, 0f, 255f);
		pixel.b = (byte)Mathf.Clamp(b + 0.5f, 0f, 255f);
	}

	void CompressToYuv(Texture2D srcTexture, string path)
	{
		var t0 = Time.realtimeSinceStartup;
		var pixels = srcTexture.GetPixels32();
		var width = srcTexture.width;
		var height = srcTexture.height;
		bool hasMipmap = (srcTexture.mipmapCount > 1);
		CompressToYuv(pixels, width, height, path, hasMipmap);
		var t1 = Time.realtimeSinceStartup;
		var time = t1 - t0;
		Debug.Log("YuvImageImporter(Separate): " + path + " takes " + time + " sec.");
	}

	void CompressToYuvAlpha(Texture2D srcTexture, string path)
	{
		var t0 = Time.realtimeSinceStartup;
		var pixels = srcTexture.GetPixels32();
		var width = srcTexture.width;
		var height = srcTexture.height;
		bool hasMipmap = (srcTexture.mipmapCount > 1);
		CompressToYuv(pixels, width, height, path, hasMipmap);
		// アルファの処理にかかる
		var aTexture = new Texture2D(width, height, TextureFormat.Alpha8, hasMipmap);
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				var color = new Color(0f, 0f, 0f, pixels[(y * width) + x].a / 255f);
				aTexture.SetPixel(x, y, color);
			}
		}
		var lastPeriodPos = path.LastIndexOf('.');
		var outPathTrunk = path.Substring(0, lastPeriodPos); // ピリオド以下を削除
		Save(aTexture, outPathTrunk + "_yuv_a.png");
		var t1 = Time.realtimeSinceStartup;
		var time = t1 - t0;
		Debug.Log("YuvImageImporter(SeparateAlpha): " + path + " takes " + time + " sec.");
	}

	void CompressToYuv(Color32[] pixels, int width, int height, string path, bool mipmap)
	{
		var uvWidth = (width + 1) / 2; // 切り上げで幅半分にする
		var uvHeight = (height + 1) / 2; // 切り上げで幅半分にする
		var yTexture = new Texture2D(width, height, TextureFormat.Alpha8, mipmap);
		var uvTexture = new Texture2D(uvWidth * 2, uvHeight, TextureFormat.Alpha8, mipmap);

		// まず全ピクセルYUV化しつつ、Yテクスチャを埋める
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				ToYuv(ref pixels[(y * width) + x]);
				yTexture.SetPixel(x, y, new Color(0f, 0f, 0f, pixels[(y * width) + x].r / 255f));
			}
		}

		// UVテクスチャの生成
		for (int y = 0; y < uvHeight; y++)
		{
			int y0 = y * 2;
			int y1 = y0 + 1;
			for (int x = 0; x < uvWidth; x++)
			{
				int x0 = x * 2;
				int x1 = x0 + 1;
				Color32 color00 = pixels[(y0 * width) + x0];
				Color32 color01, color10, color11;
				if (x1 < width) // 2画素目が含まれている時だけ
				{
					color10 = pixels[(y0 * width) + x1];
					if (y1 < height)
					{
						color11 = pixels[(y1 * width) + x1];
						color01 = pixels[(y1 * width) + x0];
					}
					else
					{
						color11 = color01 = color10;
					}
				}
				else if (y1 < height)
				{
					color10 = color00;
					color11 = color01 = pixels[(y1 * width) + x0];
				}
				else
				{
					color10 = color01 = color11 = color00;
				}
				var u = (float)(color00.g + color01.g + color10.g + color11.g) / (255f * 4f);
				var v = (float)(color00.b + color01.b + color10.b + color11.b) / (255f * 4f);
				uvTexture.SetPixel(x, y, new Color(0f, 0f, 0f, u));
				uvTexture.SetPixel(x + uvWidth, y, new Color(0f, 0f, 0f, v));
			}
		}
		var lastPeriodPos = path.LastIndexOf('.');
		var outPathTrunk = path.Substring(0, lastPeriodPos); // ピリオド以下を削除
		Save(yTexture, outPathTrunk + "_yuv_y.png");
		Save(uvTexture, outPathTrunk + "_yuv_uv.png");
	}

	void Save(Texture2D texture, string path)
	{
		var file = new FileStream(path, FileMode.Create, FileAccess.Write);
		var pngImage = texture.EncodeToPNG();
		file.Write(pngImage, 0, pngImage.Length);
		file.Close();
		AssetDatabase.ImportAsset(path);
	}
}
