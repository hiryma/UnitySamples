using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class PackedYuvImageImporter : AssetPostprocessor
{
	const string PathPattern = "/CompressToPackedYuv/";
	const string PathPatternAlpha = "/CompressToPackedYuvAlpha/";

	void OnPreprocessTexture()
	{
		var importer = this.assetImporter as TextureImporter;
		var path = this.assetPath;
		if (IsGenerated(path))
		{
			importer.alphaIsTransparency = false; // 勝手にいじるな
			importer.filterMode = FilterMode.Point; // 分離形式でなければ全てポイント
		}
		else if (path.Contains(PathPattern) || path.Contains(PathPatternAlpha))
		{
			importer.alphaIsTransparency = false; // 勝手にいじられるのを避ける
			importer.isReadable = true; // 読めないと何もできない
			importer.textureCompression = TextureImporterCompression.Uncompressed;
			importer.mipmapEnabled = false; // ミップマップ禁止(不可能ではないだろうが、とりあえず)
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
		if (path.Contains("_packed_yuv.png") || path.Contains("_packed_yuva.png"))
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
			CompressToYuvA(texture, path);
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

	Color32 ToYuv(ref Color32 rgb0, ref Color32 rgb1)
	{
		var yuv0 = ToYuv(ref rgb0);
		var yuv1 = ToYuv(ref rgb1);
		var yuv = new Color32();
		yuv.r = yuv0.r;
		yuv.g = yuv1.r;
		yuv.b = (byte)((yuv0.g + yuv1.g + 1) >> 1);
		yuv.a = (byte)((yuv0.b + yuv1.b + 1) >> 1);
		return yuv;
	}

	Color32 ToYuvA(ref Color32 rgba0, ref Color32 rgba1)
	{
		var yuv0 = ToYuv(ref rgba0);
		var yuv1 = ToYuv(ref rgba1);
		var yuva = new Color32();
		int u = (yuv0.g + yuv1.g + 1) >> 1;
		int v = (yuv0.b + yuv1.b + 1) >> 1;
		yuva.r = (byte)((yuv0.r & 0xfc) | ((u >> 6) & 0x3));
		yuva.g = (byte)((yuv1.r & 0xfc) | ((v >> 6) & 0x3));
		yuva.b = (byte)(((u << 2) & 0xf0) | ((v >> 2) & 0xf));
		yuva.a = (byte)((rgba0.a & 0xf0) | ((rgba1.a >> 4) & 0xf));
		return yuva;
	}

	void CompressToYuv(Texture2D srcTexture, string path)
	{
		var t0 = Time.realtimeSinceStartup;
		var srcPixels = srcTexture.GetPixels32();
		var height = srcTexture.height;
		var srcWidth = srcTexture.width;
		var dstWidth = (srcWidth + 1) / 2; // 切り上げで幅半分にする
		var dstPixels = new Color32[dstWidth * height];
		var dstTexture = new Texture2D(dstWidth, height, TextureFormat.RGBA32, false);
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < dstWidth; x++) // 2画素づつ処理。書き込み側の幅でループ
			{
				var color0 = srcPixels[(y * srcWidth) + (x * 2)];
				Color32 color1;
				if (((x * 2) + 1) < srcWidth) // 2画素目が含まれている時だけ
				{
					color1 = srcPixels[(y * srcWidth) + ((x * 2) + 1)];
				}
				else
				{
					color1 = color0;
				}
				// エンコします
				dstPixels[(y * dstWidth) + x] = ToYuv(ref color0, ref color1);
			}
		}
		dstTexture.SetPixels32(dstPixels);
		var lastPeriodPos = path.LastIndexOf('.');
		var outPathTrunk = path.Substring(0, lastPeriodPos); // ピリオド以下を削除
		Save(dstTexture, outPathTrunk + "_packed_yuv.png");
		var t1 = Time.realtimeSinceStartup;
		var time = t1 - t0;
		Debug.Log("PackedYuvImageImporter: " + path + " takes " + time + " sec.");
	}

	void CompressToYuvA(Texture2D srcTexture, string path)
	{
		var t0 = Time.realtimeSinceStartup;
		var srcPixels = srcTexture.GetPixels32();
		var height = srcTexture.height;
		var srcWidth = srcTexture.width;
		var dstWidth = (srcWidth + 1) / 2; // 切り上げで幅半分にする
		var dstPixels = new Color32[dstWidth * height];
		var dstTexture = new Texture2D(dstWidth, height, TextureFormat.RGBA32, false);
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < dstWidth; x++) // 2画素づつ処理。書き込み側の幅でループ
			{
				var color0 = srcPixels[(y * srcWidth) + (x * 2)];
				Color32 color1;
				if (((x * 2) + 1) < srcWidth) // 2画素目が含まれている時だけ
				{
					color1 = srcPixels[(y * srcWidth) + ((x * 2) + 1)];
				}
				else
				{
					color1 = color0;
				}
				// エンコします
				dstPixels[(y * dstWidth) + x] = ToYuvA(ref color0, ref color1);
			}
		}
		dstTexture.SetPixels32(dstPixels);
		var lastPeriodPos = path.LastIndexOf('.');
		var outPathTrunk = path.Substring(0, lastPeriodPos); // ピリオド以下を削除
		Save(dstTexture, outPathTrunk + "_packed_yuva.png");
		var t1 = Time.realtimeSinceStartup;
		var time = t1 - t0;
		Debug.Log("PackedYuvImageImporter(Alpha): " + path + " takes " + time + " sec.");
	}

	void Save(Texture2D texture, string path)
	{
		var file = new FileStream(path, FileMode.Create, FileAccess.Write);
		var pngImage = texture.EncodeToPNG();
		file.Write(pngImage, 0, pngImage.Length);
		file.Close();
	}
}
