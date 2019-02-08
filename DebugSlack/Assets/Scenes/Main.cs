using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
	// 以下2つは用途に合わせて書き換えてから動かしてみてください
	const string _defaultChannel = "unity-debug";
	const string _errorReportChannel = "unity-error-report";

	[SerializeField]
	Texture _testTex; // デバグで投げるためのテクスチャ
	[SerializeField]
	UnityEngine.UI.Text _logText;

	bool _onGuiDisabled;
	Kayac.DebugSlack _slack;
	Kayac.MemoryLogHandler _logHandler;

	void Start()
	{
		// ログ蓄積クラス初期化
		_logHandler = new Kayac.MemoryLogHandler(lineCapacity: 100); // 最新N個のログを保存
		/*
		slackのtokenをコードに直打ちすると漏れそうで怖いですね。
		実用にする場合はいろいろ考えておくのが良いと思います。
		このサンプルでは.gitignoreでgitに入らなくしたテキストファイルをStreamingAssetsから読んでいます。
		*/
		var tokenFilePath = Application.streamingAssetsPath + "/slackToken.txt";
		var tokenFile = new System.IO.StreamReader(tokenFilePath); // 暗号化しといた方がいいよ!
		var token = tokenFile.ReadToEnd();
		tokenFile.Close();
		_slack = new Kayac.DebugSlack(token, _defaultChannel);
	}

	void ReportError()
	{
		_onGuiDisabled = true;
		StartCoroutine(_slack.CoPostScreenshot(
			() => _onGuiDisabled = false,
			"エラー報告",
			null,
			_errorReportChannel,
			waitFrameCount: 1)); // 次のフレームでOnGUIで何もしない状態にしてから撮影
		var log = _logHandler.GetString();
		var sb = new System.Text.StringBuilder();
		var now = System.DateTime.Now;
		sb.Append("----SystemInfo----\n");
		sb.AppendFormat("[ErrorLog] {0}\n", now.ToString("yyyy/MM/dd HH:mm:ss.fff"));
		sb.AppendFormat("device: {0} {1} {2} Memory:{3}\n", SystemInfo.deviceModel, SystemInfo.deviceName, SystemInfo.deviceType, SystemInfo.systemMemorySize);
		sb.AppendFormat("os: {0} {1}\n", SystemInfo.operatingSystem, SystemInfo.operatingSystemFamily);
		sb.AppendFormat("graphics: {0} {1} {2} Memory:{3}\n", SystemInfo.graphicsDeviceName, SystemInfo.graphicsDeviceType, SystemInfo.graphicsDeviceVersion, SystemInfo.graphicsMemorySize);
		sb.AppendFormat("processor: {0} core: {1} {2}MHz\n", SystemInfo.processorType, SystemInfo.processorCount, SystemInfo.processorFrequency);
		sb.AppendFormat("battery: {0}% {1}\n", SystemInfo.batteryLevel * 100f, SystemInfo.batteryStatus);
		sb.AppendFormat("shaderLevel: {0}\n", SystemInfo.graphicsShaderLevel);
		sb.AppendFormat("maxTextureSize: {0}\n", SystemInfo.maxTextureSize);
		sb.AppendFormat("nonPowerOfTwoSupport: {0}\n", SystemInfo.npotSupport);
#if UNITY_2018_1_OR_NEWER
		sb.AppendFormat("hasDynamicUniformArrayIndexingInFragmentShaders: {0}\n", SystemInfo.hasDynamicUniformArrayIndexingInFragmentShaders);
		sb.AppendFormat("supports32bitsIndexBuffer: {0}\n", SystemInfo.supports32bitsIndexBuffer);
#endif
		sb.Append("----SceneInfo----\n");
		for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
		{
			sb.AppendFormat("{0}\n", UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name);
		}
		sb.Append("----Log----\n");
		var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString() + log);
		StartCoroutine(_slack.CoPostBinary(
			bytes,
			"errorLog" + now.ToString("yyyy_MM_dd__HH_mm_ss_fff") + ".txt",
			null,
			null,
			_errorReportChannel));
	}

	void OnGUI()
	{
		if (_onGuiDisabled)
		{
			return;
		}
		if (GUILayout.Button("バグ報告"))
		{
			ReportError();
		}
		if (GUILayout.Button("スクショ"))
		{
			StartCoroutine(_slack.CoPostScreenshot(
				() => Debug.Log("OnImageCaptured"),
				"スクショテスト",
				(errorMessage) => Debug.Log("CoPostScreenshot OnComplete " + errorMessage)));
		}

		if (GUILayout.Button("メッセージ"))
		{
			StartCoroutine(_slack.CoPostMessage(
				"メッセージテスト",
				(errorMessage) => Debug.Log("CoPostMessage OnComplete " + errorMessage)));
		}

		if (GUILayout.Button("スニペット"))
		{
			StartCoroutine(_slack.CoPostSnippet(
				"スニペットテスト",
				(errorMessage) => Debug.Log("CoPostSnippet OnComplete " + errorMessage)));
		}

		if (GUILayout.Button("ログ投稿"))
		{
			StartCoroutine(_slack.CoPostBinary(
				_logHandler.GetBytes(),
				"binaryTest.txt",
				null,
				(errorMessage) => Debug.Log("CoPostBinary OnComplete " + errorMessage)));
		}

		if (GUILayout.Button("テクスチャ投稿"))
		{
			StartCoroutine(_slack.CoPostTexture(
				_testTex,
				null,
				null,
				(errorMessage) => Debug.Log("CoPostTexture OnComplete " + errorMessage)));
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

		if (GUILayout.Button("Assert"))
		{
			Debug.Assert(false, "Assertion failed.");
		}

		if (GUILayout.Button("Null例外"))
		{
			string a = null;
			int b = a.Length; // null死して例外吐く。これがログに溜まることを確認する
		}
	}

	void Update()
	{
		// 画面にログの最新部を表示。なお製品でこんなことをやると激遅いので注意。
		_logText.text = _logHandler.Tail(10);
	}
}
