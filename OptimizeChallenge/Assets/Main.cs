using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    List<GameObject> konches;
    float countVelocity;
    float count;

    void Start()
    {
        Application.targetFrameRate = 1000;
        konches = new List<GameObject>();
    }

    void Update()
    {
        var marginTime = (1f / 40f) - Time.deltaTime; // 40fpsを保てる範囲で数を増やしていくよ!
        countVelocity *= 0.95f;
        countVelocity += marginTime * 100f;

        count += countVelocity;
        if (count < 0f)
        {
            count = 0f;
        }
        while (konches.Count != (int)count)
        {
            if (konches.Count < (int)count)
            {
                GenerateBall();
            }
            else if (konches.Count > 0)
            {
                Destroy(konches[konches.Count - 1]);
                konches.RemoveAt(konches.Count - 1);
            }
        }
        var textObject = GameObject.Find("KonchCountText");
        var textComponent = textObject.GetComponent<UnityEngine.UI.Text>();
        textComponent.text = konches.Count.ToString();
        var scale = Mathf.Pow((float)konches.Count, 1f / 3f) * 40f;
        Camera.main.transform.position = new Vector3(0f, 0f, -scale);
        Camera.main.farClipPlane = scale * 2f;
        Camera.main.nearClipPlane = scale * 0.002f;
    }

    void GenerateBall()
    {
        var original = GameObject.Find("OriginalKonch");
        var newObject = Instantiate(original);
        newObject.name = "Konch" + konches.Count;
        var ballsRoot = GameObject.Find("KonchesRoot");
        newObject.transform.SetParent(ballsRoot.transform, false);
        var t = Mathf.Pow((float)konches.Count, 1f / 3f) * 20f;
        newObject.transform.position = new Vector3(
            Random.Range(-t, t),
            Random.Range(-t, t),
            Random.Range(-t, t));
        newObject.transform.rotation = new Quaternion(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f));
        var konch = newObject.GetComponent<Konch>();
        t *= 0.5f;
        konch.Velocity = new Vector3(
            Random.Range(-t, t),
            Random.Range(-t, t),
            Random.Range(-t, t));
        konch.Color = new Color(
            Random.Range(0f, 1f),
            Random.Range(0f, 1f),
            Random.Range(0f, 1f),
            1f);
        konches.Add(newObject);
    }
}
