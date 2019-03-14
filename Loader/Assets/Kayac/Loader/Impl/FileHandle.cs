using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;

namespace Kayac.LoaderImpl
{
	public class FileHandle
	{
		public FileHandle(
			string name,
			string storageCachePath,
			DownloadHandle downloadHandle,
			Loader.OnError onError,
			FileHandle[] dependencies)
		{
			Debug.Assert(name != null);
			Debug.Assert(storageCachePath != null);
			Debug.Assert(onError != null);
			this.name = name;
			_onError = onError;
			_storageCachePath = storageCachePath;
			this.downloadHandle = downloadHandle;
			this.dependencies = dependencies;
			if (dependencies != null)
			{
				for (int i = 0; i < dependencies.Length; i++)
				{
					dependencies[i].IncrementReference();
				}
			}

			if (this.downloadHandle == null)
			{
				Start();
			}
			else
			{
				this.downloadHandle.IncrementReference();
			}
		}

		void Start()
		{
			var lowerPath = _storageCachePath.ToLower();
			if (lowerPath.EndsWith(".png") || lowerPath.EndsWith(".jpg"))
			{
				_fileType = FileType.Texture;
				var url = "file://" + _storageCachePath;
				_webRequest = new UnityWebRequest(url);
				_webRequest.downloadHandler = new DownloadHandlerTexture(readable: false);
				_webRequest.SendWebRequest();
			}
			else if (lowerPath.EndsWith(".ogg"))
			{
				_fileType = FileType.Audio;
				var url = "file://" + _storageCachePath;
				_webRequest = new UnityWebRequest(url);
				_webRequest.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.OGGVORBIS);
				_webRequest.SendWebRequest();
			}
			else // 認識できなければAssetBundleとする
			{
				_fileType = FileType.AssetBundle;
				_assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(_storageCachePath);
			}
		}

		public void Dispose()
		{
//Debug.Log("FileHandle Dispose " + name + " " + _fileType);
			// 完了まで破棄してはならない
			Debug.Assert(this.disposable);

			if (this.assetBundle != null)
			{
				this.assetBundle.Unload(true);
			}
			if (this.asset != null)
			{
				UnityEngine.Object.Destroy(this.asset);
				this.asset = null;
			}
			if (this.downloadHandle != null)
			{
				this.downloadHandle.DecrementReference();
				this.downloadHandle = null;
			}
			if (this.dependencies != null)
			{
				for (int i = 0; i < this.dependencies.Length; i++)
				{
					this.dependencies[i].DecrementReference();
				}
				this.dependencies = null;
			}
			this.name = null;
			this._storageCachePath = null;
			_fileType = FileType.Unknown;
			_onError = null;
		}

		public bool disposed{ get{return (this.name == null); } }

		public void Dump(System.Text.StringBuilder sb, int index)
		{
			string showName = "";
			if (asset != null)
			{
				showName = asset.name;
			}
			else if (assetBundle != null)
			{
				showName = assetBundle.name;
			}
			sb.AppendFormat("{0}: {1} {2} {3} ref:{4}\n",
				index,
				name,
				showName,
				done ? "loaded" : "loading",
				referenceCount);
		}

		public bool _selfDone;
		public bool done
		{
			get
			{
				if (!_selfDone) // 自分が終わっていなければdoneでない
				{
					return false;
				}
				if (this.dependencies == null) // 自分が終わっていて依存がないならdone
				{
					return true;
				}
				// 一個でも依存してるのが終わってなければdoneでない
				for (int i = 0; i < this.dependencies.Length; i++)
				{
					if (!this.dependencies[i].done)
					{
						return false;
					}
					else
					{
						Debug.Assert(this.dependencies[i].assetBundle != null);
					}
				}
				return true;
			}
		}

		// 自前で何もせず待っているだけなら破棄可能
		public bool disposable
		{
			get
			{
				return (referenceCount <= 0) // 誰も見てなくて
					&& (_webRequest == null) // ロードしてなくて
					&& (_assetBundleCreateRequest == null); // ABもロードしてないなら
			}
		}

		public void Update(bool selfOnly = true)
		{
			UnityEngine.Profiling.Profiler.BeginSample("Kayac.LoaderImpl.FileHandle.Update()");
			if (this.done)
			{
				// 何もしない
			}
			else if (_assetBundleCreateRequest != null)
			{
				Debug.Assert(_fileType == FileType.AssetBundle);
				if (_assetBundleCreateRequest.isDone)
				{
					this.asset = _assetBundleCreateRequest.assetBundle;
					if (this.asset == null)
					{
						_onError(
							Loader.Error.CantLoadAssetBundle,
							this.name,
							new Exception("AssetBundle.LoadFromFileAsync failed."));
					}
					else
					{
//Debug.Log("FileHandle Complete: " + this.name + " " + this.asset.GetType().Name + " " + this.asset.name);
					}
					_assetBundleCreateRequest = null;
					_selfDone = true;
				}
			}
			else if (_webRequest != null)
			{
				if (_webRequest.isDone)
				{
					if (_webRequest.error != null)
					{
						_onError(
							Loader.Error.CantLoadStorageCache,
							this.name,
							new Exception(_webRequest.error));
					}
					else if (_fileType == FileType.Audio)
					{
						this.asset = DownloadHandlerAudioClip.GetContent(_webRequest);
						if (this.asset == null)
						{
							_onError(
								Loader.Error.CantLoadAsset,
								this.name,
								new Exception("DownloadHandlerAudioClip.GetContent returned null."));
						}
					}
					else if (_fileType == FileType.Texture)
					{
						this.asset = DownloadHandlerTexture.GetContent(_webRequest);
						if (this.asset == null)
						{
							_onError(
								Loader.Error.CantLoadAsset,
								this.name,
								new Exception("DownloadHandlerTexture.GetContent failed."));
						}
					}
					_webRequest.Dispose();
					_webRequest = null;
					_selfDone = true;
				}
			}
			else if (this.downloadHandle != null) // ダウンロード待ち
			{
				if (!selfOnly)
				{
					this.downloadHandle.Update();
				}
				if (this.downloadHandle.fileAvailable)
				{
					Start();
					this.downloadHandle.DecrementReference();
					this.downloadHandle = null;
				}
				else if (this.downloadHandle.done) // ファイルが使えないのに終わっている=エラー
				{
					_selfDone = true; // 終了とする
				}
			}
			UnityEngine.Profiling.Profiler.EndSample();
		}

		public void IncrementReference()
		{
			this.referenceCount++;
		}
		public void DecrementReference()
		{
			Debug.Assert(this.referenceCount > 0, name + " refCount MINUS");
			this.referenceCount--;
		}
		enum FileType
		{
			Unknown,
			AssetBundle,
			Audio,
			Texture,
		}
		public UnityEngine.Object asset { get; private set; } // png等単一アセットを読んだ場合ここに結果を入れる
		public AssetBundle assetBundle{ get{ return asset as AssetBundle; } }
		public int referenceCount { get; private set; }
		public string name { get; private set; }
		public DownloadHandle downloadHandle { get; private set; }
		public FileHandle[] dependencies {get; private set; }
		string _storageCachePath;
		FileType _fileType;
		Loader.OnError _onError;
		AssetBundleCreateRequest _assetBundleCreateRequest;
		UnityWebRequest _webRequest;




// ----------------------------------- 邪悪なコード --------------------------------------
		// 同期ロード専用の邪悪なバージョン。わざと機能制限かけておく。アセットバンドルのみ、エラー処理なし。
		public FileHandle(string name, string storageCachePath)
		{
			this.name = name;
			_storageCachePath = storageCachePath;
			_fileType = FileType.AssetBundle;
			this.asset = AssetBundle.LoadFromFile(_storageCachePath);
			_selfDone = true;
		}
// ----------------------------------- 邪悪なコード --------------------------------------
	}
}
