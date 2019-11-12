using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class Main : MonoBehaviour
{
    [SerializeField] RawImage rawImage;
    int index;
    int count = TestTextureGenerator.Count;
    UnityWebRequest req;
    string message;

    void Update()
    {
        if (req == null)
        {
            var appDir = System.IO.Path.GetFullPath(".");
            var url = string.Format("file://{0}/TestData/{1}.png", appDir, index.ToString());
            Debug.Log(url);
            message = url;
            req = UnityWebRequestTexture.GetTexture(url);
            req.SendWebRequest();
            index = (index + 1) % count;
        }
        else if (req.isDone)
        {
            rawImage.texture = DownloadHandlerTexture.GetContent(req);
            req.Dispose(); // これ忘れちゃダメよ!!(実は最初忘れてた)
            req = null;
        }
    }

    void OnGUI()
    {
        GUILayout.Label(message);
        if (GUILayout.Button("UnloadUnusedAssets"))
        {
            Resources.UnloadUnusedAssets();
        }
    }
}
