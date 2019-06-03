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

	const float SoundInterval = 2f;
	string debugServerIndexHtml;
	DebugServer debugServer;
	float rotationSpeed;
	float soundTimer;

	IEnumerator Start()
	{
		var ipAddress = DebugServer.GetLanIpAddress();
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
		yield return CoLoad();
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
		// 再度ロード
		StartCoroutine(CoLoad());
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
		// 再度ロード
		StartCoroutine(CoLoad());
	}

	void OnWebRequestDeleteAllFile(out string outputHtml, string inputText)
	{
		DebugServerUtil.DeleteAllOverride();
		// 再度ロード
		StartCoroutine(CoLoad());
		outputHtml = null;
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
			if (soundTimer <= 0f)
			{
				audioSource.Play();
				soundTimer = SoundInterval;
			}
			else
			{
				soundTimer -= Time.deltaTime;
			}
		}

		debugServer.ManualUpdate();
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
		var retImage = new CoroutineReturnValue<Texture2D>();
		yield return DebugServerUtil.CoLoad(retImage, "image.png");
		var retSound = new CoroutineReturnValue<AudioClip>();
		yield return DebugServerUtil.CoLoad(retSound, "sound.wav");

		rotationSpeed = JsonUtility.FromJson<RotationSpeedData>(retJson.Value).rotationSpeed;
		image.texture = retImage.Value;
		audioSource.clip = retSound.Value;
	}
}
