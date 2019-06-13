using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Kayac
{
	public static class DebugServerUtil
	{
		const string DirectoryName = "override";
		public static string GetPersistentDataPath()
		{
			string ret;
#if UNITY_EDITOR // エディタではプロジェクト直下の方が便利
			// GetCurrentDirectoryがプロジェクトパスを返すことに依存している。動作が変われば動かなくなる!
			ret = Path.Combine(Directory.GetCurrentDirectory(), "PersistentData");
#else
			ret = Application.persistentDataPath;
#endif
			return ret;
		}

		// StreamingAssetsからファイルテキストをロードする
		public static IEnumerator CoLoad(
			CoroutineReturnValue<string> ret,
			string pathInStreamingAssets,
			bool overrideEnabled = true)
		{
			string url = MakeUrl(pathInStreamingAssets, overrideEnabled);
			var req = UnityWebRequest.Get(url);
			yield return req.SendWebRequest();
			if (req.error != null)
			{
				ret.Fail(new System.IO.FileLoadException(url));
			}
			else
			{
				ret.Succeed(req.downloadHandler.text);
			}
		}

		// StreamingAssetsからファイルバイナリをロードする
		public static IEnumerator CoLoad(
			CoroutineReturnValue<byte[]> ret,
			string pathInStreamingAssets,
			bool overrideEnabled = true)
		{
			string url = MakeUrl(pathInStreamingAssets, overrideEnabled);
			var req = UnityWebRequest.Get(url);
			yield return req.SendWebRequest();
			if (req.error != null)
			{
				ret.Fail(new System.IO.FileLoadException(url));
			}
			else
			{
				ret.Succeed(req.downloadHandler.data);
			}
		}

		// StreamingAssetsからテクスチャをロードする
		public static IEnumerator CoLoad(
			CoroutineReturnValue<Texture2D> ret,
			string pathInStreamingAssets,
			bool overrideEnabled = true,
			bool readable = false)
		{
			string url = MakeUrl(pathInStreamingAssets, overrideEnabled);
			var req = new UnityWebRequest(url);
			req.method = UnityWebRequest.kHttpVerbGET;
			var handler = new DownloadHandlerTexture(readable);
			req.downloadHandler = handler;
			yield return req.SendWebRequest();
			if (req.error != null)
			{
				ret.Fail(new System.IO.FileLoadException(url));
			}
			else
			{
				ret.Succeed(handler.texture);
			}
		}

		// StreamingAssetsからAudioClipをロードする
		public static IEnumerator CoLoad(
			CoroutineReturnValue<AudioClip> ret,
			string pathInStreamingAssets,
			bool overrideEnabled = true,
			bool readable = false)
		{
			string url = MakeUrl(pathInStreamingAssets, overrideEnabled);
			var req = new UnityWebRequest(url);
			req.method = UnityWebRequest.kHttpVerbGET;
			AudioType type = AudioType.UNKNOWN;
			var ext = System.IO.Path.GetExtension(url).ToLower();
			switch (ext)
			{
				case ".wav": type = AudioType.WAV; break;
				case ".wave": type = AudioType.WAV; break;
				case ".ogg": type = AudioType.OGGVORBIS; break;
				case ".mp3": type = AudioType.MPEG; break;
				case ".aiff": type = AudioType.AIFF; break;
			}
			var handler = new DownloadHandlerAudioClip(url, type);
			req.downloadHandler = handler;
			yield return req.SendWebRequest();
			if (req.error != null)
			{
				ret.Fail(new System.IO.FileLoadException(url));
			}
			else
			{
				ret.Succeed(handler.audioClip);
			}
		}

		static string MakeUrl(string path, bool overrideEnabled)
		{
			string url = null;
			if (overrideEnabled)
			{
				url = GetPersistentDataPath() + "/" + DirectoryName + "/" + path;
				if (!File.Exists(url))
				{
					url = null;
				}
			}
			if (url == null)
			{
				url = Application.streamingAssetsPath + "/" + path;
			}
			if (!url.Contains("://"))
			{
				url = "file:///" + url;
			}
			return url;
		}

		public static void SaveOverride(string path, Stream stream)
		{
			path = Path.Combine(DirectoryName, path);
			WriteFile(path, stream);
		}

		public static void SaveOverride(string path, byte[] bytes)
		{
			path = Path.Combine(DirectoryName, path);
			WriteFile(path, bytes);
		}

		public static void DeleteOverride(string path)
		{
			path = Path.Combine(DirectoryName, path);
			DeleteFile(path);
		}

		public static void DeleteAllOverride()
		{
			DeleteFileRecursive(DirectoryName);
		}

		static void TryCreateDirectory(string path)
		{
			path = Path.Combine(GetPersistentDataPath(), path);
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
		}

		static void WriteFile(string path, byte[] data)
		{
			path = Path.Combine(GetPersistentDataPath(), path);
			try
			{
				File.WriteAllBytes(path, data);
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
		}
		}

		static void WriteFile(string path, Stream stream)
		{
			path = Path.Combine(GetPersistentDataPath(), path);
			try
			{
				var dir = Path.GetDirectoryName(path);
				TryCreateDirectory(dir);
				using (var outStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					const int bufferSize = 1024 * 1024; // TODO: これ128とかにしてファイルちゃんと保存されるか確認しとけ
					var buffer = new byte[bufferSize];
					while (true)
					{
						var advance = stream.Read(buffer, 0, bufferSize);
						if (advance > 0)
						{
							outStream.Write(buffer, 0, advance);
						}
						else
						{
							break;
						}
					}
				}
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}
		}

		static void DeleteFile(string path)
		{
			path = Path.Combine(GetPersistentDataPath(), path);
			try
			{
				File.Delete(path);
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}
		}

		static void DeleteFileRecursive(string path)
		{
			var fullPath = Path.Combine(GetPersistentDataPath(), path);
			// 安全装置
			var separator = new string(Path.PathSeparator, 1);
			// 上へ行くのは危険すぎるので抜ける
			var parentPattern = separator + ".." + separator;
			if (fullPath.Contains(parentPattern))
			{
				Debug.LogError("DeleteFileRecursive: contains " + parentPattern);
				return;
			}
			if (Directory.Exists(fullPath))
			{
				try
				{
					Directory.Delete(fullPath, recursive: true);
				}
				catch (System.Exception e)
				{
					Debug.LogException(e);
				}
			}
		}
	}
}