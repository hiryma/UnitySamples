using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
	[SerializeField]
	Texture _testTex; // デバグで投げるためのテクスチャ

	void Start()
	{
		/*
		slackのtokenをコードに直打ちすると漏れそうで怖いですね。
		実用にする場合はいろいろ考えておくのが良いと思います。
		このサンプルではエディタからしか起動しないので、
		.gitignoreしたテキストファイルから読んでいます。
		*/
		var tokenFile = new System.IO.StreamReader("slackToken.txt"); // 暗号化しといた方がいいよ!
		var token = tokenFile.ReadToEnd();
		tokenFile.Close();
		// 初期化が必要
		Kayac.SlackDebug.Create(
			token,
			"unity-debug",
			"unity-debug");
		// ログ蓄積クラス初期化
		Kayac.MemoryLogHandler.Create(lineCapacity: 100); // 最新N個のログを保存
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.F1))
		{
			StartCoroutine(Kayac.SlackDebug.instance.CoPostScreenshot(
				"スクショテスト",
				() => Debug.Log("OnImageCaptured"),
				(errorMessage) => Debug.Log("CoPostScreenshot OnComplete " + errorMessage),
				channel: null,
				waitFrameCount: 0));
		}

		if (Input.GetKeyDown(KeyCode.F2))
		{
			StartCoroutine(Kayac.SlackDebug.instance.CoPostMessage(
				"メッセージテスト",
				(errorMessage) => Debug.Log("CoPostMessage OnComplete " + errorMessage),
				channel: null));
		}

		if (Input.GetKeyDown(KeyCode.F3))
		{
			StartCoroutine(Kayac.SlackDebug.instance.CoPostSnippet(
				"スニペットテスト",
				(errorMessage) => Debug.Log("CoPostSnippet OnComplete " + errorMessage),
				channel: null));
		}

		if (Input.GetKeyDown(KeyCode.F4))
		{
			StartCoroutine(Kayac.SlackDebug.instance.CoPostBinary(
				Kayac.MemoryLogHandler.GetBytes(),
				"bynaryTest.txt",
				(errorMessage) => Debug.Log("CoPostBinary OnComplete " + errorMessage),
				null,
				null));
		}

		if (Input.GetKeyDown(KeyCode.F5))
		{
			StartCoroutine(Kayac.SlackDebug.instance.CoPostTexture(
				_testTex,
				null,
				null,
				(errorMessage) => Debug.Log("CoPostTexture OnComplete " + errorMessage),
				channel: null));
		}

		if (Input.GetKeyDown(KeyCode.F6))
		{
			Debug.Log("F6 pressed.");
		}

		if (Input.GetKeyDown(KeyCode.F7))
		{
			Debug.LogWarning("F7 pressed.");
		}

		if (Input.GetKeyDown(KeyCode.F8))
		{
			Debug.LogError("F8 pressed.");
		}

		if (Input.GetKeyDown(KeyCode.F9))
		{
			string a = null;
			Debug.Log(a.Length);
			throw new System.InvalidOperationException("F9 pressed.");
		}
	}
}
