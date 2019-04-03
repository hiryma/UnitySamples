using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

namespace Kayac
{
	public class DebugSlack
	{
		// 簡易暗号化
		public static string EncryptXor(string key, string from)
		{
			var fromBytes = System.Text.Encoding.UTF8.GetBytes(from);
			var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
			var toBytes = Xor(keyBytes, fromBytes);
			return System.Convert.ToBase64String(toBytes);
		}

		static byte[] Xor(byte[] key, byte[] from)
		{
			var to = new byte[from.Length];
			int fromIndex = 0;
			while (fromIndex < from.Length)
			{
				int keyIndex = 0;
				while (keyIndex < key.Length)
				{
					if (fromIndex >= from.Length)
					{
						break;
					}
					var xored = from[fromIndex] ^ key[keyIndex];
					to[fromIndex] = (byte)xored;
					keyIndex++;
					fromIndex++;
				}
			}
			return to;
		}

		public static string DecryptXor(string key, string encrypted)
		{
			var fromBytes = System.Convert.FromBase64String(encrypted);
			var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
			var toBytes = Xor(keyBytes, fromBytes);
			return System.Text.Encoding.UTF8.GetString(toBytes);
		}

		public DebugSlack(string apiToken, string defaultChannel)
		{
			_apiToken = apiToken;
			_defaultChannel = defaultChannel;
		}
		public delegate void OnComplete(string errorMessage); // エラーがなく成功すればnull

		// この関数だけはStartCoroutineで呼ばないと動かない
		public IEnumerator CoPostScreenshot(
			System.Action onImageCaptured = null, // 画像が取れた後で呼ぶコールバック(デバグUIを復活させるなど)
			string message = null,
			OnComplete onComplete = null,  // 完了コールバック(ポップアップを出す、ログを出す、等々)
			string channel = null, // チャネル名を変更したければここに与える(与えなければdefaultChannel)
			int waitFrameCount = 0, // デバグUIを消すなどがすぐに済まない場合、ここにフレーム数を指定
			bool withAlpha = false,
			TextureUtil.FileType fileType = TextureUtil.FileType.Png)
		{
			var coRet = new CoRetVal<byte[]>();
			yield return TextureUtil.CoGetScreenshot(coRet, withAlpha, waitFrameCount, fileType);
			if (onImageCaptured != null)
			{
				onImageCaptured();
			}

			if (channel == null)
			{
				channel = _defaultChannel;
			}

			var now = System.DateTime.Now;
			string filename = now.ToString("yyyyMMdd_HHmmss");
			filename += (fileType == TextureUtil.FileType.Jpeg) ? ".jpg" : ".png";
			yield return CoPostBinary(coRet.value, filename, message, onComplete, channel);
		}

		public IEnumerator CoPostTexture(
			Texture texture,
			string filename = null,
			string message = null,
			OnComplete onComplete = null,
			string channel = null,
			TextureUtil.FileType fileType = TextureUtil.FileType.Png)
		{
			Debug.Assert(texture != null);
			if (filename == null)
			{
				filename = texture.name;
				filename += (fileType == TextureUtil.FileType.Jpeg) ? ".jpg" : ".png";
			}

			var coroutines = new List<IEnumerator>();
			string error = ""; // エラーは全部まとめた
			PostTextureInternal(
				coroutines,
				error,
				texture,
				filename,
				message,
				channel,
				fileType);

			// 一気に待ち
			foreach (var coroutine in coroutines)
			{
				while (coroutine.MoveNext())
				{
					yield return null;
				}
			}

			if (onComplete != null)
			{
				onComplete(error);
			}
		}

		public IEnumerator CoPostTextures(
			IEnumerable<Texture> textures,
			string message = null,
			OnComplete onComplete = null,
			string channel = null,
			TextureUtil.FileType fileType = TextureUtil.FileType.Png)
		{
			var coroutines = new List<IEnumerator>();
			string error = ""; // エラーは全部まとめた
			// 全部1フレでpngファイル生成までやらないといけない
			Debug.Assert(textures != null);
			bool first = true;
			foreach (var texture in textures)
			{
				var filename = texture.name;
				filename += (fileType == TextureUtil.FileType.Jpeg) ? ".jpg" : ".png";
				PostTextureInternal(
					coroutines,
					error,
					texture,
					filename,
					first ? message : null,
					channel,
					fileType);

				first = false;
			}

			// 一気に待ち
			foreach (var coroutine in coroutines)
			{
				while (coroutine.MoveNext())
				{
					yield return null;
				}
			}

			if (onComplete != null)
			{
				onComplete(error);
			}
		}

		// 全ミップレベル一気にファイルイメージを生成してコルーチンをリストに溜める。
		void PostTextureInternal(
			List<IEnumerator> coroutinesOut,
			string errorAppend,
			Texture texture,
			string filename,
			string message,
			string channel,
			TextureUtil.FileType fileType)
		{
			var trunc = System.IO.Path.GetFileNameWithoutExtension(filename);
			var ext = System.IO.Path.GetExtension(filename); //.が入ってる
			var hasMipmap = TextureUtil.GetMipmapCount(texture) > 1;
			var bytesList = TextureUtil.ConvertAllLevelToFile(texture, fileType);

			for (int level = 0; level < bytesList.Length; level++)
			{
				var levelFilename = trunc;
				if (hasMipmap)
				{
					levelFilename += "_" + level;
				}
				levelFilename += ext;
				var coPostBinary = CoPostBinary(
					bytesList[level],
					levelFilename,
					(level == 0) ? message : null, // messageは最初だけ
					onComplete: e =>
					{
						if (!string.IsNullOrEmpty(e))
						{
							if (!string.IsNullOrEmpty(errorAppend))
							{
								errorAppend += "\n";
							}
							errorAppend += e;
						}
					},
					channel);
				coroutinesOut.Add(coPostBinary);
			}
		}

		public IEnumerator CoPostMessage(
			string message,
			OnComplete onComplete = null, // 完了コールバック(ポップアップを出す、ログを出す、等々)
			string channel = null) // チャネル名を変更したければここに与える
		{
			Debug.Assert(message != null);
			if (channel == null)
			{
				channel = _defaultChannel;
			}
			var form = new WWWForm();
			form.AddField("token", _apiToken);
			form.AddField("channel", channel);
			form.AddField("text", message);
			var coPost = CoPost(_postMessageUri, form, onComplete);
			while (coPost.MoveNext())
			{
				yield return null;
			}
		}

		public IEnumerator CoPostSnippet(
			string message,
			OnComplete onComplete = null, // 完了コールバック(ポップアップを出す、ログを出す、等々)
			string channel = null,
			string filename = null)
		{
			Debug.Assert(message != null);
			if (channel == null)
			{
				channel = _defaultChannel;
			}
			var form = new WWWForm();
			form.AddField("token", _apiToken);
			form.AddField("channels", channel);
			form.AddField("content", message);
			if (!string.IsNullOrEmpty(filename))
			{
				form.AddField("filename", filename);
			}

			var coPost = CoPost(_fileUploadUri, form, onComplete);
			while (coPost.MoveNext())
			{
				yield return null;
			}
		}

		public IEnumerator CoPostBinary(
			byte[] binary,
			string filename,
			string message = null,
			OnComplete onComplete = null,
			string channel = null)
		{
			Debug.Assert(binary != null);
			Debug.Assert(filename != null);
			if (channel == null)
			{
				channel = _defaultChannel;
			}
			var form = new WWWForm();
			form.AddField("token", _apiToken);
			form.AddField("channels", channel);
			form.AddBinaryData("file", binary, filename);

			if (message == null)
			{
				message = GenerateDefaultMessage(filename);
			}
			form.AddField("initial_comment", message);

			var coPost = CoPost(_fileUploadUri, form, onComplete);
			while (coPost.MoveNext())
			{
				yield return null;
			}
		}

		// ------------------ 以下private ----------------------

		string GenerateDefaultMessage(string filename)
		{
			return filename + " " + SystemInfo.deviceModel + " " + SystemInfo.operatingSystem;
		}

		static IEnumerator CoPost(string uri, WWWForm form, OnComplete onComplete)
		{
#if UNITY_2017_1_OR_NEWER
			var www = UnityWebRequest.Post(uri, form);
			var operation = www.SendWebRequest();
			while (!operation.isDone)
#else
			var www = new WWW(uri, form);
			while (!www.isDone)
#endif
			{
				yield return null;
			}

			if (onComplete != null)
			{
				onComplete(www.error);
			}
		}

		readonly string _apiToken;
		readonly string _defaultChannel;
		const string _baseUri = "https://slack.com/api/";
		const string _fileUploadUri = _baseUri + "files.upload";
		const string _postMessageUri = _baseUri + "chat.postMessage";
	}
}
