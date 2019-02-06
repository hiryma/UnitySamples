using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
	[SerializeField]
	Texture _testTex; // デバグで投げるためのテクスチャ
	[SerializeField]
	UnityEngine.UI.Text _logText;

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

	void OnGUI()
	{
		var slack = Kayac.SlackDebug.instance;
		if (GUILayout.Button("スクショ"))
		{
			StartCoroutine(slack.CoPostScreenshot(
				"スクショテスト",
				() => Debug.Log("OnImageCaptured"),
				(errorMessage) => Debug.Log("CoPostScreenshot OnComplete " + errorMessage),
				channel: null,
				waitFrameCount: 0));
		}

		if (GUILayout.Button("メッセージ"))
		{
			StartCoroutine(slack.CoPostMessage(
				"メッセージテスト",
				(errorMessage) => Debug.Log("CoPostMessage OnComplete " + errorMessage),
				channel: null));
		}

		if (GUILayout.Button("スニペット"))
		{
			StartCoroutine(slack.CoPostSnippet(
				"スニペットテスト",
				(errorMessage) => Debug.Log("CoPostSnippet OnComplete " + errorMessage),
				channel: null));
		}

		if (GUILayout.Button("ログ投稿"))
		{
			StartCoroutine(slack.CoPostBinary(
				Kayac.MemoryLogHandler.instance.GetBytes(),
				"binaryTest.txt",
				(errorMessage) => Debug.Log("CoPostBinary OnComplete " + errorMessage),
				null,
				null));
		}

		if (GUILayout.Button("テクスチャ投稿"))
		{
			StartCoroutine(slack.CoPostTexture(
				_testTex,
				null,
				null,
				(errorMessage) => Debug.Log("CoPostTexture OnComplete " + errorMessage),
				channel: null));
		}

		if (GUILayout.Button("Log"))
		{
			Debug.Log("LogButton pressed.");
		}

		if (GUILayout.Button("Warning"))
		{
			Debug.LogWarning("WarningButton pressed.");
		}

		if (GUILayout.Button("Error"))
		{
			Debug.LogError("ErrorButton pressed.");
		}

		if (GUILayout.Button("Null例外"))
		{
			string a = null;
			int b = a.Length; // null死して例外吐く。これがログに溜まることを確認する
		}
	}

	void Update()
	{	// 画面にログの最新部を表示
		_logText.text = Kayac.MemoryLogHandler.instance.Tail(10);
	}
}
