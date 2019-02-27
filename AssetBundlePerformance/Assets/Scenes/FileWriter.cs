using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading;

namespace Kayac
{
	public class FileWriter : System.IDisposable
	{
		public class Handle
		{
			public Handle(string path)
			{
				this.path = path;
			}
			public string path { get; private set; }

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

		public FileWriter(string root)
		{
			_requests = new Queue<Request>();
			_root = root;
			if (!_root.EndsWith("/"))
			{
				_root += '/';
			}
			_semaphore = new Semaphore(0, int.MaxValue);
			_thread = new Thread(ThreadFunc);
			_thread.Start();
		}

		public void Dispose()
		{
			Enqueue(null, null, 0, 0);
			_thread.Join();
		}

		public Handle Begin(string path)
		{
			Debug.Assert(path != null);
			Debug.Assert(!path.Contains("/../"));
			var handle = new HandleImpl(path);
			Enqueue(handle, null, 0, 0);
			return handle;
		}

		public void Write(Handle handle, byte[] data, int offset, int length)
		{
			Debug.Assert(handle != null);
			Debug.Assert(!handle.done);
			Debug.Assert(data != null);
			if (length < 0) // 全量
			{
				length = data.Length - offset;
			}
			Enqueue(handle, data, offset, length);
		}

		public void End(Handle handle)
		{
			Debug.Assert(handle != null);
			Debug.Assert(!handle.done);
			Enqueue(handle, null, 0, 0);
		}

		public int queuedCount
		{
			get
			{
				var ret = 0;
				lock (_requests)
				{
					ret = _requests.Count;
				}
				return ret;
			}
		}
		public int restBytes
		{
			get
			{
				Thread.MemoryBarrier();
				return _restBytes;
			}
		}

		void Enqueue(Handle handle, byte[] data, int offset, int length)
		{
			Request req;
			req.handle = handle as HandleImpl;
			Debug.Assert(req.handle != null); // ありえない
			req.data = data;
			req.offset = offset;
			req.length = length;
			lock (_requests)
			{
				Interlocked.Add(ref _restBytes, length);
				_requests.Enqueue(req);
			}
			_semaphore.Release();
		}

		void ThreadFunc()
		{
			Request req;
			while (true)
			{
				_semaphore.WaitOne();
				lock (_requests)
				{
					Debug.Assert(_requests.Count > 0);
					req = _requests.Dequeue();
				}
				if (req.handle == null) // dummy job
				{
					break;
				}
				req.Execute(_root, ref _restBytes);
			}
		}

		struct Request
		{
			public void Execute(string root, ref int restBytesRef)
			{
				try
				{
					if (handle.done) // もう終わってるのに来てるのは不正
					{
						Debug.Assert(false);
					}
					else if (!handle.opened) // 開いてない。開ける要求と解釈する
					{
						handle.Open(root);
					}
					else if (data == null) // 書き込むものがない。閉じる要求と解釈する
					{
						handle.Close();
					}
					else // 開いていて書きこむ
					{
						handle.Write(data, offset, length);
						Interlocked.Add(ref restBytesRef, -length);
					}
				}
				catch (System.Exception e)
				{
					handle.Close(); // 何かしくじったら閉じて終わらせる TODO: 真面目なエラー処理
					Debug.LogException(e);
				}
			}
			public HandleImpl handle;
			public byte[] data;
			public int offset;
			public int length;
		}

		class HandleImpl : Handle
		{
			public HandleImpl(string path) : base(path){}
			public void Open(string root)
			{
				_file = new FileStream(root + this.path, FileMode.Create, FileAccess.Write);
			}

			public void Close()
			{
				if (_file != null)
				{
					_file.Close();
				}
				_done = true;
			}

			public void Write(byte[] data, int offset, int length)
			{
				_file.Write(data, offset, length);
			}

			public bool opened { get { return _file != null; } }

			FileStream _file; // ロードスレッドからしか触らない
		}

		Thread _thread;
		Queue<Request> _requests; // スレッドセーフの必要性
		Semaphore _semaphore;
		string _root;
		int _restBytes; // Interlockedで保護
	}
}