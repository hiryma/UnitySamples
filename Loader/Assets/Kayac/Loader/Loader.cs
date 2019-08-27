using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;

namespace Kayac
{
	using LoaderImpl;
	public class Loader
	{
		public enum Error
		{
			Network, // 何かネットがおかしくて落ちてこない。ポップアップを出して客にリトライをお勧めするのが良いかと。
			CantWriteStorageCache, // おそらくはストレージ容量。SDカードが壊れてる、とかもありうる。客に確認してもらう他なさそう。
			CantLoadAssetBundle, // アセットバンドルのロードに失敗。ファイル破壊か、プラットホームを間違えている可能性。キャッシュの破棄がおすすめ。
			CantLoadStorageCache, // キャッシュからデータを読み出せない。SDカードが壊れている、等。該当するファイルをキャッシュから破棄すべき。
			NoAssetInAssetBundle, // そんな名前のアセットはアセットバンドル内に存在しない。高確率でバグ。
			AssetTypeMismatch, // 指定の名前のアセットはあるが型が異なるのでロードできない。高確率でバグ。
			CantLoadAsset, // 何故かアセットをロードできない。おそらくファイルが壊れている。該当するファイルをキャッシュから破棄すべき。
			ExceedMemoryLimit, // 設定したメモリ量限界を超えたのでロードしない
			Other, // その他。使い方の間違いなど、コードのバグ由来のものはここにまとめて文字列で詳細を出す。
		}

		// 継承して使ってね
		public interface IAssetFileDatabase
		{
			bool ParseIdentifier(
				out string fileName, // アセットが入っているファイルの、Loaderに渡したrootからの相対パス
				out string assetName, // ファイル内のアセットの名前。assetBundle生成の設定次第ではフルパスが必要になる。
				string identifier); // Loader.Loadに渡された文字列
			IEnumerable<string> GetDependencies(string fileName);
			bool GetFileMetaData(out FileHash hash, out int sizeBytes, string fileName);
		}

		// IAssetFileDatabaseのデフォルト実装
		public class AssetFileDatabase : IAssetFileDatabase
		{
			public virtual bool ParseIdentifier(
				out string fileName, // アセットが入っているファイルの、Loaderに渡したrootからの相対パス
				out string assetName, // ファイル内のアセットの名前。assetBundle生成の設定次第ではフルパスが必要になる。
				string identifier) // Loader.Loadに渡された文字列
			{ // デフォルト実装は1ファイル1アセットを想定したもの
				fileName = identifier;
				assetName = identifier;
				return true;
			}
			public virtual IEnumerable<string> GetDependencies(string fileName)
			{
				return null;
			}
			public virtual bool GetFileMetaData(out FileHash hash, out int sizeBytes, string fileName)
			{
				hash = new FileHash();
				sizeBytes = 0;
				return true;
			}
		}
		// AssetIdentifier → AssetFileName + ファイル内AssetName → URLかキャッシュファイル名
		public delegate void OnComplete(UnityEngine.Object asset);
		public delegate void OnError(
			Error errorType,
			string fileOrAssetName,
			Exception exception);

		public Loader(string storageCacheRoot, bool useHashInStorageCache)
		{
			_storageCache = new StorageCache(storageCacheRoot, useHashInStorageCache);
			this.loadLimit = long.MaxValue;
		}

		public void Start(
			string downloadRoot,
			IAssetFileDatabase database,
			int parallelDownloadCount = 16,
			int downloadRetryCount = 3,
			int timeoutSeconds = 30,
			int fileWriteBufferSize = 16 * 1024 * 1024,
			int downloadHandlerBufferSize = 16 * 1024)
		{
			if (IsStarted())
			{
				Debug.LogWarning("Kayac.Loader.Start() called twice.");
				return;
			}
			_storageCache.Start(database);
			_fileWriter = new FileWriter(
				_storageCache.root,
				StorageCache.temporaryFilePostfix,
				fileWriteBufferSize);

			_downloadRetryCount = downloadRetryCount;
			_parallelDownloadCount = parallelDownloadCount;
			_timeoutSeconds = timeoutSeconds;
			_database = database;
			_downloadRoot = downloadRoot + "/";

			_downloadHandlerBuffers = new byte[_parallelDownloadCount][];
			for (int i = 0; i < _parallelDownloadCount; i++)
			{
				_downloadHandlerBuffers[i] = new byte[downloadHandlerBufferSize];
			}

			_assetHandles = new Dictionary<string, AssetHandle>();
			_watchingAssetHandles = new List<AssetHandle>();
			_memoryCachedAssetHandles = new LinkedList<AssetHandle>();

			_downloadHandles = new Dictionary<string, DownloadHandle>();
			_waitingDownloadHandles = new LinkedList<DownloadHandle>();
			_goingDownloadHandles = new DownloadHandle[_parallelDownloadCount]; // 固定的に取り、バッファとの対応を固定化する

			_fileHandles = new Dictionary<string, FileHandle>();
			_watchingFileHandles = new List<FileHandle>();
			_watchingFileHandleAddList = new List<FileHandle>();
		}

		/// 初期化及びクリアが終わってブロックなしで利用可能かを返す。これがfalseを返す状態でも、ブロックはするが利用できる。
		public bool ready { get { return _storageCache.ready; } }
		/// この量に収まっていればアセットをできるだけ捨てないでメモリに残す
		public int memoryCacheLimit { get; private set; }
		public void SetMemoryCacheLimit(int limitBytes)
		{
			this.memoryCacheLimit = limitBytes;
		}
		/// この量を超えた状態だとロードが失敗する
		public long loadLimit { get; private set; }
		public void SetLoadLimit(long limitBytes)
		{
			this.loadLimit = limitBytes;
		}

		public void CheckDownload(
			out int downloadFileCount,
			out long downloadSizeBytes,
			IEnumerable<string> fileNames,
			bool useDependency)
		{
			downloadFileCount = 0;
			downloadSizeBytes = 0;
			if (!IsStarted())
			{
				Debug.Assert(this.ready, "Call Start().");
				return;
			}
			// 第一ループ。HashSetにとにかくつっこむ。必要なら依存するファイルもつっこむ。
			var uniqueSet = new HashSet<string>();
			foreach (var fileName in fileNames)
			{
				uniqueSet.Add(fileName);
				if (useDependency)
				{
					var dependencies = _database.GetDependencies(fileName);
					if (dependencies != null)
					{
						foreach (var dependency in dependencies)
						{
							uniqueSet.Add(dependency);
						}
					}
				}
			}

			// 第二ループ。ダウンロードが必要か判定しながら加算
			foreach (var fileName in uniqueSet)
			{
				FileHash hash;
				int sizeBytes;
				// メタデータなければ飛ばす
				if (!_database.GetFileMetaData(out hash, out sizeBytes, fileName))
				{
					continue;
				}
				DownloadHandle handleUnused;
				if (_downloadHandles.TryGetValue(fileName, out handleUnused)) // ハンドルがあるので飛ばす
				{
					continue;
				}

				if (_storageCache.Has(fileName, ref hash)) // キャッシュにあるので飛ばす
				{
					continue;
				}

				// 加算
				downloadFileCount++;
				downloadSizeBytes += sizeBytes;
			}
		}

		public void Download(
			string fileName,
			OnError onError,
			bool useDependency)
		{
			if (!IsStarted())
			{
				Debug.Assert(this.ready, "Call Start().");
				if (onError != null)
				{
					onError(Error.Other, fileName, new Exception("Loader.Start() hasn't called."));
				}
				return;
			}

			DownloadHandle handle = null;
			FileHash hash;
			int sizeBytesUnused;
			if (!_database.GetFileMetaData(out hash, out sizeBytesUnused, fileName))
			{
				if (onError != null)
				{
					onError(Error.Other, fileName, new Exception("IAssetFileDatabase.GetFileMetaData() returned false."));
				}
				return;
			}

			if (!_downloadHandles.TryGetValue(fileName, out handle))
			{
				if (!_storageCache.Has(fileName, ref hash)) // キャッシュにない
				{
					MakeDownloadHandle(fileName, ref hash, onError);
				}
			}
			// 依存関係があれば再帰
			if (useDependency)
			{
				var dependencies = _database.GetDependencies(fileName);
				if (dependencies != null)
				{
					foreach (var dependency in dependencies)
					{
						Download(
							dependency,
							onError,
							useDependency: true);
					}
				}
			}
		}

		public void Download(
			IEnumerable<string> fileNames,
			OnError onError,
			bool useDependency,
			bool shuffle)
		{
			if (shuffle) // 一時配列を作ってシャッフルする
			{
				var shuffledFileNames = new List<string>();
				foreach (var fileName in fileNames)
				{
					shuffledFileNames.Add(fileName);
				}
				Shuffle(shuffledFileNames);
				fileNames = shuffledFileNames;
			}

			foreach (var fileName in fileNames)
			{
				Download(fileName, onError, useDependency);
			}
		}

		static void Shuffle<T>(IList<T> a) // Fisher-Yates shuffle.
		{
			Debug.Assert(a != null);
			var n = a.Count;
			for (int i = 0; i < n; i++)
			{
				int srcIndex = 0;
				srcIndex = UnityEngine.Random.Range(i, n);
				var tmp = a[srcIndex];
				a[srcIndex] = a[i];
				a[i] = tmp; // これでi番は確定
			}
		}

		public int downloadingCount
		{
			get
			{
				return _downloadHandles.Count;
			}
		}

		public int downloadedBytes { get; private set; }
		public void ClearDownloadedBytes()
		{
			this.downloadedBytes = 0;
		}

		public LoadHandle Load(
			string identifier,
			Type type,
			OnError onError,
			OnComplete onComplete = null,
			GameObject holderGameObject = null)
		{
//Debug.Log("Loader.Load: " + identifier + " " + type.Name);
			if (!IsStarted())
			{
				if (onError != null)
				{
					onError(Error.Other, identifier, new System.Exception("Loader.Start() hasn't called."));
				}
				Debug.Assert(this.ready, "Call Start().");
				return null;
			}

			// 容量制限に引っかかってロードできない!!
			if (_memoryUsingBytes > this.loadLimit)
			{
				if (onError != null)
				{
					onError(Error.ExceedMemoryLimit, identifier, new System.Exception("memory limit exceed. LOAD ABORTED. limit=" + this.loadLimit));
				}
				return null;
			}

			var assetDictionaryKey = identifier + "_" + type.Name;

			AssetHandle assetHandle = null;
			_assetHandles.TryGetValue(assetDictionaryKey, out assetHandle);

			// みつからなかったので生成
			if (assetHandle == null)
			{
				string fileName;
				string assetName;
				if (!_database.ParseIdentifier(out fileName, out assetName, identifier))
				{
					if (onError != null)
					{
						onError(Error.Other, identifier, new System.Exception("IAssetFileDatabase.ParseIdentifier() returned false."));
					}
					return null;
				}
				Debug.Assert(fileName != null);
				Debug.Assert(assetName != null);
				Debug.Assert(fileName[0] != '/', "fileName must not starts with slash."); // スラッシュ始まりは許容しない

				var fileHandle = GetOrMakeFileHandle(fileName, onError);
				assetHandle = new AssetHandle(fileHandle, assetName, assetDictionaryKey, type, onError);
				lock (_watchingAssetHandles)
				{
					_watchingAssetHandles.Add(assetHandle); // 生成直後は監視が必要
				}
				_assetHandles.Add(assetDictionaryKey, assetHandle);
			}
			else if (assetHandle.memoryCachedListNode != null) // 参照カウント0だけどキャッシュされてる奴がみつかった
			{
				// キャッシュから外す
				Debug.Assert(assetHandle.disposable); // 破棄可能なはず
				_memoryCachedAssetHandles.Remove(assetHandle.memoryCachedListNode);
				assetHandle.SetMemoryCachedListNode(null); // 切断
			}

			if (onComplete != null)
			{
				if (assetHandle.done)
				{
					onComplete(assetHandle.asset);
				}
				else
				{
					assetHandle.AddCallback(onComplete);
				}
			}
			assetHandle.IncrementReferenceThreadSafe();

			var handle = new LoadHandle(assetHandle, this);
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

		DownloadHandle MakeDownloadHandle(
			string fileName,
			ref FileHash hash,
			OnError onError)
		{
			var downloadPath = _downloadRoot + fileName;
			var handle = new DownloadHandle(
				fileName,
				downloadPath,
				ref hash,
				onError,
				_downloadRetryCount,
				_timeoutSeconds,
				bytes =>
				{
					this.downloadedBytes += bytes;
				},
				_storageCache,
				_fileWriter);
			_downloadHandles.Add(fileName, handle);
			// 空きを探す
			int vacantIndex = -1;
			for (int i = 0; i < _goingDownloadHandles.Length; i++)
			{
				if (_goingDownloadHandles[i] == null)
				{
					vacantIndex = i;
					break;
				}
			}
			if (vacantIndex < 0) // 空きがない時は待たせる
			{
				var node = _waitingDownloadHandles.AddLast(handle); // 後で消す時のためにNodeを覚えておいて持たせておく
				handle.SetWaitingListNode(node);
			}
			else
			{
				handle.Start(_downloadHandlerBuffers[vacantIndex]);
				_goingDownloadHandles[vacantIndex] = handle;
			}
			return handle;
		}

		FileHandle GetOrMakeFileHandle(
			string fileName,
			OnError onError)
		{
			// すでにあれば返して終わり
			FileHandle ret;
			if (_fileHandles.TryGetValue(fileName, out ret))
			{
				return ret;
			}

			FileHash hash;
			int sizeBytesUnused;
			if (!_database.GetFileMetaData(out hash, out sizeBytesUnused, fileName))
			{
				if (onError != null)
				{
					onError(Error.Other, fileName, new Exception("IAssetFileDatabase.GetFileMetaData() returned false."));
				}
				return null;
			}

			DownloadHandle downloadHandle = null;
			if (!_storageCache.Has(fileName, ref hash)) // キャッシュにない
			{
				if (!_downloadHandles.TryGetValue(fileName, out downloadHandle))
				{
					downloadHandle = MakeDownloadHandle(fileName, ref hash, onError);
				}
			}

			// 依存関係側を先に処理
			FileHandle[] dependencyHandles = null;
			var dependencies = _database.GetDependencies(fileName);
			if (dependencies != null)
			{
				// 先に数える。結構な数になるのでListで余計なメモリを食いたくない
				int count = 0;
				foreach (var unused in dependencies)
				{
					count++;
				}
				dependencyHandles = new FileHandle[count];
				count = 0;
				foreach (var dependency in dependencies)
				{
					Debug.Log("Dependency: " + fileName + " -> " + dependency);
					var handle = GetOrMakeFileHandle(dependency, onError);
					dependencyHandles[count] = handle;
					count++;
				}
			}

			var storageCachePath = _storageCache.MakeCachePath(fileName, ref hash, absolute: true);
			ret = new FileHandle(
				fileName,
				storageCachePath,
				downloadHandle,
				onError,
				dependencyHandles);
			_fileHandles.Add(fileName, ret);
			_watchingFileHandles.Add(ret);
			return ret;
		}

		public void Update()
		{
			if (!IsStarted())
			{
				return;
			}
			UnityEngine.Profiling.Profiler.BeginSample("Kayac.Loader.Update");
			UpdateDownload();
			UpdateFile();
			UpdateAsset();
			UnityEngine.Profiling.Profiler.EndSample();
		}

		public bool HasStorageCache(string name, FileHash hash)
		{
			return _storageCache.Has(name, ref hash);
		}

		void UpdateDownload()
		{
			// ダウンロード進捗確認。終わるまで待ってから閉じなくてはならない
			UnityEngine.Profiling.Profiler.BeginSample("Kayac.Loader.UpdateDownload");
			for (int goingIndex = 0; goingIndex < _goingDownloadHandles.Length; goingIndex++)
			{
				var handle = _goingDownloadHandles[goingIndex];
				if (handle != null)
				{
					if (handle.disposed) // 破棄済みなので消す
					{
						_goingDownloadHandles[goingIndex] = null;
						handle = null;
					}
					else
					{
						handle.Update();
						if (handle.disposable) // 破棄可能なので破棄
						{
							Debug.Assert(!handle.disposed);
							_downloadHandles.Remove(handle.name);
							handle = null;
						}
					}
				}

				// 空いた所にwaitingを詰める
				if ((handle == null) && (_waitingDownloadHandles.First != null))
				{
					handle = _waitingDownloadHandles.First.Value;
					_waitingDownloadHandles.RemoveFirst();
					handle.SetWaitingListNode(null); // waitingから移動
					Debug.Assert(!handle.disposed);
					_goingDownloadHandles[goingIndex] = handle;
					handle.Start(_downloadHandlerBuffers[goingIndex]);
				}
			}
			UnityEngine.Profiling.Profiler.EndSample();
		}

		void UpdateFile()
		{
			UnityEngine.Profiling.Profiler.BeginSample("Kayac.Loader.UpdateLoad");
			_watchingFileHandleAddList.Clear();
			int dst = 0;
			for (int i = 0; i < _watchingFileHandles.Count; i++)
			{
				var handle = _watchingFileHandles[i];
				handle.Update();
				_watchingFileHandles[dst] = handle;
				bool skip = false;
				if (handle.disposed) // 破棄済みならスキップ
				{
					skip = true;
				}
				else if (handle.disposable) // 破棄可能(誰も見てなくて何もしてない)なので破棄
				{
					skip = true;
					_fileHandles.Remove(handle.name);
					var downloadHandle = handle.downloadHandle;
					var dependencies = handle.dependencies;
					handle.Dispose();
					// DownloadHandleが誰も見てない状態になった場合は、破棄をここで行う
					if ((downloadHandle != null) && downloadHandle.disposable)
					{
						Debug.Assert(!downloadHandle.disposed);
						_downloadHandles.Remove(downloadHandle.name);
						// 待ち状態での破棄でれば、待ち行列から削除
						var node = downloadHandle.waitingListNode;
						if (node != null)
						{
							_waitingDownloadHandles.Remove(node);
						}
						downloadHandle.Dispose();
					}
					// 依存するFileHandleが誰も見てない状態になった場合は、監視リストに放り込んで次のフレームで処理する
					if (dependencies != null)
					{
						for (int dependencyIndex = 0;
							dependencyIndex < dependencies.Length;
							dependencyIndex++)
						{
							var dependency = dependencies[dependencyIndex];
							if (dependency.disposable)
							{
								Debug.Assert(!dependency.disposed);
								_watchingFileHandleAddList.Add(dependency);
							}
						}
					}
				}
				else if (handle.done) // 完了したら監視対象から外す
				{
					skip = true;
				}
				if (!skip)
				{
					dst++;
				}
			}
			_watchingFileHandles.RemoveRange(dst, _watchingFileHandles.Count - dst);
			_watchingFileHandles.AddRange(_watchingFileHandleAddList); // 依存関係監視で増えたものを追加
			UnityEngine.Profiling.Profiler.EndSample();
		}

		void UpdateAsset()
		{
			UnityEngine.Profiling.Profiler.BeginSample("Kayac.Loader.UpdateAsset");
			lock (_watchingAssetHandles)
			{
				int dst = 0;
				for (int i = 0; i < _watchingAssetHandles.Count; i++)
				{
					var handle = _watchingAssetHandles[i];
					_watchingAssetHandles[dst] = handle;
					handle.Update();
					bool skip = false;
					if (handle.disposed)
					{
						skip = true;
					}
					else
					{
						if (handle.done) // 終わってるので監視対象からは外す。
						{
							skip = true;
							if (!handle.memorySizeEstimated) //メモリ量カウント。
							{
								handle.EstimateMemorySize();
								_memoryUsingBytes += handle.memorySize;
								CheckMemoryCache();
							}
						}

						if (handle.disposable) // 破棄可能なら破棄
						{
							skip = true;
							// キャッシュへ行くか判定
							if (handle.done // 終わってて
								&& (handle.memoryCachedListNode == null) // まだリストに入ってなくて
								&& ((_memoryUsingBytes + handle.memorySize) < this.memoryCacheLimit)) // 容量が入れば
							{
								var node = _memoryCachedAssetHandles.AddLast(handle);
								handle.SetMemoryCachedListNode(node);
							}
							else // 破棄
							{
								DisposeAssetHandle(handle);
							}
						}
					}

					if (!skip)
					{
						dst++;
					}
				}
				_watchingAssetHandles.RemoveRange(dst, _watchingAssetHandles.Count - dst);
			}
			UnityEngine.Profiling.Profiler.EndSample();
		}

		void CheckMemoryCache()
		{
			var node = _memoryCachedAssetHandles.First;
			while ((node != null) && (_memoryUsingBytes >= this.memoryCacheLimit))
			{
				var next = node.Next;
				var handle = node.Value;
				if (!handle.disposed)
				{
//Debug.Log("CheckCache: " + handle.name + " " + handle.dictionaryKey + " " + handle.memorySize);
					Debug.Assert(handle.disposable, "not disposable: ref=" + handle.GetReferenceCountThreadSafe());
					Debug.Assert(handle.memoryCachedListNode != null, "node is null. " + handle.name);
					Debug.Assert(handle.memoryCachedListNode.Value.name == _memoryCachedAssetHandles.First.Value.name,
						handle.memoryCachedListNode.Value.name + " != " + _memoryCachedAssetHandles.First.Value.name);

					handle.SetMemoryCachedListNode(null);
					DisposeAssetHandle(handle);
				}
				_memoryCachedAssetHandles.RemoveFirst();
				node = next;
			}
		}

		void ClearMemoryCache()
		{
			int limitBackup = this.memoryCacheLimit;
			this.memoryCacheLimit = 0;
			CheckMemoryCache();
			this.memoryCacheLimit = limitBackup;
			Debug.Assert(_memoryCachedAssetHandles.Count == 0, "ClearMemoryCache incomplete: count=" + _memoryCachedAssetHandles.Count);
		}

		void DisposeAssetHandle(AssetHandle handle)
		{
			_assetHandles.Remove(handle.dictionaryKey);
			var fileHandle = handle.fileHandle;
			_watchingFileHandles.Add(fileHandle); // 監視対象に入れる。不要なら次で外れるのでとりあえず入れる。
			if (handle.memorySizeEstimated)
			{
				_memoryUsingBytes -= handle.memorySize;
			}
			handle.Dispose();
		}

		int CountGoingDownload()
		{
			var ret = 0;
			foreach (var item in _goingDownloadHandles)
			{
				if ((item != null) && !item.done)
				{
					ret++;
				}
			}
			return ret;
		}

		public void Dump(System.Text.StringBuilder stringBuilderToAppend, bool summaryOnly = false)
		{
			UnityEngine.Profiling.Profiler.BeginSample("Kayac.Loader.Dump");
			var sb = stringBuilderToAppend; // 短い別名
			sb.AppendFormat("downloadRoot: {0}\n", _downloadRoot);
			_storageCache.Dump(sb, summaryOnly);
			if (IsStarted())
			{
				sb.AppendFormat("[DownloadingFiles] waiting:{0} going:{1} all:{2}\n",
					_waitingDownloadHandles.Count,
					CountGoingDownload(),
					_downloadHandles.Count);
				if (!summaryOnly)
				{
					DumpDownloads(sb, _downloadHandles);
				}
				sb.AppendFormat("[Files] watching:{0} all:{1}\n",
					_watchingFileHandles.Count,
					_fileHandles.Count);
				if (!summaryOnly)
				{
					DumpFiles(sb, _fileHandles);
				}
				lock (_watchingAssetHandles)
				{
					sb.AppendFormat("[Assets] watching:{0} all:{1} memoryCached:{2} memorySize:{3}/{4}/{5}\n",
						_watchingAssetHandles.Count,
						_assetHandles.Count,
						_memoryCachedAssetHandles.Count,
						((float)_memoryUsingBytes / (1024f * 1024f)).ToString("F1"),
						((float)this.memoryCacheLimit / (1024f * 1024f)).ToString("F1"),
						((float)this.loadLimit / (1024f * 1024f)).ToString("F1"));
				}
				if (!summaryOnly)
				{
					DumpAssets(sb, _assetHandles);
				}
			}
			UnityEngine.Profiling.Profiler.EndSample();
		}

		void DumpDownloads(System.Text.StringBuilder sb, Dictionary<string, DownloadHandle> handles)
		{
			UnityEngine.Profiling.Profiler.BeginSample("Kayac.Loader.DumpDownloads");
			int i = 0;
			foreach (var item in handles)
			{
				item.Value.Dump(sb, i);
				i++;
			}
			UnityEngine.Profiling.Profiler.EndSample();
		}

		void DumpFiles(System.Text.StringBuilder sb, Dictionary<string, FileHandle> handles)
		{
			UnityEngine.Profiling.Profiler.BeginSample("Kayac.Loader.DumpFiles");
			int i = 0;
			foreach (var item in handles)
			{
				item.Value.Dump(sb, i);
				i++;
			}
			UnityEngine.Profiling.Profiler.EndSample();
		}

		void DumpAssets(System.Text.StringBuilder sb, Dictionary<string, AssetHandle> handles)
		{
			UnityEngine.Profiling.Profiler.BeginSample("Kayac.Loader.DumpAssets");
			int i = 0;
			foreach (var item in handles)
			{
				item.Value.Dump(sb, i);
				i++;
			}
			UnityEngine.Profiling.Profiler.EndSample();
		}

		/// 開始すればtrueを返す。一つでもメモリ内にAssetが存在していればfalseを返して何もしない
		public bool StartClearStorageCache()
		{
			// 開始後は全ハンドルが空でない限り受け付けない
			if (IsStarted())
			{
				// メモリキャッシュをクリアする
				ClearMemoryCache();
				if ((_assetHandles.Count > 0)
					|| (_fileHandles.Count > 0)
					|| (_downloadHandles.Count > 0))
				{
					return false;
				}
				Debug.Assert(_fileWriter.requestCount == 0, "FileWriter.requestCount=" + _fileWriter.requestCount);
				Debug.Assert(_memoryUsingBytes == 0, "ClearMemoryCache incomplete: size=" + _memoryUsingBytes);
			}
			_storageCache.StartClear();
			return true;
		}

		bool IsStarted()
		{
			return (_database != null);
		}

		/// 元から存在しないケースも含めて、ファイルが結果としてなくなればtrueを返す。使用中だと消せないのでfalseを返す。
		public bool DeleteStorageCache(string fileName)
		{
			// これを使っているハンドルが存在していれば消せない
			if (IsStarted())
			{
				if (_fileHandles.ContainsKey(fileName)
					|| _downloadHandles.ContainsKey(fileName))
				{
					return false;
				}
			}
			_storageCache.TryDelete(fileName);
			return true;
		}

		public void UnloadThreadSafe(AssetHandle assetHandle)
		{
//			Debug.Log("AssetHandle Unload:" + assetHandle.dictionaryKey + " " + assetHandle.GetReferenceCountThreadSafe());
			if (assetHandle.DecrementReferenceThreadSafe() <= 0)
			{
				lock (_watchingAssetHandles)
				{
					_watchingAssetHandles.Add(assetHandle);
				}
			}
		}

		IAssetFileDatabase _database;
		string _downloadRoot;
		int _parallelDownloadCount;
		int _downloadRetryCount;
		int _timeoutSeconds;
		readonly StorageCache _storageCache;
		FileWriter _fileWriter;
		byte[][] _downloadHandlerBuffers;
		Dictionary<string, AssetHandle> _assetHandles;
		List<AssetHandle> _watchingAssetHandles; // Updateで状態を見ないといけないハンドルはここにリスト。lockで保護が必要。
		LinkedList<AssetHandle> _memoryCachedAssetHandles;
		int _memoryUsingBytes; // 推定バイト数
		Dictionary<string, DownloadHandle> _downloadHandles;
		LinkedList<DownloadHandle> _waitingDownloadHandles;
		DownloadHandle[] _goingDownloadHandles;
		Dictionary<string, FileHandle> _fileHandles;
		List<FileHandle> _watchingFileHandles;
		List<FileHandle> _watchingFileHandleAddList; // テンポラリ使用


		// ----------------------------------- 邪悪なコード --------------------------------------
		// 著しく使ってほしくない同期バージョン。アセットバンドルからのロードのみサポート。該当AssetBundleが非同期ロード中だと失敗。
		public LoadHandle LoadSynchronous_SHOULD_BE_REMOVED(string identifier, Type type)
		{
			AssetHandle assetHandle = null;
			var assetDictionaryKey = identifier + "_" + type.Name;
			_assetHandles.TryGetValue(assetDictionaryKey, out assetHandle);

			// みつからなかったので生成
			if (assetHandle == null)
			{
				string fileName;
				string assetName;
				if (_database.ParseIdentifier(out fileName, out assetName, identifier))
				{
					FileHandle fileHandle;
					if (!_fileHandles.TryGetValue(fileName, out fileHandle))
					{
						FileHash hash;
						int sizeBytesUnused;
						_database.GetFileMetaData(
							out hash,
							out sizeBytesUnused,
							fileName);
						var storageCachePath = _storageCache.MakeCachePath(fileName, ref hash, absolute: true);
						fileHandle = new FileHandle(fileName, storageCachePath);
						_fileHandles.Add(fileName, fileHandle);
					}
					Debug.Assert(fileHandle.done);
					assetHandle = new AssetHandle(fileHandle, assetName, assetDictionaryKey, type);
					_assetHandles.Add(assetDictionaryKey, assetHandle);
				}
			}
			else if (assetHandle.memoryCachedListNode != null) // 参照カウント0だけどキャッシュされてる奴がみつかった
			{
				// キャッシュから外す
				Debug.Assert(assetHandle.disposable); // 破棄可能なはず
				_memoryCachedAssetHandles.Remove(assetHandle.memoryCachedListNode);
				assetHandle.SetMemoryCachedListNode(null); // 切断
			}
			if (assetHandle != null)
			{
				assetHandle.IncrementReferenceThreadSafe();
				return new LoadHandle(assetHandle, this);
			}
			else
			{
				return null;
			}
		}
		//----------------------------------- ここまで邪悪なコード ------------------------------

	}
}
