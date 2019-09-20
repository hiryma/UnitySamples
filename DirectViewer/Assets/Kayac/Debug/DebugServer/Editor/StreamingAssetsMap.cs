using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build;

namespace Kayac
{
	[InitializeOnLoad] // OnPlayModeStateChangedを呼ぶため
	public class StreamingAssetsMap : IPreprocessBuildWithReport
	{
		public int callbackOrder // 順序にこだわりはない
		{
			get
			{
				return 0;
			}
		}

		public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
		{

		}

		static StreamingAssetsMap()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.EnteredPlayMode)
			{
				GenerateMap();
			}
		}


		[MenuItem("Kayac/DebugServer/MakeStreamingAssetsMap")]
		public static void GenerateMap()
		{
			var map = new DebugFileService.Map();
			var root = new DebugFileService.Directory();
			map.directories.Add(root);

			root.name = "StreamingAssets";
			var path = Application.streamingAssetsPath;
			Generate(map, root, path);

			// jsonを吐く
			var json = JsonUtility.ToJson(map, true);

			var dir = System.IO.Path.GetDirectoryName("Assets/Resources/" + DebugFileService.MapPath);
			System.IO.Directory.CreateDirectory(dir);
			System.IO.File.WriteAllText(
				Application.dataPath + "/Resources/" + DebugFileService.MapPath,
				json);
			AssetDatabase.ImportAsset("Assets/Resources/" + DebugFileService.MapPath);
		}

		static void Generate(
			DebugFileService.Map map,
			DebugFileService.Directory node,
			string path)
		{
			if (!System.IO.Directory.Exists(path))
			{
				return;
			}
			// 幅優先探索
			var directoryPaths = System.IO.Directory.GetDirectories(path);
			DebugFileService.Directory lastChild = null;
			foreach (var directoryPath in directoryPaths)
			{
				var directory = new DebugFileService.Directory();
				directory.name = directoryPath.Remove(0, path.Length + 1); // スラッシュ付きで除去
				if (lastChild == null)
				{
					node.firstChild = map.directories.Count;
				}
				else
				{
					lastChild.nextBrother = map.directories.Count;
				}
				map.directories.Add(directory);
				lastChild = directory;
			}

			var filePaths = System.IO.Directory.GetFiles(path);
			DebugFileService.File lastFile = null;
			foreach (var filePath in filePaths)
			{
				if ((System.IO.Path.GetExtension(filePath) != ".meta") // .meta除外
					&& (filePath[0] != '.')) // 隠しファイル除外
				{
					var file = new DebugFileService.File();
					file.name = filePath.Remove(0, path.Length + 1);
					if (lastFile == null)
					{
						node.firstFile = map.files.Count;
					}
					else
					{
						lastFile.nextFile = map.files.Count;
					}
					map.files.Add(file);
					lastFile = file;
				}
			}
			// 再帰呼び出し
			var childIndex = node.firstChild;
			while (childIndex >= 0)
			{
				var child = map.directories[childIndex];
				Generate(map, child, path + "/" + child.name);
				childIndex = child.nextBrother;
			}
		}
	}

}