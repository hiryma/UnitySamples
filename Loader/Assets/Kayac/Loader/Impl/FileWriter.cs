using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Threading;

namespace Kayac.LoaderImpl
{
	public class FileWriter : System.IDisposable
	{
		public abstract class Handle
		{
			public Handle(string path)
			{
				this.path = path;
			}
			public string path { get; private set; }
			public abstract Exception exception { get; }

			public bool done
			{
				get
				{
					Thread.MemoryBarrier(); // 複数スレッドアクセスにつきバリア
					return _done;
				}
			}

			protected bool _done; // スレッドセーフである必要あり
		}

		public FileWriter(string root, string temporaryFilePostfix, int bufferSize)
		{
			_root = root;
			_temporaryFilePostfix = temporaryFilePostfix;
			if (!_root.EndsWith("/"))
			{
				_root += '/';
			}
			_buffer = new byte[bufferSize];
			_writePos = _readPos = 0;
			_requestQueue = new Queue<Request>();
			_semaphore = new Semaphore(0, int.MaxValue);
			_threadBusySampler = UnityEngine.Profiling.CustomSampler.Create("Busy");
			_thread = new Thread(ThreadEntryPoint);
			_thread.Start();
		}

		public void Dispose()
		{
			Enqueue(Request.Type.None, null, 0, 0);
			_thread.Join();
		}

		public Handle Begin(string path)
		{
			Debug.Assert(path != null);
			Debug.Assert(!path.Contains("/../"));
			var handle = new HandleImpl(path);
			Enqueue(Request.Type.Begin, handle, 0, 0);
			return handle;
		}

		/// 書きこみに成功したサイズを引数に返す。dataからのコピーは済んでいるので好きに書き換えて良い
		public void Write(out int writtenLength, Handle handle, byte[] data, int srcOffset, int length)
		{
			Debug.Assert(handle != null);
			if (handle.done) // エラーで失敗しているのでスルー
			{
				writtenLength = 0;
				return;
			}
			Debug.Assert(data != null);
			// コピーする範囲を決定。
			Thread.MemoryBarrier(); // 別のスレッドから書き込まれている可能性があることを明示。合ってるかは不明。
			int wp = _writePos;
			int rp = _readPos; // この後で別スレッドからrpが進められてもかまわない。書く量が減るだけで誤作動はしない。
			int maxLength = rp - wp - 1; // 最大書き込めるのはこれだけ。readに並ぶ1バイト前まで
			if (maxLength < 0) //r < wの場合一周追加
			{
				maxLength += _buffer.Length;
			}
			writtenLength = Mathf.Min(maxLength, length);
			if (writtenLength > 0)
			{
				int length0 = Mathf.Min(_buffer.Length - wp, writtenLength);
				var dstOffset = wp;
				// コピー
				if (length0 > 0)
				{
					System.Buffer.BlockCopy(data, srcOffset, _buffer, dstOffset, length0);
					int length1 = writtenLength - length0;
					if (length1 > 0)
					{
						System.Buffer.BlockCopy(data, srcOffset + length0, _buffer, 0, length1);
					}
				}

				// コピー完了後にポインタ移動
				wp += writtenLength;
				if (wp >= _buffer.Length)
				{
					wp -= _buffer.Length;
				}
				Interlocked.Exchange(ref _writePos, wp);
				Enqueue(Request.Type.Write, handle, dstOffset, writtenLength);
			}
		}

		public void End(Handle handle)
		{
			Debug.Assert(handle != null);
			if (handle.done) // エラーで失敗しているのでスルー
			{
				return;
			}
			Enqueue(Request.Type.End, handle, 0, 0);
		}

		public void Abort(Handle handle)
		{
			Debug.Assert(handle != null);
			if (handle.done) // エラーで失敗しているのでスルー
			{
				return;
			}
			Enqueue(Request.Type.Abort, handle, 0, 0);
		}

		public int requestCount
		{
			get
			{
				int ret;
				lock (_requestQueue)
				{
					ret = _requestQueue.Count;
				}
				return ret;
			}
		}

		public int restBytes
		{
			get
			{
				int ret = 0;
				Thread.MemoryBarrier(); // メモリから読みたい
				ret = _writePos - _readPos;
				if (ret < 0)
				{
					ret += _buffer.Length;
				}
				return ret;
			}
		}

		void Enqueue(Request.Type type, Handle handle, int offset, int length)
		{
			Request req;
			req.type = type;
			req.handle = handle as HandleImpl;
			req.offset = offset;
			req.length = length;
			lock (_thread) // ロック消したい
			{
				_requestQueue.Enqueue(req);
			}
			_semaphore.Release();
		}

		void ThreadEntryPoint()
		{
			UnityEngine.Profiling.Profiler.BeginThreadProfiling("Kayac", "LoaderImpl.FileWriter");
			Request req;
			bool end = false;
			while (!end)
			{
				_semaphore.WaitOne(); // 何か投入されるまで待つ
				_threadBusySampler.Begin();
				lock (_thread)
				{
					req = _requestQueue.Dequeue();
				}
				if (req.type == Request.Type.None) // ダミージョブにつき抜ける
				{
					end = true;
				}
				else
				{
					Execute(ref req);
				}
				_threadBusySampler.End();
			}
			UnityEngine.Profiling.Profiler.EndThreadProfiling();
		}

		void Execute(ref Request req)
		{
			HandleImpl handle = req.handle;
			Request.Type type = req.type;
			if (handle.done)
			{
				// 終わってれば前の要求に重なっている。でもバグじゃね?
				Debug.Assert(false, "Handle.done == true. バグじゃね?");
			}
			else if (type == Request.Type.None)
			{
				// 何もしない
				Debug.Assert(false, "Request.type == None. バグじゃね?");
			}
			else if (type == Request.Type.Begin) // 開いてない。開ける要求と解釈する
			{
				Debug.Assert(!handle.opened);
				handle.BeginWrite(_root, _temporaryFilePostfix);
			}
			else if (type == Request.Type.End) // 書き込むものがない。閉じる要求と解釈する
			{
				handle.EndWrite(_root);
			}
			else if (type == Request.Type.Abort)
			{
				handle.Abort();
			}
			else if (type == Request.Type.Write)
			{
				int length0 = Mathf.Min(_buffer.Length - req.offset, req.length);
				handle.Write(_buffer, req.offset, length0);
				int length1 = req.length - length0;
				if (length1 > 0)
				{
					handle.Write(_buffer, 0, length1);
				}
				int rp = _readPos;
				rp += req.length;
				if (rp >= _buffer.Length)
				{
					rp -= _buffer.Length;
				}
				Interlocked.Exchange(ref _readPos, rp);
			}
			else
			{
				Debug.Assert(false, "Unknown RequestType. " + (int)type);
			}
		}

		struct Request
		{
			public enum Type
			{
				None,
				Begin,
				Write,
				End,
				Abort,
			}
			public Type type;
			public HandleImpl handle;
			public int offset;
			public int length;
		}

		class HandleImpl : Handle
		{
			public HandleImpl(string path) : base(path) { }

			~HandleImpl() // 参照が尽きた時にファイルが開いていた場合閉じる
			{
				Close();
			}

			public void BeginWrite(string root, string temporaryFilePostfix)
			{
				try
				{
					var tmpPath = root + this.path + temporaryFilePostfix;
					_fileInfo = new FileInfo(tmpPath);
					// フォルダがない場合生成
					var dir = _fileInfo.Directory;
					if (!dir.Exists)
					{
						dir.Create();
					}
					if (_fileInfo.Exists)
					{
//Debug.Log("FileWriter: Open: " + tmpPath);
						_fileStream = _fileInfo.OpenWrite();
					}
					else
					{
//Debug.Log("FileWriter: Create: " + tmpPath);
						_fileStream = _fileInfo.Create();
					}
				}
				catch (Exception e)
				{
					_exception = FileUtility.InspectIoError(_fileInfo.FullName, null, e);
					_done = true;
				}
			}

			void Close()
			{
				if (_fileStream != null)
				{
					try
					{
//Debug.Log("FileWriter: Close: " + _fileInfo.Name);
						_fileStream.Close();
					}
					catch (Exception e)
					{
						_exception = FileUtility.InspectIoError(_fileInfo.FullName, null, e);
					}
				}
			}

			public void Abort()
			{
				Debug.Assert(_fileInfo != null);
				Debug.Assert(_fileStream != null);
				Close();
				try
				{
//Debug.Log("FileWriter: Delete: " + _fileInfo.Name);
					_fileInfo.Delete();
				}
				catch (Exception e)
				{
					_exception = FileUtility.InspectIoError(_fileInfo.FullName, null, e);
				}
				_done = true;
			}

			public void EndWrite(string root)
			{
				Debug.Assert(_fileInfo != null);
				Debug.Assert(_fileStream != null);
				Close();
				var dst = root + this.path;
				try
				{
//Debug.Log("FileWriter: Move: " + _fileInfo.Name + " -> " + dst);
					_fileInfo.MoveTo(dst); // 本番ファイル名に変更
				}
				catch (Exception e)
				{
					_exception = FileUtility.InspectIoError(_fileInfo.FullName, dst, e);
				}
				_done = true;
			}

			public void Write(byte[] data, int offset, int length)
			{
				try
				{
					_fileStream.Write(data, offset, length);
				}
				catch (Exception e)
				{
					_exception = FileUtility.InspectIoError(_fileInfo.FullName, null, e);
					_done = true;
				}
			}
			public override Exception exception { get { return _exception; } }
			public bool opened { get { return _fileStream != null; } }

			Exception _exception;
			FileStream _fileStream; // ロードスレッドからしか触らない
			FileInfo _fileInfo;
		}

		Thread _thread;
		UnityEngine.Profiling.CustomSampler _threadBusySampler;
		Semaphore _semaphore;
		string _root;
		string _temporaryFilePostfix;
		Queue<Request> _requestQueue; // スレッドセーフ必要
		byte[] _buffer;
		int _writePos; // ユーザが次に書きこむ位置(バッファから見てwrite)
		int _readPos; // 次に読み出してファイルに送る位置(バッファから見てread)
	}
}
