using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEditor;

public class TestDataGenerator
{
	const int AbCount = 1000;
	const int Min = 2 * 1024;
	const int Max = 64 * 1024 * 1024;
	const float LnSigma = 3f;
	[MenuItem("Test/GenerateData")]
	static void GenerateData()
	{
		var root = Application.dataPath + "/AssetBundleSource";
		if (!Directory.Exists(root))
		{
			Directory.CreateDirectory(root);
		}

		// サイズ生成。サイズ昇順で番号を振りたいので一旦配列に生成
		var sizes = new int[AbCount];
		for (int i = 0; i < AbCount; i++)
		{
			sizes[i] = GetRandomFilesize(Min, Max, LnSigma);
		}
		Array.Sort(sizes);
		for (int i = 0; i < AbCount; i++)
		{
			var filePath = root + "/" + i.ToString("D4") + ".txt";
			GenerateRandomContentFile(filePath, sizes[i]);
		}
		AssetDatabase.Refresh();
	}

	[MenuItem("Test/BuildData")]
	static void BuildData()
	{
		var buildList = new List<AssetBundleBuild>();
		var dstRoot = Application.dataPath + "/../AssetBundleBuild";
		if (!Directory.Exists(dstRoot))
		{
			Directory.CreateDirectory(dstRoot);
		}

		var srcRoot = Application.dataPath + "/AssetBundleSource";
		var paths = Directory.GetFiles(srcRoot);
		foreach (var path in paths)
		{
			var filename = Path.GetFileName(path);
			var files = new List<string>();
			files.Add("Assets/AssetBundleSource/" + filename);

			var build = new AssetBundleBuild();
			build.assetBundleName = Path.GetFileNameWithoutExtension(path) + ".unity3d";
			build.assetNames = files.ToArray();
			buildList.Add(build);
		}
		var builds = buildList.ToArray();
		var options = BuildAssetBundleOptions.ChunkBasedCompression
			| BuildAssetBundleOptions.DisableLoadAssetByFileName
			| BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension;
		var manifest = BuildPipeline.BuildAssetBundles(
			dstRoot,
			builds,
			options,
			EditorUserBuildSettings.activeBuildTarget);
		MakeMetaDataJson(manifest, dstRoot);
	}

	static void MakeMetaDataJson(AssetBundleManifest manifest, string buildDataRoot)
	{
		// 依存関係面倒くさいから扱わない。でもサイズは欲しいので足したデータを作ってjsonで吐く
		var container = new Kayac.AssetBundleMetaDataContainer();
		var names = manifest.GetAllAssetBundles();
		// 名前でソート
		Array.Sort(names);
		container.items = new Kayac.AssetBundleMetaData[names.Length];
		for (int i = 0; i < names.Length; i++)
		{
			var name = names[i];
			var item = new Kayac.AssetBundleMetaData();
			item.name = name;
			item.hash = manifest.GetAssetBundleHash(name).ToString();
			var info = new FileInfo(buildDataRoot + "/" + name);
			item.size = (int)info.Length;
			container.items[i] = item;
		}
		var json = JsonUtility.ToJson(container);
		File.WriteAllText(Application.streamingAssetsPath + "/assetbundle_metadata.json", json);
	}

	static int GetRandomFilesize(int min, int max, float lnSigma)
	{
		float d = GenerateNormalDistribution();
		float x = min * Mathf.Exp(Mathf.Abs(d * lnSigma));
		var ret = Mathf.CeilToInt(x);
		if (ret > max)
		{
			ret = max;
		}
		return ret;
	}

	static float GenerateNormalDistribution() // sigma==1の正規分布の平均からの差分だけ返す
	{
		float r0 = UnityEngine.Random.Range(0f, 1f);
		float r1 = UnityEngine.Random.Range(0f, 1f);
		float z = Mathf.Sqrt(-2f * Mathf.Log(r0)) * Mathf.Sin(2f * Mathf.PI * r1);
		return z;
	}

	static void GenerateRandomContentFile(string path, int size)
	{
		try
		{
			var s = File.Create(path);
			var bytes = new byte[size];
			for (int j = 0; j < size; j++)
			{
				bytes[j] = (byte)UnityEngine.Random.Range(0, 256);
			}
			s.Write(bytes, 0, bytes.Length);
			s.Close();
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}
}
