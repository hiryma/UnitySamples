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
				var handler = new DownloadHandlerTexture(readable: false);
				_webRequest.SendWebRequest();
			}
			else if (lowerPath.EndsWith(".ogg"))
			{
				_fileType = FileType.Audio;
				var url = "file://" + _storageCachePath;
				_webRequest = new UnityWebRequest(url);
				var handler = new DownloadHandlerAudioClip(url, AudioType.OGGVORBIS); // TODO: 自分で読んだデータからAudioClipを作る方法が見当たらない。
				_webRequest.SendWebRequest();
			}
			else
			{
				this.isDone = true;
				Debug.Assert(false, "can't decide fileType from extension: " + Path.GetExtension(_storageCachePath));
			}
		}

		public void Dispose()
		{
			// 完了まで破棄してはならない
			Debug.Assert(referenceCount == 0);
			Debug.Assert(_assetBundleCreateRequest == null);
			Debug.Assert(_webRequest == null);

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
		}

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
				isDone ? "loaded" : "loading",
				referenceCount);
		}

		public bool isDone{ get; private set; }
		// 自前で何もせず待っているだけならキャンセル可能
		public bool cancelable
		{
			get
			{
				return (_webRequest == null) && (_assetBundleCreateRequest == null);
			}
		}

		public void Update(bool selfOnly = true)
		{
			if (this.isDone)
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
							"AssetBundle.LoadFromFileAsync failed.");
					}
					_assetBundleCreateRequest = null;
					this.isDone = true;
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
							_webRequest.error);
					}
					else if (_fileType == FileType.Audio)
					{
						this.asset = DownloadHandlerAudioClip.GetContent(_webRequest);
						if (this.asset == null)
						{
							_onError(
								Loader.Error.CantLoadAsset,
								this.name,
								"DownloadHandlerAudioClip.GetContent returned null.");
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
								"DownloadHandlerTexture.GetContent failed.");
						}
					}
					_webRequest.Dispose();
					_webRequest = null;
					this.isDone = true;
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
				else if (this.downloadHandle.isDone) // ファイルが使えないのに終わっている=エラー
				{
					this.isDone = true; // 終了とする
				}
			}
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
