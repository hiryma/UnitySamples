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
        static DirectViewer instance;
        const string outputPath = "Assets/DirectViewerTemp";

        [MenuItem("Kayac/DirectViewer")]
        static void CreateWindow()
        {
            if (instance == null)
            {
                instance = (DirectViewer)GetWindow(typeof(DirectViewer));
            }
            instance.Show();
        }

        void OnGUI()
        {
            if (request == null)
            {
                ip = EditorGUILayout.TextField("スマホのIP", ip);
                port = EditorGUILayout.IntField("スマホのポート番号", port);
                if (GUILayout.Button("選択してるものを送る"))
                {
                    if (EditorApplication.isPlaying) //プレイ中はビルドできないので、あるものを送る。あれば、だが。
                    {
                        SendExisting();
                    }
                    else
                    {
                        BuildAssetBundle();
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("転送中");
            }
        }

        void BuildAssetBundle()
        {
            var root = Selection.activeObject;
            var rootPath = AssetDatabase.GetAssetPath(root);
            if (!AssetDatabase.IsValidFolder(rootPath))
            {
                Debug.LogError("フォルダを選択した状態で押してね! " + rootPath + " はフォルダじゃないよ!");
                return;
            }
            EditorUtility.DisplayProgressBar("DirectViewer", "送る物のリストを作ってます", 0f);
            // 再帰的に掘ってアセットを列挙する
            var assetPaths = new List<string>();
            CollectAssetPaths(assetPaths, rootPath);
            if (assetPaths.Count <= 0)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogWarning("選択されたフォルダ以下に何もないから何もしないよ!");
                return;
            }

            // アセットが存在するかを確認する。
            var validAssetPaths = new List<string>();
            foreach (var path in assetPaths)
            {
                var guid = AssetDatabase.AssetPathToGUID(path);
                if (guid != null)
                {
                    validAssetPaths.Add(path);
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

            if (!Directory.Exists("Assets/DirectViewerTemp"))
            {
                AssetDatabase.CreateFolder("Assets", "DirectViewerTemp");
            }
            EditorUtility.DisplayProgressBar("DirectViewer", "荷造り中", 33f);
            try
            {
                var manifest = BuildPipeline.BuildAssetBundles(
                    outputPath,
                    builds,
                    BuildAssetBundleOptions.ChunkBasedCompression,
                    EditorUserBuildSettings.activeBuildTarget);

                Debug.Assert(manifest != null);
                // 確認
                if (manifest != null)
                {
                    var abs = manifest.GetAllAssetBundles();
                    foreach (var ab in abs)
                    {
                        Debug.Log("AssetBundle: " + ab);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                EditorUtility.ClearProgressBar();
                return;
            }
            EditorUtility.DisplayProgressBar("DirectViewer", "転送中", 67f);
            //TODO: 転送
            Send(outputPath + "/" + build.assetBundleName);
            EditorUtility.ClearProgressBar();
        }

        void SendExisting()
        {
            var root = Selection.activeObject;
            var rootPath = AssetDatabase.GetAssetPath(root);
            var assetBundleName = Path.GetFileName(rootPath) + ".unity3d";
            Send(outputPath + "/" + assetBundleName);
        }

        void Send(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError("選択されてるファイルに対応するAssetBundleがない");
                return;
            }
            var url = string.Format("http://{0}:{1}/upload/{2}.unity3d", ip, port, Path.GetFileName(path));
            Debug.Log("Send: " + url);
            request = new UnityWebRequest(url);
            request.method = "PUT";
            request.uploadHandler = new UploadHandlerFile(path);
            request.SendWebRequest();
        }

        void Update()
        {
            if (request != null)
            {
                if (request.isDone)
                {
                    if (request.error != null)
                    {
                        Debug.LogError(request.error);
                    }
                    request.Dispose();
                    request = null;
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