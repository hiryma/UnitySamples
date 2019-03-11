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
		}

		public interface IAssetFileDatabase
		{
			bool ParseIdentifier(
				out string fileName, // アセットが入っているファイルの、Loaderに渡したrootからの相対パス
				out string assetName, // ファイル内のアセットの名前。assetBundle生成の設定次第ではフルパスが必要になる。
				string identifier); // Loader.Loadに渡された文字列
			bool GetFileMetaData( // 後でCRCなりサイズなりのメタデータを足す可能性があるので、名前をこうしておく
				out FileHash hash, // アセットファイルのバージョンを示すハッシュ
				string fileName);
		}
		// AssetIdentifier → AssetFileName + ファイル内AssetName → URLかキャッシュファイル名
		public delegate void OnComplete(UnityEngine.Object asset);
		public delegate void OnError(
			Error errorType,
			string fileOrAssetName,
			Exception exception);

		public Loader(string storageCacheRoot)
		{
			_storageCache = new StorageCache(storageCacheRoot);
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

			_downloadHandles = new Dictionary<string, DownloadHandle>();
			_waitingDownloadHandles = new LinkedList<DownloadHandle>();
			_goingDownloadHandles = new DownloadHandle[_parallelDownloadCount]; // 固定的に取り、バッファとの対応を固定化する

			_fileHandles = new Dictionary<string, FileHandle>();
			_watchingFileHandles = new List<FileHandle>();
		}

		/// 初期化及びクリアが終わってブロックなしで利用可能かを返す。これがfalseを返す状態でも、ブロックはするが利用できる。
		public bool ready { get { return _storageCache.ready; } }

		/// ダウンロードを開始すればtrue、すでにキャッシュにあるか、すでに開始済みであればfalseを返す。
		public bool Download(
			string fileName,
			OnError onError)
		{
			if (!IsStarted())
			{
				Debug.Assert(this.ready, "Call Start().");
				return false;
			}

			var ret = false;
			DownloadHandle handle = null;
			FileHash hash;
			_database.GetFileMetaData(out hash, fileName);

			if (!_storageCache.Has(fileName, ref hash)) // キャッシュにない
			{
				if (!_downloadHandles.TryGetValue(fileName, out handle))
				{
					MakeDownloadHandle(fileName, ref hash, onError);
					ret = true;
				}
			}
			return ret;
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
			if (!IsStarted())
			{
				Debug.Assert(this.ready, "Call Start().");
				return null;
			}

			string fileName;
			string assetName;
			_database.ParseIdentifier(out fileName, out assetName, identifier);

			var assetDictionaryKey = identifier + "_" + type.Name;

			AssetHandle assetHandle = null;
			_assetHandles.TryGetValue(assetDictionaryKey, out assetHandle);

			// みつからなかったので生成
			if (assetHandle == null)
			{
				FileHandle fileHandle = null;
				if (!_fileHandles.TryGetValue(fileName, out fileHandle))
				{
					fileHandle = MakeFileHandle(fileName, onError);
				}
				assetHandle = new AssetHandle(fileHandle, assetName, assetDictionaryKey, type, onError);
				lock (_watchingAssetHandles)
				{
					_watchingAssetHandles.Add(assetHandle); // 生成直後は監視が必要
				}
				_assetHandles.Add(assetDictionaryKey, assetHandle);
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

		FileHandle MakeFileHandle(
			string fileName,
			OnError onError)
		{
			FileHash hash;
			_database.GetFileMetaData(out hash, fileName);

			DownloadHandle downloadHandle = null;
			if (!_storageCache.Has(fileName, ref hash)) // キャッシュにない
			{
				if (!_downloadHandles.TryGetValue(fileName, out downloadHandle))
				{
					downloadHandle = MakeDownloadHandle(fileName, ref hash, onError);
				}
			}


			var storageCachePath = _storageCache.MakeCachePath(fileName, ref hash, absolute: true);
			var fileHandle = new FileHandle(
				fileName,
				storageCachePath,
				downloadHandle,
				onError);
			_fileHandles.Add(fileName, fileHandle);
			_watchingFileHandles.Add(fileHandle);
			return fileHandle;
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
					else if (handle.disposable) // 破棄可能なら破棄
					{
						skip = true;
						_assetHandles.Remove(handle.dictionaryKey);
						var fileHandle = handle.fileHandle;
						_watchingFileHandles.Add(fileHandle); // 監視対象に入れる。不要なら次で外れるのでとりあえず入れる。
						handle.Dispose();
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
					sb.AppendFormat("[Assets] watching:{0} all:{1}\n",
						_watchingAssetHandles.Count,
						_assetHandles.Count);
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
				if ((_assetHandles.Count > 0)
					|| (_fileHandles.Count > 0)
					|| (_downloadHandles.Count > 0))
				{
					return false;
				}
				Debug.Assert(_fileWriter.requestCount == 0, "FileWriter.requestCount=" + _fileWriter.requestCount);
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
		Dictionary<string, DownloadHandle> _downloadHandles;
		LinkedList<DownloadHandle> _waitingDownloadHandles;
		DownloadHandle[] _goingDownloadHandles;
		Dictionary<string, FileHandle> _fileHandles;
		List<FileHandle> _watchingFileHandles;
	}
}
