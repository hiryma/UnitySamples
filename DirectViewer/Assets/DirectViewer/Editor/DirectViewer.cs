using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.IO;

namespace Kayac
{

    public class DirectViewer : EditorWindow
    {
        string ip;
        int port = 8080;
        UnityWebRequest request;
        UnityWebRequest pingRequest;
        long uploadFileSize;
        ulong uploadedBytes;
        static DirectViewer instance;
        const string outputPath = "Assets/DirectViewer/Temp";
        System.DateTime lastPingTime;
        const float pingInterval = 10f;
        bool pingSucceeded;
        string message;
        string error;
        const string ipKey = "Kayac/DirectViewer/ip";
        const string portKey = "Kayac/DirectViewer/port";
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
                instance = (DirectViewer)GetWindow(typeof(DirectViewer));
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
            ip = EditorGUILayout.TextField("実機のIPアドレス", ip);
            port = EditorGUILayout.IntField("実機のポート番号", port);
            GUILayout.Label("状態: " + message);
            GUILayout.Label("エラー: " + error);
            if (state == State.Connecting)
            {
                // やることない
                message = "実機に接続試行中";
            }
            else if (state == State.Retrying)
            {
                if (GUILayout.Button("接続再試行"))
                {
                    lastPingTime -= System.TimeSpan.FromSeconds(pingInterval);
                }
                message = "実機とつながってない";
            }
            else if (state == State.Ready)
            {
                message = "実機に送信できます";
                if (GUILayout.Button("選択してるものを送る"))
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
                message = "転送中: " + uploadedBytes + " / " + uploadFileSize;
            }
            if (GUILayout.Button("一時データ削除"))
            {
                AssetDatabase.DeleteAsset("Assets/DirectViewer/Temp");
            }
            if (!EditorApplication.isPlaying)
            {
                if (GUILayout.Button("[デバグ用]選択してるものをAB化"))
                {
                    BuildAssetBundle();
                }
            }
        }

        void BuildAssetBundle()
        {
            var root = Selection.activeObject;
            var rootPath = AssetDatabase.GetAssetPath(root);
            EditorUtility.DisplayProgressBar("DirectViewer", "送る物のリストを作ってます", 0f);
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
                    error = "フォルダにアセットが入ってないです!";
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

            if (!Directory.Exists("Assets/DirectViewer/Temp"))
            {
                AssetDatabase.CreateFolder("Assets/DirectViewer", "Temp");
            }
            EditorUtility.DisplayProgressBar("DirectViewer", "荷造り中", 33f);
            try
            {
                var manifest = BuildPipeline.BuildAssetBundles(
                    outputPath,
                    builds,
                    BuildAssetBundleOptions.ChunkBasedCompression,
                    EditorUserBuildSettings.activeBuildTarget);

                if (manifest == null)
                {
                    error = "荷造りに失敗!たぶんバグだからプログラマに伝えてね!";
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
                error = "送信失敗しました";
                return;
            }
            EditorUtility.ClearProgressBar();
            error = "正常";
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
                error = "フォルダが選択されていないか、フォルダが空です。";
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
            error = "正常";
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