using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Kayac
{
	public class MemoryLogHandler : IDisposable
	{
		public MemoryLogHandler(int lineCapacity)
		{
			_buffer = new string[lineCapacity];
			_bufferPos = 0;
			_tmpStringBuilder = new System.Text.StringBuilder();
			Application.logMessageReceived += HandleLog;
		}

		public void Dispose()
		{
			_buffer = null;
			_bufferPos = 0;
			_tmpStringBuilder = null;
			Application.logMessageReceived -= HandleLog;
		}

		// 最新maxLines行を改行で連結して返す
		public string Tail(int maxLines)
		{
			if (maxLines >= _buffer.Length)
			{
				maxLines = _buffer.Length;
			}
			_tmpStringBuilder.Length = 0;
			int pos = _bufferPos - maxLines;
			if (pos < 0)
			{
				pos += _buffer.Length;
			}
			for (int i = 0; i < maxLines; i++)
			{
				if (_buffer[pos] != null)
				{
					_tmpStringBuilder.Append(_buffer[pos]);
					_tmpStringBuilder.Append('\n');
				}
				pos++;
				if (pos >= _buffer.Length)
				{
					pos = 0;
				}
			}
			return _tmpStringBuilder.ToString();
		}

		// 全量をUTF8でエンコードしてbyte[]として返す
		public byte[] GetBytes()
		{
			var str = GetString();
			return System.Text.Encoding.UTF8.GetBytes(str);
		}

		public string GetString()
		{
			return Tail(int.MaxValue);
		}

		// ---- 以下private ----
		void HandleLog(string logString, string stackTrace, LogType type)
		{
			var message = DateTime.Now.ToString("MM/dd HH:mm:ss.fff") + " : " + type.ToString() + " : " + logString;
			// コールスタックはError系でだけ吐くことにする。設定可能にしても良いかもしれない。
			if ((type == LogType.Exception) || (type == LogType.Error) || (type == LogType.Assert))
			{
				message += "\n" + stackTrace;
			}
			Add(message);
		}

		void Add(string message)
		{
			_buffer[_bufferPos] = message;
			_bufferPos++;
			if (_bufferPos >= _buffer.Length)
			{
				_bufferPos = 0;
			}
		}

		string[] _buffer;
		int _bufferPos;
		System.Text.StringBuilder _tmpStringBuilder;
	}
}