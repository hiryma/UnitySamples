using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Kayac
{
	public class MemoryLogHandler : ILogHandler
	{
		static MemoryLogHandler _instance;
		ILogHandler _defaultHandler;
		string[] _buffer;
		int _bufferPos;

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

		void HandleLog(string logString, string stackTrace, LogType type)
		{
			if (type == LogType.Exception)
			{
				var message = DateTime.Now.ToString("MM/dd HH:mm:ss.fff") + " : " + type.ToString() + " : " + logString + "\n" + stackTrace;
				_instance.Add(message);
			}
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

		public static void Clear()
		{
			if (_instance != null)
			{
				_instance.ClearInner();
			}
		}

		void ClearInner()
		{
			for (int i = 0; i < _buffer.Length; i++)
			{
				_buffer[i] = null;
			}
			_bufferPos = 0;
		}

		public static byte[] GetBytes()
		{
			if (_instance != null)
			{
				return _instance.GetBytesInner();
			}
			else
			{
				return new byte[0];
			}
		}

		byte[] GetBytesInner()
		{
			var sb = new System.Text.StringBuilder();
			int pos = _bufferPos;
			for (int i = 0; i < _buffer.Length; i++)
			{
				if (_buffer[pos] != null)
				{
					sb.Append(_buffer[pos]);
					sb.Append('\n');
				}
				pos++;
				if (pos >= _buffer.Length)
				{
					pos = 0;
				}
			}
			var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
			return bytes;
		}

		MemoryLogHandler(int lineCapacity)
		{
			_buffer = new string[lineCapacity];
			_bufferPos = 0;
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
	}
}