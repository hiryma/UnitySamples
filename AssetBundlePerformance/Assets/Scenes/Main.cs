using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class Main : MonoBehaviour
{
	public Text _fpsText;
	public Text _stateText;
	public Dropdown _modeDropdown;
	public InputField _parallelCountInputField;
	public InputField _inputBufferSizeInputField;
	public InputField _writerBufferSizeInputField;
	public Toggle _shuffleToggle;
	public Button _button;
	enum Mode
	{
		WwwSyncWrite,
		UwrSyncWrite,
		UwrAsyncWrite,
		UwrDownloadHandlerFile,
		UnityWebRequestAssetBundle,
	};
	string _root;
	string _cachePath;
	int _doneFileCount;
	int _totalFileCount;
	int _doneBytes;
	int _maxSpike;
	float _time;
	int _maxMemory;
	bool _testing;
	IEnumerator _enumerator;
	Kayac.FrameTimeWatcher _frametimeWatcher;
	Kayac.AssetBundleMetaData[] _metaData;
	Kayac.AssetBundleMetaData[] _shuffledMetaData;
	Kayac.FileLogHandler _log;
	Kayac.FileLogHandler _appendLog;
	Kayac.FileWriter _writer;

	void Start()
	{
		// 設定。環境に合わせていじっていい
		_cachePath = Application.dataPath;
#if !UNITY_EDITOR && UNITY_STANDALONE_OSX
		_cachePath += "/../.."; //MACはContentsの外まで戻す
#else
		_cachePath += "/.."; //Assetの横でいい
#endif
		_cachePath += "/AssetBundleCache";

		var metaDataJson = File.ReadAllText(Application.streamingAssetsPath + "/assetbundle_metadata.json");
		var metaDataContainer = JsonUtility.FromJson<Kayac.AssetBundleMetaDataContainer>(metaDataJson);
		_metaData = metaDataContainer.items;
		_shuffledMetaData = new Kayac.AssetBundleMetaData[_metaData.Length];
		Array.Copy(_metaData, _shuffledMetaData, _metaData.Length);
		Shuffle(_shuffledMetaData);

#if false // 単純FileIO。最速なので、書き込み側オーバーヘッドの測定に用いる。
		_root = "file://" + Application.dataPath + "/../AssetBundleBuild/";
#elif false // ローカルからのダウンロード。一応httpを経由させたい範囲で速度が欲しい時、簡易的に用いる。
		_root = "http://localhost/~hirayama-takashi/AssetBundlePerformanceTestData/";
#else // 遠隔からのダウンロード。準備が必要。
		_root = "https://hiryma.github.io/AssetBundlePerformanceTestData/";
#endif

		// 落とすサーバとファイルリストを上書き
		try
		{
			var customListPath = Application.streamingAssetsPath + "/list.txt";
			if (System.IO.File.Exists(customListPath))
			{
				var file = new StreamReader(customListPath);
				_root = file.ReadLine(); // 1行目がサーバ。例えばhttp://localhost/~hirayama-takashi/hoge/"
				var tmpList = new List<string>();
				while (!file.EndOfStream) // 2行目以降がassetBundleファイル名
				{
					var line = file.ReadLine();
					if (!string.IsNullOrEmpty(line))
					{
						tmpList.Add(line + ".unity3d"); // リストに拡張子がついてるなら、あるいは拡張子なしなら抜いてください
					}
				}
				_metaData = new Kayac.AssetBundleMetaData[tmpList.Count];
				for (int i = 0; i < tmpList.Count; i++)
				{
					_metaData[i].name = tmpList[i];
					_metaData[i].hash = null;
					_metaData[i].size = 0; // 不明
				}
			}
		}
		catch
		{
		}

		// 以下は初期化実コード
		_log = new Kayac.FileLogHandler("log.txt");
		_appendLog = new Kayac.FileLogHandler("appendLog.txt", append: true);
		_modeDropdown.ClearOptions();
		var modeNames = System.Enum.GetNames(typeof(Mode));
		foreach (var mode in modeNames)
		{
			var option = new Dropdown.OptionData();
			option.text = mode;
			_modeDropdown.options.Add(option);
		}
		_modeDropdown.value = 0;
		_modeDropdown.captionText.text = modeNames[0];

		_frametimeWatcher = new Kayac.FrameTimeWatcher();
	}

	void Update()
	{
		_log.Update();
		_appendLog.Update();
		_frametimeWatcher.Update();
		if (_testing)
		{
			_maxMemory = Mathf.Max(_maxMemory, (int)UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong());
			_maxSpike = Mathf.Max(_maxSpike, _frametimeWatcher.maxFrameTime);
		}
		var mode = (Mode)_modeDropdown.value;
		if (_enumerator != null)
		{
			if (!_enumerator.MoveNext())
			{
				_enumerator = null;

				string msg = mode + " : " + _time.ToString("F2");
				if (mode != Mode.WwwSyncWrite)
				{
					int slotCount;
					int.TryParse(_parallelCountInputField.text, out slotCount);
					slotCount = Mathf.Max(slotCount, 1);
					msg += " Parallel: " + slotCount;
					msg += " Shuffle: " + _shuffleToggle.isOn;
					msg += " MaxSpike: " + _maxSpike;
					msg += " MaxMem: " + (_maxMemory / (1024 * 1024));
				}
				if (mode == Mode.UwrAsyncWrite)
				{
					int writerBufferSize;
					int.TryParse(_writerBufferSizeInputField.text, out writerBufferSize);
					writerBufferSize = Mathf.Max(writerBufferSize, 1);
					writerBufferSize *= 1024;
					int inputBufferSize;
					int.TryParse(_inputBufferSizeInputField.text, out inputBufferSize);
					inputBufferSize = Mathf.Max(inputBufferSize, 1);
					inputBufferSize *= 1024;
					msg += " WriterBufferSize: " + writerBufferSize;
					msg += " InputBufferSize: " + inputBufferSize;
				}
				Debug.Log(msg);
			}
		}

		_fpsText.text = _frametimeWatcher.averageFrameTime.ToString() + " MaxSpike: " + _maxSpike.ToString() + " Time: " + _time.ToString("F2") + " MaxMem: " + (_maxMemory / (1024 * 1024));
		var restBytes = (_writer != null) ? _writer.restBytes : 0;
		_stateText.text = string.Format("FileCount:{0}/{1} Read:{2} RestWrite:{3}", _doneFileCount, _totalFileCount, _doneBytes, restBytes);
		_button.interactable = (_enumerator == null);
		_inputBufferSizeInputField.interactable = (mode == Mode.UwrAsyncWrite);
		_writerBufferSizeInputField.interactable = (mode == Mode.UwrAsyncWrite);
	}

	public void OnClickStart()
	{
		int slotCount;
		int.TryParse(_parallelCountInputField.text, out slotCount);
		slotCount = Mathf.Max(slotCount, 1);
		var mode = (Mode)_modeDropdown.value;
		switch (mode)
		{
			case Mode.WwwSyncWrite:
				_enumerator = CoWwwSyncWrite(_cachePath);
				break;
			case Mode.UwrSyncWrite:
			case Mode.UwrAsyncWrite:
			case Mode.UwrDownloadHandlerFile:
			case Mode.UnityWebRequestAssetBundle:
				_enumerator = CoUwr(_cachePath, slotCount, mode);
				break;
		}
	}

	void DeleteCache(string path)
	{
		try
		{
			if (System.IO.Directory.Exists(path))
			{
				System.IO.Directory.Delete(path, true); // 第二引数trueは超危険。サンプルでなければ絶対やらない
			}
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
		}
	}

	IEnumerator CoPrepare(Mode mode)
	{
		if (_writer != null)
		{
			while (_writer.restBytes > 0) // 書き込み待ち
			{
				yield return null;
			}
			_writer.Dispose();
			_writer = null;
		}
		var cache = Caching.GetCacheByPath(_cachePath);
		if (cache.valid)
		{
			Caching.RemoveCache(cache);
		}
		DeleteCache(_cachePath);
		try
		{
			System.IO.Directory.CreateDirectory(_cachePath);
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
		}
		if (mode == Mode.UnityWebRequestAssetBundle)
		{
			while (!Caching.ready)
			{
				yield return null;
			}
			cache = Caching.AddCache(_cachePath);
			Caching.currentCacheForWriting = cache;
		}
		System.GC.Collect();
		for (int i = 0; i < 60; i++) // frametimeWatcherのmaxを吐き出させる
		{
			yield return null;
		}

		_totalFileCount = _metaData.Length;
		_doneFileCount = 0;
		_doneBytes = 0;
		_maxSpike = 0;
		_time = 0f;
		_maxMemory = 0;
	}

	IEnumerator CoWwwSyncWrite(string cachePath)
	{
		var prepare = CoPrepare(Mode.WwwSyncWrite);
		while (prepare.MoveNext())
		{
			yield return null;
		}

		var startTime = System.DateTime.Now;
		_testing = true;

		var metaData = _shuffleToggle.isOn ? _shuffledMetaData : _metaData;
		for (int i = 0; i < metaData.Length; i++)
		{
			var meta = metaData[i];
			var name = meta.name;
			var prevBytes = 0;
			var srcPath = _root + name;
			var req = new WWW(srcPath);
//			_log.Write("Begin: " + _files[i]);
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
				System.IO.File.WriteAllBytes(cachePath + "/" + name, req.bytes);
			}
			req.Dispose();
			_doneFileCount++;
//			_log.Write("End: " + file);
		}
		_time = (float)((System.DateTime.Now - startTime).TotalSeconds);
		_testing = false;

		var coVerify = CoVerify(Mode.WwwSyncWrite);
		while (coVerify.MoveNext())
		{
			yield return null;
		}
	}

	class Slot
	{
		public void Update(
			Mode mode,
			Kayac.FileLogHandler log,
			ref int doneBytes,
			ref int doneFileCount) // サンプルじゃなかったらこんな雑なrefの用法はしない
		{
			if (req != null)
			{
				var currentBytes = (int)req.downloadedBytes;
				if (req.error != null)
				{
					Debug.LogError("Error: " + req.url + " " + req.error);
					req.Dispose();
					req = null;
					prevBytes = 0;
					doneFileCount++;
				}
				else if (req.isDone)
				{
//					DumpResponse(log);
//					log.Write("End: " + path + " " + currentBytes);
					if (mode == Mode.UwrSyncWrite)
					{
//						log.Write("Write: " + path);
						System.IO.File.WriteAllBytes(cachePath + "/" + path, req.downloadHandler.data);
					}
					else if (mode == Mode.UnityWebRequestAssetBundle)
					{
						// 一回取り出してやる必要がある?ない?よくわからない
						var ab = DownloadHandlerAssetBundle.GetContent(req);
						if (ab != null)
						{
							ab.Unload(true);
						}
					}
					doneBytes += (currentBytes - prevBytes);
					req.Dispose();
					req = null;
					prevBytes = 0;
					doneFileCount++;
				}
				else
				{
					doneBytes += (currentBytes - prevBytes);
					prevBytes = currentBytes;
				}
			}

			if (writerHandle != null)
			{
				if (writerHandle.done)
				{
//					log.Write("WriteDone: " + path);
					writerHandle = null;
				}
			}
		}

		void DumpResponse(Kayac.FileLogHandler log)
		{
			var sb = new System.Text.StringBuilder();
			sb.AppendFormat("[ResponseHeader] {0} length:{1}\n", req.url, req.downloadedBytes);
			foreach (var item in req.GetResponseHeaders())
			{
				sb.AppendFormat("\t{0}: {1}\n", item.Key, item.Value);
			}
			log.Write(sb.ToString());
		}

		public bool isDone
		{
			get
			{
				return (req == null) && (writerHandle == null);
			}
		}
		public UnityWebRequest req;
		public string path;
		public string cachePath;
		public byte[] inputBuffer;
		public int prevBytes;
		public Kayac.FileWriter.Handle writerHandle;
	}

	IEnumerator CoUwr(string cachePath, int slotCount, Mode mode)
	{
		var prepare = CoPrepare(mode);
		while (prepare.MoveNext())
		{
			yield return null;
		}
		var startTime = System.DateTime.Now;
		_testing = true;

		var slots = new Slot[slotCount];
		for (int i = 0; i < slots.Length; i++)
		{
			slots[i] = new Slot();
			slots[i].cachePath = cachePath;
			slots[i].prevBytes = 0;
		}

		if (mode == Mode.UwrAsyncWrite)
		{
			UnityEngine.Profiling.Profiler.BeginSample("CoUwr.BufferAllocation");

			int writerBufferSize;
			int.TryParse(_writerBufferSizeInputField.text, out writerBufferSize);
			writerBufferSize = Mathf.Max(writerBufferSize, 1);
			writerBufferSize *= 1024;

			int inputBufferSize;
			int.TryParse(_inputBufferSizeInputField.text, out inputBufferSize);
			inputBufferSize = Mathf.Max(inputBufferSize, 1);
			inputBufferSize *= 1024;

			_writer = new Kayac.FileWriter(_cachePath, writerBufferSize);

			for (int i = 0; i < slots.Length; i++)
			{
				slots[i].inputBuffer = new byte[inputBufferSize];
			}
			UnityEngine.Profiling.Profiler.EndSample();
		}

		int fileIndex = 0;
		int slotIndex = 0;
		var metaData = _shuffleToggle.isOn ? _shuffledMetaData : _metaData;
		while (true)
		{
			slots[slotIndex].Update(mode, _log, ref _doneBytes, ref _doneFileCount);
			if (slots[slotIndex].isDone)
			{
				UnityEngine.Profiling.Profiler.BeginSample("Main.CoUwr: createUnityWebRequest");
				var slot = slots[slotIndex];
				var meta = metaData[fileIndex];
				slot.path = meta.name;
				// モードごとにdownloadHandlerを差し、書き込み準備をする
				slot.req = new UnityWebRequest(_root + slot.path);
				if (mode == Mode.UwrSyncWrite)
				{
					slot.req.downloadHandler = new DownloadHandlerBuffer();
				}
				else if (mode == Mode.UwrAsyncWrite)
				{
					slot.writerHandle = _writer.Begin(slot.path);
					slot.req.downloadHandler = new Kayac.DownloadHandlerFileWriter(
						_writer,
						slot.writerHandle,
						slot.inputBuffer);
				}
				else if (mode == Mode.UwrDownloadHandlerFile)
				{
					slot.req.downloadHandler = new DownloadHandlerFile(cachePath + "/" + slot.path);
				}
				else if (mode == Mode.UnityWebRequestAssetBundle)
				{
					slot.req.downloadHandler = new DownloadHandlerAssetBundle(
						_root + slot.path,
						meta.GenerateHash128(),
						crc: 0);
				}
				else
				{
					Debug.Assert(false);
				}
				slot.req.SendWebRequest();
				fileIndex++; // 最後までやったので終わる
				UnityEngine.Profiling.Profiler.EndSample();
				if (fileIndex == metaData.Length)
				{
					break;
				}
//				_log.Write("Begin: " + slot.req.url);
			}
			slotIndex++;
			if (slotIndex == slots.Length) // 全スロット見たところでyield
			{
				slotIndex = 0;
				yield return null;
			}
		}
		// 全スロット終了を待つ
		slotIndex = 0;
		while (slotIndex < slots.Length)
		{
			var slot = slots[slotIndex];
			slot.Update(mode, _log, ref _doneBytes, ref _doneFileCount);
			if (slot.isDone) // 終わってれば次へ
			{
				slotIndex++;
			}
			else
			{
				yield return null;
			}
		}
		_time = (float)((System.DateTime.Now - startTime).TotalSeconds);
		_testing = false;

		var coVerify = CoVerify(mode);
		while (coVerify.MoveNext())
		{
			yield return null;
		}
	}

	IEnumerator CoVerify(Mode mode)
	{
		_doneFileCount = 0;
		for (int i = 0; i < _metaData.Length; i++)
		{
			var meta = _metaData[i];
			var name = meta.name;
			AssetBundle ab = null;
			if (mode == Mode.UnityWebRequestAssetBundle)
			{
				var req = new UnityWebRequest(_root + _metaData[i].name);
				req.downloadHandler = new DownloadHandlerAssetBundle(
					_root + name,
					meta.GenerateHash128(),
					crc: 0);
				req.SendWebRequest();
				while (!req.isDone)
				{
					yield return null;
				}
				ab = DownloadHandlerAssetBundle.GetContent(req);
			}
			else
			{
				ab = AssetBundle.LoadFromFile(_cachePath + "/" + name);
			}
			if (ab == null)
			{
				Debug.LogError("Load Failed: " + name);
			}
			else
			{
				ab.Unload(true);
			}
			_doneFileCount++;
		}
	}

	// 第2引数はnullならUnityのrandomを使う
	static void Shuffle<T>(T[] a) // Fisher-Yates shuffle.
	{
		Debug.Assert(a != null);
		var n = a.Length;
		for (int i = 0; i < n; i++)
		{
			int srcIndex = 0;
			srcIndex = UnityEngine.Random.Range(i, n);
			var tmp = a[srcIndex];
			a[srcIndex] = a[i];
			a[i] = tmp; // これでi番は確定
		}
	}
}
