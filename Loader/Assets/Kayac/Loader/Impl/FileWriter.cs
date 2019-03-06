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

		public FileWriter(string root, int bufferSize)
		{
			_root = root;
			if (!_root.EndsWith("/"))
			{
				_root += '/';
			}
			_buffer = new byte[bufferSize];
			_writePos = _readPos = 0;
			_requestQueue = new Queue<Request>();
			_semaphore = new Semaphore(0, int.MaxValue);
			_thread = new Thread(ThreadFunc);
			_thread.Start();
		}

		public void Dispose()
		{
			Enqueue(null, 0, 0);
			_thread.Join();
		}

		public Handle Begin(string path)
		{
			Debug.Assert(path != null);
			Debug.Assert(!path.Contains("/../"));
			var handle = new HandleImpl(path);
			Enqueue(handle, 0, 0);
			return handle;
		}

		/// 書きこみに成功したサイズを引数に返す。dataからのコピーは済んでいるので好きに書き換えて良い
		public void Write(out int writtenLength, Handle handle, byte[] data, int srcOffset, int length)
		{
			Debug.Assert(handle != null);
			Debug.Assert(!handle.done);
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
				Enqueue(handle, dstOffset, writtenLength);
			}
		}

		public void End(Handle handle)
		{
			Debug.Assert(handle != null);
			Debug.Assert(!handle.done);
			Enqueue(handle, 0, 0);
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

		void Enqueue(Handle handle, int offset, int length)
		{
			Request req;
			req.handle = handle as HandleImpl;
			req.offset = offset;
			req.length = length;
			lock (_thread) // ロック消したい
			{
				_requestQueue.Enqueue(req);
			}
			_semaphore.Release();
		}

		void ThreadFunc()
		{
			Request req;
			while (true)
			{
				_semaphore.WaitOne(); // 何か投入されるまで待つ
				lock (_thread)
				{
					req = _requestQueue.Dequeue();
				}
				if (req.handle == null) // ダミージョブにつき抜ける
				{
					break;
				}
				Execute(ref req);
			}
		}

		void Execute(ref Request req)
		{
			HandleImpl handle = req.handle;
			try
			{
				if (handle.done) // もう終わってるのに来てるのは不正
				{
					Debug.Assert(false);
				}
				else if (!handle.opened) // 開いてない。開ける要求と解釈する
				{
					handle.Open(_root);
				}
				else if (req.length == 0) // 書き込むものがない。閉じる要求と解釈する
				{
					handle.Close();
				}
				else // 開いていて書きこむ
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
			}
			catch (System.Exception e)
			{
				handle.Close(); // 何かしくじったら閉じて終わらせる TODO: 真面目なエラー処理
				Debug.LogException(e);
			}
		}

		struct Request
		{
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

			public void Open(string root)
			{
				try
				{
					_file = new FileStream(root + this.path, FileMode.Create, FileAccess.Write);
				}
				catch (Exception e)
				{
					_exception = e;
					_done = true;
				}
			}

			public void Close()
			{
				if (_file != null)
				{
					try
					{
						_file.Close();
					}
					catch (Exception e)
					{
						_exception = e;
					}
				}
				_done = true;
			}

			public void Write(byte[] data, int offset, int length)
			{
				try
				{
					_file.Write(data, offset, length);
				}
				catch (Exception e)
				{
					_exception = e;
					_done = true;
				}
			}

			Exception _exception;
			public override Exception exception { get { return _exception; } }

			public bool opened { get { return _file != null; } }
			FileStream _file; // ロードスレッドからしか触らない
		}

		Thread _thread;
		Semaphore _semaphore;
		string _root;
		Queue<Request> _requestQueue; // スレッドセーフ必要
		byte[] _buffer;
		int _writePos; // ユーザが次に書きこむ位置(バッファから見てwrite)
		int _readPos; // 次に読み出してファイルに送る位置(バッファから見てread)
	}
}