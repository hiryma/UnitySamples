using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Kayac
{
	public class MemoryLogHandler : ILogHandler
	{
		public static void Create(int lineCapacity)
		{
			if (_instance != null)
			{
				Destory();
			}
			_instance = new MemoryLogHandler(lineCapacity);
			_instance._defaultHandler = Debug.unityLogger.logHandler;
			Debug.unityLogger.logHandler = _instance;
			Application.logMessageReceived += _instance.HandleLog;
		}

		public static void Destory()
		{
			if (_instance != null)
			{
				Debug.unityLogger.logHandler = _instance._defaultHandler;
				Application.logMessageReceived -= _instance.HandleLog;
			}
			_instance = null;
		}

		public static MemoryLogHandler instance { get; private set; }

		public void Clear()
		{
			for (int i = 0; i < _buffer.Length; i++)
			{
				_buffer[i] = null;
			}
			_bufferPos = 0;
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
			var str = Tail(int.MaxValue);
			return System.Text.Encoding.UTF8.GetBytes(str);
		}

		public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
		{
			var message = DateTime.Now.ToString("MM/dd HH:mm:ss.fff") + " : " + logType.ToString() + " : " + string.Format(format, args);
			Add(message);
			_defaultHandler.LogFormat(logType, context, format, args);
		}

		public void LogException(Exception exception, UnityEngine.Object context)
		{
			// Debug.LogExceptionを自分で呼ばない限り来ないので、実質使い物にならない
			var message = DateTime.Now.ToString("MM/dd HH:mm:ss.fff") + " : " + exception.ToString() + " : " + exception.Message;
			Add(message);
			_defaultHandler.LogException(exception, context);
		}

		// ---- 以下private ----
		MemoryLogHandler(int lineCapacity)
		{
			_buffer = new string[lineCapacity];
			_bufferPos = 0;
			_tmpStringBuilder = new System.Text.StringBuilder();
		}

		void HandleLog(string logString, string stackTrace, LogType type)
		{
			if (type == LogType.Exception)
			{
				var message = DateTime.Now.ToString("MM/dd HH:mm:ss.fff") + " : " + type.ToString() + " : " + logString + "\n" + stackTrace;
				_instance.Add(message);
			}
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

		static MemoryLogHandler _instance;
		ILogHandler _defaultHandler;
		string[] _buffer;
		int _bufferPos;
		System.Text.StringBuilder _tmpStringBuilder;
	}
}