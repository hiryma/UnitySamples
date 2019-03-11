using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;

namespace Kayac.LoaderImpl
{
	public class DownloadHandle
	{
		public DownloadHandle(
			string name,
			string downloadPath,
			ref FileHash hash,
			Loader.OnError onError,
			int retryCount,
			int timeoutSeconds,
			System.Action<int> onProgress,
			StorageCache storageCache,
			FileWriter fileWriter)
		{
			Debug.Assert(name != null);
			Debug.Assert(downloadPath != null);
			Debug.Assert(onError != null);
			Debug.Assert(storageCache != null);
			Debug.Assert(fileWriter != null);

			this.name = name;
			_onError = onError;
			_downloadPath = downloadPath;
			_hash = hash;
			_restRetryCount = retryCount;
			_timeoutSeconds = timeoutSeconds;
			_onProgress = onProgress;
			_storageCache = storageCache;
			_fileWriter = fileWriter;
		}

		public void SetWaitingListNode(LinkedListNode<DownloadHandle> node)
		{
			this.waitingListNode = node;
		}

		public void Dispose()
		{
			Debug.Assert(disposable);
			if (_webRequest != null)
			{
				_webRequest.Dispose();
				_webRequest = null;
			}

			_fileWriterHandle = null;
			this.name = null;
			_downloadPath = null;
			_onError = null;
			_onProgress = null;
			_storageCache = null;
			_fileWriter = null;
			_fileWriterHandle = null;
			_downloadHandlerBuffer = null;
		}

		public bool disposed { get { return this.name == null; } }
		public LinkedListNode<DownloadHandle> waitingListNode{ get; private set; }

		public void Dump(System.Text.StringBuilder sb, int index)
		{
			sb.AppendFormat("{0}: {1} {2} ref:{3}\n",
				index,
				name,
				done ? "cached" : "downloading",
				referenceCount);
		}

		public void Start(byte[] downloadHandlerBuffer)
		{
			_downloadHandlerBuffer = downloadHandlerBuffer;
			SendRequest();
		}

		public bool fileAvailable
		{
			get
			{
				return IsCacheWriteDone() && !_error;
			}
		}

		public bool done
		{
			get
			{
				if (_error)
				{
					return true;
				}
				else
				{
					return IsDownloadDone() && IsCacheWriteDone();
				}
			}
		}

		public bool disposable
		{
			get
			{
				return (referenceCount <= 0) // 誰も見てなくて
					&& (!IsStarted() || this.done); // 始まってないか終わっていれば
			}
		}

		public void Update()
		{
			// 始まってなければ何もしない
			if (!IsStarted())
			{
				return;
			}

			// 書き込み監視
			if (!IsCacheWriteDone())
			{
				if (_fileWriterHandle.done)
				{
					var exception = _fileWriterHandle.exception;
					if (exception != null) // 書き込み側のエラーはリトライしない。容量不足ならリトライしてもダメだし、故障の類はどうにもならない。
					{
						_onError(
							Loader.Error.CantWriteStorageCache,
							this.name,
							exception);
						_error = true;
					}
					else // 書き込み成功したので登録処理
					{
						_storageCache.OnFileSaved(name, ref _hash);
					}
					_fileWriterHandle = null;
					_fileWriter = null;
					_storageCache = null;
				}
			}

			// ダウンロード監視
			if (!IsDownloadDone())
			{
				var bytes = (int)_webRequest.downloadedBytes;
				if (bytes != _downloadedBytes)
				{
					if (_onProgress != null)
					{
						_onProgress((int)(bytes - _downloadedBytes));
					}
					_lastReceiveTime = DateTime.Now;
					_downloadedBytes = bytes;
				}

				if (_webRequest.isDone)
				{
					if (_webRequest.error == null) // エラーがないならFileWriterを待つ
					{
						_webRequest.Dispose();
						_webRequest = null;
					}
					else
					{
						_downloadHandler.OnError();
						if (!_error && (_restRetryCount > 0)) // エラーでリトライが残っている場合
						{
Debug.LogError("Retry : " + _webRequest.error);
							Retry();
						}
						else // エラー停止確定
						{
							_onError(
								Loader.Error.Network,
								this.name,
								new Exception(_webRequest.error));
							_webRequest.Dispose();
							_webRequest = null;
							_error = true;
						}
					}
				}
				else if ((DateTime.Now - _lastReceiveTime).TotalSeconds >= _timeoutSeconds) // タイムアウト
				{
					_downloadHandler.OnError();
					if (!_error && (_restRetryCount > 0)) // リトライが残っている場合
					{
Debug.LogError("Timeout Retry");
						Retry();
					}
					else // エラー停止確定
					{
						_onError(
							Loader.Error.Network,
							this.name,
							new Exception(_webRequest.error));
						_webRequest.Dispose();
						_webRequest = null;
						_error = true;
					}
				}
			}
		}

		void Retry()
		{
			Debug.Assert(_restRetryCount > 0);
			_restRetryCount--;
			if (_onProgress != null)
			{
				_onProgress(-((int)_downloadedBytes));
			}
			Debug.LogWarning("AssetFile Download retry: " + _restRetryCount + " " + _downloadPath);
			SendRequest();
		}

		void SendRequest()
		{
			if (_webRequest != null)
			{
				_webRequest.Dispose();
			}
			_downloadedBytes = 0;
			_webRequest = new UnityWebRequest(_downloadPath);
			var cachePath = _storageCache.MakeCachePath(name, ref _hash, absolute: false);
			_fileWriterHandle = _fileWriter.Begin(cachePath);
			_downloadHandler = new DownloadHandlerFileWriter(
				_fileWriter,
				_fileWriterHandle,
				_downloadHandlerBuffer);
			_webRequest.downloadHandler = _downloadHandler;
			_webRequest.SendWebRequest();
			_lastReceiveTime = DateTime.Now;
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

		bool IsStarted() { return _downloadHandlerBuffer != null; }
		bool IsCacheWriteDone() { return (IsStarted() && (_fileWriterHandle == null)); }
		bool IsDownloadDone() { return (IsStarted() && (_webRequest == null)); }

		bool _error;
		public int referenceCount { get; private set; }
		public string name { get; private set; }
		string _downloadPath;
		FileHash _hash;
		Loader.OnError _onError;
		UnityWebRequest _webRequest;
		DownloadHandlerFileWriter _downloadHandler;
		int _restRetryCount;
		System.Action<int> _onProgress;
		int _downloadedBytes;
		DateTime _lastReceiveTime;
		int _timeoutSeconds;
		StorageCache _storageCache;
		FileWriter _fileWriter;
		FileWriter.Handle _fileWriterHandle;
		byte[] _downloadHandlerBuffer;
	}
}
