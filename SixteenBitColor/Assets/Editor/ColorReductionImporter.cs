using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class ColorReductionImporter : AssetPostprocessor
{
	const string PathPattern = "/CompressTo16Bit/";

	void OnPreprocessTexture()
	{
		var importer = this.assetImporter as TextureImporter;
		var path = this.assetPath;
		if (path.Contains(PathPattern))
		{
			importer.alphaIsTransparency = false; // 勝手にいじられるのを避ける
			importer.isReadable = true; // 読めないと何もできない
			importer.textureCompression = TextureImporterCompression.Uncompressed;
		}
	}

	void OnPostprocessTexture(Texture2D texture)
	{
		var path = this.assetPath;
		if (path.Contains(PathPattern))
		{
			CompressTo16Bit(texture, path);
		}
	}

	void CompressTo16Bit(Texture2D texture, string path)
	{
		var t0 = Time.realtimeSinceStartup;
		var pixels = texture.GetPixels32(0);
		// アルファチャネルを使っているか調べる
		bool hasAlpha = true;
		if (texture.format == TextureFormat.RGB24)
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
		var width = texture.width;
		var height = texture.height;
		System.Func<Color32, Color32> func = null;
		var format = (TextureFormat)int.MinValue; // 絶対不正値
		if (hasAlpha)
		{
//			func = ColorReductionUtil.To1111;
			func = ColorReductionUtil.To4444;
			format = TextureFormat.ARGB4444;
		}
		else
		{
//			func = ColorReductionUtil.To1111;
			func = ColorReductionUtil.To5650;
			format = TextureFormat.RGB565;
		}
		// 全ミップレベルで回す
		for (int i = 0; i < texture.mipmapCount; i++)
		{
			if (i != 0) // 一番上はすでに取ってあるので繰り返さない
			{
				pixels = texture.GetPixels32(i);
			}
			ColorReductionUtil.FloydSteinberg(pixels, func, width, height);
			texture.SetPixels32(pixels, i);
			width = Mathf.Max(1, width / 2); // 次のサイズへ
			height = Mathf.Max(1, height / 2); // 次のサイズへ
		}
		EditorUtility.CompressTexture(texture, format, quality: 100);
		var t1 = Time.realtimeSinceStartup;
		Debug.Log("ColorReductionImporter: " + path + " t:" + (t1 - t0));
	}
}
