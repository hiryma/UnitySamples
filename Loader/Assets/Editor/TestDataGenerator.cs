using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TestDataGenerator
{
	[MenuItem("Test/GenerateData")]
	static void GenerateData()
	{
		var buildList = new List<AssetBundleBuild>();
		var root = Application.dataPath + "/Build/";
		for (int i = 0; i < 10; i++)
		{
			var build = new AssetBundleBuild();
			var dir = root + i.ToString();
			System.IO.Directory.CreateDirectory(dir);
			var files = new List<string>();
			for (int j = 0; j < 10; j++)
			{
				var assetPath = i.ToString() + "/" + j.ToString() + ".txt";
				var s = System.IO.File.Create(root + "/" + assetPath);
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
				files.Add("Assets/Build/" + assetPath);
			}
			build.assetBundleName = i.ToString();
			build.assetNames = files.ToArray();
			buildList.Add(build);
		}

		var builds = buildList.ToArray();
		var options = BuildAssetBundleOptions.ChunkBasedCompression
			| BuildAssetBundleOptions.DisableLoadAssetByFileName
			| BuildAssetBundleOptions.DisableLoadAssetByFileNameWithExtension;
		var manifest = BuildPipeline.BuildAssetBundles(
			Application.streamingAssetsPath,
			builds,
			options,
			EditorUserBuildSettings.activeBuildTarget);
	}
}
