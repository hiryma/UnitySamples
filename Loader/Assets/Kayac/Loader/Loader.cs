using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Kayac
{
	public class Loader
	{
		public class AssetHandle : IEnumerator
		{
			public bool MoveNext(){ return !isDone; }
			public void Reset(){}
			object IEnumerator.Current { get { return null; } }
			public UnityEngine.Object asset{ get { return _asset; } }

			public AssetHandle(AssetBundleHandle abHandle, string name, string dictionaryKey)
			{
				_abHandle = abHandle;
				_abHandle.IncrementReference();
				_name = name;
				_dictionaryKey = dictionaryKey;
			}

			public bool TryDispose()
			{
				Debug.Assert(_referenceCount > 0, _dictionaryKey + " refCount MINUS");
				_referenceCount--;
				if (_referenceCount == 0)
				{
					Debug.Assert(_referenceCount == 0);
					if (_abHandle != null)
					{
						_abHandle.DecrementReference();
						_abHandle = null;
					}
					_req = null; // AssetBundle.Unloadで破棄するので、途中でも無視
					_asset = null; //
					_dictionaryKey = null;
					ExecuteCallbacks(); // 残っているコールバックを実行して破棄
					_callbacks = null;
					return true;
				}
				else
				{
					return false;
				}
			}

			public bool succeeded
			{
				get
				{
					return isDone && (_asset != null);
				}
			}
			public bool isDone
			{
				get
				{
					var ret = false;
					if (_asset != null)
					{
						ret = true;
					}
					else if (_req != null)
					{
						if (_req.isDone)
						{
							_asset = _req.asset;
							_req = null;
							ret = true;
							ExecuteCallbacks();
						}
					}
					else if (_abHandle != null)
					{
						if (_abHandle.isDone)
						{
							var assetBundle = _abHandle.assetBundle;
							if (assetBundle != null)
							{
								_req = _abHandle.assetBundle.LoadAssetAsync(_name);
							}
						}
					}
					else
					{
						ret = true;
					}
					return ret;
				}
			}
			public void IncrementReference()
			{
				_referenceCount++;
			}
			public void AddCallback(OnComplete callback)
			{
				if (_callbacks == null)
				{
					_callbacks = new List<OnComplete>();
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
					_callbacks[i](this);
				}
				_callbacks.Clear();
			}
			public int referenceCount{ get{ return _referenceCount; } }
			public AssetBundleHandle abHandle{ get{ return _abHandle; } }
			public string dictionaryKey{ get{ return _dictionaryKey; } }
			public int callbackCount{ get {return (_callbacks != null) ? _callbacks.Count : 0; } }

			int _referenceCount;
			AssetBundleHandle _abHandle;
			AssetBundleRequest _req;
			string _dictionaryKey;
			UnityEngine.Object _asset;
			string _name;
			List<OnComplete> _callbacks;
		}

		public class AssetBundleHandle
		{
			public AssetBundleHandle(string uri, CachedAssetBundle cachedAssetBundle, uint crc, string dictionaryKey)
			{
				_dictionaryKey = dictionaryKey;
				_req = UnityWebRequestAssetBundle.GetAssetBundle(
					uri,
					cachedAssetBundle,
					crc);
				_req.SendWebRequest();
			}

			public void Dispose()
			{
				if (_assetBundle != null)
				{
					_assetBundle.Unload(true);
					_assetBundle = null;
				}
				if (_req != null)
				{
					_req.Dispose();
					_req = null;
				}
			}

			public bool isDone
			{
				get
				{
					var ret = false;
					if (_assetBundle != null)
					{
						ret = true;
					}
					else if (_req != null)
					{
						if (_req.isDone)
						{
							if (_req.error != null)
							{
								Debug.Log(_req.error);
							}
							else
							{
								_assetBundle = DownloadHandlerAssetBundle.GetContent(_req);
							}
							_req.Dispose();
							_req = null;
							ret = true;
						}
					}
					else
					{
						ret = true;
					}
					return ret;
				}
			}
			public AssetBundle assetBundle
			{
				get
				{
					return _assetBundle;
				}
			}

			public void IncrementReference()
			{
				_referenceCount++;
			}
			public void DecrementReference()
			{
				Debug.Assert(_referenceCount > 0, _dictionaryKey + " refCount MINUS");
				_referenceCount--;
			}
			public int referenceCount{ get{ return _referenceCount; } }
			public string dictionaryKey{ get{ return _dictionaryKey; } }

			int _referenceCount;
			UnityWebRequest _req;
			AssetBundle _assetBundle;
			string _dictionaryKey;
		}

		public delegate void OnComplete(AssetHandle handle);

		public Loader(string root)
		{
			_root = root;
			_abHandles = new Dictionary<string, AssetBundleHandle>();
			_loadingHandles = new Dictionary<string, AssetHandle>();
			_completeHandles = new Dictionary<string, AssetHandle>();
			_tmpHandleList = new List<AssetHandle>();
		}

		public void Unload(AssetHandle handle)
		{
			if (handle == null)
			{
				return;
			}
			if (handle.referenceCount <= 0) // もうDisposeされている参照がどこかに残っていたケースなので無視
			{
				return;
			}
			var abHandle = handle.abHandle;
			var handleDictionaryKey = handle.dictionaryKey;
			if (handle.TryDispose())
			{
				_loadingHandles.Remove(handleDictionaryKey);
				_completeHandles.Remove(handleDictionaryKey);
				if (abHandle.referenceCount == 0)
				{
					_abHandles.Remove(abHandle.dictionaryKey);
					abHandle.Dispose();
				}
			}
		}

		public AssetHandle Load(string path, OnComplete onComplete = null)
		{
			// TODO: 以下はデフォルト動作
			var lastSlashPos = path.LastIndexOf('/');
			var abName = path.Substring(0, lastSlashPos);
			var assetName = "Assets/Build/" + path; // TODO: これ渡せるようにしなきゃ

			var uri = _root + abName;
			var cachedAssetBundle = new CachedAssetBundle();
			cachedAssetBundle.hash = new Hash128(); // TODO:
			cachedAssetBundle.name = abName; // TODO:

			AssetHandle handle = null;
			// まず完了済みから探す
			if (!_completeHandles.TryGetValue(path, out handle))
			{
				// ロード中から探す
				if (!_loadingHandles.TryGetValue(path, out handle))
				{
					// ないのでハンドル生成が確定
					AssetBundleHandle abHandle = null;
					if (!_abHandles.TryGetValue(uri, out abHandle))
					{
						uint crc = 0;
						abHandle = new AssetBundleHandle(uri, cachedAssetBundle, crc, uri);
						_abHandles.Add(uri, abHandle);
					}
					handle = new AssetHandle(abHandle, assetName, path);
					_loadingHandles.Add(path, handle);
				}
			}

			if (onComplete != null)
			{
				if (handle.isDone)
				{
					onComplete(handle);
				}
				else
				{
					handle.AddCallback(onComplete);
				}
			}
			handle.IncrementReference();
			return handle;
		}

		public void Update()
		{
			_tmpHandleList.Clear();
			foreach (var handle in _loadingHandles.Values)
			{
				if (handle.isDone)
				{
					_tmpHandleList.Add(handle);
				}
			}
			for (int i = 0; i < _tmpHandleList.Count; i++)
			{
				var handle = _tmpHandleList[i];
				_loadingHandles.Remove(handle.dictionaryKey);
				_completeHandles.Add(handle.dictionaryKey, handle);
			}
		}

		public string Dump()
		{
			var sb = new System.Text.StringBuilder();
			sb.AppendLine("[AssetBundles]");
			int i = 0;
			foreach (var item in _abHandles)
			{
				var abHandle = item.Value;
				var ab = abHandle.assetBundle;
				var abName = "";
				if (ab != null)
				{
					abName = ab.name;
				}
				sb.AppendFormat("{0}: {1} {2} {3} ref:{4}\n",
					i,
					item.Key,
					abName,
					abHandle.isDone ? "done" : "loading",
					abHandle.referenceCount);
				i++;
			}
			sb.AppendFormat("[LoadingAssets {0}]\n", _loadingHandles.Count);
			DumpAssets(sb, _loadingHandles);
			sb.AppendFormat("[LoadedAssets {0}]\n", _completeHandles.Count);
			DumpAssets(sb, _completeHandles);
			return sb.ToString();
		}

		void DumpAssets(System.Text.StringBuilder sb, Dictionary<string, AssetHandle> handles)
		{
			int i = 0;
			foreach (var item in handles)
			{
				var handle = item.Value;
				var asset = handle.asset;
				var assetName = "";
				if (asset != null)
				{
					assetName = asset.name;
				}
				sb.AppendFormat("{0}: {1} {2} {3} ref:{4} callbacks:{5}\n",
					i,
					item.Key,
					assetName,
					handle.isDone ? "done" : "loading",
					handle.referenceCount,
					handle.callbackCount);
				i++;
			}
		}

		string _root;
		Dictionary<string, AssetHandle> _loadingHandles;
		Dictionary<string, AssetHandle> _completeHandles;
		Dictionary<string, AssetBundleHandle> _abHandles;
		List<AssetHandle> _tmpHandleList; // loadingからcompleteに移す時に使うテンポラリ
	}
}
