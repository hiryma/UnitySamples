using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TestDataGenerator
{
	const int AbCount = 10;
	const int FileCountPerAb = 10;
	[MenuItem("Test/GenerateData")]
	static void GenerateData()
	{
		var buildList = new List<AssetBundleBuild>();
		var root = Application.dataPath;
		for (int i = 0; i < AbCount; i++)
		{
			var build = new AssetBundleBuild();
			var dir = root + "/Build/" + i.ToString();
			System.IO.Directory.CreateDirectory(dir);
			var files = new List<string>();
			for (int j = 0; j < FileCountPerAb; j++)
			{
				var assetPath = "Build/" + i.ToString() + "/" + j.ToString() + ".txt";
				var filePath = root + "/" + assetPath;
				var s = System.IO.File.Create(filePath);
				var content = System.Text.Encoding.UTF8.GetBytes(assetPath);
				s.Write(content, 0, content.Length);
				int size = (int)Mathf.Pow(10f, UnityEngine.Random.Range(0f, 6f));
				var bytes = new byte[size];
				for (int k = 0; k < size; k++)
				{
					bytes[k] = (byte)UnityEngine.Random.Range(0, 256);
				}
				s.Write(bytes, 0, bytes.Length);
				s.Close();
				files.Add("Assets/" + assetPath);
			}
			build.assetBundleName = i.ToString();
			build.assetNames = files.ToArray();
			buildList.Add(build);
		}
		AssetDatabase.Refresh();

		var builds = buildList.ToArray();
		var options = BuildAssetBundleOptions.ChunkBasedCompression
			| BuildAssetBundleOptions.DisableLoadAssetByFileName
			| BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension;
		var manifest = BuildPipeline.BuildAssetBundles(
			Application.streamingAssetsPath,
			builds,
			options,
			EditorUserBuildSettings.activeBuildTarget);
		AssetDatabase.Refresh();
	}
}
