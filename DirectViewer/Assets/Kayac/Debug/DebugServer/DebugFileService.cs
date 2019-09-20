using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Text;
using System.Xml;

namespace Kayac
{
    public class DebugFileService
    {
        public const string MapPath = "Kayac/DebugServer/StreamingAssetsMap.json";
        public delegate void ChangedCallback(string path);
        public event ChangedCallback OnChanged;
        public string pathPrefix { get; private set; }

        public DebugFileService(string pathPrefix)
        {
            coroutineRunner = new ManualCoroutineRunner();
            this.pathPrefix = pathPrefix;

            var ext = System.IO.Path.GetExtension(MapPath);
            var pathWitoutExt = MapPath.Remove(MapPath.Length - ext.Length, ext.Length);
            var mapAsset = Resources.Load<TextAsset>(pathWitoutExt); // TODO: 同期でいいのか?
            if (mapAsset != null)
            {
                var json = mapAsset.text;
                streamingAssetsMap = JsonUtility.FromJson<Map>(json);
            }
        }

        public void ProcessRequest(HttpListenerContext context)
        {
            UnityEngine.Profiling.Profiler.BeginSample("DebugFileService.ProcessRequest");
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
            UnityEngine.Profiling.Profiler.EndSample();
        }

        public void ManualUpdate()
        {
            coroutineRunner.MoveNext();
        }

        [System.Serializable]
        public class Directory
        {
            public Directory()
            {
                firstFile = firstChild = nextBrother = int.MinValue;
            }
            public string name;
            public int firstFile;
            public int firstChild;
            public int nextBrother;
        }

        [System.Serializable]
        public class File
        {
            public File()
            {
                nextFile = int.MinValue;
            }
            public string name;
            public int nextFile;
        }

        [System.Serializable]
        public class Map
        {
            public Map()
            {
                directories = new List<Directory>();
                files = new List<File>();
            }
            public List<Directory> directories;
            public List<File> files;
        }

        // non public -------------------
        ManualCoroutineRunner coroutineRunner;
        Map streamingAssetsMap;
        // TODO: この下のあたり、もっと綺麗な書き方ないの?
        const string fileGetScript = @"
var log = document.getElementById('log');
var downloadAnchor = document.getElementById('download');
var textArea = document.getElementById('text');

var onUpdate = function () {
	var request = new XMLHttpRequest();
	request.onload = function () {
		log.value = '編集受理\n';
	};
	request.onerror = function () {
		log.value = '編集失敗\n';
	};
	request.open('PUT', document.location.href, true);

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
var onDownload = function () {
	var filename = document.location.pathname.replace(/^.*\//, '');
	downloadAnchor.download = filename;
	var blob = new Blob([textArea.value], { 'type': 'text/plane' });
	downloadAnchor.href = window.URL.createObjectURL(blob);
};
var updateButton = document.getElementById('update');
if (updateButton !== null){
	updateButton.addEventListener('click', onUpdate, false);
}
document.getElementById('upload').addEventListener('click', onUpload, false);
document.getElementById('delete').addEventListener('click', onDelete, false);
downloadAnchor.addEventListener('click', onDownload, false);
		";

        void ProcessFilePut(HttpListenerContext context, string path)
        {
            var request = context.Request;
            Debug.Assert(request.HttpMethod == "PUT");
            // 内容があるが?delete=trueがあれば、それは削除
            var deleteValue = request.QueryString["delete"];
            if (deleteValue == "true")
            {
                Debug.Log("ProcessPut: Delete");
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
                var ext = System.IO.Path.GetExtension(path);
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
                    HtmlUtil.WriteTextarea(writer, "text", 20, 60, ret.Value);
                    HtmlUtil.WriteBr(writer);
                    HtmlUtil.WriteInput(writer, "update", "button", "submit");
                }

                HtmlUtil.WriteInput(writer, "delete", "button", "delete file");
                HtmlUtil.WriteBr(writer);

                HtmlUtil.WriteInput(writer, "file", "file");
                HtmlUtil.WriteInput(writer, "upload", "button", "upload file");
                HtmlUtil.WriteA(writer, "#", "download", "download", "");
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

            var parentDirectory = FindDirectory(path);
            if (parentDirectory == null)
            {
                writer.WriteElementString("p", "not found in map. use MakeStreamingAssetsMap tool in Editor.");
            }
            else
            {
                writer.WriteStartElement("ul");
                var directoryIndex = parentDirectory.firstChild;
                while (directoryIndex >= 0)
                {
                    var directory = streamingAssetsMap.directories[directoryIndex];
                    writer.WriteStartElement("li");
                    HtmlUtil.WriteA(writer, directory.name + "/", directory.name);
                    writer.WriteEndElement(); //li
                    directoryIndex = directory.nextBrother;
                }

                var fileIndex = parentDirectory.firstFile;
                while (fileIndex >= 0)
                {
                    var file = streamingAssetsMap.files[fileIndex];
                    writer.WriteStartElement("li");
                    HtmlUtil.WriteA(writer, file.name, file.name);
                    writer.WriteEndElement(); //li
                    fileIndex = file.nextFile;
                }
                writer.WriteEndElement(); //ul
            }
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

        // 速度とかガン無視ですよ今は
        Directory FindDirectory(string path)
        {
            if (streamingAssetsMap == null)
            {
                Debug.LogError("No StreamingAssetsMap.json!");
                return null;
            }
            else
            {
                return FindDirectory(streamingAssetsMap.directories[0], path); // 0が必ずルート
            }
        }

        Directory FindDirectory(Directory root, string path)
        {
            // スラッシュ除去
            while ((path.Length > 0) && (path[0] == '/'))
            {
                path = path.Remove(0, 1);
            }
            if (path.Length == 0) // 尽きたらこれ
            {
                return root;
            }
            // 次のスラッシュまでを切り出し
            int nextSlash = path.IndexOf('/');
            if (nextSlash < 0)
            {
                nextSlash = path.Length; // なければ末尾にあるものとする
            }
            var search = path.Substring(0, nextSlash);
            var directoryIndex = root.firstChild;
            while (directoryIndex >= 0)
            {
                var directory = streamingAssetsMap.directories[directoryIndex];
                if (directory.name == search)
                {
                    path = path.Remove(0, search.Length);
                    return FindDirectory(directory, path);
                }
                directoryIndex = directory.nextBrother;
            }
            return null; // 見つからず
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

            public static void WriteA(
                XmlWriter writer,
                string hrefAttribute,
                string innerText,
                string idAttribute = null,
                string downloadAttribute = null)
            {
                writer.WriteStartElement("a");
                writer.WriteAttributeString("href", hrefAttribute);
                if (idAttribute != null)
                {
                    writer.WriteAttributeString("id", idAttribute);
                }
                if (downloadAttribute != null)
                {
                    writer.WriteAttributeString("download", downloadAttribute);
                }
                writer.WriteString(innerText);
                writer.WriteEndElement();
            }

            public static void WriteTextarea(
                XmlWriter writer,
                string id,
                int rows = 0,
                int cols = 0,
                string value = "")
            {
                writer.WriteStartElement("textarea");
                writer.WriteAttributeString("id", id);
                if (rows > 0)
                {
                    writer.WriteAttributeString("rows", rows.ToString());
                }
                if (cols > 0)
                {
                    writer.WriteAttributeString("cols", cols.ToString());
                }
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