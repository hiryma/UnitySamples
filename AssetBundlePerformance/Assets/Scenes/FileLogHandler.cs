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
			_writer = new System.IO.StreamWriter(path, append);
			Application.logMessageReceivedThreaded += HandleLog;
		}

		public void Dispose()
		{
			_writer.Dispose();
			_writer = null;
			Application.logMessageReceivedThreaded -= HandleLog;
		}

		public void Write(string logString)
		{
			var message = _frameCountCopy + "\t" + DateTime.Now.ToString("MM/dd HH:mm:ss.fff") + " : " + logString;
			_writer.WriteLine(message);
			_writer.Flush();
		}

		public void Update()
		{
			_frameCountCopy = Time.frameCount;
		}

		// ---- 以下private ----
		void HandleLog(string logString, string stackTrace, LogType type)
		{
			var message = _frameCountCopy + "\t" + DateTime.Now.ToString("MM/dd HH:mm:ss.fff") + " : " + type.ToString() + " : " + logString;
			// コールスタックはError系でだけ吐くことにする。設定可能にしても良いかもしれない。
			if ((type == LogType.Exception) || (type == LogType.Error) || (type == LogType.Assert))
			{
				message += "\n" + stackTrace;
			}
			_writer.WriteLine(message);
			_writer.Flush();
		}

		System.IO.StreamWriter _writer;
		int _frameCountCopy; // メインスレッドでしかTime.frameCountにアクセスできないため
	}
}