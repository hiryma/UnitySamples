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
			IAssetBundleDatabase database)
		{
			string storageCacheRoot = null;
#if UNITY_EDITOR || UNITY_STANDALONE_OSX
			storageCacheRoot = Application.dataPath + "/..";
#elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
			storageCacheRoot = Application.dataPath;
#endif
			if (storageCacheRoot != null)
			{
				storageCacheRoot += "/AssetBundleCache/";
				Directory.CreateDirectory(storageCacheRoot);
				Caching.currentCacheForWriting = Caching.AddCache(storageCacheRoot);
			}

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

					var cachedAssetBundle = new CachedAssetBundle();
					cachedAssetBundle.hash = hash;
					cachedAssetBundle.name = assetBundleName;
					var path = _downloadRoot + assetBundleName;
					abHandle = new AssetBundleHandle(
						assetBundleName,
						ref cachedAssetBundle,
						path);
					_abHandles.Add(abHandleDictionaryKey, abHandle);
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
				if (abHandle.isDone) // 完了したら外す
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
			UnityEngine.Profiling.Profiler.BeginSample("Kayac.Loader.Dump");
			if (_stringBuilderForDump == null)
			{
				_stringBuilderForDump = new System.Text.StringBuilder();
			}
			var sb = _stringBuilderForDump;
			sb.Length = 0;
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
				sb.AppendFormat("{0}: {1} {2} {3} ref:{4}\n",
					i,
					item.Key,
					abName,
					abHandle.isDone ? "loaded" : "loading",
					abHandle.referenceCount);
				i++;
			}
			sb.AppendFormat("[LoadingAssets {0}]\n", _loadingHandles.Count);
			DumpAssets(sb, _loadingHandles);
			sb.AppendFormat("[LoadedAssets {0}]\n", _completeHandles.Count);
			DumpAssets(sb, _completeHandles);
			var ret = sb.ToString();
			UnityEngine.Profiling.Profiler.EndSample();
			return ret;
		}

		void DumpAssets(System.Text.StringBuilder sb, Dictionary<string, AssetHandle> handles)
		{
			UnityEngine.Profiling.Profiler.BeginSample("Kayac.Loader.DumpAsset");
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
			UnityEngine.Profiling.Profiler.EndSample();
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
			Caching.ClearCache();
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
				this.abHandle = abHandle;
				this.abHandle.IncrementReference();
				this.name = name;
				this.dictionaryKey = dictionaryKey;
			}

			public void Dispose()
			{
				Debug.Assert(_referenceCount <= 0, this.dictionaryKey + " refCount is " + _referenceCount);
				if (this.abHandle != null)
				{
					this.abHandle.DecrementReference();
					this.abHandle = null;
				}
				_req = null; // AssetBundle.Unloadで破棄するので、途中でも無視
				_asset = null;
				this.dictionaryKey = null;
				ExecuteCallbacks(); // 残っているコールバックを実行して破棄
				_callbacks = null;
			}

			public bool isDone
			{
				get
				{
					bool isDone;
					Update(out isDone);
					return isDone;
				}
			}

			void Update(out bool isDone)
			{
				isDone = false;
				if (_asset != null)
				{
					isDone = true;
				}
				else if (_req != null)
				{
					if (_req.isDone)
					{
						_asset = _req.asset;
						_req = null;
						isDone = true;
						ExecuteCallbacks();
					}
				}
				else if (this.abHandle != null)
				{
					this.abHandle.Update();
					if (this.abHandle.isDone)
					{
						var assetBundle = this.abHandle.assetBundle;
						if (assetBundle != null)
						{
							_req = this.abHandle.assetBundle.LoadAssetAsync(this.name);
						}
						else
						{
							isDone = true;
						}
					}
				}
				else
				{
					isDone = true;
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
				System.Threading.Thread.MemoryBarrier(); // 確実に現在値を読むためにバリア
				return _referenceCount;
			}
			public AssetBundleHandle abHandle { get; private set; }
			public string dictionaryKey { get; private set; }
			public string name {get; private set; }
			public int callbackCount { get { return (_callbacks != null) ? _callbacks.Count : 0; } }

			int _referenceCount;
			AssetBundleRequest _req;
			UnityEngine.Object _asset;
			List<OnComplete> _callbacks;
		}

		class AssetBundleHandle
		{
			public AssetBundleHandle(
				string name,
				ref CachedAssetBundle cachedAssetBundle,
				string path)
			{
				this.name = name;
				_cachedAssetBundle = cachedAssetBundle;
				_path = path;
				Update();
			}

			public void Dispose()
			{
				if (this.assetBundle != null)
				{
					this.assetBundle.Unload(true);
					this.assetBundle = null;
				}
				if (_webRequest != null)
				{
					_webRequest.Dispose();
					_webRequest = null;
				}
			}

			public void Update()
			{
				if (this.assetBundle != null)
				{
					; // もうあるからやることない
				}
				else if (_webRequest != null) // ダウンロードの場合
				{
					if (_webRequest.isDone)
					{
						if (_webRequest.error != null)
						{
							this.error = _webRequest.error;
						}
						else
						{
							this.assetBundle = DownloadHandlerAssetBundle.GetContent(_webRequest);
						}
						_webRequest.Dispose();
						_webRequest = null;
						this.isDone = true;
					}
				}
				else if (Caching.ready)
				{
#if UNITY_2018_3_OR_NEWER
					_webRequest = UnityWebRequestAssetBundle.GetAssetBundle(_path, _cachedAssetBundle, _crc);
#else
					_webRequest = UnityWebRequest.GetAssetBundle(_path, _cachedAssetBundle, _crc);
#endif
					_webRequest.SendWebRequest();
				}
			}

			public void IncrementReference()
			{
				this.referenceCount++;
			}
			public void DecrementReference()
			{
				Debug.Assert(this.referenceCount > 0, name + " refCount MINUS");
				this.referenceCount--;
			}
			public AssetBundle assetBundle { get; private set; }
			public int referenceCount { get; private set; }
			public string name { get; private set; }
			public string error { get; private set; }
			public bool isDone{ get; private set; }
			string _path; // ダウンロードもしくは読み込むパス
			UnityWebRequest _webRequest;
			CachedAssetBundle _cachedAssetBundle;
			uint _crc;
		}
	}
}
