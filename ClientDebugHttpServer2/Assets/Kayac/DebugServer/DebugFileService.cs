using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace Kayac
{
	public class DebugFileService
	{
		public delegate void ChangedCallback(string path);
		public event ChangedCallback OnChanged;
		public string pathPrefix { get; private set; }

		public DebugFileService(string pathPrefix)
		{
			coroutineRunner = new ManualCoroutineRunner();
			this.pathPrefix = pathPrefix;
		}

		public void ProcessRequest(HttpListenerContext context)
		{
			var response = context.Response;
			// index.html処理
			var path = DebugServerUtil.RemoveQueryString(context.Request.RawUrl);
			// prefixチェック
			if (path.StartsWith(pathPrefix))
			{
				path = path.Remove(0, pathPrefix.Length);
				// 空になったか、末尾が/か、その名前のディレクトリが存在すれば
				if ((path == "") || (path[path.Length - 1] == '/') || DebugServerUtil.DirectoryExists(path))
				{
					ProcessDirectory(context, path);
				}
				else // そうでなければファイル
				{
					var request = context.Request;
					if (request.HttpMethod == "PUT") // Putなら即時処理
					{
						ProcessFilePut(context, path);
					}
					else if (request.HttpMethod == "GET") // GETはロードが必要なのでコルーチンで処理
					{
						coroutineRunner.Start(CoProcessFileGet(context, path));
					}
					else
					{
						response.StatusCode = (int)HttpStatusCode.InternalServerError;
						response.Close();
					}
				}
			}
			else
			{
				Debug.Assert(false, "url does not contain prefix: " + pathPrefix);
				response.StatusCode = (int)HttpStatusCode.InternalServerError;
				response.Close();
			}
		}

		public void ManualUpdate()
		{
			coroutineRunner.MoveNext();
		}

		// non public -------------------
		ManualCoroutineRunner coroutineRunner;
		// TODO: この下のあたり、もっと綺麗な書き方ないの?
		const string fileGetScript = @"
var log = document.getElementById('log');
var onUpdate = function () {
	var request = new XMLHttpRequest();
	request.onload = function () {
		log.value = '編集受理\n';
	};
	request.onerror = function () {
		log.value = '編集失敗\n';
	};
	request.open('PUT', document.location.href, true);

	var textArea = document.getElementById('text');
	request.send(textArea.value);
};
var onLoad = function (arrayBuffer) {
	var request = new XMLHttpRequest();
	request.onload = function () {
		log.value = 'アップロード受理\n';
	};
	request.onerror = function () {
		log.value = 'アップロード失敗\n';
	};
	request.open('PUT', document.location.href, true);
	request.send(new Int8Array(arrayBuffer));
};
var onUpload = function () {
	var files = document.getElementById('file').files;
	if (files.length == 0) {
		return;
	}
	var reader = new FileReader();
	reader.onload = function (e) {
		onLoad(e.target.result);
	};
	reader.readAsArrayBuffer(files[0]);
};
var onDelete = function () {
	var request = new XMLHttpRequest();
	request.onload = function () {
		log.value = '削除受理\n';
	};
	request.onerror = function () {
		log.value = '削除失敗\n';
	};
	request.open('PUT', document.location.href + '?delete=true', true);
	request.send();
};
var updateButton = document.getElementById('update');
if (updateButton !== null){
	updateButton.addEventListener('click', onUpdate, false);
}
document.getElementById('upload').addEventListener('click', onUpload, false);
document.getElementById('delete').addEventListener('click', onDelete, false);
		";

		void ProcessFilePut(HttpListenerContext context, string path)
		{
			var request = context.Request;
			Debug.Assert(request.HttpMethod == "PUT");
			// 内容があるが?delete=trueがあれば、それは削除
			var deleteValue = request.QueryString["delete"];
			if (deleteValue == "true")
			{
				DebugServerUtil.DeleteOverride(path);
			}
			else
			{
				var bodyData = request.InputStream;
				DebugServerUtil.SaveOverride(path, bodyData); // TODO: 非同期化
				bodyData.Close();
			}
			var response = context.Response;
			response.Close();
			OnChanged(path);
		}

		IEnumerator CoProcessFileGet(HttpListenerContext context, string path)
		{
			var request = context.Request;
			Debug.Assert(request.HttpMethod == "GET");
			var ret = new CoroutineReturnValue<string>();
			yield return DebugServerUtil.CoLoad(ret, path, overrideEnabled: true);
			var response = context.Response;
			if (ret.Exception != null)
			{
				response.StatusCode = (int)HttpStatusCode.InternalServerError;
				response.Close();
			}
			else
			{
				var ext = Path.GetExtension(path);
				var isText = (ext == ".txt")
					|| (ext == ".json")
					|| (ext == ".html")
					|| (ext == ".htm")
					|| (ext == ".xml")
					|| (ext == ".yaml")
					|| (ext == ".csv");
				var sb = new StringBuilder();
				var writer = HtmlUtil.CreateWriter(sb);
				var title = path;
				HtmlUtil.WriteHeader(writer, title);
				writer.WriteStartElement("body");
				writer.WriteElementString("h1", title);
				if (isText)
				{
					HtmlUtil.WriteTextarea(writer, "text", 30,80, ret.Value);
					HtmlUtil.WriteBr(writer);
					HtmlUtil.WriteInput(writer, "update", "button", "submit");
				}

				HtmlUtil.WriteInput(writer, "delete", "button", "delete file");
				HtmlUtil.WriteBr(writer);

				HtmlUtil.WriteInput(writer, "file", "file");
				HtmlUtil.WriteInput(writer, "upload", "button", "upload file");
				HtmlUtil.WriteBr(writer);

				HtmlUtil.WriteOutput(writer, "log");

				writer.WriteStartElement("script");
				writer.WriteString(fileGetScript);
				writer.WriteEndElement(); //script
				writer.WriteEndElement(); //body
				writer.WriteEndElement(); //html
				writer.Close();

				var html = sb.ToString();
				var bytes = Encoding.UTF8.GetBytes(html);
				response.Close(bytes, willBlock: false);
			}
		}

		void ProcessDirectory(HttpListenerContext context, string path)
		{
			var response = context.Response;
			bool redirected = false;
			if (path.Length > 0)
			{
				if (path[path.Length - 1] == '/') // 末尾スラッシュ消し
				{
					path = path.Remove(path.Length - 1, 1);
				}
				else
				{
					redirected = true; // スラッシュ付きにリダイレクトする
				}
			}
			var sb = new StringBuilder();
			var writer = HtmlUtil.CreateWriter(sb);
			var title = "index of /" + path;
			HtmlUtil.WriteHeader(writer, title);
			writer.WriteStartElement("body");
			writer.WriteElementString("h1", title);
			writer.WriteStartElement("ul");

			var directories = DebugServerUtil.EnumerateDirectories(path);
			foreach (var directory in directories)
			{
				writer.WriteStartElement("li");
				HtmlUtil.WriteA(writer, directory + "/", directory);
				writer.WriteEndElement(); //li
			}
			var files = DebugServerUtil.EnumerateFiles(path);
			foreach (var file in files)
			{
				writer.WriteStartElement("li");
				HtmlUtil.WriteA(writer, file, file);
				writer.WriteEndElement(); //li
			}
			writer.WriteEndElement(); //ul
			writer.WriteEndElement(); //body
			writer.WriteEndElement(); //html
			writer.Close();

			var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
			if (redirected)
			{
				var url = DebugServerUtil.RemoveQueryString(context.Request.Url.ToString());
				url += "/";
				response.Redirect(url);
			}
			else
			{
				response.StatusCode = (int)HttpStatusCode.OK;
			}
			response.Close(bytes, willBlock: false);
		}

		static class HtmlUtil
		{
			public static XmlWriter CreateWriter(StringBuilder sb)
			{
				var settings = new XmlWriterSettings();
				settings.Indent = true;
				settings.IndentChars = "\t";
				settings.NewLineChars = "\n";
				settings.OmitXmlDeclaration = true;
				var writer = XmlWriter.Create(sb, settings);
				return writer;
			}

			public static void WriteHeader(XmlWriter writer, string title)
			{
				writer.WriteDocType("html", null, null, null);
				writer.WriteStartElement("html");
				writer.WriteStartElement("head");
				writer.WriteStartElement("meta");
				writer.WriteAttributeString("charset", "UTF-8");
				writer.WriteEndElement();
				writer.WriteElementString("title", title);
				writer.WriteEndElement(); // head
			}

			public static void WriteA(XmlWriter writer, string hrefAttribute, string innerText)
			{
				writer.WriteStartElement("a");
				writer.WriteAttributeString("href", hrefAttribute);
				writer.WriteString(innerText);
				writer.WriteEndElement();
			}

			public static void WriteTextarea(
				XmlWriter writer,
				string id,
				int rows,
				int cols,
				string value = "")
			{
				writer.WriteStartElement("textarea");
				writer.WriteAttributeString("id", id);
				writer.WriteAttributeString("rows", rows.ToString());
				writer.WriteAttributeString("cols", cols.ToString());
				writer.WriteString(value);
				writer.WriteEndElement(); // textarea
			}

			public static void WriteBr(XmlWriter writer)
			{
				writer.WriteStartElement("br");
				writer.WriteEndElement();
			}

			public static void WriteInput(
				XmlWriter writer,
				string id,
				string type,
				string value = "")
			{
				writer.WriteStartElement("input");
				writer.WriteAttributeString("id", id);
				writer.WriteAttributeString("type", type);
				writer.WriteAttributeString("value", value);
				writer.WriteEndElement();
			}

			public static void WriteOutput(
				XmlWriter writer,
				string id,
				string value = "")
			{
				writer.WriteStartElement("output");
				writer.WriteAttributeString("id", id);
				writer.WriteAttributeString("value", value);
				writer.WriteEndElement();
			}
		}
	}
}