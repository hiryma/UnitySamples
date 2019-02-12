using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class ColorReductionImporter : AssetPostprocessor
{
	const string PathPattern = "/CompressTo16Bit/";
	const string Postfix4444 = "_R4G4B4A4.png";
	const string Postfix565 = "_R5G6B5.png";

	void OnPreprocessTexture()
	{
		var importer = this.assetImporter as TextureImporter;
		var path = this.assetPath;
		if (path.Contains(Postfix4444) || path.Contains(Postfix565))
		{
			importer.alphaIsTransparency = false; // 勝手にいじるな
			// SetPlatformTextureSettingsした後に使い回して大丈夫かわからなので全部別インスタンスでやる
			var settings = new TextureImporterPlatformSettings();
			if (path.Contains(Postfix565))
			{
				settings.format = TextureImporterFormat.RGB16;
			}
			else if (path.Contains(Postfix4444))
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
		if (path.Contains(PathPattern))
		{
			importer.alphaIsTransparency = false; // 勝手にいじられるのを避ける
			importer.filterMode = FilterMode.Point; // 比較しやすくするためにポイント
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

	void OnPostprocessTexture(Texture2D texture)
	{
		var path = this.assetPath;
		if (path.Contains(Postfix4444)
			|| path.Contains(Postfix565))
		{
			return;
		}
		if (path.Contains(PathPattern))
		{
			CompressTo16Bit(texture, path);
		}
	}

	void CompressTo16Bit(Texture2D srcTexture, string path)
	{
		var pixels = srcTexture.GetPixels32();
		// アルファチャネルを使っているか調べる
		bool hasAlpha = true;
		if (srcTexture.format == TextureFormat.RGB24)
		{
			hasAlpha = false;
		}
		else
		{
			for (int i = 0; i < pixels.Length; i++)
			{
				if (pixels[i].a != 255)
				{
					hasAlpha = true;
					break;
				}
			}
		}
		var width = srcTexture.width;
		var height = srcTexture.height;
		System.Func<Color32, Color32> func = null;
		string postfix;
		if (hasAlpha)
		{
//			func = ColorReductionUtil.To1111;
			func = ColorReductionUtil.To4444;
			postfix = Postfix4444;
		}
		else
		{
//			func = ColorReductionUtil.To1111;
			func = ColorReductionUtil.To5650;
			postfix = Postfix565;
		}
		ColorReductionUtil.FloydSteinberg(pixels, func, width, height);
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
