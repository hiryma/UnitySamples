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
			Loader.OnError onError)
		{
			Debug.Assert(name != null);
			Debug.Assert(storageCachePath != null);
			Debug.Assert(onError != null);
			this.name = name;
			_onError = onError;
			_storageCachePath = storageCachePath;
			this.downloadHandle = downloadHandle;

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
			if (lowerPath.EndsWith(".unity3d"))
			{
				_fileType = FileType.AssetBundle;
				_assetBundleCreateRequest = AssetBundle.LoadFromFileAsync(_storageCachePath);
			}
			else if (lowerPath.EndsWith(".png") || lowerPath.EndsWith(".jpg"))
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
			else
			{
				this.done = true;
				Debug.Assert(false, "can't decide fileType from extension: " + Path.GetExtension(_storageCachePath));
			}
		}

		public void Dispose()
		{
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

		public bool done{ get; private set; }
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
					_assetBundleCreateRequest = null;
					this.done = true;
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
					this.done = true;
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
					this.done = true; // 終了とする
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
		string _storageCachePath;
		FileType _fileType;
		Loader.OnError _onError;
		AssetBundleCreateRequest _assetBundleCreateRequest;
		UnityWebRequest _webRequest;
	}
}
