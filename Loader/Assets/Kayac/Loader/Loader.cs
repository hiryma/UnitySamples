using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

namespace Kayac
{
	public class Loader
	{
		public interface IAssetBundleDatabase
		{
			// アセット識別子(パスとは限らない)からABのパス(Loaderに渡したroot相対)と、AB内に格納されたアセットのフルパスを引く
			void ParseIdentifier(
				out string assetBundleName, // アセットバンドルのパス(Loaderに渡したrootからの相対)
				out string assetName, // アセットのフルパス
				string assetIdentifier);
			// キャッシュのファイル名から情報を得るコールバック(kuwata専用になるかもしれない)
			void GetStorageCacheMetaData(
				out string assetBundleName,
				out Hash128 hash,
				out long lastAccessed,
				string storageCachePath);
			// アセットバンドル名+ハッシュ+最終アクセス時刻 → キャッシュフォルダ相対のパス
			string MakeStorageCachePath(
				string assetBundleName,
				ref Hash128 hash,
				long lastAccessed);
			// アセットバンドル名からhashとcrcを得る
			void GetAssetBundleMetaData(
				out Hash128 hash,
				out uint crc,
				string assetBundleName);
		}
		// AssetIdentifier → AssetBundleName + AssetName → URLかキャッシュファイル名
		public delegate void OnComplete(UnityEngine.Object asset);

		public Loader(
			string downloadRoot,
			string storageCacheRoot,
			IAssetBundleDatabase database)
		{
			_storageCache = new StorageCache(
				database.GetStorageCacheMetaData,
				database.MakeStorageCachePath,
				storageCacheRoot);
			_database = database;
			_downloadRoot = downloadRoot;
			_abHandles = new Dictionary<string, AssetBundleHandle>();
			_notReferencedAbHandles = new List<AssetBundleHandle>();
			_loadingHandles = new Dictionary<string, AssetHandle>();
			_completeHandles = new Dictionary<string, AssetHandle>();
			_tmpHandleList = new List<AssetHandle>();
			_unloadingAssetHandles = new List<AssetHandle>();
			_lock = new object();
		}

		public void DownloadToStorageCache(string identifier)
		{
			string assetBundleName, assetName;
			_database.ParseIdentifier(out assetBundleName, out assetName, identifier);
			var abHandleDictionaryKey = assetBundleName;

			AssetBundleHandle abHandle = null;
			// メモリにロードされてるならやることない。当然キャッシュにもある。
			if (_abHandles.TryGetValue(abHandleDictionaryKey, out abHandle))
			{
				return;
			}
			Hash128 hash;
			uint crc;
			_database.GetAssetBundleMetaData(out hash, out crc, assetBundleName);

			// キャッシュされてるならやることない
			if (_storageCache.Contains(assetBundleName, ref hash))
			{
				return;
			}
			var downloadPath = _downloadRoot + assetBundleName;
			string storageCachePath = _storageCache.MakePath(assetBundleName, ref hash);
			abHandle = new AssetBundleHandle(
				_storageCache,
				assetBundleName,
				downloadPath,
				storageCachePath,
				fromStorage: false,
				writeCacheOnly: true);
			_abHandles.Add(abHandleDictionaryKey, abHandle);
			_notReferencedAbHandles.Add(abHandle);
			abHandle.IncrementReference();
		}

		public LoadHandle Load(
			string identifier,
			OnComplete onComplete = null,
			GameObject holderGameObject = null)
		{
			string assetBundleName, assetName;
			_database.ParseIdentifier(out assetBundleName, out assetName, identifier);

			var abHandleDictionaryKey = assetBundleName;
			var assetHandleDictionaryKey = identifier;

			AssetHandle assetHandle = null;
			lock (_lock)
			{
				// まず完了済みから探す
				if (!_completeHandles.TryGetValue(assetHandleDictionaryKey, out assetHandle))
				{
					// ロード中から探す
					_loadingHandles.TryGetValue(assetHandleDictionaryKey, out assetHandle);
				}
			}

			// みつからなかったので生成
			if (assetHandle == null)
			{
				// ないのでハンドル生成が確定
				AssetBundleHandle abHandle = null;
				if (!_abHandles.TryGetValue(abHandleDictionaryKey, out abHandle))
				{
					Hash128 hash;
					uint crc;
					_database.GetAssetBundleMetaData(out hash, out crc, assetBundleName);

					// キャッシュの日付を更新して新しい日付けのファイルを得る
					var path = _storageCache.TryUpdatePath(assetBundleName, ref hash);
					if (path != null)
					{
						abHandle = new AssetBundleHandle(
							_storageCache,
							assetBundleName,
							path,
							writePath: null,
							fromStorage: true,
							writeCacheOnly: false);
					}

					if (abHandle == null)
					{
						var downloadPath = _downloadRoot + assetBundleName;
						var storageCachePath = _storageCache.MakePath(assetBundleName, ref hash);
						abHandle = new AssetBundleHandle(
							_storageCache,
							assetBundleName,
							downloadPath,
							storageCachePath,
							fromStorage: false,
							writeCacheOnly: false);
					}
					_abHandles.Add(abHandleDictionaryKey, abHandle);
				}
				else if (abHandle.isWriteCacheOnly) // あったけどキャッシュ専用である場合
				{
					abHandle.RequestLoad();
				}
				assetHandle = new AssetHandle(abHandle, assetName, assetHandleDictionaryKey);
				_loadingHandles.Add(assetHandleDictionaryKey, assetHandle);
			}

			if (onComplete != null)
			{
				if (assetHandle.isDone)
				{
					onComplete(assetHandle.asset);
				}
				else
				{
					assetHandle.AddCallback(onComplete);
				}
			}
			assetHandle.IncrementReferenceThreadSafe();

			var handle = new LoadHandle(assetHandleDictionaryKey, this);
			if (holderGameObject != null)
			{
				var holder = holderGameObject.GetComponent<LoadHandleHolder>();
				if (holder == null)
				{
					holder = holderGameObject.AddComponent<LoadHandleHolder>();
				}
				holder.Add(handle);
			}
			return handle;
		}

		public void Update()
		{
			UnityEngine.Profiling.Profiler.BeginSample("Kayac.Loader.Update");
			// 破棄予定を破棄
			for (int i = 0; i < _unloadingAssetHandles.Count; i++)
			{
				var assetHandle = _unloadingAssetHandles[i];
				if (assetHandle.GetReferenceCountThreadSafe() <= 0) // _unloadingAssetHandlesに入れた後にLoadで参照カウントが復活した場合は、スルー
				{
					if (assetHandle.dictionaryKey != null) // まだ破棄されてない
					{
						var abHandle = assetHandle.abHandle;
						var handleDictionaryKey = assetHandle.dictionaryKey;
						_unloadingAssetHandles[i].Dispose();
						_loadingHandles.Remove(handleDictionaryKey);
						_completeHandles.Remove(handleDictionaryKey);
						if (abHandle.referenceCount == 0) // この時点では破棄はしない。ロード/書き込み完了待ち配列に移動する
						{
							abHandle.IncrementReference();
							_notReferencedAbHandles.Add(abHandle);
						}
					}
				}
			}
			_unloadingAssetHandles.Clear();

			// ロード状況更新
			_tmpHandleList.Clear();
			foreach (var handle in _loadingHandles.Values)
			{
				if (handle.isDone)
				{
					_tmpHandleList.Add(handle);
				}
			}
			for (int i = 0; i < _tmpHandleList.Count; i++)
			{
				var handle = _tmpHandleList[i];
				_loadingHandles.Remove(handle.dictionaryKey);
				_completeHandles.Add(handle.dictionaryKey, handle);
			}

			// 完了進捗確認
			int dst = 0;
			for (int i = 0; i < _notReferencedAbHandles.Count; i++)
			{
				var abHandle = _notReferencedAbHandles[i];
				abHandle.Update();
				_notReferencedAbHandles[dst] = abHandle;
				if (abHandle.isWriteDone && abHandle.isReadDone) // 完了したら外す
				{
					abHandle.DecrementReference(); // 参照カウントが残っていれば、たぶん通常経路での参照もあるので放っておく
					if (abHandle.referenceCount == 0)
					{
						_abHandles.Remove(abHandle.name);
						abHandle.Dispose();
					}
				}
				else
				{
					dst++;
				}
			}
			_notReferencedAbHandles.RemoveRange(dst, _notReferencedAbHandles.Count - dst);
			UnityEngine.Profiling.Profiler.EndSample();
		}

		public string Dump()
		{
			if (_stringBuilderForDump == null)
			{
				_stringBuilderForDump = new System.Text.StringBuilder();
			}
			var sb = _stringBuilderForDump;
			sb.Clear();
			sb.AppendLine("[AssetBundles]");
			int i = 0;
			foreach (var item in _abHandles)
			{
				var abHandle = item.Value;
				var ab = abHandle.assetBundle;
				var abName = "[NULL]";
				if (ab != null)
				{
					abName = ab.name;
				}
				sb.AppendFormat("{0}: {1} {2} {3} {4} ref:{5}\n",
					i,
					item.Key,
					abName,
					abHandle.isReadDone ? "loaded" : "loading",
					abHandle.isWriteDone ? "cached" : "writing",
					abHandle.referenceCount);
				i++;
			}
			sb.AppendFormat("[LoadingAssets {0}]\n", _loadingHandles.Count);
			DumpAssets(sb, _loadingHandles);
			sb.AppendFormat("[LoadedAssets {0}]\n", _completeHandles.Count);
			DumpAssets(sb, _completeHandles);
			return sb.ToString();
		}

		void DumpAssets(System.Text.StringBuilder sb, Dictionary<string, AssetHandle> handles)
		{
			int i = 0;
			foreach (var item in handles)
			{
				var handle = item.Value;
				var asset = handle.asset;
				var assetName = "[NULL]";
				if (asset != null)
				{
					assetName = asset.name;
				}
				sb.AppendFormat("{0}: {1} {2} {3} ref:{4} callbacks:{5}\n",
					i,
					item.Key,
					assetName,
					handle.isDone ? "done" : "loading",
					handle.GetReferenceCountThreadSafe(),
					handle.callbackCount);
				i++;
			}
		}

		public void ForceUnloadAll()
		{
			lock (_lock)
			{
				foreach (var item in _completeHandles.Values)
				{
					while (item.GetReferenceCountThreadSafe() > 0)
					{
						item.DecrementReferenceThreadSafe();
					}
					_unloadingAssetHandles.Add(item);
				}
				foreach (var item in _loadingHandles.Values)
				{
					while (item.GetReferenceCountThreadSafe() > 0)
					{
						item.DecrementReferenceThreadSafe();
					}
					_unloadingAssetHandles.Add(item);
				}
			}
			Update(); // 破棄を反映
			Debug.Assert(_completeHandles.Count == 0);
			Debug.Assert(_loadingHandles.Count == 0);
			Debug.Assert(_abHandles.Count == 0);
		}

		// 成功すればtrueを返す。一つでもメモリ内にAssetが存在していれば失敗する
		public bool ClearStorageCache()
		{
			if ((_completeHandles.Count > 0)
				|| (_loadingHandles.Count > 0)
				|| (_abHandles.Count > 0))
			{
				return false;
			}
			_storageCache.Clear();
			return true;
		}

		AssetHandle FindAssetHandleThreadSafe(string assetHandleDictionaryKey)
		{
			AssetHandle assetHandle = null;
			lock (_lock)
			{
				if (!_completeHandles.TryGetValue(assetHandleDictionaryKey, out assetHandle))
				{
					_loadingHandles.TryGetValue(assetHandleDictionaryKey, out assetHandle);
				}
			}
			return assetHandle;
		}

		public void CheckLoadingThreadSafe(
			out bool isDone,
			out UnityEngine.Object asset,
			string assetHandleDictionaryKey)
		{
			var assetHandle = FindAssetHandleThreadSafe(assetHandleDictionaryKey);
			isDone = assetHandle.isDone;
			asset = isDone ? assetHandle.asset : null;
		}

		public void UnloadThreadSafe(string assetHandleDictionaryKey)
		{
			var assetHandle = FindAssetHandleThreadSafe(assetHandleDictionaryKey);
			assetHandle.DecrementReferenceThreadSafe();
			lock (_lock)
			{
				if (assetHandle.GetReferenceCountThreadSafe() == 0)
				{
					_unloadingAssetHandles.Add(assetHandle);
				}
			}
		}

		IAssetBundleDatabase _database;
		string _downloadRoot;
		StorageCache _storageCache;
		Dictionary<string, AssetHandle> _loadingHandles; // lockで保護
		Dictionary<string, AssetHandle> _completeHandles; // lockで保護
		Dictionary<string, AssetBundleHandle> _abHandles;
		List<AssetBundleHandle> _notReferencedAbHandles;
		List<AssetHandle> _tmpHandleList; // loadingからcompleteに移す時に使うテンポラリ
		List<AssetHandle> _unloadingAssetHandles; // lockで保護
		object _lock;
		System.Text.StringBuilder _stringBuilderForDump;

		// ---- 以下内部クラス----

		class AssetHandle
		{
			public UnityEngine.Object asset { get { return _asset; } }

			public AssetHandle(AssetBundleHandle abHandle, string name, string dictionaryKey)
			{
				_abHandle = abHandle;
				_abHandle.IncrementReference();
				_name = name;
				_dictionaryKey = dictionaryKey;
			}

			public void Dispose()
			{
				Debug.Assert(_referenceCount <= 0, _dictionaryKey + " refCount is " + _referenceCount);
				if (_abHandle != null)
				{
					_abHandle.DecrementReference();
					_abHandle = null;
				}
				_req = null; // AssetBundle.Unloadで破棄するので、途中でも無視
				_asset = null; //
				_dictionaryKey = null;
				ExecuteCallbacks(); // 残っているコールバックを実行して破棄
				_callbacks = null;
			}

			public bool isDone
			{
				get
				{
					var ret = false;
					if (_asset != null)
					{
						ret = true;
					}
					else if (_req != null)
					{
						if (_req.isDone)
						{
							_asset = _req.asset;
							_req = null;
							ret = true;
							ExecuteCallbacks();
						}
					}
					else if (_abHandle != null)
					{
						_abHandle.Update();
						if (_abHandle.isReadDone)
						{
							var assetBundle = _abHandle.assetBundle;
							if (assetBundle != null)
							{
								_req = _abHandle.assetBundle.LoadAssetAsync(_name);
							}
							else
							{
								ret = true;
							}
						}
					}
					else
					{
						ret = true;
					}
					return ret;
				}
			}
			public void IncrementReferenceThreadSafe()
			{
				System.Threading.Interlocked.Increment(ref _referenceCount);
			}

			public void DecrementReferenceThreadSafe()
			{
				System.Threading.Interlocked.Decrement(ref _referenceCount);
			}
			public void AddCallback(OnComplete callback)
			{
				if (_callbacks == null)
				{
					_callbacks = new List<OnComplete>();
				}
				_callbacks.Add(callback);
			}
			void ExecuteCallbacks()
			{
				if (_callbacks == null)
				{
					return;
				}
				for (int i = 0; i < _callbacks.Count; i++)
				{
					_callbacks[i](_asset);
				}
				_callbacks.Clear();
			}
			public int GetReferenceCountThreadSafe()
			{
				System.Threading.Interlocked.MemoryBarrier(); // 確実に現在値を読むためにバリア
				return _referenceCount;
			}
			public AssetBundleHandle abHandle { get { return _abHandle; } }
			public string dictionaryKey { get { return _dictionaryKey; } }
			public int callbackCount { get { return (_callbacks != null) ? _callbacks.Count : 0; } }

			int _referenceCount;
			AssetBundleHandle _abHandle;
			AssetBundleRequest _req;
			string _dictionaryKey;
			UnityEngine.Object _asset;
			string _name;
			List<OnComplete> _callbacks;
		}

		class AssetBundleHandle
		{
			public AssetBundleHandle(
				StorageCache storageCache,
				string name,
				string readPath,
				string writePath,
				bool fromStorage,
				bool writeCacheOnly)
			{
				_storageCache = storageCache;
				_name = name;
				_readPath = readPath;
				_writePath = writePath;
				_writeDone = _fromStorage = fromStorage; // ストレージから読むなら書き終わっているのでdone
				_readDone = _writeCacheOnly = writeCacheOnly; // キャッシュに書くだけなら読む必要がないのでdone
				Update();
			}

			public void Dispose()
			{
				var ab = this.assetBundle;
				if (ab != null)
				{
					ab.Unload(true);
				}
				_assetBundleCreateRequest = null; // TODO: キャンセル手続きとか必要なの?どうなの?
				if (_webRequest != null)
				{
					_webRequest.Dispose();
					_webRequest = null;
				}
			}

			public string error { get { return _error; } }

			public void Update()
			{
				if (_assetBundleCreateRequest != null)
				{
					if (_assetBundleCreateRequest.isDone)
					{
						_readDone = true;
					}
				}
				else if (_webRequest != null) // ダウンロードの場合
				{
					if (_webRequest.isDone)
					{
						if (_webRequest.error != null)
						{
							_error = _webRequest.error;
						}
						else
						{
							var temporaryWritePath = _writePath + TemporaryPostfix;
							File.Move(temporaryWritePath, _writePath); // TODO: 最終的には別スレッドに移さないといけない。エディタで1msもかかってる。
							_storageCache.Register(_writePath);
							_writeDone = true;
							if (!_writeCacheOnly)
							{
								_assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(_writePath);
							}
						}
					}
				}
				else if (_storageCache.ready // キャッシュが使用可能になっていればロード開始
					|| ((_writePath == null) && !_fromStorage)) // キャッシュへの読み書きがなければ開始
				{
					if (_fromStorage)
					{
						_assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(_readPath);
					}
					else
					{
						_webRequest = new UnityWebRequest(_readPath);
						var temporaryWritePath = _writePath + TemporaryPostfix;
						var handler = new DownloadHandlerFile(temporaryWritePath);
						handler.removeFileOnAbort = true;
						_webRequest.downloadHandler = handler;
						_webRequest.SendWebRequest();
					}
				}
			}

			public AssetBundle assetBundle
			{
				get
				{
					AssetBundle ret = null;
					if (_assetBundleCreateRequest != null)
					{
						if (_assetBundleCreateRequest.isDone)
						{
							_readDone = true;
							if (_assetBundleCreateRequest.assetBundle != null)
							{
								ret = _assetBundleCreateRequest.assetBundle;
							}
						}
					}
					return ret;
				}
			}

			// キャッシュに書いて終わりのつもりだったが気が変わってロードしたくなった場合に呼ぶ
			public void RequestLoad()
			{
				Debug.Assert(_writeCacheOnly);
				_readDone = _writeCacheOnly = false;
				if (_writeDone) // 書き込みが終わっていればロード開始
				{
					_assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(_writePath);
				}
			}

			public void IncrementReference()
			{
				_referenceCount++;
			}
			public void DecrementReference()
			{
				Debug.Assert(_referenceCount > 0, _readPath + " refCount MINUS");
				_referenceCount--;
			}
			public int referenceCount { get { return _referenceCount; } }
			public string name { get { return _name; } }
			public bool isWriteDone { get { return _writeDone; } }
			public bool isReadDone { get { return _readDone; } }
			public bool isWriteCacheOnly { get { return _writeCacheOnly; } }
			const string TemporaryPostfix = ".tmp";
			string _name;
			string _readPath; // ダウンロードもしくは読み込むパス
			string _writePath; // 書き込みを行う場合のパス
			bool _fromStorage; // ストレージからの読み出しか、リモートからのダウンロードか
			bool _writeCacheOnly; // キャッシュに配置して終わりか、ロードまで行うか
			bool _readDone; // 読み終わったか、読む予定がなければtrueにする
			bool _writeDone; // 書き終わったか、書く予定がなければtrueにする
			int _referenceCount;
			UnityWebRequest _webRequest;
			AssetBundleCreateRequest _assetBundleCreateRequest;
			string _error;
			StorageCache _storageCache;
		}
	}

	class StorageCache
	{
		// キャッシュのファイル名から情報を得るコールバック(kuwata専用になるかもしれない)
		public delegate void OnGetMetaData(
			out string assetBundleName,
			out Hash128 hash,
			out long lastAccessed,
			string storageCachePath);
		// アセットバンドル名+ハッシュ+最終アクセス時刻 → キャッシュフォルダ相対のパス
		public delegate string OnMakePath(
			string assetBundleName,
			ref Hash128 hash,
			long lastAccessed);

		struct Item
		{
			public Hash128 hash;
			public long lastAccessed;
			public string path;
		}

		public StorageCache(
			OnGetMetaData onGetMetaData,
			OnMakePath onMakePath,
			string root)
		{
			Debug.Assert(onGetMetaData != null);
			Debug.Assert(root[root.Length - 1] == '/');
			_root = root;
			_onGetMetaData = onGetMetaData;
			_onMakePath = onMakePath;
			var t0 = Time.realtimeSinceStartup;
			var cacheExists = Directory.Exists(_root);
			var t1 = Time.realtimeSinceStartup;
			if (!cacheExists)
			{
				Directory.CreateDirectory(_root);
			}
			var t2 = Time.realtimeSinceStartup;

			// TODO: ここあとで非同期化が必要
			_items = new Dictionary<string, Item>();
			ScanCacheRecursive(_root);
			var t3 = Time.realtimeSinceStartup;

			Debug.LogWarning((t1 - t0) + " " + (t2 - t1) + " " + (t3 - t2));
		}

		public bool ready { get { return true; } } // TODO: 後で

		// 全消し。事前に全てのAssetBundleHandle,AssetHandleが破棄されていることを確認すること。それは上層の責任。
		// 開けているAssetBundleのファイルを消した場合、Editorごと落ちることがある。
		public void Clear()
		{
			// 項目を全て消す。フォルダの根っこで再帰的に消すことは危険すぎるのでしない。つまり、認識されないファイルは勝手には消さない。
			foreach (var item in _items)
			{
				File.Delete(item.Value.path);
			}
			_items.Clear();
			if (Directory.Exists(_root))
			{
				DeleteEmptyDirectoryRecursive(_root);
			}
		}

		void DeleteEmptyDirectoryRecursive(string path)
		{
			var directories = Directory.GetDirectories(path);
			for (int i = 0; i < directories.Length; i++)
			{
				DeleteEmptyDirectoryRecursive(directories[i]);
			}
			var files = Directory.GetFiles(path);
			if (files.Length == 0)
			{
				Directory.Delete(path);
			}
		}

		public bool Contains(string name, ref Hash128 hash)
		{
			Item item;
			if (_items.TryGetValue(name, out item))
			{
				if (item.hash == hash)
				{
					return true;
				}
			}
			return false;
		}

		public string TryUpdatePath(string name, ref Hash128 hash)
		{
			string ret = null;
			Item item;
			if (_items.TryGetValue(name, out item))
			{
				if (item.hash == hash)
				{
					var newPath = MakePath(name, ref hash);
					try
					{
						File.Move(item.path, newPath);
						item.path = newPath;
						_items[name] = item;
						ret = newPath;
					}
					catch (System.Exception e)
					{
						Debug.LogError("TryUpdatePath Failed. " + e.GetType().Name + " " + e.Message);
						File.Delete(item.path);
						_items.Remove(name);
					}
				}
			}
			return ret;
		}

		public string MakePath(string name, ref Hash128 hash)
		{
			string path;
			long lastAccessed = System.DateTime.UtcNow.Ticks;
			path = _onMakePath(name, ref hash, lastAccessed);
			path = _root + path;
			return path;
		}

		public void Register(string path)
		{
			// pathからrootを削る
			if (!path.StartsWith(_root))
			{
				return; // 不正なパス
			}
			var relativePath = path.Substring(_root.Length); // _rootを削る
			Item item;
			string name;
			_onGetMetaData(out name, out item.hash, out item.lastAccessed, relativePath);
			if (name != null) // nullが返れば有効なファイルでないとして無視する
			{
				item.path = path;
				// すでにエントリがあるなら置き変える
				Item oldItem;
				if (_items.TryGetValue(name, out oldItem))
				{
					File.Delete(oldItem.path);
					_items[name] = item;
				}
				else
				{
					_items.Add(name, item);
				}
			}
		}

		void ScanCacheRecursive(string path)
		{
			var files = Directory.GetFiles(path);
			for (int i = 0; i < files.Length; i++)
			{
				Register(files[i]);
			}
			var directories = Directory.GetDirectories(path);
			for (int i = 0; i < directories.Length; i++)
			{
				ScanCacheRecursive(directories[i]);
			}
		}
		string _root;
		Dictionary<string, Item> _items;
		OnGetMetaData _onGetMetaData;
		OnMakePath _onMakePath;
	}
}
