using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;

namespace Kayac.LoaderImpl
{
	public class AssetHandle
	{
		public UnityEngine.Object asset { get { return _asset; } }

		public AssetHandle(
			FileHandle fileHandle,
			string name,
			string dictionaryKey,
			Type type,
			Loader.OnError onError)
		{
			Debug.Assert(fileHandle != null);
			Debug.Assert(name != null);
			Debug.Assert(dictionaryKey != null);
			Debug.Assert(onError != null);

			this.fileHandle = fileHandle;
			this.fileHandle.IncrementReference();
			this.name = name;
			_type = type;
			this.dictionaryKey = dictionaryKey;
			_onError = onError;
		}

		public void Dispose()
		{
			Debug.Assert(this.disposable);
			if (this.fileHandle != null)
			{
				this.fileHandle.DecrementReference();
				this.fileHandle = null;
			}
			_asset = null;
			_type = null;
			this.dictionaryKey = null;
			ExecuteCallbacks(); // 残っているコールバックを実行して破棄
			_callbacks = null;
			_onError = null;
		}

		public bool disposed { get { return (this.dictionaryKey == null); } }

		public void Dump(System.Text.StringBuilder sb, int index)
		{
			var assetName = "[NULL]";
			if (asset != null)
			{
				assetName = asset.name;
			}
			sb.AppendFormat("{0}: {1} {2} {3} ref:{4} callbacks:{5}\n",
				index,
				this.dictionaryKey,
				assetName,
				done ? "done" : "loading",
				GetReferenceCountThreadSafe(),
				callbackCount);
		}

		public bool done { get; private set; }
		// 自前で何かをしておらず上流を待っているだけであれば破棄可能
		public bool disposable
		{
			get
			{
				return (GetReferenceCountThreadSafe() <= 0) && (_req == null);
			}
		}

		public void Update(bool selfOnly = true)
		{
			if (this.done)
			{
				// 何もしない
			}
			else if (_req != null)
			{
				if (_req.isDone)
				{
					if (_type.IsSubclassOf(typeof(Component)))
					{
						var go = _req.asset as GameObject;
						if (go != null)
						{
							_asset = go.GetComponent(_type);
						}
					}
					else
					{
						_asset = _req.asset;
					}
					if (_asset == null)
					{
						// あるのか確認してエラーを絞り込む
						if (this.fileHandle.assetBundle.Contains(this.name))
						{
							if (_type != null)
							{
								_onError(
									Loader.Error.AssetTypeMismatch,
									this.name,
									new Exception("AssetBundle.LoadAssetAsync failed. probably type mismatch."));
							}
							else
							{
								_onError(
									Loader.Error.CantLoadAsset,
									this.name,
									new Exception("AssetBundle.LoadAssetAsync failed."));
							}
						}
						else
						{
							_onError(
								Loader.Error.NoAssetInAssetBundle,
								this.name,
								new Exception("AssetBundle.LoadAssetAsync failed. " + this.name + " is not contained in AssetBundle:" + this.fileHandle.assetBundle.name));
						}
					}
					_req = null;
					ExecuteCallbacks();
					this.done = true;
				}
			}
			else if (this.fileHandle != null)
			{
				if (!selfOnly)
				{
					this.fileHandle.Update();
				}
				if (this.fileHandle.done)
				{
					if (this.fileHandle.assetBundle != null)
					{
						if (_type == null) // 型指定されてない
						{
							_req = this.fileHandle.assetBundle.LoadAssetAsync(this.name);
						}
						else
						{
							var extractType = _type;
							if (_type.IsSubclassOf(typeof(Component)))
							{
								extractType = typeof(GameObject);
							}
							_req = this.fileHandle.assetBundle.LoadAssetAsync(this.name, extractType);
						}
					}
					else if (this.fileHandle.asset != null)
					{
						_asset = this.fileHandle.asset;
						ExecuteCallbacks();
						this.done = true;
					}
					else // エラーの場合終わり
					{
						this.done = true;
					}
				}
			}
		}

		public void IncrementReferenceThreadSafe()
		{
			System.Threading.Interlocked.Increment(ref _referenceCount);
		}

		// 減らした後の値を返す
		public int DecrementReferenceThreadSafe()
		{
			System.Threading.Interlocked.Decrement(ref _referenceCount);
			return _referenceCount;
		}

		public void AddCallback(Loader.OnComplete callback)
		{
			if (_callbacks == null)
			{
				_callbacks = new List<Loader.OnComplete>();
			}
			_callbacks.Add(callback);
		}

		void ExecuteCallbacks()
		{
			if (_callbacks == null)
			{
				return;
			}
			for (int i = 0; i < _callbacks.Count; i++)
			{
				_callbacks[i](_asset);
			}
			_callbacks.Clear();
		}

		public int GetReferenceCountThreadSafe()
		{
			System.Threading.Thread.MemoryBarrier(); // 確実に現在値を読むためにバリア
			return _referenceCount;
		}
		public FileHandle fileHandle { get; private set; }
		public string dictionaryKey { get; private set; }
		public string name { get; private set; }
		public int callbackCount { get { return (_callbacks != null) ? _callbacks.Count : 0; } }

		int _referenceCount;
		AssetBundleRequest _req;
		UnityEngine.Object _asset;
		Type _type;
		List<Loader.OnComplete> _callbacks;
		Loader.OnError _onError;
	}
}
