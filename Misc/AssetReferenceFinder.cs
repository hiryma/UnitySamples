using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections;
using System;

namespace Kayac
{
	/*
	[使い方]
	- 「この中にあるものへの参照があっちゃダメ!!」というフォルダをProjectビューで選択
	   - 主にAssetBundle素材フォルダを指定することを想定
	   - ResoucesとStreamingAssetsは自動でダメなフォルダとして検索される
	- メニューからKayac/AssetReferenceFinderを実行
	- 操作可能になるまで待つ
	- プロジェクトのルートにAssetReferenceFinder.txtができているので中を見る
	*/

	public class AssetReferenceFinder
	{
		[MenuItem("Kayac/AssetRefereneFinder")]
		public static void EntryPoint()
		{
			var instance = new AssetReferenceFinder();
			instance.Process();
		}

		Dictionary<string, string> _guidAndPath;
//		StreamWriter _log;

		void Process()
		{
//			_log = new StreamWriter("AssetReferenceFinderLog.txt");
			var output = new StreamWriter("AssetReferenceFinder.txt");
			_guidAndPath = new Dictionary<string, string>();
			// ResourcesとStreamingAssetsを検索して、ダメフォルダリストに入れる
			var targetFolders = new List<string>();
			var folderGuids = AssetDatabase.FindAssets("t:folder");
			foreach (var folderGuid in folderGuids)
			{
				var path = AssetDatabase.GUIDToAssetPath(folderGuid);
				if ((path == "Assets/StreamingAssets")
					|| path.EndsWith("/Resources"))
				{
					targetFolders.Add(path);
				}
			}

			foreach (var selectedGuid in Selection.assetGUIDs)
			{
				var path = AssetDatabase.GUIDToAssetPath(selectedGuid);
				targetFolders.Add(path);
			}

			// フォルダ以下検索
			foreach (var path in targetFolders)
			{
//				_log.Write("TargetFolder: " + path + "\n");
//				_log.Flush();
				if (Directory.Exists(path))
				{
					CollectGuidRecursive(path);
				}
				var guid = AssetDatabase.AssetPathToGUID(path);
				if (!_guidAndPath.ContainsKey(guid))
				{
					_guidAndPath.Add(guid, path);
				}
			}
			// 全アセットを舐めて、prefab、sceneであれば中のguidを探して問題のものがあるか調べる。
			var assetGuids = AssetDatabase.FindAssets("t:object");
			var results = new Dictionary<string, HashSet<string>>();
			int assetCount = assetGuids.Length;
			int i = 0;
			foreach (var assetGuid in assetGuids)
			{
				if (!_guidAndPath.ContainsKey(assetGuid))
				{
					var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
					Debug.Log("ExternalAssetReferenceFinder scan " + i + "/" + assetCount + " : " + assetPath);
					if (!results.ContainsKey(assetPath))
					{
						if (assetPath.EndsWith(".unity")
							|| assetPath.EndsWith(".prefab")
							|| assetPath.EndsWith(".asset")
							|| assetPath.EndsWith(".anim")
							|| assetPath.EndsWith(".mat")
							|| assetPath.EndsWith(".controller"))
						{
							var set = MakeSet(assetPath);
							if (set.Count > 0)
							{
								results.Add(assetPath, set);
							}
						}
					}
				}
				i++;
			}
			// 書き出し
			foreach (var item in results)
			{
				output.Write(item.Key);
				output.Write('\n');
				foreach (var path in item.Value)
				{
					output.Write('\t');
					output.Write(path);
					output.Write('\n');
				}
			}
			output.Flush();
		}

		void CollectGuidRecursive(string path)
		{
			foreach (var childPath in Directory.GetFiles(path))
			{
				var childGuid = AssetDatabase.AssetPathToGUID(childPath);
				if (!_guidAndPath.ContainsKey(childGuid))
				{
//					_log.Write("\tAsset: " + childPath + "\n");
//					_log.Flush();
					_guidAndPath.Add(childGuid, childPath);
				}
			}
			foreach (var childPath in Directory.GetDirectories(path))
			{
				var childGuid = AssetDatabase.AssetPathToGUID(childPath);
				if (!_guidAndPath.ContainsKey(childGuid))
				{
//					_log.Write("\tAsset: " + childPath + "\n");
//					_log.Flush();
					_guidAndPath.Add(childGuid, childPath);
				}
				CollectGuidRecursive(childPath);
			}
		}

		HashSet<string> MakeSet(string path)
		{
//			_log.Write("MakeList: " + path + "\n");
//			_log.Flush();
			var ret = new HashSet<string>();
			StreamReader reader = null;
			try
			{
				reader = new StreamReader(path);
			}
			catch
			{
				return ret;
			}
			var regex = new Regex(@"guid:\s+(?<guid>[0-9a-fA-F]{32})", RegexOptions.Compiled);
			var text = reader.ReadToEnd();
			var matches = regex.Matches(text);
			foreach (Match match in matches)
			{
				var guid = match.Groups["guid"].Value;
				string matchPath;
				if (_guidAndPath.TryGetValue(guid, out matchPath))
				{
					if (!ret.Contains(matchPath))
					{
						ret.Add(matchPath);
					}
				}
			}
//			_log.Flush();
			return ret;
		}
	}
}