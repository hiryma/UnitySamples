using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Kayac
{
	public class FileLogHandler : IDisposable
	{
		public FileLogHandler(string path, bool append = false)
		{
			_stringBuilder = new System.Text.StringBuilder();
			try
			{
				_writer = new System.IO.StreamWriter(path, append);
				Application.logMessageReceivedThreaded += HandleLog;
			}
			catch (System.Exception e)
			{
				Debug.LogError("StreamWriter constructor failed. e: " + e.GetType());
			}
		}

		public void Dispose()
		{
			if (_writer != null)
			{
				_writer.Dispose();
				_writer = null;
			}
			Application.logMessageReceivedThreaded -= HandleLog;
		}

		public void Write(string logString)
		{
			if (_writer == null)
			{
				return;
			}
			string str = null;
			lock (_stringBuilder)
			{
				_stringBuilder.Length = 0;
				_stringBuilder.Append(_frameCountCopy);
				_stringBuilder.Append('\t');
				_stringBuilder.AppendFormat("{0:MM}/{0:dd} {0:HH}:{0:mm}:{0:ss}.{0:fff}", DateTime.Now);
				_stringBuilder.Append(':');
				_stringBuilder.Append(logString);
				str = _stringBuilder.ToString();
			}
			lock (_writer)
			{
				_writer.WriteLine(str);
				_writer.Flush();
			}
		}

		public void Update()
		{
			_frameCountCopy = Time.frameCount;
		}

		// ---- 以下private ----
		void HandleLog(string logString, string stackTrace, LogType type)
		{
			if (_writer == null)
			{
				return;
			}
			string str = null;
			lock (_stringBuilder)
			{
				_stringBuilder.Length = 0;
				_stringBuilder.Append(_frameCountCopy);
				_stringBuilder.Append(' ');
				_stringBuilder.AppendFormat("{0:MM}/{0:dd} {0:HH}:{0:mm}:{0:ss}.{0:fff}", DateTime.Now);
				_stringBuilder.Append(':');
				_stringBuilder.Append(type);
				_stringBuilder.Append(':');
				_stringBuilder.Append(logString);
				// コールスタックはError系でだけ吐くことにする。設定可能にしても良いかもしれない。
				if ((type == LogType.Exception) || (type == LogType.Error) || (type == LogType.Assert))
				{
					_stringBuilder.Append('\n');
					_stringBuilder.Append(stackTrace);
				}
				str = _stringBuilder.ToString();
			}
			lock (_writer)
			{
				_writer.WriteLine(str);
				_writer.Flush();
			}
		}

		System.IO.StreamWriter _writer;
		int _frameCountCopy; // メインスレッドでしかTime.frameCountにアクセスできないため
		System.Text.StringBuilder _stringBuilder;
	}
}