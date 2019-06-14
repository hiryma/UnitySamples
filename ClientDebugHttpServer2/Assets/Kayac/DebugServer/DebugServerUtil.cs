using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
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

		public static string GetLanIpAddress()
		{
			string ret = null;
			foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
			{
				if ((ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) || (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
				{
					foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
					{
						if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
						{
							ret = ip.Address.ToString();
						}
					}
				}
			}
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
			Debug.Assert(req.isDone);
			if (req.error != null)
			{
				ret.Fail(new System.IO.FileLoadException(url));
			}
			else
			{
				ret.Succeed(req.downloadHandler.text);
			}
			req.Dispose();
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
			req.Dispose();
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
			req.Dispose();
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
			req.Dispose();
		}

		public static void SaveOverride(string path, Stream stream)
		{
			path = Path.Combine(DirectoryName, path);
			WriteFile(path, stream);
		}

		public static void DeleteOverride(string path)
		{
			Debug.Assert(!path.StartsWith("/"));
			path = Path.Combine(DirectoryName, path);
			DeleteFile(path);
		}

		public static string MakeUrl(string path, bool overrideEnabled)
		{
			Debug.Assert(!path.StartsWith("/"));
			string url = null;
			if (overrideEnabled)
			{
				url = string.Format(
					"{0}/{1}/{2}",
					GetPersistentDataPath(),
					DirectoryName,
					path);
				if (!File.Exists(url))
				{
					url = null;
				}
			}
			if (url == null)
			{
				url = MakeStreamingAssetsPath(path);
			}
			if (!url.Contains("://"))
			{
				url = "file:///" + url;
			}
			return url;
		}

		public static bool DirectoryExists(string path)
		{
			var absolutePath = MakeStreamingAssetsPath(path);
			return Directory.Exists(absolutePath);
		}

		public static string RemoveQueryString(string url)
		{
			var qIndex = url.IndexOf('?'); // Query捨てる
			if (qIndex >= 0)
			{
				url = url.Remove(qIndex);
			}
			return url;
		}

		// non public -----------------------

		public static string MakeStreamingAssetsPath(string path)
		{
			var ret = string.Format(
				"{0}/{1}",
				Application.streamingAssetsPath,
				path);
			return ret;
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