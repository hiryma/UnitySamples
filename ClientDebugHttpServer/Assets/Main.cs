using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Kayac;

public class Main : MonoBehaviour
{
	[SerializeField]
	RawImage image;
	[SerializeField]
	AudioSource audioSource;
	[SerializeField]
	TextAsset debugServerIndexHtmlAsset;
	[SerializeField]
	int debugServerPort;
	[SerializeField]
	Text logText;

	string debugServerIndexHtml;
	DebugServer debugServer;
	float rotationSpeed;
	Coroutine coroutine;
	bool loadRequested;

	void Start()
	{
		Application.logMessageReceived += OnLogReceived;
		var ipAddress = DebugServer.GetLanIpAddress();
		Debug.Log("IP: " + ipAddress);
		debugServerIndexHtml = debugServerIndexHtmlAsset.text;
		debugServerIndexHtml = debugServerIndexHtml.Replace(
			"__TO_BE_REPLACED_WITH_ACTUAL_IP_ADDRESS_IN_RUNTIME__",
			ipAddress);

		debugServer = new DebugServer(debugServerPort);

		// 上書き検出
		debugServer.RegisterRequestCallback("/", OnWebRequestRoot);
		debugServer.RegisterRequestCallback("/upload-file", OnWebRequestUploadFile);
		debugServer.RegisterRequestCallback("/delete-file", OnWebRequestDeleteFile);
		debugServer.RegisterRequestCallback("/delete-all-file", OnWebRequestDeleteAllFile);
		loadRequested = true;
	}

	void OnLogReceived(string message, string callStack, LogType type)
	{
		logText.text += message + '\n';
	}

	void OnWebRequestRoot(out string outputHtml, string inputText)
	{
		// html返して終わり
		outputHtml = debugServerIndexHtml;
	}

	void OnWebRequestUploadFile(out string outputHtml, string inputText)
	{
		outputHtml = null;
		if (string.IsNullOrEmpty(inputText))
		{
			outputHtml = "入力テキストがない.";
			return;
		}
		var arg = UnityEngine.JsonUtility.FromJson<UploadFileArg>(inputText);
		if (string.IsNullOrEmpty(arg.path))
		{
			outputHtml = "アップロードしたファイルのパスが空.";
			return;
		}
		if (arg.contentBase64 == null)
		{
			outputHtml = "ファイルの中身が空.";
			return;
		}
		var bytes = System.Convert.FromBase64String(arg.contentBase64);
		DebugServerUtil.SaveOverride(arg.path, bytes);
		loadRequested = true;
	}

	void OnWebRequestDeleteFile(out string outputHtml, string inputText)
	{
		outputHtml = null;
		if (string.IsNullOrEmpty(inputText))
		{
			outputHtml = "入力テキストがない.";
			return;
		}
		var arg = UnityEngine.JsonUtility.FromJson<UploadFileArg>(inputText);
		if (string.IsNullOrEmpty(arg.path))
		{
			outputHtml = "消したファイルのパスが空.";
			return;
		}
		DebugServerUtil.DeleteOverride(arg.path);
		loadRequested = true;
	}

	void OnWebRequestDeleteAllFile(out string outputHtml, string inputText)
	{
		DebugServerUtil.DeleteAllOverride();
		outputHtml = null;
		loadRequested = true;
	}

	void Update()
	{
		// 絵を回転
		var angles = image.transform.localRotation.eulerAngles;
		angles.z += rotationSpeed;
		image.transform.localRotation = Quaternion.Euler(angles);

		// 音鳴らす
		if (!audioSource.isPlaying)
		{
			audioSource.Play();
		}
		debugServer.ManualUpdate();

		// ロード。重複実行を防ぐ
		if (loadRequested && (coroutine == null))
		{
			loadRequested = false;
			coroutine = StartCoroutine(CoLoad());
		}
	}


	[System.Serializable]
	class UploadFileArg
	{
		public string path;
		public string contentBase64;
	}

	[System.Serializable]
	class RotationSpeedData
	{
		public float rotationSpeed;
	}

	IEnumerator CoLoad()
	{
		audioSource.Stop();
		var retJson = new CoroutineReturnValue<string>();
		yield return DebugServerUtil.CoLoad(retJson, "rotation_speed.json");
		if (retJson.Exception != null)
		{
			Debug.LogException(retJson.Exception);
		}
		var retImage = new CoroutineReturnValue<Texture2D>();
		yield return DebugServerUtil.CoLoad(retImage, "image.png");
		if (retImage.Exception != null)
		{
			Debug.LogException(retImage.Exception);
		}
		var retSound = new CoroutineReturnValue<AudioClip>();
		yield return DebugServerUtil.CoLoad(retSound, "sound.wav");
		if (retSound.Exception != null)
		{
			Debug.LogException(retSound.Exception);
		}

		if (retJson.Value != null)
		{
			rotationSpeed = JsonUtility.FromJson<RotationSpeedData>(retJson.Value).rotationSpeed;
		}

		if (retImage.Value != null)
		{
			image.texture = retImage.Value;
		}

		if (retSound.Value != null)
		{
			audioSource.clip = retSound.Value;
		}
		coroutine = null;
	}
}
