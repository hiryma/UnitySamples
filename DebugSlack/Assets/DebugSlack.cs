using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace Kayac
{
	public class SlackDebug
	{
		// シングルトンにしているのは、不要な時(リリースビルド)等でインスタンスすら作りたくないという要求と、どこでも使いたいという要求の間の妥協
		public static void Create(
			string apiToken,
			string defaultScreenShotChannel,
			string defaultMessageChannel)
		{
			instance = new SlackDebug(apiToken, defaultScreenShotChannel, defaultMessageChannel);
		}

		public static SlackDebug instance{ get; private set; }
		public delegate void OnComplete(string errorMessage); // エラーがなく成功すればnull

		public IEnumerator CoPostScreenshot(
			string message = null,
			System.Action onImageCaptured = null, // 画像が取れた後で呼ぶコールバック(デバグUIを復活させるなど)
			OnComplete onComplete = null,  // 完ー了コルバック(ポップアップを出す、ログを出す、等々)
			string channel = null, // チャネル名を変更したければここに与える(与えなければdefaultScreenShotChannel)
			int waitFrameCount = 0) // デバグUIを消すなどがすぐに済まない場合、ここにフレーム数を指定
		{
			for (int i = 0; i < waitFrameCount; i++)
			{
				yield return null;
			}
			yield return new WaitForEndOfFrame();

			var width = Screen.width;
			var height = Screen.height;
			var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
			tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
			if (onImageCaptured != null)
			{
				onImageCaptured();
			}
			var pngBytes = tex.EncodeToPNG();

			if (channel == null)
			{
				channel = _defaultScreenShotChannel;
			}

			var now = System.DateTime.Now;
			string filename = now.ToString("yyyyMMdd_HHmmss") + ".png";

			var coPostBinary = CoPostBinary(pngBytes, filename, onComplete, channel, message);
			while (coPostBinary.MoveNext())
			{
				yield return null;
			}
		}

		public IEnumerator CoPostTexture(
			Texture texture,
			string filename = null,
			string message = null,
			OnComplete onComplete = null,
			string channel = null)
		{
			Debug.Assert(texture != null);
			if (filename == null)
			{
				filename = texture.name + ".png";
			}

			var tex2d = texture as Texture2D;
#if UNITY_2018_1_OR_NEWER
			if ((tex2d == null) || !tex2d.isReadable) // 2Dでない場合及び、そのまま読めない場合
#else
			if (true) // 2018より前にはisReadableがなく判定できないため、常にRenderTextureを経由する
#endif
			{
				RenderTexture renderTexture = texture as RenderTexture;
				// 来たのがRenderTextureでないならRenderTexture生成してそこにコピー
				if (renderTexture == null)
				{
					renderTexture = new RenderTexture(texture.width, texture.height, 0);
					Graphics.Blit(texture, renderTexture);
				}

				// 読み出し用テクスチャを生成して差し換え
				tex2d = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
				RenderTexture.active = renderTexture;
				tex2d.ReadPixels(new Rect(0, 0, tex2d.width, tex2d.height), 0, 0);
			}
			var pnbBytes = tex2d.EncodeToPNG();
			var coPostBinary = CoPostBinary(pnbBytes, filename, onComplete, channel, message);
			while (coPostBinary.MoveNext())
			{
				yield return null;
			}
		}

		public IEnumerator CoPostMessage(
			string message,
			OnComplete onComplete = null, // 完了コールバック(ポップアップを出す、ログを出す、等々)
			string channel = null) // チャネル名を変更したければここに与える
		{
			Debug.Assert(message != null);
			var uri = new System.Uri(_baseUri + _postMessageUri).ToString();

			var wwwForm = new WWWForm();
			if (channel == null)
			{
				channel = _defaultMessageChannel;
			}
			wwwForm.AddField("channel", channel);
			wwwForm.AddField("token", _apiToken);
			wwwForm.AddField("text", message);

			var coPost = CoPost(uri, wwwForm, onComplete);
			while (coPost.MoveNext())
			{
				yield return null;
			}
		}

		public IEnumerator CoPostSnippet(
			string message,
			OnComplete onComplete = null, // 完了コールバック(ポップアップを出す、ログを出す、等々)
			string channel = null,
			string fileName = null)
		{
			Debug.Assert(message != null);
			var uri = new System.Uri(_baseUri + _fileUploadUri).ToString();

			if (channel == null)
			{
				channel = _defaultMessageChannel;
			}
			var wwwForm = new WWWForm();
			wwwForm.AddField("channels", channel);
			wwwForm.AddField("token", _apiToken);
			wwwForm.AddField("content", message);
			if (!string.IsNullOrEmpty(fileName))
			{
				wwwForm.AddField("filename", fileName);
			}

			var coPost = CoPost(uri, wwwForm, onComplete);
			while (coPost.MoveNext())
			{
				yield return null;
			}
		}

		public IEnumerator CoPostBinary(
			byte[] binary,
			string filename,
			OnComplete onComplete = null,
			string channel = null,
			string message = null)
		{
			Debug.Assert(binary != null);
			Debug.Assert(filename != null);
			var uri = new System.Uri(_baseUri + _fileUploadUri).ToString();

			var wwwForm = new WWWForm();

			wwwForm.AddBinaryData("file", binary, filename);

			if (channel == null)
			{
				channel = _defaultMessageChannel;
			}
			wwwForm.AddField("channels", channel);
			wwwForm.AddField("token", _apiToken);

			if (message == null)
			{
				message = GenerateDefaultMessage(filename);
			}
			wwwForm.AddField("initial_comment", message);

			var coPost = CoPost(uri, wwwForm, onComplete);
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

		SlackDebug(
			string apiToken,
			string defaultScreenShotChannel,
			string defaultMessageChannel)
		{
			_apiToken = apiToken;
			_defaultScreenShotChannel = defaultScreenShotChannel;
			_defaultMessageChannel = defaultMessageChannel;
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
		readonly string _defaultScreenShotChannel;
		readonly string _defaultMessageChannel;
		const string _baseUri = "https://slack.com/api/";
		const string _fileUploadUri = "files.upload";
		const string _postMessageUri = "chat.postMessage";
	}
}
