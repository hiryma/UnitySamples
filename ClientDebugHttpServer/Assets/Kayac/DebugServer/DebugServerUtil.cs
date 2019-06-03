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
			var dataPath = Application.dataPath;
			if (dataPath.EndsWith("/Assets"))
			{
				dataPath = dataPath.Substring(0, dataPath.Length - "/Assets".Length);
			}
			else
			{
				Debug.LogError("Application.dataPathが/Assetsで終わってない!!仕様が変わったか?! " + dataPath);
			}
			// GetCurrentDirectoryがプロジェクトパスを返すことに依存している。動作が変われば動かなくなる!
			ret = Path.Combine(Directory.GetCurrentDirectory(), "persistent-data");
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
			if (url.StartsWith("file:///"))
			{
				try
				{
					var text = File.ReadAllText(url.Replace("file:///", ""));
					ret.Succeed(text);
				}
				catch (System.Exception e)
				{
					ret.Fail(e);
					yield break;
				}
			}
			else
			{
				var req = UnityWebRequest.Get(url);
				req.SendWebRequest();
				yield return req;
				if (req.error != null)
				{
					ret.Fail(new System.IO.FileLoadException(url));
					yield break;
				}
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
			if (url.StartsWith("file:///"))
			{
				try
				{
					var bytes = File.ReadAllBytes(url.Replace("file:///", ""));
					ret.Succeed(bytes);
				}
				catch (System.Exception e)
				{
					ret.Fail(e);
					yield break;
				}
			}
			else
			{
				var req = UnityWebRequest.Get(url);
				req.SendWebRequest();
				yield return req;
				if (req.error != null)
				{
					ret.Fail(new System.IO.FileLoadException(url));
					yield break;
				}
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
			//DownloadHandlerTextureを使うとうまくいかないので手抜き
			var innerRet = new CoroutineReturnValue<byte[]>();
			yield return CoLoad(innerRet, pathInStreamingAssets, overrideEnabled);
			if (innerRet.Exception != null)
			{
				ret.Fail(innerRet.Exception);
				yield break;
			}
			else
			{
				var texture = new Texture2D(1, 1);
				Debug.Log("CoLoad<Texture2D> : " + innerRet.Value.Length);
				if (UnityEngine.ImageConversion.LoadImage(texture, innerRet.Value, markNonReadable: !readable))
				{
					ret.Succeed(texture);
				}
				else
				{
					ret.Fail(new System.IO.InvalidDataException("ImageConversion.LoadImage failed."));
				}
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
			req.SendWebRequest();
			yield return req;
			Debug.Assert(req.isDone);
			while (!req.isDone)
			{
				yield return null;
			}
			if (req.error != null)
			{
				ret.Fail(new System.IO.FileLoadException(url));
				yield break;
			}
			var clip = handler.audioClip;
			Debug.Log("Audio: " + handler.isDone + " " + handler.data.Length);
			Debug.Log(clip.name + " " + clip.length + " " + clip.samples);
			ret.Succeed(handler.audioClip);
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

		public static void SaveOverride(string path, byte[] bytes)
		{
			TryCreateDirectory(DirectoryName);
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
				Debug.Log("Save " + path + " " + data.Length + " bytes.");
				FileInfo fi = new FileInfo(path);
				Debug.Log(fi.Exists + " " + fi.Length);
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
				Debug.Log("Delete " + path);
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