using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class YuvImageImporter : AssetPostprocessor
{
	const string PathPattern = "/CompressToYuv/";
	const string PathPatternAlpha = "/CompressToYuvAlpha/";

	void OnPreprocessTexture()
	{
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

	// https://en.wikipedia.org/wiki/YUV
	Color32 ToYuv(ref Color32 rgb)
	{
		var yuv = new Color32();
		yuv.r = (byte)((0.299f * rgb.r) + (0.587f * rgb.g) + (0.114f * rgb.b));
		yuv.g = (byte)((-0.169f * rgb.r) + (-0.331f * rgb.g) + (0.499f * rgb.b) + 128f);
		yuv.b = (byte)((0.499f * rgb.r) + (-0.418f * rgb.g) + (-0.0813f * rgb.b) + 128f);
		yuv.a = 255;
		return yuv;
	}

	void CompressToYuv(Texture2D srcTexture, string path)
	{
		var t0 = Time.realtimeSinceStartup;
		var srcPixels = srcTexture.GetPixels32();
		var width = srcTexture.width;
		var height = srcTexture.height;
		bool hasMipmap = (srcTexture.mipmapCount > 1);
		CompressToYuv(srcPixels, width, height, path, hasMipmap);
		var t1 = Time.realtimeSinceStartup;
		var time = t1 - t0;
		Debug.Log("YuvImageImporter(Separate): " + path + " takes " + time + " sec.");
	}

	void CompressToYuvAlpha(Texture2D srcTexture, string path)
	{
		var t0 = Time.realtimeSinceStartup;
		var srcPixels = srcTexture.GetPixels32();
		var width = srcTexture.width;
		var height = srcTexture.height;
		bool hasMipmap = (srcTexture.mipmapCount > 1);
		CompressToYuv(srcPixels, width, height, path, hasMipmap);
		// アルファの処理にかかる
		var aTexture = new Texture2D(width, height, TextureFormat.Alpha8, hasMipmap);
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				var color = new Color(0f, 0f, 0f, srcPixels[(y * width) + x].a / 255f);
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

	void CompressToYuv(Color32[] srcPixels, int width, int height, string path, bool mipmap)
	{
		var uvWidth = (width + 1) / 2; // 切り上げで幅半分にする
		var uvHeight = (height + 1) / 2; // 切り上げで幅半分にする
		var yTexture = new Texture2D(width, height, TextureFormat.Alpha8, mipmap);
		var uvTexture = new Texture2D(uvWidth * 2, uvHeight, TextureFormat.Alpha8, mipmap);

		// まず全ピクセルYUV化しつつ、Yテクスチャを埋める
		var yuvPixels = new Color32[srcPixels.Length];
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				var yuv = ToYuv(ref srcPixels[(y * width) + x]);
				yuvPixels[(y * width) + x] = yuv;
				yTexture.SetPixel(x, y, new Color(0f, 0f, 0f, yuv.r / 255f));
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
				Color32 color00 = yuvPixels[(y0 * width) + x0];
				Color32 color01, color10, color11;
				if (x1 < width) // 2画素目が含まれている時だけ
				{
					color10 = yuvPixels[(y0 * width) + x1];
					if (y1 < height)
					{
						color11 = yuvPixels[(y1 * width) + x1];
						color01 = yuvPixels[(y1 * width) + x0];
					}
					else
					{
						color11 = color01 = color10;
					}
				}
				else if (y1 < height)
				{
					color10 = color00;
					color11 = color01 = yuvPixels[(y1 * width) + x0];
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
