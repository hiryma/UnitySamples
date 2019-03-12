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
		public const int DefaultMemorySizeEstimate = 4096; // 4KBとしてみる
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

		public void EstimateMemorySize()
		{
			Debug.Assert(this.done);
			this.memorySize = EstimateMemorySize(_asset);
		}

		public static int EstimateMemorySize(UnityEngine.Object asset)
		{
			int ret = DefaultMemorySizeEstimate; // とりあえずデフォルト値で初期化
			if (asset is Texture2D) // テクスチャの場合
			{
				var texture = asset as Texture2D;
				ret = texture.width * texture.height;
				var bitPerPixel = GetBitsPerPixel(texture.format);
				ret *= bitPerPixel;
				ret /= 8; // ブロック物で4x4に丸めたりしないといけないが気にしない。所詮推定なので。
Debug.Log("Tex " + ret);
			}
			else if (asset is TextAsset)
			{
				var text = asset as TextAsset;
				ret = text.bytes.Length;
Debug.Log("Text " + ret);
			}
			else if (asset is AudioClip)
			{
				var clip = asset as AudioClip;
				ret = clip.channels * clip.samples * 2; // 無圧縮16bitと仮定(この仮定は実際には厳しすぎるのだが)
Debug.Log("Audio " + ret);
			}
			return ret;
		}

		public static int GetBitsPerPixel(TextureFormat format)
		{
			int ret = 32; // とりあえず認識できなければ32を返しておく
			bool supported = SystemInfo.SupportsTextureFormat(format);
			switch (format)
			{
				// TextureFormatのマニュアル順に書く https://docs.unity3d.com/ScriptReference/TextureFormat.html
				case TextureFormat.Alpha8: ret = 8; break;
				case TextureFormat.ARGB4444: ret = 16; break;
				case TextureFormat.RGB24: ret = 32; break; // 結構な確率でメモリ内では32bit
				case TextureFormat.ARGB32: ret = 32; break;
				case TextureFormat.RGB565: ret = 16; break;
				// R16,DXT1,DXT5 これらはスマホじゃ来ないだろ
				case TextureFormat.RGBA4444: ret = 16; break;
				case TextureFormat.BGRA32: ret = 32; break;
				// RHalf,RGHalf,RGBAHalf,RGFloat,RGFloat,RGBAFloat,YUY2, RGB9e5Float,BC4,BC5,BC6H, BC7, DXT1Crunched, DXT5Crunched

				case TextureFormat.PVRTC_RGB2:
				case TextureFormat.PVRTC_RGBA2:
					ret = supported ? 2 : 32; break;
				case TextureFormat.PVRTC_RGB4:
				case TextureFormat.PVRTC_RGBA4:
					ret = supported ? 4 : 32; break;

				case TextureFormat.ETC_RGB4: ret = supported ? 4 : 32; break;

				case TextureFormat.EAC_R: ret = supported ? 4 : 32; break;
				case TextureFormat.EAC_R_SIGNED: ret = supported ? 4 : 32; break;
				case TextureFormat.EAC_RG: ret = supported ? 8 : 32; break;
				case TextureFormat.EAC_RG_SIGNED: ret = supported ? 8 : 32; break;
				case TextureFormat.ETC2_RGB: ret = supported ? 4 : 32; break;
				case TextureFormat.ETC2_RGBA1: ret = supported ? 4 : 32; break;
				case TextureFormat.ETC2_RGBA8: ret = supported ? 8 : 32; break;

				case TextureFormat.ASTC_RGB_4x4: ret = supported ? (128 / 16) : 32; break;
				case TextureFormat.ASTC_RGB_5x5: ret = supported ? (128 / 25) : 32; break;
				case TextureFormat.ASTC_RGB_6x6: ret = supported ? (128 / 36) : 32; break;
				case TextureFormat.ASTC_RGB_8x8: ret = supported ? (128 / 64) : 32; break;
				case TextureFormat.ASTC_RGB_10x10: ret = supported ? (128 / 100) : 32; break;
				case TextureFormat.ASTC_RGB_12x12: ret = supported ? 1 : 32; break;

				case TextureFormat.ASTC_RGBA_4x4: ret = supported ? (128 / 16) : 32; break;
				case TextureFormat.ASTC_RGBA_5x5: ret = supported ? (128 / 25) : 32; break;
				case TextureFormat.ASTC_RGBA_6x6: ret = supported ? (128 / 36) : 32; break;
				case TextureFormat.ASTC_RGBA_8x8: ret = supported ? (128 / 64) : 32; break;
				case TextureFormat.ASTC_RGBA_10x10: ret = supported ? (128 / 100) : 32; break;
				case TextureFormat.ASTC_RGBA_12x12: ret = supported ? 1 : 32; break;
				// RG16, R8
				case TextureFormat.ETC_RGB4Crunched: ret = supported ? 4 : 32; break;
				case TextureFormat.ETC2_RGBA8Crunched: ret = supported ? 8 : 32; break;
			}
			return ret;
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
		public int memorySize { get; private set; }
		public bool memorySizeEstimated { get{ return (memorySize > 0); } }
		public LinkedListNode<AssetHandle> memoryCachedListNode{ get; private set; }
		public void SetMemoryCachedListNode(LinkedListNode<AssetHandle> node)
		{
			this.memoryCachedListNode = node;
		}

		int _referenceCount;
		AssetBundleRequest _req;
		UnityEngine.Object _asset;
		Type _type;
		List<Loader.OnComplete> _callbacks;
		Loader.OnError _onError;
	}
}
