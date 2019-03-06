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
			// writerで詰まるようならここでブロックする。 TODO: 後で何か考えろ
			int offset = 0;
			while (true)
			{
				int written;
				_writer.Write(out written, _writerHandle, data, offset, length);
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
			_writer.End(_writerHandle);
			_writerHandle = null;
		}

		FileWriter _writer;
		FileWriter.Handle _writerHandle;
	}
}