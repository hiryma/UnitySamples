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
	public InputField _bufferSizeInputField;
	public Button _button;
	enum Mode
	{
		WwwSyncWrite,
		UwrSyncWrite,
		UwrAsyncWrite,
		UwrDivAsyncWrite,
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
	IEnumerator _enumerator;
	Kayac.FileWriter _fileWriter;
	Kayac.FrameTimeWatcher _frametimeWatcher;
	string[] _files;
	Kayac.FileLogHandler _log;
	Kayac.FileLogHandler _appendLog;

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

		var ab = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/AssetBundleBuild");
		var manifest = ab.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
		_files = manifest.GetAllAssetBundles();

#if true
		_root = "file://" + Application.dataPath + "/../AssetBundleBuild/"; // ファイル
#elif false
		_root = "http://localhost/~hirayama-takashi/AssetBundlePerformanceTestData/"; // ローカルサーバ
#else
		_root = "https://hiryma.github.io/AssetBundlePerformanceTestData/"; // 遠隔サーバ
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
				_files = tmpList.ToArray();
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
		_fileWriter = new Kayac.FileWriter(_cachePath);
	}

	void Update()
	{
		_frametimeWatcher.Update();
		_maxMemory = Mathf.Max(_maxMemory, (int)UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong());
		_maxSpike = Mathf.Max(_maxSpike, _frametimeWatcher.maxFrameTime);
		if (_enumerator != null)
		{
			if (!_enumerator.MoveNext())
			{
				_enumerator = null;
				var mode = (Mode)_modeDropdown.value;
				int slotCount;
				int.TryParse(_parallelCountInputField.text, out slotCount);
				slotCount = Mathf.Max(slotCount, 1);
				int bufferSizeTotal;
				int.TryParse(_bufferSizeInputField.text, out bufferSizeTotal);
				bufferSizeTotal = Mathf.Max(bufferSizeTotal, 1);
				bufferSizeTotal *= 1024 * 1024;

				Debug.Log(mode + " : " + _time.ToString("F2") + " Parallel: " + slotCount + " BufferSize: " + bufferSizeTotal + " MaxSpike: " + _maxSpike + " MaxMem: " + (_maxMemory / (1024 * 1024)));
			}
		}

		_fpsText.text = _frametimeWatcher.averageFrameTime.ToString() + " MaxSpike: " + _maxSpike.ToString() + " Time: " + _time.ToString("F2") + " MaxMem: " + (_maxMemory / (1024 * 1024));
		_stateText.text = string.Format("FileCount:{0}/{1} Read:{2} RestWrite:{3}", _doneFileCount, _totalFileCount, _doneBytes, _fileWriter.restBytes);
		_button.interactable = (_enumerator == null);
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
			case Mode.UwrDivAsyncWrite:
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

		_totalFileCount = _files.Length;
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

		for (int i = 0; i < _files.Length; i++)
		{
			var prevBytes = 0;
			var srcPath = _root + _files[i];
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
				System.IO.File.WriteAllBytes(cachePath + "/" + _files[i], req.bytes);
			}
			req.Dispose();
			_doneFileCount++;
//			_log.Write("End: " + _files[i]);
		}
		_time = (float)((System.DateTime.Now - startTime).TotalSeconds);

		var coTest = CoTest(Mode.WwwSyncWrite);
		while (coTest.MoveNext())
		{
			yield return null;
		}
	}

	class Slot
	{
		public void Update(
			Kayac.FileWriter writer,
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
					else if (mode == Mode.UwrAsyncWrite)
					{
//						log.Write("Write: " + path);
						var data = req.downloadHandler.data;
						writer.Write(writerHandle, data, 0, data.Length);
						writer.End(writerHandle);
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
		public byte[] outputBuffer;
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

		var slots = new Slot[slotCount];
		for (int i = 0; i < slots.Length; i++)
		{
			slots[i] = new Slot();
			slots[i].cachePath = cachePath;
			slots[i].prevBytes = 0;
		}

		if (mode == Mode.UwrDivAsyncWrite)
		{
			UnityEngine.Profiling.Profiler.BeginSample("CoUwr.BufferAllocation");
			int bufferSizeTotal;
			int.TryParse(_bufferSizeInputField.text, out bufferSizeTotal);
			bufferSizeTotal = Mathf.Max(bufferSizeTotal, 1);
			bufferSizeTotal *= 1024 * 1024;
			// input,outputで2分、さらにslotCountで割った値を個別に設定する
			for (int i = 0; i < slots.Length; i++)
			{
				slots[i].inputBuffer = new byte[bufferSizeTotal / slotCount / 2];
				slots[i].outputBuffer = new byte[bufferSizeTotal / slotCount / 2];
			}
			UnityEngine.Profiling.Profiler.EndSample();
		}

		int fileIndex = 0;
		int slotIndex = 0;
		while (true)
		{
			slots[slotIndex].Update(_fileWriter, mode, _log, ref _doneBytes, ref _doneFileCount);
			if (slots[slotIndex].isDone)
			{
				var slot = slots[slotIndex];
				slot.path = _files[fileIndex];
				// モードごとにdownloadHandlerを差し、書き込み準備をする
				slot.req = new UnityWebRequest(_root + slot.path);
				if (mode == Mode.UwrSyncWrite)
				{
					slot.req.downloadHandler = new DownloadHandlerBuffer();
				}
				else if (mode == Mode.UwrAsyncWrite)
				{
					slot.req.downloadHandler = new DownloadHandlerBuffer();
					slot.writerHandle = _fileWriter.Begin(slot.path);
				}
				else if (mode == Mode.UwrDivAsyncWrite)
				{
					slot.writerHandle = _fileWriter.Begin(slot.path);
					slot.req.downloadHandler = new Kayac.DownloadHandlerAsyncFile(
						_fileWriter,
						slot.writerHandle,
						slot.inputBuffer,
						slot.outputBuffer);
				}
				else if (mode == Mode.UwrDownloadHandlerFile)
				{
					slot.req.downloadHandler = new DownloadHandlerFile(cachePath + "/" + slot.path);
				}
				else if (mode == Mode.UnityWebRequestAssetBundle)
				{
					slot.req.downloadHandler = new DownloadHandlerAssetBundle(_root + slot.path, new Hash128(), crc: 0);
				}
				else
				{
					Debug.Assert(false);
				}
				slot.req.SendWebRequest();
				fileIndex++; // 最後までやったので終わる
				if (fileIndex == _files.Length)
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
			slot.Update(_fileWriter, mode, _log, ref _doneBytes, ref _doneFileCount);
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

		var coTest = CoTest(mode);
		while (coTest.MoveNext())
		{
			yield return null;
		}
	}

	IEnumerator CoTest(Mode mode)
	{
		_doneFileCount = 0;
		for (int i = 0; i < _files.Length; i++)
		{
			AssetBundle ab = null;
			if (mode == Mode.UnityWebRequestAssetBundle)
			{
				var req = new UnityWebRequest(_root + _files[i]);
				req.downloadHandler = new DownloadHandlerAssetBundle(_root + _files[i], new Hash128(), crc: 0);
				req.SendWebRequest();
				while (!req.isDone)
				{
					yield return null;
				}
				ab = DownloadHandlerAssetBundle.GetContent(req);
			}
			else
			{
				ab = AssetBundle.LoadFromFile(_cachePath + "/" + _files[i]);
			}
			if (ab == null)
			{
				Debug.LogError("Load Failed: " + _files[i]);
			}
			else
			{
				ab.Unload(true);
			}
			_doneFileCount++;
		}
	}
}
