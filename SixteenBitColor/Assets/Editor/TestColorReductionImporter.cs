using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class TestColorReductionImporter : AssetPostprocessor
{
	const string PathPattern4444 = "/CompressTo4444/";
	const string PathPattern5650 = "/CompressTo5650/";
	const string PathPattern4444D = "/CompressTo4444Dither/";
	const string PathPattern5650D = "/CompressTo5650Dither/";
	const string PathPattern4444Unity = "/CompressTo4444Unity/";

	void OnPreprocessTexture()
	{
		var importer = this.assetImporter as TextureImporter;
		// このプロジェクトでは比較のために全テクスチャをポイントサンプリング
		importer.filterMode = FilterMode.Point; // 比較しやすくするためにポイント

		var path = this.assetPath;
		if (path.Contains("_4444.png")
			|| path.Contains("_5650.png")
			|| path.Contains("_4444D.png")
			|| path.Contains("_5650D.png"))
		{
			importer.alphaIsTransparency = false; // 勝手にいじるな
			// SetPlatformTextureSettingsした後に使い回して大丈夫かわからなので全部別インスタンスでやる
			var settings = new TextureImporterPlatformSettings();
			if (path.Contains("_5650.png") || path.Contains("_5650D"))
			{
				settings.format = TextureImporterFormat.RGB16;
			}
			else if (path.Contains("_4444.png") || path.Contains("_4444D.png"))
			{
				settings.format = TextureImporterFormat.ARGB16;
			}
			settings.overridden = true;
			settings.maxTextureSize = importer.maxTextureSize;
			SetPlatformSettingForIndex(importer, "Standalone", settings);
			SetPlatformSettingForIndex(importer, "Android", settings);
			SetPlatformSettingForIndex(importer, "iPhone", settings);
			SetPlatformSettingForIndex(importer, "WebGL", settings);
		}
		else if (path.Contains(PathPattern4444)
			|| path.Contains(PathPattern5650))
		{
			importer.alphaIsTransparency = false; // 勝手にいじられるのを避ける
			importer.isReadable = true; // 読めないと何もできない
			importer.textureCompression = TextureImporterCompression.Uncompressed;
		}
		else if (path.Contains(PathPattern4444Unity))
		{
			importer.alphaIsTransparency = false; // 勝手にいじるな
			// SetPlatformTextureSettingsした後に使い回して大丈夫かわからなので全部別インスタンスでやる
			var settings = new TextureImporterPlatformSettings();
			settings.format = TextureImporterFormat.ARGB16;
			settings.overridden = true;
			settings.maxTextureSize = importer.maxTextureSize;
			SetPlatformSettingForIndex(importer, "Standalone", settings);
			SetPlatformSettingForIndex(importer, "Android", settings);
			SetPlatformSettingForIndex(importer, "iPhone", settings);
			SetPlatformSettingForIndex(importer, "WebGL", settings);
		}
	}

	void SetPlatformSettingForIndex(TextureImporter importer, string name, TextureImporterPlatformSettings original)
	{
		var settings = new TextureImporterPlatformSettings();
		original.CopyTo(settings);
		settings.name = name;
		importer.SetPlatformTextureSettings(settings);
	}

	void OnPostprocessTexture(Texture2D texture)
	{
		var path = this.assetPath;
		if (path.Contains("_4444.png")
			|| path.Contains("_5650.png")
			|| path.Contains("_4444D.png")
			|| path.Contains("_5650D.png"))
		{
			return;
		}
		if (path.Contains(PathPattern4444))
		{
			CompressTo4444(texture, path, dither: false);
		}
		else if (path.Contains(PathPattern5650))
		{
			CompressTo5650(texture, path, dither: false);
		}
		else if (path.Contains(PathPattern4444D))
		{
			CompressTo4444(texture, path, dither: true);
		}
		else if (path.Contains(PathPattern5650D))
		{
			CompressTo5650(texture, path, dither: true);
		}
	}

	void CompressTo4444(Texture2D srcTexture, string path, bool dither)
	{
		var pixels = srcTexture.GetPixels32();
		var width = srcTexture.width;
		var height = srcTexture.height;
		string postfix;
		if (dither)
		{
			ColorReductionUtil.FloydSteinberg(pixels, ColorReductionUtil.To4444, width, height);
			postfix = "_4444D.png";
		}
		else
		{
			for (int i = 0; i < pixels.Length; i++)
			{
				pixels[i] = ColorReductionUtil.To4444(pixels[i]);
			}
			postfix = "_4444.png";
		}
		Save(pixels, width, height, (srcTexture.mipmapCount > 1), path, postfix);
	}

	void CompressTo5650(Texture2D srcTexture, string path, bool dither)
	{
		var pixels = srcTexture.GetPixels32();
		var width = srcTexture.width;
		var height = srcTexture.height;
		string postfix;
		if (dither)
		{
			ColorReductionUtil.FloydSteinberg(pixels, ColorReductionUtil.To5650, width, height);
			postfix = "_5650D.png";
		}
		else
		{
			for (int i = 0; i < pixels.Length; i++)
			{
				pixels[i] = ColorReductionUtil.To5650(pixels[i]);
			}
			postfix = "_5650.png";
		}
		Save(pixels, width, height, (srcTexture.mipmapCount > 1), path, postfix);
	}

	void Save(Color32[] pixels, int width, int height, bool hasMipmap, string originalPath, string postfix)
	{
		var dstTexture = new Texture2D(width, height, TextureFormat.RGBA32, hasMipmap);
		dstTexture.SetPixels32(pixels);
		if (hasMipmap)
		{
			dstTexture.Apply();
		}
		var lastPeriodPos = originalPath.LastIndexOf('.');
		var outPathTrunk = originalPath.Substring(0, lastPeriodPos); // ピリオド以下を削除
		var path = outPathTrunk + postfix;
		var file = new FileStream(path, FileMode.Create, FileAccess.Write);
		var pngImage = dstTexture.EncodeToPNG();
		file.Write(pngImage, 0, pngImage.Length);
		file.Close();
		AssetDatabase.ImportAsset(path);
	}
}
