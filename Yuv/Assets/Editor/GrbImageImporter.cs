using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class GrbImageImporter : AssetPostprocessor
{
	const string PathPattern = "/CompressToGrb/";

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
		else if (path.Contains(PathPattern))
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
		if (path.Contains("_grb_g.png")
			|| path.Contains("_grb_rb.png"))
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
			CompressToGrb(texture, path);
		}
	}

	void CompressToGrb(Texture2D srcTexture, string path)
	{
		var t0 = Time.realtimeSinceStartup;
		var srcPixels = srcTexture.GetPixels32();
		var width = srcTexture.width;
		var height = srcTexture.height;
		bool hasMipmap = (srcTexture.mipmapCount > 1);
		CompressToGrb(srcPixels, width, height, path, hasMipmap);
		var t1 = Time.realtimeSinceStartup;
		var time = t1 - t0;
		Debug.Log("RgbImageImporter: " + path + " takes " + time + " sec.");
	}

	void CompressToGrb(Color32[] srcPixels, int width, int height, string path, bool mipmap)
	{
		var rbWidth = (width + 1) / 2; // 切り上げで幅半分にする
		var rbHeight = (height + 1) / 2; // 切り上げで幅半分にする
		var gTexture = new Texture2D(width, height, TextureFormat.Alpha8, mipmap);
		var rbTexture = new Texture2D(rbWidth * 2, rbHeight, TextureFormat.Alpha8, mipmap);

		// まずGテクスチャを埋める
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				var g = srcPixels[(y * width) + x].g;
				gTexture.SetPixel(x, y, new Color(0f, 0f, 0f, g / 255f));
			}
		}

		// RBテクスチャの生成
		for (int y = 0; y < rbHeight; y++)
		{
			int y0 = y * 2;
			int y1 = y0 + 1;
			for (int x = 0; x < rbWidth; x++)
			{
				int x0 = x * 2;
				int x1 = x0 + 1;
				Color32 color00 = srcPixels[(y0 * width) + x0];
				Color32 color01, color10, color11;
				if (x1 < width) // 2画素目が含まれている時だけ
				{
					color10 = srcPixels[(y0 * width) + x1];
					if (y1 < height)
					{
						color11 = srcPixels[(y1 * width) + x1];
						color01 = srcPixels[(y1 * width) + x0];
					}
					else
					{
						color11 = color01 = color10;
					}
				}
				else if (y1 < height)
				{
					color10 = color00;
					color11 = color01 = srcPixels[(y1 * width) + x0];
				}
				else
				{
					color10 = color01 = color11 = color00;
				}
				var r = (float)(color00.r + color01.r + color10.r + color11.r) / (255f * 4f);
				var b = (float)(color00.b + color01.b + color10.b + color11.b) / (255f * 4f);
				rbTexture.SetPixel(x, y, new Color(0f, 0f, 0f, r));
				rbTexture.SetPixel(x + rbWidth, y, new Color(0f, 0f, 0f, b));
			}
		}
		var lastPeriodPos = path.LastIndexOf('.');
		var outPathTrunk = path.Substring(0, lastPeriodPos); // ピリオド以下を削除
		Save(gTexture, outPathTrunk + "_grb_g.png");
		Save(rbTexture, outPathTrunk + "_grb_rb.png");
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
