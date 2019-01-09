using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class IndexedColorImageImporter : AssetPostprocessor
{
	const string PathPatternForIndexed256 = "/CompressToIndex256/";
	const string PathPatternForIndexed16 = "/CompressToIndex16/";

	void OnPreprocessTexture()
	{
		var importer = this.assetImporter as TextureImporter;
		var path = this.assetPath;
		if (IsGenerated(path))
		{
			importer.filterMode = FilterMode.Point; // 生成されたものは全てポイント
			if (IsGeneratedIndex(path)) // インデクスはAlpha8にしてロード容量を削る
			{
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
		}
		else if (path.Contains(PathPatternForIndexed256) || path.Contains(PathPatternForIndexed16))
		{
			importer.alphaIsTransparency = false; // 勝い手にいじられるのを避ける
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

	bool IsGeneratedIndex(string path)
	{
		if (path.Contains("_index256.png")
			|| path.Contains("_index16.png"))
		{
			return true;
		}
		return false;
	}

	bool IsGeneratedTable(string path)
	{
		if (path.Contains("_table256.png")
			|| path.Contains("_table16.png"))
		{
			return true;
		}
		return false;
	}

	bool IsGenerated(string path)
	{
		if (IsGeneratedIndex(path) || IsGeneratedTable(path))
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
		if (path.Contains(PathPatternForIndexed256))
		{
			if (!TryCompressToIndexed256(texture, path))
			{
				Debug.LogError("IndexedColorImageImporter: 256色より多いので変換できません。減色よろしく。" + path);
			}
		}
		else if (path.Contains(PathPatternForIndexed16))
		{
			if (!TryCompressToIndexed16(texture, path))
			{
				Debug.LogError("IndexedColorImageImporter: 16色より多いので変換できません。減色よろしく。" + path);
			}
		}
	}

	bool TryCompressToIndexed256(Texture2D texture, string path)
	{
		// 色辞書を生成
		var srcPixels = texture.GetPixels32();
		var map = MakeColorMap(srcPixels, 256);
		if (map == null)
		{
			return false;
		}
		var tableTexture = MakeTableTexture(map, 256);
		var indexTexture = MakeIndexTexture256(map, srcPixels, texture.width, texture.height);

		var lastPeriodPos = path.LastIndexOf('.');
		var outPathTrunk = path.Substring(0, lastPeriodPos); // ピリオド以下を削除
		Save(tableTexture, outPathTrunk + "_table256.png");
		Save(indexTexture, outPathTrunk + "_index256.png");
		return true;
	}

	bool TryCompressToIndexed16(Texture2D texture, string path)
	{
		// 色辞書を生成
		var srcPixels = texture.GetPixels32();
		var map = MakeColorMap(srcPixels, 16);
		if (map == null)
		{
			return false;
		}
		var tableTexture = MakeTableTexture(map, 16);
		var indexTexture = MakeIndexTexture16(map, srcPixels, texture.width, texture.height);

		var lastPeriodPos = path.LastIndexOf('.');
		var outPathTrunk = path.Substring(0, lastPeriodPos); // ピリオド以下を削除
		Save(tableTexture, outPathTrunk + "_table16.png");
		Save(indexTexture, outPathTrunk + "_index16.png");
		return true;
	}

	void Save(Texture2D texture, string path)
	{
		var file = new FileStream(path, FileMode.Create, FileAccess.Write);
		var pngImage = texture.EncodeToPNG();
		file.Write(pngImage, 0, pngImage.Length);
		file.Close();
	}

	Texture2D MakeIndexTexture16(Dictionary<Color32, int> map, Color32[] srcPixels, int width, int height)
	{
		var dstWidth = (width + 1) / 2; // 切り上げで幅半分にする
		var texture = new Texture2D(dstWidth, height, TextureFormat.Alpha8, false);
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < dstWidth; x++) // 2画素づつ処理。書き込み側の幅でループ
			{
				var color0 = srcPixels[(y * width) + (x * 2)];
				var index0 = map[color0];
				int index1 = 0;
				if (((x * 2) + 1) < width) // 2画素目が含まれている時だけ
				{
					var color1 = srcPixels[(y * width) + ((x * 2) + 1)];
					index1 = map[color1];
				}
				Debug.Assert((index0 < 16) || (index1 < 16));
				var composedIndex = (index0 << 4) | index1;
				var indexAsColor = new Color(0f, 0f, 0f, (float)composedIndex / 255f);
				texture.SetPixel(x, y, indexAsColor);
			}
		}
		return texture;
	}

	Texture2D MakeIndexTexture256(Dictionary<Color32, int> map, Color32[] srcPixels, int width, int height)
	{
		var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				var color = srcPixels[(y * width) + x];
				var index = map[color];
				Debug.Assert(index < 256);
				var encoded = ((float)index) / 255f;
				var indexAsColor = new Color(0f, 0f, 0f, encoded);
				texture.SetPixel(x, y, indexAsColor);
			}
		}
		return texture;
	}

	Texture2D MakeTableTexture(Dictionary<Color32, int> map, int colorCount)
	{
		var texture = new Texture2D(colorCount, 1, TextureFormat.RGBA32, false);
		// 配列化してテクスチャに書き込み
		var colors = new Color32[colorCount];
		foreach (var color in map.Keys)
		{
			var index = map[color];
			Debug.Assert(index < colorCount);
			colors[index] = color;
		}
		// 確認のために残りは紫で埋める
		var magenta = new Color32(255, 0, 255, 255);
		for (int i = map.Keys.Count; i < colorCount; i++)
		{
			colors[i] = magenta;
		}
		texture.SetPixels32(colors);
		return texture;
	}


	// 色が多すぎるとnullを返す
	Dictionary<Color32, int> MakeColorMap(Color32[] srcPixels, int maxColorCount)
	{
		// 色辞書を生成
		var map = new Dictionary<Color32, int>();
		var index = 0;
		foreach (var color in srcPixels)
		{
			if (!map.ContainsKey(color))
			{
				if (index >= maxColorCount)
				{
					return null;
				}
				map.Add(color, index);
				index++;
			}
		}
		return map;
	}


}
