using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class Main : MonoBehaviour
{
	const int FileCount = 19;
	const int HandleCount = 64;
	const int ReleaseWaitMax = 8;
	const float DeferenceScreenHeight = 432f;
	const int RandSeed = 1;
	uint _rand = RandSeed;

	public Text dump;
	public Canvas canvas;
	public Toggle autoTestToggle;
	public int downloadParallelCount;
	public int memoryCacheLimitMB;
	public int loadLimitMB = 256;

	RawImage[] _images;
	Kayac.Loader _loader;
	GameObject _handleHolder;
	Kayac.FileLogHandler _log;
	int _releasedFrame;
	bool _listFileExists;
	System.Text.StringBuilder _sb;
	int _releaseWait;
	AssetFileDatabase _database;
	List<string> _fileList;

	class AssetFileDatabase : Kayac.Loader.AssetFileDatabase
	{
		public void SetHashMap(Dictionary<string, Kayac.FileHash> hashMap)
		{
			_hashMap = hashMap;
		}
		public override bool ParseIdentifier(
			out string assetFileName, // ファイル名(Loaderに渡したroot相対なのでフォルダがあるならそれも含む)
			out string assetName, // ファイル内でアセットを識別する名前
			string assetIdentifier) // コード内から指定された識別子。今回はurlそのもの。
		{
			assetFileName = assetIdentifier; // そのままで良い
			assetName = Path.GetFileNameWithoutExtension(assetIdentifier);
			return true;
		}

		public override bool GetFileMetaData(
			out Kayac.FileHash hash, // アセットファイルのバージョンを示すハッシュ
			out int sizeBytes,
			string fileName)
		{
			sizeBytes = 0;
			return _hashMap.TryGetValue(fileName, out hash);
		}
		Dictionary<string, Kayac.FileHash> _hashMap;
	}

	void Start()
	{
		_log = new Kayac.FileLogHandler("log.txt");
#if UNITY_EDITOR
		var storageCacheRoot = Application.dataPath;
#else
		var storageCacheRoot = Application.persistentDataPath;
#endif
		storageCacheRoot += "/../AssetFileCache";
		// キャッシュのスキャンはできるだけ早く始めた方が良い
		_loader = new Kayac.Loader(storageCacheRoot, useHashInStorageCache: true);

		_sb = new System.Text.StringBuilder();
		_images = new RawImage[HandleCount];
		int sqrtHandleCount = Mathf.FloorToInt(Mathf.Sqrt((float)HandleCount));
		float imageSize = DeferenceScreenHeight / sqrtHandleCount;
		for (int i = 0; i < HandleCount; i++)
		{
			var go = new GameObject(i.ToString());
			_images[i] = go.AddComponent<RawImage>();
			var rect = _images[i].rectTransform;
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.zero;
			rect.pivot = Vector2.zero;
			rect.sizeDelta = Vector2.one * imageSize;
			rect.anchoredPosition = new Vector2(imageSize * (i / sqrtHandleCount), imageSize * (i % sqrtHandleCount));
			rect.SetParent(canvas.transform, false);
		}

		_database = new AssetFileDatabase();

		string downloadRoot;
		_fileList = new List<string>();
		if (!ReadAssetFileList(out downloadRoot))
		{
			for (int i = 1; i < FileCount; i++)
			{
				var path = string.Format("stamp_{0}.png", i);
				_fileList.Add(path);
			}
			downloadRoot = "file://" + Application.dataPath + "/../AssetBuild";
		}
		UpdateHashMap();

		if (downloadParallelCount < 1)
		{
			downloadParallelCount = 1;
		}
		// Databaseの準備ができたらロード可能にするためにStartを呼ぶ
		_loader.Start(
			downloadRoot,
			_database,
			downloadParallelCount);
		_loader.SetMemoryCacheLimit(this.memoryCacheLimitMB * 1024 * 1024);
		_loader.SetLoadLimit(this.loadLimitMB * 1024 * 1024);
	}

	bool ReadAssetFileList(out string downloadRoot)
	{
		downloadRoot = null;
		var ret = false;
		// 落とすサーバとファイルリストを上書き
		try
		{
			var customListPath = "list.txt";
			if (System.IO.File.Exists(customListPath))
			{
				var file = new StreamReader(customListPath);
				downloadRoot = file.ReadLine(); // 1行目がサーバ。例えばhttp://localhost/~hirayama-takashi/hoge/"
				while (!file.EndOfStream) // 2行目以降がassetBundleファイル名
				{
					var line = file.ReadLine();
					if (!string.IsNullOrEmpty(line))
					{
						var path = line + ".unity3d";
						_fileList.Add(path);
					}
				}
				ret = true;
				_listFileExists = true;
			}
		}
		catch (System.Exception e)
		{
			Debug.LogException(e);
		}
		return ret;
	}

	public void UpdateHashMap()
	{
		var hashMap = new Dictionary<string, Kayac.FileHash>();
		for (int i = 0; i < _fileList.Count; i++)
		{
			var hash128 = new Hash128(Rand(), Rand(), Rand(), Rand()); // Unityのハッシュが先にある場合、変換可能
			var hash = new Kayac.FileHash(hash128);
			Debug.Assert(hash128.ToString() == hash.ToString());
			hashMap.Add(_fileList[i], hash);
		}
		_database.SetHashMap(hashMap);
	}

	public void DownloadAll()
	{
		int fileCount;
		long sizeBytes;
		_loader.CheckDownload(out fileCount, out sizeBytes, _fileList, useDependency: true);
		Debug.Log("CheckDownload: count=" + fileCount + " size=" + sizeBytes);
		_loader.Download(_fileList, OnError, useDependency: true, shuffle: true);
	}

	void Update()
	{
		_log.Update();

		_loader.Update();
		_sb.Length = 0;
		bool summaryOnly = (_fileList.Count > 20);
		_loader.Dump(_sb, summaryOnly);
		dump.text = _sb.ToString();

		// 破棄中は何もしない
		int sinceRelease = Time.frameCount - _releasedFrame;
		if (sinceRelease == (_releaseWait / 2))
		{
			System.GC.Collect(); // GC走らせて素材消す
		}
		else if (sinceRelease == _releaseWait)
		{
			if (autoTestToggle.isOn) // 自動なら一定確率でキャッシュ消す
			{
				if (UnityEngine.Random.Range(0f, 1f) < 0.5f)
				{
					ClearStorageCache();
				}
			}
		}
		if (sinceRelease < _releaseWait)
		{
			return;
		}

		if (autoTestToggle.isOn)
		{
			Load();
			if (UnityEngine.Random.Range(0f, 1f) < 0.1f)
			{
				Release();
			}
			if (UnityEngine.Random.Range(0f, 1f) < 0.1f)
			{
				if (!_listFileExists)
				{
					UpdateHashMap();
				}
			}
		}
	}

	public void Dump()
	{
		_sb.Length = 0;
		_loader.Dump(_sb, summaryOnly: false);
		Debug.Log(_sb.ToString());
	}

	public void Release()
	{
		Destroy(_handleHolder);
		foreach (var image in _images)
		{
			image.texture = null;
		}
		_releasedFrame = Time.frameCount;
		_releaseWait = UnityEngine.Random.Range(0, ReleaseWaitMax);
	}

	public void ClearStorageCache()
	{
		bool result = _loader.StartClearStorageCache();
	}

	public void Load()
	{
		if (_handleHolder != null) // ロード済みなので抜ける
		{
			return;
		}
		// ハンドル保持オブジェクトを生成
		_handleHolder = new GameObject("HandleHolder");
		_handleHolder.transform.SetParent(gameObject.transform, false);
		for (int i = 0; i < HandleCount; i++)
		{
			int indexCaptured = i; // ラムダから使う値をコピー
			var path = _fileList[UnityEngine.Random.Range(0, _fileList.Count)];
			_loader.Load(path, typeof(Texture2D),
			OnError,
			onComplete: asset =>
			{
				if (asset != null)
				{
					var texture = asset as Texture2D;
					if (texture != null)
					{
						_images[indexCaptured].texture = texture;
					}
					else
					{
						Debug.LogError("asset is not texture2D. name:" + asset.name + " type:" + asset.GetType() + " index:" + indexCaptured);
					}
				}
			},
			holderGameObject: _handleHolder);
		}
	}

	public void OnError(
		Kayac.Loader.Error errorType,
		string fileOrAssetName,
		System.Exception exception)
	{
		// typeに応じてポップアップを出すなどする
		switch (errorType)
		{
			case Kayac.Loader.Error.AssetTypeMismatch:
			case Kayac.Loader.Error.NoAssetInAssetBundle:
				break; // このサンプルでは中身知らずにロードしてたまたま見つかって画像だったら表示してるだけなので、これらは無視。
			default:
				Debug.LogError("Kayac.Loader error: " + errorType + " : " + fileOrAssetName + " : " + exception.GetType().Name + " : " + exception.Message);
				break;
		}
	}

	public uint Rand() // hash生成用。毎回起動直後は同じ値にしたいので自前で持つ
	{
		_rand ^= _rand >> 13;
		_rand ^= _rand << 17;
		_rand ^= _rand >> 5;
		return _rand;
	}
}
