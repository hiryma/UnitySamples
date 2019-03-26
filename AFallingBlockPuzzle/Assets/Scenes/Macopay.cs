using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Macopay
{
	string _apiKey;
	string _serverRoot;
	int _currency; // 所持金

	public int currency{ get{ return _currency; } }

	public Macopay(string apiKey, string serverRoot)
	{
		_apiKey = apiKey;
		_serverRoot = serverRoot;
	}

	public IEnumerator CoAcount(System.Action onComplete)
	{
#if UNITY_WEBGL // 他のサイト叩けないのでWebGLでは即座に終わらせる
		yield return null;
#else
		var url = _serverRoot + "api/external/account?api_key=" + _apiKey;
		var req = new UnityWebRequest();
		req.downloadHandler = new DownloadHandlerBuffer();
		req.url = url;
		req.SetRequestHeader("Content-Type", "application/json; charset=UTF-8");
		req.method = UnityWebRequest.kHttpVerbGET;
		req.SendWebRequest();
		while (!req.isDone)
		{
			yield return null;
		}

		if (req.error != null)
		{
			Debug.LogError(req.error + " : " + req.url);
		}
		else
		{
			var json = req.downloadHandler.text;
			var response = JsonUtility.FromJson<AcountResponse>(json);
			_currency = response.currency;
			Debug.Log(_currency);
		}
		req.Dispose();
#endif
		if (onComplete != null)
		{
			onComplete();
		}
	}

	public IEnumerator CoWalletCurrency(int chargeAmount, System.Action onComplete)
	{
#if UNITY_WEBGL // 他のサイト叩けないのでWebGLでは即座に終わらせる
		yield return null;
#else
		var form = new WWWForm();
		form.AddField("api_key", _apiKey);
		form.AddField("before", _currency);
		form.AddField("after", _currency + chargeAmount);
		Debug.Log(System.Text.Encoding.UTF8.GetString(form.data));

		var url = _serverRoot + "api/external/wallet/currency";

		var req = new UnityWebRequest();
		req.uploadHandler = new UploadHandlerRaw(form.data);
		req.downloadHandler = new DownloadHandlerBuffer();
		req.url = url;
		req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
		req.method = UnityWebRequest.kHttpVerbPUT;
		req.SendWebRequest();

		while (!req.isDone)
		{
			yield return null;
		}

		if (req.error != null)
		{
			Debug.LogError(req.error + " : " + req.url);
		}
		else
		{
			var json = req.downloadHandler.text;
			var response = JsonUtility.FromJson<WalletCurrencyResponse>(json);
			if (response.status == "ok")
			{
				_currency = _currency + chargeAmount;
			}
			Debug.Log(_currency);
		}
		req.Dispose();
#endif
		if (onComplete != null)
		{
			onComplete();
		}
	}

	[System.Serializable]
	class WalletCurrencyResponse
	{
		public string status;
	}
	[System.Serializable]
	class AcountResponse
	{
		public int currency;
	}
}

