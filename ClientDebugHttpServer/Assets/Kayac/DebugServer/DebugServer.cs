using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.NetworkInformation;

public class DebugServer
{
	public delegate void OnRequest(out string outputHtml, string inputText);

	HttpListener listener;
	Dictionary<string, OnRequest> callbacks;
	Queue<HttpListenerContext> requestContexts;

	public void RegisterRequestCallback(string path, OnRequest onRequest)
	{
		callbacks.Add(path, onRequest);
	}

	public static string GetLanIpAddress()
	{
		string ret = null;
		foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
		{
			if ((ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211) || (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
			{
				foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
				{
					if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
					{
						ret = ip.Address.ToString();
					}
				}
			}
		}
		return ret;
	}

	public DebugServer(int port)
	{
		if (!HttpListener.IsSupported)
		{
			return;
		}
		listener = new HttpListener();
		listener.Prefixes.Add("http://*:" + port + "/");
		callbacks = new Dictionary<string, OnRequest>();
		requestContexts = new Queue<HttpListenerContext>();
		listener.Start();
		listener.BeginGetContext(OnRequestArrival, this);
	}

	public void Dispose()
	{
		if (listener != null)
		{
			listener.Stop();
			listener = null;
		}
	}

	// 別のスレッドから呼ばれ得るのでキューに溜めて、ManualUpdateからコールバックを呼ぶ
	// TODO: 別スレでやっていいい処理であればそのまま実行する、という選択肢はあっていい。その方が性能は良い。
	void OnRequestArrival(IAsyncResult asyncResult)
	{
		var context = listener.EndGetContext(asyncResult);
		listener.BeginGetContext(OnRequestArrival, this); // 次の受け取り
		lock (requestContexts)
		{
			requestContexts.Enqueue(context);
		}
	}

	public void ManualUpdate()
	{
		while (requestContexts.Count > 0)
		{
			ProcessRequestContext(requestContexts.Dequeue());
		}
	}

	void ProcessRequestContext(HttpListenerContext context)
	{
		var request = context.Request;
		var response = context.Response;

		OnRequest callback = null;
		if (!callbacks.TryGetValue(request.RawUrl, out callback))
		{
			response.StatusCode = 404;
			response.Close();
		}
		else
		{
			string outputHtml = null;
			var input = request.InputStream;
			string inputText = null;
			if (input != null)
			{
				var encoding = request.ContentEncoding;
				var reader = new System.IO.StreamReader(input, encoding);
				inputText = reader.ReadToEnd();
			}

			try // ユーザ処理でコケたら500返す。それ以外は200にしちゃってるがマズければ番号ももらうようにした方がいいのかも。
			{
				callback(out outputHtml, inputText);
				response.StatusCode = 200;
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
				response.StatusCode = 500;
			}
			if (outputHtml != null)
			{
				var outputData = System.Text.Encoding.UTF8.GetBytes(outputHtml);
				response.Close(outputData, willBlock: false);
			}
			else
			{
				response.Close();
			}
		}
	}
}
