using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

// TODO: ファイル開ける所その他も非同期化したければスレッドも検討せよ
// サンプル用なので、書き込みで止めなければ良しよする

namespace Kayac
{
	public class FileWriter
	{
		public const int DefaultCapacity = 16 * 1024 * 1024;
		// 第二引数は最大でキューに溜められるバイト数
		public FileWriter(string root, int capacity = DefaultCapacity)
		{
			_requests = new Queue<Request>();
			_capacity = capacity;
			_root = root;
			if (!_root.EndsWith("/"))
			{
				_root += '/';
			}
		}

		public bool TryWrite(string path, byte[] data, int offset = 0, int length = int.MinValue)
		{
			TryEndWrite();
			if (length < 0) // 全量
			{
				length = data.Length - offset;
			}

			if (_asyncResult == null)
			{
				_lengthInQueue += length;
				BeginWrite(path, data, offset, length);
			}
			else if ((_lengthInQueue + length) > _capacity) // キューが空でない場合、容量チェック
			{
				return false;
			}
			else
			{
				Request req;
				req.path = path;
				req.data = data;
				req.offset = offset;
				req.length = length;
				_requests.Enqueue(req);
				_lengthInQueue += length;
			}
			return true;
		}

		public void Update()
		{
			TryEndWrite();
			if (_asyncResult == null)
			{
				if (_requests.Count > 0)
				{
					var req = _requests.Dequeue();
					BeginWrite(req.path, req.data, req.offset, req.length);
				}
			}
		}

		public int queuedCount{ get{ return _requests.Count; } }
		public int lengthInQUeue{ get{ return _lengthInQueue; } }

		void BeginWrite(string path, byte[] data, int offset, int length)
		{
			try
			{
				Debug.Assert(_file == null);
				Debug.Assert(_asyncResult == null);
				Debug.Assert(_writingLength == 0);
				_writingLength = length;
				_file = new FileStream(_root + path, FileMode.Create, FileAccess.Write);
				_asyncResult = _file.BeginWrite(data, offset, length, null, null);
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}
		}

		void TryEndWrite()
		{
			if (_asyncResult != null)
			{
				if (_asyncResult.IsCompleted)
				{
					Debug.Assert(_file != null);
					_file.EndWrite(_asyncResult);
					_asyncResult = null;
					_file.Close();
					_file = null;
					_lengthInQueue -= _writingLength;
					_writingLength = 0;
				}
			}
		}

		struct Request
		{
			public string path;
			public byte[] data;
			public int offset;
			public int length;
		}

		Queue<Request> _requests;
		System.IAsyncResult _asyncResult;
		FileStream _file;
		string _root;
		int _capacity;
		int _lengthInQueue;
		int _writingLength;
	}
}