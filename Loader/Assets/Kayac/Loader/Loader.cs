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
				out Hash128 hash, // アセットファイルのバージョンを示すハッシュ
				string fileName);
		}
		// AssetIdentifier → AssetFileName + ファイル内AssetName → URLかキャッシュファイル名
		public delegate void OnComplete(UnityEngine.Object asset);
		public delegate void OnError(
			Error errorType,
			string fileOrAssetName,
			string message);

		public Loader(
			string downloadRoot,
			string storageCacheRoot,
			IAssetFileDatabase database,
			int parallelDownloadCount = 16,
			int downloadRetryCount = 3,
			int timeoutSeconds = 30,
			int fileWriteBufferSize = 16 * 1024 * 1024,
			int downloadHandlerBufferSize = 16 * 1024)
		{
			_storageCache = new StorageCache(storageCacheRoot, database);
			_fileWriter = new FileWriter(storageCacheRoot, fileWriteBufferSize);

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

			_handles = new Dictionary<string, AssetHandle>();
			_watchingHandles = new List<AssetHandle>();

			_downloadHandles = new Dictionary<string, DownloadHandle>();
			_waitingDownloadHandles = new List<DownloadHandle>();
			_goingDownloadHandles = new DownloadHandle[_parallelDownloadCount]; // 固定的に取り、バッファとの対応を固定化する

			_fileHandles = new Dictionary<string, FileHandle>();
			_watchingFileHandles = new List<FileHandle>();
		}

		public void Download(
			string fileName,
			OnError onError)
		{
			DownloadHandle handle = null;
			Hash128 hash;
			_database.GetFileMetaData(out hash, fileName);

			if (!_storageCache.Has(fileName, ref hash)) // キャッシュにない
			{
				if (!_downloadHandles.TryGetValue(fileName, out handle))
				{
					MakeDownloadHandle(fileName, ref hash, onError);
				}
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
			string fileName;
			string assetName;
			_database.ParseIdentifier(out fileName, out assetName, identifier);

			var assetDictionaryKey = identifier + "_" + type.Name;

			AssetHandle assetHandle = null;
			_handles.TryGetValue(assetDictionaryKey, out assetHandle);

			// みつからなかったので生成
			if (assetHandle == null)
			{
				FileHandle fileHandle = null;
				if (!_fileHandles.TryGetValue(fileName, out fileHandle))
				{
					fileHandle = MakeFileHandle(fileName, onError);
				}
				assetHandle = new AssetHandle(fileHandle, assetName, assetDictionaryKey, type, onError);
				lock (_watchingHandles)
				{
					_watchingHandles.Add(assetHandle); // 生成直後は監視が必要
				}
				_handles.Add(assetDictionaryKey, assetHandle);
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
			ref Hash128 hash,
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
				_waitingDownloadHandles.Add(handle);
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
			Hash128 hash;
			_database.GetFileMetaData(out hash, fileName);

			DownloadHandle downloadHandle = null;
			if (!_storageCache.Has(fileName, ref hash)) // キャッシュにない
			{
				if (!_downloadHandles.TryGetValue(fileName, out downloadHandle))
				{
					downloadHandle = MakeDownloadHandle(fileName, ref hash, onError);
				}
			}

			var storageCachePath = _storageCache.MakeCachePath(fileName, ref hash);
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
			UnityEngine.Profiling.Profiler.BeginSample("Kayac.Loader.Update");
			UpdateDownload();
			UpdateLoad();
			UpdateAsset();
			UnityEngine.Profiling.Profiler.EndSample();
		}

		void UpdateDownload()
		{
			// ダウンロード進捗確認。終わるまで待ってから閉じなくてはならない
			UnityEngine.Profiling.Profiler.BeginSample("Kayac.Loader.UpdateDownload");
			int moveCount = 0;
			for (int goingIndex = 0; goingIndex < _goingDownloadHandles.Length; goingIndex++)
			{
				var handle = _goingDownloadHandles[goingIndex];
				if (handle != null)
				{
					handle.Update();
					if (handle.referenceCount <= 0) // 誰も見てない
					{
						if (handle.isDone) // 完了したら削除
						{
							_downloadHandles.Remove(handle.name);
							handle.Dispose();
							_goingDownloadHandles[goingIndex] = null;
							handle = null;
						}
					}
				}

				// 空いた所にwaitingを詰める
				if (handle == null)
				{
					if (moveCount < _waitingDownloadHandles.Count)
					{
						handle = _waitingDownloadHandles[moveCount];
						_goingDownloadHandles[goingIndex] = handle;
						handle.Start(_downloadHandlerBuffers[goingIndex]);
						moveCount++;
					}
				}
			}

			// waitingを移動した分だけ詰める
			for (int i = moveCount; i < _waitingDownloadHandles.Count; i++)
			{
				_waitingDownloadHandles[i - moveCount] = _waitingDownloadHandles[i];
			}
			_waitingDownloadHandles.RemoveRange(_waitingDownloadHandles.Count - moveCount, moveCount);
			UnityEngine.Profiling.Profiler.EndSample();
		}

		void UpdateLoad()
		{
			UnityEngine.Profiling.Profiler.BeginSample("Kayac.Loader.UpdateLoad");
			int dst = 0;
			for (int i = 0; i < _watchingFileHandles.Count; i++)
			{
				var handle = _watchingFileHandles[i];
				handle.Update();
				_watchingFileHandles[dst] = handle;
				bool skip = false;
				if (handle.referenceCount <= 0) // 誰も見てない
				{
					if (handle.isDone || handle.cancelable) // 終わっているかキャンセル可能であれば破棄
					{
						_fileHandles.Remove(handle.name);
						handle.Dispose();
						skip = true;
					}
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
			lock (_watchingHandles)
			{
				int dst = 0;
				for (int i = 0; i < _watchingHandles.Count; i++)
				{
					var handle = _watchingHandles[i];
					_watchingHandles[dst] = handle;
					handle.Update();
					bool skip = false;
					if (handle.GetReferenceCountThreadSafe() <= 0) // 参照カウントがなく、
					{
						if (handle.isDone || handle.cancelable) // 完了しているかキャンセル可能であれば、破棄する
						{
							skip = true;
							if (handle.dictionaryKey != null) // 同じものが_watchingHandlesに2個以上あることがありうる。その時はもう破棄済みなのでスキップ
							{
								_handles.Remove(handle.dictionaryKey);
								handle.Dispose();
							}
						}
					}
					if (!skip)
					{
						dst++;
					}
				}
				_watchingHandles.RemoveRange(dst, _watchingHandles.Count - dst);
			}
			UnityEngine.Profiling.Profiler.EndSample();
		}

		int CountGoingDownload()
		{
			var ret = 0;
			foreach (var item in _goingDownloadHandles)
			{
				if (item != null)
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
			lock (_watchingHandles)
			{
				sb.AppendFormat("[Assets] watching:{0} all:{1}\n",
					_watchingHandles.Count,
					_handles.Count);
			}
			if (!summaryOnly)
			{
				DumpAssets(sb, _handles);
			}
			var ret = sb.ToString();
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

		// 成功すればtrueを返す。一つでもメモリ内にAssetが存在していれば失敗する
		public bool ClearStorageCache()
		{
			// 全ハンドルが空でない限り受け付けない
			if ((_handles.Count > 0)
				|| (_fileHandles.Count > 0)
				|| (_downloadHandles.Count > 0))
			{
				return false;
			}
			_storageCache.Clear();
			return true;
		}

		public void UnloadThreadSafe(AssetHandle assetHandle)
		{
			if (assetHandle.DecrementReferenceThreadSafe() <= 0)
			{
				lock (_watchingHandles)
				{
					_watchingHandles.Add(assetHandle);
				}
			}
		}

		readonly IAssetFileDatabase _database;
		readonly string _downloadRoot;
		readonly int _parallelDownloadCount;
		readonly int _downloadRetryCount;
		readonly int _timeoutSeconds;
		readonly StorageCache _storageCache;
		readonly FileWriter _fileWriter;
		readonly byte[][] _downloadHandlerBuffers;
		readonly Dictionary<string, AssetHandle> _handles;
		readonly List<AssetHandle> _watchingHandles; // Updateで状態を見ないといけないハンドルはここにリスト。lockで保護が必要。
		readonly Dictionary<string, DownloadHandle> _downloadHandles;
		readonly List<DownloadHandle> _waitingDownloadHandles;
		readonly DownloadHandle[] _goingDownloadHandles;
		readonly Dictionary<string, FileHandle> _fileHandles;
		readonly List<FileHandle> _watchingFileHandles;
	}
}
