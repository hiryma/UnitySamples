using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.IO;

namespace Kayac
{

    public class DirectViewerEditor : EditorWindow
    {
        const string outputPath = "Assets/DirectViewer/Temp";
        const string ipKey = "Kayac/DirectViewer/ip";
        const string portKey = "Kayac/DirectViewer/port";
        const float pingInterval = 10f;

        string ip;
        int port = 8080;
        UnityWebRequest request;
        UnityWebRequest pingRequest;
        long uploadFileSize;
        ulong uploadedBytes;
        static DirectViewerEditor instance;
        System.DateTime lastPingTime;
        bool pingSucceeded;
        string message;
        string error;
        enum State
        {
            Connecting,
            Retrying,
            Ready,
            Sending,
        }
        State state;

        [MenuItem("Kayac/DirectViewer")]
        static void CreateWindow()
        {
            if (instance == null)
            {
                instance = (DirectViewerEditor)GetWindow(typeof(DirectViewerEditor));
            }
            instance.Show();
        }

        void Awake()
        {
            state = State.Retrying;
            lastPingTime = System.DateTime.Now - System.TimeSpan.FromSeconds(pingInterval);
            ip = EditorPrefs.GetString(ipKey);
            port = EditorPrefs.GetInt(portKey);
        }

        void OnGUI()
        {
            ip = EditorGUILayout.TextField("Device IP Address", ip);
            port = EditorGUILayout.IntField("Device Port", port);
            GUILayout.Label("state: " + message);
            GUILayout.Label("error: " + error);
            if (state == State.Connecting)
            {
                // やることない
                message = "connecting...";
            }
            else if (state == State.Retrying)
            {
                if (GUILayout.Button("Connect Now"))
                {
                    lastPingTime -= System.TimeSpan.FromSeconds(pingInterval);
                }
                message = "disconnected.";
            }
            else if (state == State.Ready)
            {
                message = "ready.";
                if (GUILayout.Button("Send Selected"))
                {
                    if (!EditorApplication.isPlaying) //プレイ中はビルドできないので、あるものを送る。あれば、だが。
                    {
                        BuildAssetBundle();
                    }
                    Send();
                }
                if (GUILayout.Button("DebugUI On/Off"))
                {
                    var url = string.Format("http://{0}:{1}/toggle-debug-ui", ip, port);
                    request = UnityWebRequest.Get(url);
                    request.SendWebRequest();
                }
            }
            else if (state == State.Sending)
            {
                message = "sending: " + uploadedBytes + " / " + uploadFileSize;
            }
            if (GUILayout.Button("Delete Temporary"))
            {
                AssetDatabase.DeleteAsset(outputPath);
            }
            if (!EditorApplication.isPlaying)
            {
                if (GUILayout.Button("[DEBUG]build AB from selected."))
                {
                    BuildAssetBundle();
                }
            }
        }

        void BuildAssetBundle()
        {
            var root = Selection.activeObject;
            var rootPath = AssetDatabase.GetAssetPath(root);
            EditorUtility.DisplayProgressBar("DirectViewer", "making transfer list", 0f);
            var validAssetPaths = new List<string>();
            if (AssetDatabase.IsValidFolder(rootPath))
            {
                var assetPaths = new List<string>();
                // 再帰的に掘ってアセットを列挙する なんでAssetDatabaseの関数にそれがないの?
                CollectAssetPaths(assetPaths, rootPath);
                // アセットが存在するかを確認する。
                foreach (var path in assetPaths)
                {
                    // スクリプトはスキップ
                    if (Path.GetExtension(path).ToLower() == ".cs")
                    {
                        continue;
                    }
                    var guid = AssetDatabase.AssetPathToGUID(path);
                    if (guid != null)
                    {
                        validAssetPaths.Add(path);
                    }
                }
                if (validAssetPaths.Count <= 0)
                {
                    EditorUtility.ClearProgressBar();
                    error = "no asset in folder!";
                    return;
                }
            }
            else // フォルダじゃなければそれだけ送る
            {
                var guid = AssetDatabase.AssetPathToGUID(rootPath);
                if (guid != null)
                {
                    validAssetPaths.Add(rootPath);
                }
            }

            var build = new AssetBundleBuild()
            {
                assetBundleName = Path.GetFileName(rootPath) + ".unity3d",
                assetNames = new string[validAssetPaths.Count]
            };
            for (int i = 0; i < validAssetPaths.Count; i++)
            {
                build.assetNames[i] = validAssetPaths[i];
            }
            var builds = new AssetBundleBuild[1];
            builds[0] = build;

            if (!Directory.Exists(outputPath))
            {
                AssetDatabase.CreateFolder(
                    Path.GetDirectoryName(outputPath),
                    Path.GetFileName(outputPath));
            }
            EditorUtility.DisplayProgressBar("DirectViewer", "packaging...", 33f);
            try
            {
                var manifest = BuildPipeline.BuildAssetBundles(
                    outputPath,
                    builds,
                    BuildAssetBundleOptions.ChunkBasedCompression,
                    EditorUserBuildSettings.activeBuildTarget);

                if (manifest == null)
                {
                    error = "packaging failure! IT MUST BE BUG.";
                }
                // 確認
#if false
                if (manifest != null)
                {
                    var abs = manifest.GetAllAssetBundles();
                    foreach (var ab in abs)
                    {
                        UnityEngine.Debug.Log("AssetBundle: " + ab);
                    }
                }
#endif
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogException(e);
                EditorUtility.ClearProgressBar();
                error = "send failed.";
                return;
            }
            EditorUtility.ClearProgressBar();
            error = "";
        }

        void Send()
        {
            var root = Selection.activeObject;
            var rootPath = AssetDatabase.GetAssetPath(root);
            var assetBundleName = Path.GetFileName(rootPath) + ".unity3d";
            var path = outputPath + "/" + assetBundleName;
            var info = new FileInfo(path);
            if (!info.Exists)
            {
                error = "no asset in selection.";
                return;
            }
            uploadFileSize = info.Length;
            var url = string.Format("http://{0}:{1}/upload/{2}.unity3d", ip, port, Path.GetFileName(path));
            UnityEngine.Debug.Log("Send: " + url);
            request = new UnityWebRequest(url);
            request.method = "PUT";
            request.uploadHandler = new UploadHandlerFile(path);
            request.SendWebRequest();
            uploadedBytes = 0;
            error = "";
            state = State.Sending;
        }

        void Update()
        {
            if (request != null)
            {
                if (request.isDone)
                {
                    if (request.error != null)
                    {
                        UnityEngine.Debug.LogError(request.error);
                    }
                    request.Dispose();
                    request = null;
                    Repaint();
                    state = State.Ready;
                }
                else
                {
                    if (uploadedBytes != request.uploadedBytes)
                    {
                        Repaint();
                        uploadedBytes = request.uploadedBytes;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(ip) && (port != 0)) // 送信中でなくIPとポート設定があればping
            {
                var now = System.DateTime.Now;
                if ((pingRequest == null) && ((now - lastPingTime).TotalSeconds >= pingInterval))
                {
                    var url = string.Format("http://{0}:{1}/ping", ip, port);
                    pingRequest = UnityWebRequest.Get(url);
                    pingRequest.timeout = 5;
                    pingRequest.SendWebRequest();
                    Repaint();
                    state = State.Connecting;
                }
                else if ((pingRequest != null) && pingRequest.isDone)
                {
                    pingSucceeded = (pingRequest.error == null);
                    pingRequest = null;
                    lastPingTime = now;
                    Repaint();
                    if (pingSucceeded)
                    {
                        state = State.Ready;
                        // 最後にpingが通った設定を保存しておく
                        EditorPrefs.SetString(ipKey, ip);
                        EditorPrefs.SetInt(portKey, port);
                    }
                    else
                    {
                        state = State.Retrying;
                    }
                }
            }
            
        }

        void CollectAssetPaths(List<string> output, string folder)
        {
            var files = Directory.GetFiles(folder);
            foreach (var item in files)
            {
                output.Add(item);
            }
            var subFolders = Directory.GetDirectories(folder);
            foreach (var item in subFolders)
            {
                CollectAssetPaths(output, item);
            }
        }
    }

}