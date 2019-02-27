using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Kayac
{
	public class DownloadHandlerAsyncFile : DownloadHandlerScript
	{
		public DownloadHandlerAsyncFile(
			FileWriter writer,
			FileWriter.Handle writerHandle,
			byte[] inputBuffer,
			byte[] outputBuffer) : base(inputBuffer)
		{
			_outputBuffer = outputBuffer;
			_writer = writer;
			_writerHandle = writerHandle;
		}

		protected override bool ReceiveData(byte[] data, int dataLength)
		{
			UnityEngine.Profiling.Profiler.BeginSample("DownloadHandlerAsyncFile.ReceiveData BlockCopy");
			// 書きこみバッファはFileWriterが書きこむ間保持せねばならないので、コピー。
			// 本当はダブルバッファにしたいがたぶんやる術がない
			Debug.Assert(_outputBuffer.Length >= dataLength);
			System.Buffer.BlockCopy(data, 0, _outputBuffer, 0, dataLength);

			_writer.Write(_writerHandle, _outputBuffer, 0, dataLength);
			UnityEngine.Profiling.Profiler.EndSample();
			return true;
		}

		protected override void CompleteContent()
		{
			_writer.End(_writerHandle);
			_writerHandle = null;
			// TODO: 本当はReceiveDataで最後のデータであることを識別したい。しかしcontentLengthヘッダが必ず来るという保証がどこにもないので、サイズが前もっては不明。
		}

		byte[] _outputBuffer;
		FileWriter _writer;
		FileWriter.Handle _writerHandle;
	}
}