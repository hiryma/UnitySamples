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

		HashSet<string> _targetAssetPaths;
//		StreamWriter _log;

		void Process()
		{
//			_log = new StreamWriter("AssetReferenceFinderLog.txt");
			var output = new StreamWriter("AssetReferenceFinder.txt");
			_targetAssetPaths = new HashSet<string>();
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
				if (!_targetAssetPaths.Contains(path))
				{
					_targetAssetPaths.Add(path);
				}
			}
			// 全アセットを舐めて、prefab、sceneであれば中のguidを探して問題のものがあるか調べる。
			var assetGuids = AssetDatabase.FindAssets("t:object");
			int assetCount = assetGuids.Length;
			int i = 0;
			var t0 = System.DateTime.Now;
			var set = new HashSet<string>();
			foreach (var assetGuid in assetGuids)
			{
				var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
				if (!_targetAssetPaths.Contains(assetPath)) // 問題のフォルダの外にあるアセットのみ調べる
				{
					set.Clear();
					var dependencies = AssetDatabase.GetDependencies(assetPath, false);
					foreach (var dependency in dependencies)
					{
						if (_targetAssetPaths.Contains(dependency)) // 問題のものを発見!
						{
							if (!set.Contains(dependency))
							{
								set.Add(dependency);
							}
						}
					}
					if (set.Count > 0)
					{
						output.Write(assetPath);
						output.Write('\n');
						foreach (var path in set)
						{
							output.Write('\t');
							output.Write(path);
							output.Write('\n');
						}
					}
				}
				i++;
			}
			output.Flush();
			var t1 = System.DateTime.Now;
			Debug.Log("Scan " + i + " files. time:" + (t1 - t0).TotalSeconds + " sec.");
		}

		void CollectGuidRecursive(string path)
		{
			foreach (var childPath in Directory.GetFiles(path))
			{
				if (!_targetAssetPaths.Contains(childPath))
				{
//					_log.Write("\tAsset: " + childPath + "\n");
//					_log.Flush();
					_targetAssetPaths.Add(childPath);
				}
			}
			foreach (var childPath in Directory.GetDirectories(path))
			{
				if (!_targetAssetPaths.Contains(childPath))
				{
//					_log.Write("\tAsset: " + childPath + "\n");
//					_log.Flush();
					_targetAssetPaths.Add(childPath);
				}
				CollectGuidRecursive(childPath);
			}
		}
	}
}