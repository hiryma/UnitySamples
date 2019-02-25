using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Main : MonoBehaviour
{
	string[] _modes = new string[]
	{
		"WwwSyncWrite",
		"UwrSyncWrite",
		"UwrAsyncWrite",
		"UWR + DownloadHandlerFile + CustomCache",
		"UnityWebRequestAssetBundle",
	};
	string _root;
	string _cachePath;
	int _modeIndex;
	int _doneFileCount;
	int _totalFileCount;
	int _doneBytes;
	int _totalBytes;
	AssetBundleManifest _manifest;
	IEnumerator _enumerator;
	System.DateTime _startTime;
	Kayac.FileWriter _fileWriter;
	Kayac.FrameTimeWatcher _frametimeWatcher;

	IEnumerator Start()
	{
		_frametimeWatcher = new Kayac.FrameTimeWatcher();
		while (!Caching.ready)
		{
			yield return null;
		}
		_cachePath = Application.dataPath + "/../AssetBundleCache";
		var cache = Caching.AddCache(_cachePath);
		Caching.currentCacheForWriting = cache;
		Caching.ClearCache();
		_fileWriter = new Kayac.FileWriter(_cachePath);
#if true
		_root = "file://" + Application.dataPath + "/../AssetBundleBuild/"; // ローカル
#else
		_root = "http://localhost/AssetBundleBuild/"; // サーバ
#endif
		var ab = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/AssetBundleBuild");
		_manifest = ab.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
	}

	void Update()
	{
		_frametimeWatcher.Update();
		_fileWriter.Update();
		if (_enumerator != null)
		{
			if (!_enumerator.MoveNext())
			{
				_enumerator = null;
				Debug.Log(_modes[_modeIndex] + " complete. " + (System.DateTime.Now - _startTime).TotalSeconds + " s.");
			}
		}
	}

	void OnGUI()
	{
		GUILayout.Label(_frametimeWatcher.averageFrameTime + " " + _frametimeWatcher.maxFrameTime);
		var text = string.Format("FileCount:{0}/{1} Bytes:{2}/{3}", _doneFileCount, _totalFileCount, _doneBytes, _totalBytes);
		GUILayout.Label(text);
		_modeIndex = GUILayout.SelectionGrid(_modeIndex, _modes, _modes.Length);
		if (_enumerator == null)
		{
			if (GUILayout.Button("Start"))
			{
				var files = _manifest.GetAllAssetBundles();
				_totalFileCount = files.Length;
				_doneFileCount = 0;
				_totalBytes = 0;
				_doneBytes = 0;
				DeleteCache(_cachePath);
				try
				{
					System.IO.Directory.CreateDirectory(_cachePath);
				}
				catch (System.Exception e)
				{
					Debug.LogException(e);
				}
				_startTime = System.DateTime.Now;
				if (_modeIndex == 0)
				{
					_enumerator = CoWwwSyncWrite(files, _cachePath);
				}
				else if (_modeIndex == 1)
				{
					_enumerator = CoUwr(files, _cachePath, sync: true);
				}
				else if (_modeIndex == 2)
				{
					_enumerator = CoUwr(files, _cachePath, sync: false);
				}
				else if (_modeIndex == 3)
				{
					_enumerator = CoStandard(files, _cachePath);
				}
			}
		}
	}

	void DeleteCache(string path)
	{
		try
		{
			System.IO.Directory.Delete(path, true); // 超危険。サンプルでなければ絶対やらない
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
		}
	}

	IEnumerator CoWwwSyncWrite(string[] files, string cachePath)
	{
		for (int i = 0; i < files.Length; i++)
		{
			var prevBytes = 0;
			var srcPath = _root + files[i];
			var req = new WWW(srcPath);
			while (!req.isDone)
			{
				_doneBytes += (req.bytesDownloaded - prevBytes);
				prevBytes = req.bytesDownloaded;
				yield return null;
			}
			_doneBytes += (req.bytesDownloaded - prevBytes);
			if (req.error != null)
			{
				Debug.LogError(i + " " + srcPath + " " + req.error);
			}
			else
			{
				System.IO.File.WriteAllBytes(cachePath + "/" + files[i], req.bytes);
			}
			req.Dispose();
			_doneFileCount++;
		}
	}

	IEnumerator CoUwr(string[] files, string cachePath, bool sync)
	{
		for (int i = 0; i < files.Length; i++)
		{
			var prevBytes = 0;
			var srcPath = _root + files[i];
			var req = UnityWebRequest.Get(srcPath);
			req.SendWebRequest();
			while (!req.isDone)
			{
				_doneBytes += ((int)req.downloadedBytes - prevBytes);
				prevBytes = (int)req.downloadedBytes;
				yield return null;
			}
			_doneBytes += ((int)req.downloadedBytes - prevBytes);
			if (req.error != null)
			{
				Debug.LogError(i + " " + srcPath + " " + req.error);
			}
			else
			{
				if (sync)
				{
					System.IO.File.WriteAllBytes(cachePath + "/" + files[i], req.downloadHandler.data);
				}
				else
				{
					while (!_fileWriter.TryWrite(files[i], req.downloadHandler.data))
					{
						yield return null;
					}
				}
			}
			req.Dispose();
			_doneFileCount++;
		}
	}

	IEnumerator CoStandard(string[] files, string cachePath)
	{
		for (int i = 0; i < files.Length; i++)
		{
			var prevBytes = 0;
			var srcPath = _root + files[i];
			var req = UnityWebRequestAssetBundle.GetAssetBundle(srcPath, new Hash128(), 0);
			req.SendWebRequest();
			while (!req.isDone)
			{
				_doneBytes += ((int)req.downloadedBytes - prevBytes);
				prevBytes = (int)req.downloadedBytes;
				yield return null;
			}
			_doneBytes += ((int)req.downloadedBytes - prevBytes);
			if (req.error != null)
			{
				Debug.LogError(i + " " + srcPath + " " + req.error);
			}
			var ab = DownloadHandlerAssetBundle.GetContent(req);
			ab.Unload(true);
			req.Dispose();
			_doneFileCount++;
		}
	}
}
