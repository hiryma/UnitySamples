using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using Kayac;

public class Main : MonoBehaviour
{
    [SerializeField] TextAsset indexHtml;
    [SerializeField] Kayac.Debug.Ui.DebugUiManager debugUi;
    [SerializeField] int debugServerPort = 8080;
    [SerializeField] Camera mainCamera;
    AssetBundle assetBundle;
    GameObject[] prefabs;
    int prefabIndex ;
    GameObject instance;
    Coroutine loadCoroutine;
    DebugServer debugServer;
    Ui ui;
    IncompatibleShaderReplacer shaderReplacer;

    void Start()
    {
        ui = new Ui(
            debugUi,
            debugServerPort,
            OnClickPlay,
            OnClickStop,
            OnClickNext);
        debugServer = new DebugServer(debugServerPort, "/upload/", OnDebugFileServiceChanged);
        debugServer.RegisterRequestCallback("/", OnAccessRoot);
        debugServer.RegisterRequestCallback("/ping", OnAccessPing);
        debugServer.RegisterRequestCallback("/toggle-debug-ui", OnAccessToggleDebugUi);
        shaderReplacer = new IncompatibleShaderReplacer();
    }

    void OnAccessRoot(
		out string outputHtml,
        NameValueCollection queryString,
		Stream bodyDataStream)
    {
        outputHtml = indexHtml.text;
    }

    void OnAccessPing(
    out string outputHtml,
    NameValueCollection queryString,
    Stream bodyDataStream)
    {
        outputHtml = "ok";
    }

    void OnAccessToggleDebugUi(
    out string outputHtml,
    NameValueCollection queryString,
    Stream bodyDataStream)
    {
        outputHtml = "ok";
        debugUi.gameObject.SetActive(!debugUi.gameObject.activeSelf);
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
        ui.ManualUpdate();
        debugServer.ManualUpdate();
    }

    void OnClickPlay()
    {
        if (prefabs == null)
        {
            return;
        }
        if (instance != null)
        {
            Destroy(instance);
        }
        CreateInstance();
    }

    void OnClickStop()
    {
        if (instance != null)
        {
            Destroy(instance);
        }
    }

    void OnClickNext()
    {
        if (prefabs == null)
        {
            return;
        }
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

    IEnumerator CoLoad(string path)
    {
        Unload();
        var ret = new CoroutineReturnValue<AssetBundle>();
        yield return DebugServerUtil.CoLoad(ret, path);
        if (ret.Value == null)
        {
            if (ret.Exception != null)
            {
                Debug.LogException(ret.Exception);
            }
            else
            {
                Debug.LogError("CoLoad: Can't get value.");
            }
            loadCoroutine = null;
            yield break;
        }
        assetBundle = ret.Value;

#if false
        var names = assetBundle.GetAllAssetNames();
        foreach (var name in names)
        {
            Debug.Log("\tAsset: " + name);
        }
#endif
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
//            Debug.Log("\tprefab: " + prefabs[i].name);
        }

        prefabIndex = 0;

        CreateInstance();
        loadCoroutine = null;
    }

    void CreateInstance()
    {
        instance = Instantiate(prefabs[prefabIndex], transform, false);
        // エディタでPC以外のAssetBundleを読むとシェーダが非互換で動かないので、プロジェクト内のもので差し換える
        shaderReplacer.Replace(instance.transform);
        Focus(instance);
    }

    void Focus(GameObject instance)
    {
        // バウンディング計算
        var renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = -min;
            foreach (var renderer in renderers)
            {
                var bounds = renderer.bounds;
                min = Vector3.Min(min, bounds.min);
                max = Vector3.Max(max, bounds.max);
            }
            // 面倒くさいから球でやっちゃうよ TODO: 後でいい感じにしようね
            var rad = (max - min).magnitude * 0.5f;
            var tan = Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            var d = rad / tan;
            if (mainCamera.aspect < 1f)
            {
                d /= mainCamera.aspect;
            }
            mainCamera.transform.position = Vector3.Lerp(min, max, 0.5f) + new Vector3(0f, 0f, -d);
        }
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
