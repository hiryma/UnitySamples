using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kayac
{
	public class LoadHandle : IEnumerator, System.IDisposable
	{
		public bool isDone
		{
			get
			{
				return !MoveNext(); // 終わればfalseが帰るので流用
			}
		}

		public bool succeeded
		{
			get
			{
				return isDone && (_asset != null);
			}
		}

		public void Dispose()
		{
			_loader.UnloadThreadSafe(_assetHandleDictionaryKey);
			_assetHandleDictionaryKey = null;
			_loader = null;
			_asset = null;
		}


		/// yieldで完了を待てる
		public bool MoveNext()
		{
			bool isDone;
			_loader.CheckLoadingThreadSafe(out isDone, out _asset, _assetHandleDictionaryKey);
			return !isDone;
		}
		public void Reset(){}
		object IEnumerator.Current { get { return null; } }
		public UnityEngine.Object asset{ get { return _asset; } }

		public LoadHandle(string assetHandleDictionaryKey, Loader loader)
		{
			_assetHandleDictionaryKey = assetHandleDictionaryKey;
			_loader = loader;
		}

		~LoadHandle() // 注意!!! これはどこのスレッドで呼ばれるか全くわからない!!!
		{
			Dispose();
		}

		string _assetHandleDictionaryKey;
		Loader _loader;
		UnityEngine.Object _asset;
	}
}
