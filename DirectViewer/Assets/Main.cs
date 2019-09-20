using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using Kayac;

public class Main : MonoBehaviour
{
    [SerializeField] TextAsset indexHtml;
    AssetBundle assetBundle;
    GameObject[] prefabs;
    int prefabIndex = 0;
    GameObject instance;
    Coroutine loadCoroutine;
    DebugServer debugServer;
    string ipAddress;
    

    void Start()
    {
        debugServer = new DebugServer(8080, "/upload/", OnDebugFileServiceChanged);
        debugServer.RegisterRequestCallback("/", OnAccessRoot);
        ipAddress = DebugServerUtil.GetLanIpAddress();
    }

    void OnAccessRoot(
		out string outputHtml,
        NameValueCollection queryString,
		Stream bodyDataStream)
    {
        outputHtml = indexHtml.text;
    }

    void OnDebugFileServiceChanged(string path)
    {
        Debug.Log("FileService Receive: " + path);
        if (loadCoroutine == null)
        {
            loadCoroutine = StartCoroutine(CoLoad(path));
        }
    }

    void Update()
    {
        debugServer.ManualUpdate();
    }

    void OnGUI()
    {
        GUILayout.Label(ipAddress);
        if (loadCoroutine == null)
        {
            if (GUILayout.Button("Load"))
            {
                loadCoroutine = StartCoroutine(CoLoad("testAssets.unity3d"));
            }
        }
        if (GUILayout.Button("Next Prefab"))
        {
            if (instance != null)
            {
                Destroy(instance);
            }
            prefabIndex++;
            if (prefabIndex >= prefabs.Length)
            {
                prefabIndex = 0;
            }
            CreateInstance();
        }
    }

    IEnumerator CoLoad(string path)
    {
        Unload();
        var ret = new CoroutineReturnValue<AssetBundle>();
        yield return DebugServerUtil.CoLoad(ret, path);
        if (ret.Value == null)
        {
            Debug.LogException(ret.Exception);
            loadCoroutine = null;
            yield break;
        }
        assetBundle = ret.Value;
        
        var names = assetBundle.GetAllAssetNames();
        foreach (var name in names)
        {
            Debug.Log("\tAsset: " + name);
        }
        var op = assetBundle.LoadAllAssetsAsync<GameObject>();
        yield return op;
        if (op.allAssets == null)
        {
            Debug.LogError("No Prefab Contained.");
            loadCoroutine = null;
            yield break;
        }
        prefabs = new GameObject[op.allAssets.Length];
        for (int i = 0; i < op.allAssets.Length; i++)
        {
            prefabs[i] = op.allAssets[i] as GameObject;
            Debug.Log("\tprefab: " + prefabs[i].name);
        }

        prefabIndex = 0;

        CreateInstance();
        loadCoroutine = null;
    }

    void CreateInstance()
    {
        instance = Instantiate(prefabs[prefabIndex], transform, false);
    }

    void Unload()
    {
        if (instance != null)
        {
            Destroy(instance);
        }
        prefabs = null;
        if (assetBundle != null)
        {
            assetBundle.Unload(false);
            assetBundle = null;
        }
    }
}
