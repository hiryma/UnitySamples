using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kayac
{
	using LoaderImpl;
	public class LoadHandle : IEnumerator, System.IDisposable
	{
		// isDoneをポーリングしていれば、同フレーム内であってもいずれロードが終わることを保証する
		public bool isDone
		{
			get
			{
				return !MoveNext(); // 終わればfalseが帰るので流用
			}
		}

		public void Dispose()
		{
			if (_loader != null)
			{
				_loader.UnloadThreadSafe(_handle);
				_handle = null;
				_loader = null;
			}
		}

		/// yieldで完了を待てる
		public bool MoveNext()
		{
			_handle.Update(selfOnly: false); // 下流含めて更新する
			return !_handle.isDone;
		}
		public void Reset(){}
		object IEnumerator.Current { get { return null; } }
		public UnityEngine.Object asset{ get { return (_handle != null) ? _handle.asset : null; } }

		public LoadHandle(AssetHandle handle, Loader loader)
		{
			_handle = handle;
			_loader = loader;
		}

		~LoadHandle() // 注意!!! これはどこのスレッドで呼ばれるか全くわからない!!!
		{
			Dispose();
		}

		AssetHandle _handle;
		Loader _loader;
	}
}
