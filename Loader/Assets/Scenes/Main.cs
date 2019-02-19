#define USE_CALLBACK
//#define USE_COROUTINE
#if USE_CALLBACK
#define USE_HANDLE_HOLDER
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
	[SerializeField]
	Transform _textRoot;
	[SerializeField]
	UnityEngine.UI.Text _textPrefab;
	[SerializeField]
	UnityEngine.UI.Text _dump;

	UnityEngine.UI.Text[] _texts;
	Kayac.Loader _loader;
	const int HandleCount = 16;
#if USE_HANDLE_HOLDER
	GameObject _handleHolder;
#else
	Kayac.LoadHandle[] _handles;
#endif
	Kayac.FileLogHandler _log;
	bool _autoTest;

	class AssetBundleDatabase : Kayac.Loader.IAssetBundleDatabase
	{
		public AssetBundleDatabase()
		{
			var manifestAssetBundle = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/StreamingAssets");
			_manifest = manifestAssetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
		}

		public void ParseIdentifier(
			out string assetBundleName, // アセットバンドルのパス(Loaderに渡したrootからの相対)
			out string assetName, // アセットのフルパス
			string assetIdentifier)
		{
			var lastSlashPos = assetIdentifier.LastIndexOf('/');
			assetBundleName = assetIdentifier.Substring(0, lastSlashPos);
			assetName = "Assets/Build/" + assetIdentifier;
		}

		// アセットバンドル名からhashとcrcを得る
		public void GetAssetBundleMetaData(
			out Hash128 hash,
			out uint crc,
			string assetBundleName)
		{
			hash = _manifest.GetAssetBundleHash(assetBundleName);
			crc = 0;
		}
		AssetBundleManifest _manifest;
	}

	void Start()
	{
		var database = new AssetBundleDatabase();

		var downloadRoot = "file:///" + Application.streamingAssetsPath + "/";
#if !USE_HANDLE_HOLDER
		_handles = new Kayac.LoadHandle[HandleCount];
#endif

#if UNITY_EDITOR || UNITY_STANDALONE_OSX
		var storageCacheRoot = Application.dataPath + "/..";
#elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
		var storageCacheRoot = Application.dataPath;
#else
		var storageCacheRoot = Application.persistentDataPath;
#endif
		storageCacheRoot += "/AssetBundleCache/";
		_loader = new Kayac.Loader(downloadRoot, database);
		// ログファイルへ
		_log = new Kayac.FileLogHandler("log.txt");
		_texts = new UnityEngine.UI.Text[HandleCount];
		for (int i = 0; i < HandleCount; i++)
		{
			_texts[i] = Instantiate(_textPrefab, _textRoot, false);
		}
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.F1))
		{
			_autoTest = !_autoTest;
		}

		_loader.Update();
		for (int i = 0; i < HandleCount; i++)
		{
			Update(i);
		}

		if (_autoTest)
		{
			if (UnityEngine.Random.Range(0f, 1f) < 0.01f)
			{
				OnClickClearStorageCacheButton();
			}
			if (UnityEngine.Random.Range(0f, 1f) < 0.1f)
			{
				Release();
			}
		}
	}

	public void Release()
	{
#if USE_HANDLE_HOLDER
			Destroy(_handleHolder);
#else
			for (int i = 0; i < _handles.Length; i++)
			{
				_handles[i] = null;
			}
#endif
	}

	public void OnClickClearStorageCacheButton()
	{
		var t0 = Time.realtimeSinceStartup;
		bool result = _loader.ClearStorageCache();
		var t1 = Time.realtimeSinceStartup;
		Debug.Log("ClearCache : " + result + " " + (t1 - t0));
	}

	void OnLoadComplete(UnityEngine.Object asset, int index)
	{
		var textAsset = asset as TextAsset;
		if (textAsset != null)
		{
			_texts[index].text = textAsset.name + " size: " + textAsset.bytes.Length;
		}
		else
		{
			Debug.LogError("asset is null. " + index);
		}
	}

	string MakeRandomAssetName()
	{
		int ab = UnityEngine.Random.Range(0, 1000);
		int file = UnityEngine.Random.Range(0, 10);
		var path = string.Format("{0}/{1}.txt", ab, file);
		return path;
	}

	void Update(int i)
	{
#if USE_HANDLE_HOLDER
		if ((_handleHolder == null) || (_handleHolder.GetComponent<Kayac.LoadHandleHolder>().handleCount < HandleCount))
#else
		if (_handles[i] == null)
#endif
		{
			var path = MakeRandomAssetName();
			_texts[i].text = path + " loading...";

#if USE_CALLBACK
#if USE_HANDLE_HOLDER
			if (_handleHolder == null)
			{
				_handleHolder = new GameObject("HandleHolder");
				_handleHolder.transform.SetParent(gameObject.transform, false);
			}
			_loader.Load(path, asset =>
			{
				if (asset != null)
				{
					OnLoadComplete(asset, i);
				}
			},
			_handleHolder);
#else
			_handles[i] = _loader.Load(path, asset =>
			{
				if (asset != null)
				{
					OnLoadComplete(asset, i);
				}
			});

#endif
#elif USE_COROUTINE
			StartCoroutine(CoLoad(i, path));
#else
			_handles[i] = _loader.Load(path);
#endif
		}

#if !USE_CALLBACK && !USE_COROUTINE
		if ((_handles[i] != null) && _handles[i].succeeded)
		{
			OnLoadComplete(_handles[i], i);
		}
#endif
		_dump.text = _loader.Dump();
	}

#if USE_COROUTINE
	IEnumerator CoLoad(int i, string path)
	{
		_handles[i] = _loader.Load(path);
		if (!_handles[i].isDone)
		{
			yield return _handles[i];
		}
		if (_handles[i].succeeded)
		{
			OnLoadComplete(_handles[i].asset, i);
		}
	}
#endif
}
