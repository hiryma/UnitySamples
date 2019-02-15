#define USE_CALLBACK
//#define USE_COROUTINE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
	[SerializeField]
	Transform _textRoot;
	[SerializeField]
	UnityEngine.UI.Text _textPrefab;
	[SerializeField]
	UnityEngine.UI.Text _dump;

	UnityEngine.UI.Text[] _texts;
	Kayac.Loader _loader;
	const int HandleCount = 20;
	Kayac.LoadHandle[] _handles;
	System.IO.StreamWriter _log;

	void Start()
	{
		var abRoot = "file:///" + Application.streamingAssetsPath + "/";
		_handles = new Kayac.LoadHandle[HandleCount];
		_loader = new Kayac.Loader(abRoot);
		// ログファイルへ
		_log = new System.IO.StreamWriter("log.txt");
		_texts = new UnityEngine.UI.Text[HandleCount];
		for (int i = 0; i < HandleCount; i++)
		{
			_texts[i] = Instantiate(_textPrefab, _textRoot, false);
		}
		Application.logMessageReceivedThreaded += HandleLog;
   	}

	void HandleLog(string logString, string stackTrace, LogType type)
	{
		lock (_log)
		{
			_log.WriteLine(logString);
			_log.WriteLine(stackTrace);
		}
	}

	void Update()
	{
		if (UnityEngine.Random.Range(0, 100) < 10) // 1/100でまっさらに
//		if (Input.anyKeyDown)
		{
			for (int i = 0; i < _handles.Length; i++)
			{
				_handles[i] = null;
			}
		}

		_loader.Update();
		for (int i = 0; i < HandleCount; i++)
		{
			Update(i);
		}
	}

	void OnLoadComplete(UnityEngine.Object asset, int index)
	{
		var textAsset = asset as TextAsset;
		if (textAsset != null)
		{
			_texts[index].text = textAsset.text;
		}
	}

	void Update(int i)
	{
		if (_handles[i] == null)
		{
			int ab = UnityEngine.Random.Range(0, 10);
			int file = UnityEngine.Random.Range(0, 10);
			var path = string.Format("{0}/{1}.txt", ab, file);
			_texts[i].text = path + " loading...";

#if USE_CALLBACK
			_handles[i] = _loader.Load(path, asset =>
			{
				if (asset != null)
				{
					OnLoadComplete(asset, i);
				}
			});
#elif USE_COROUTINE
			StartCoroutine(CoLoad(i, path));
#else
			_handles[i] = _loader.Load(path);
#endif
		}

#if !USE_CALLBACK && !USE_COROUTINE
		if ((_handles[i] != null) && _handles[i].succeeded)
		{
			OnLoadComplete(_handles[i], i);
		}
#endif
		_dump.text = _loader.Dump();
	}

	IEnumerator CoLoad(int i, string path)
	{
		_handles[i] = _loader.Load(path);
		if (!_handles[i].isDone)
		{
			yield return _handles[i];
		}
		if (_handles[i].succeeded)
		{
			OnLoadComplete(_handles[i].asset, i);
		}
	}
}
