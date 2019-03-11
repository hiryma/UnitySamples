using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Kayac.LoaderImpl
{
	public class DownloadHandlerFileWriter : DownloadHandlerScript
	{
		public DownloadHandlerFileWriter(
			FileWriter writer,
			FileWriter.Handle writerHandle,
			byte[] inputBuffer) : base(inputBuffer)
		{
			_writer = writer;
			_writerHandle = writerHandle;
		}

		protected override bool ReceiveData(byte[] data, int length)
		{
			if (_writerHandle == null) // エラー終了してる
			{
				return false;
			}
			// writerで詰まるようならここでブロックする。 TODO: 後で何か考えろ
			int offset = 0;
			while (true)
			{
				int written;
				_writer.Write(out written, _writerHandle, data, offset, length);
				if (_writerHandle.exception != null) // エラーを起こしているので抜ける
				{
					break;
				}
				length -= written;
				if (length <= 0)
				{
					break;
				}
				System.Threading.Thread.Sleep(1); // 寝て待つ
				offset += written;
			}
			return true;
		}

		protected override void CompleteContent()
		{
			if (_writerHandle != null)
			{
				_writer.End(_writerHandle);
				_writerHandle = null;
			}
		}

		public void OnError() // エラー時にファイル閉じる
		{
			Debug.LogError("DownloadHandler.fileWriter.OnError called.");
			_writer.Abort(_writerHandle);
			_writerHandle = null;
		}

		FileWriter _writer;
		FileWriter.Handle _writerHandle;
	}
}