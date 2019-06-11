using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace Kayac
{
	public class DebugServer
	{
		public delegate void OnRequest(
			out string outputHtml,
			NameValueCollection queryString,
			Stream bodyDataStream);

		public void RegisterRequestCallback(string path, OnRequest onRequest)
		{
			callbacks.Add(path, onRequest);
		}

		public DebugServer(
			int port,
			string fileServicePathPrefix,
			DebugFileService.ChangedCallback onFileChanged = null)
		{
			if (!HttpListener.IsSupported)
			{
				return;
			}
			callbacks = new Dictionary<string, OnRequest>();
			requestContexts = new Queue<HttpListenerContext>();
#if UNITY_EDITOR || !UNITY_WEBGL // エディタか、WebGLでない時だけサーバ起動
			listener = new HttpListener();
			listener.Prefixes.Add("http://*:" + port + "/");
			listener.Start();
			listener.BeginGetContext(OnRequestArrival, this);
#endif
			fileService = new DebugFileService(fileServicePathPrefix);
			fileService.OnChanged += onFileChanged;
		}

		public void Dispose()
		{
			if (listener != null)
			{
				listener.Stop();
				listener = null;
			}
		}

		public void ManualUpdate()
		{
			fileService.ManualUpdate();
			// ファイル処理中なら止める
			while (requestContexts.Count > 0)
			{
				ProcessRequestContext(requestContexts.Dequeue());
			}
		}

		// non public -------------------
		HttpListener listener;
		Dictionary<string, OnRequest> callbacks;
		Queue<HttpListenerContext> requestContexts;
		DebugFileService fileService;

		void OnRequestArrival(IAsyncResult asyncResult)
		{
			var context = listener.EndGetContext(asyncResult);
			listener.BeginGetContext(OnRequestArrival, this); // 次の受け取り
			lock (requestContexts) // コールバックをメインスレッド実行するためにキューに溜める
			{
				requestContexts.Enqueue(context);
			}
		}

		void ProcessRequestContext(HttpListenerContext context)
		{
			var request = context.Request;
			var path = DebugServerUtil.RemoveQueryString(request.RawUrl); // QueryStringいらない
			if (path.StartsWith(fileService.pathPrefix))
			{
				fileService.ProcessRequest(context);
			}
			else
			{
				OnRequest callback = null;
				if (callbacks.TryGetValue(path, out callback))
				{
					Process(context, callback);
				}
				else
				{
					var response = context.Response;
					response.StatusCode = 404;
					response.Close();
				}
			}
		}

		void Process(HttpListenerContext context, OnRequest callback)
		{
			var request = context.Request;
			var response = context.Response;
			string outputHtml = null;
			try // ユーザ処理でコケたら500返す。それ以外は200にしちゃってるがマズければ番号ももらうようにした方がいいのかも。
			{
				var bodyData = request.InputStream;
				callback(out outputHtml, request.QueryString, bodyData);
				bodyData.Close();
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
			catch (System.Exception e)
			{
				Debug.LogException(e);
				response.StatusCode = (int)HttpStatusCode.InternalServerError;
				response.Close();
			}
		}
	}
}
