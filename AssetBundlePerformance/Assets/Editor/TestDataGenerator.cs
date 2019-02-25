using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TestDataGenerator
{
	const int AbCount = 100;
	[MenuItem("Test/GenerateData")]
	static void GenerateData()
	{
		var root = Application.dataPath;
		System.IO.Directory.CreateDirectory(root + "/AssetBundleSource/");
		for (int i = 0; i < AbCount; i++)
		{
			var assetPath = "AssetBundleSource/" + i + ".txt";
			var filePath = root + "/" + assetPath;
			int size = GenerateExponentiallyDistributedNumber() * 100;
			GenerateRandomContentFile(filePath, size);
		}
		AssetDatabase.Refresh();
	}

	[MenuItem("Test/BuildData")]
	static void BuildData()
	{
		var buildList = new List<AssetBundleBuild>();
		var root = Application.dataPath;

		var paths = System.IO.Directory.GetFiles("Assets/AssetBundleSource/");
		foreach (var path in paths)
		{
			var filename = System.IO.Path.GetFileName(path);
			var files = new List<string>();
			files.Add("Assets/AssetBundleSource/" + filename);

			var build = new AssetBundleBuild();
			build.assetBundleName = System.IO.Path.GetFileNameWithoutExtension(path);
			build.assetNames = files.ToArray();
			buildList.Add(build);
		}
		var builds = buildList.ToArray();
		var options = BuildAssetBundleOptions.ChunkBasedCompression
			| BuildAssetBundleOptions.DisableLoadAssetByFileName
			| BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension;
		var manifest = BuildPipeline.BuildAssetBundles(
			Application.dataPath + "/../AssetBundleBuild/",
			builds,
			options,
			EditorUserBuildSettings.activeBuildTarget);
	}

	static int GenerateExponentiallyDistributedNumber() // 汎用化を今はあきらめた
	{
		float min = Mathf.Log(4096f);
		float max = Mathf.Log(3f * 1024f * 1024f);
		var x = UnityEngine.Random.Range(0f, 1f);
		var y = Mathf.Exp(min + (Mathf.Pow(x, 12f) * (max - min)));
		return Mathf.CeilToInt(y);
	}

	static void GenerateRandomContentFile(string path, int size)
	{
		try
		{
			var s = System.IO.File.Create(path);
			var bytes = new byte[size];
			for (int j = 0; j < size; j++)
			{
				bytes[j] = (byte)UnityEngine.Random.Range(0, 256);
			}
			s.Write(bytes, 0, bytes.Length);
			s.Close();
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
		}
	}
}
