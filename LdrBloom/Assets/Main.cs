using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    [SerializeField] Kayac.LightPostProcessor postProcess;
    [SerializeField] GameObject spherePrefab;
    [SerializeField] GameObject cylinderPrefab;
    [SerializeField] Transform objectsRoot;
    [SerializeField] Shader shader;
    [SerializeField] Slider speedSlider;
    [SerializeField] Slider scaleSlider;
    [SerializeField] FillRenderer fillRenderer;
    [SerializeField] Text text;
    [SerializeField] Toggle benchmarkToggle;
    [SerializeField] Toggle enableToggle;
    [SerializeField] Toggle enableStdToggle;
    [SerializeField] Toggle fps24Toggle;
    [SerializeField] Toggle logToggle;
    [SerializeField] Text logText;
    [SerializeField] UnityEngine.Rendering.PostProcessing.PostProcessLayer stdPostProcessLayer;

    const int sphereCount = 100;
    const int cylinderCount = 100;
    const float worldSize = 10f;

    GameObject[] objects;
    Vector3[] velocities;
    Vector3[] angularVelocities;
    Material material;
    Kayac.SecretData secretData;
    float[] times;
    int timeIndex;
    float count;
    float countVelocity;
    Kayac.DebugSlack slack;
    Kayac.MemoryLogHandler log;

    void Start()
    {
        logText.text = "Start Called.";
        log = new Kayac.MemoryLogHandler(1000);
        logText.text = "Log Initialized.";

#if !UNITY_WEBGL || UNITY_EDITOR //WebGLではSlack初期化しない。なので叩くと死ぬ。
        StartCoroutine(CoSetupSlack());
#endif

        benchmarkToggle.onValueChanged.AddListener(toggle =>
        {
            if (benchmarkToggle.isOn)
            {
                count = 0f; //1から始めることでだいぶ収束が速くなる。TODO: 速度も適切な値がありそう。
            }
            else
            {
                count = 0f;
            }
            countVelocity = 0f;
        });

        speedSlider.value = 0.5f;
        objects = new GameObject[sphereCount + cylinderCount];
        velocities = new Vector3[objects.Length];
        angularVelocities = new Vector3[objects.Length];
        material = new Material(shader);
        for (int i = 0; i < sphereCount; i++)
        {
            objects[i] = Instantiate(spherePrefab, objectsRoot, false);
        }
        for (int i = 0; i < cylinderCount; i++)
        {
            objects[sphereCount + i] = Instantiate(cylinderPrefab, objectsRoot, false);
        }
        var block = new MaterialPropertyBlock();
        var propertyId = Shader.PropertyToID("_Color");
        for (int i = 0; i < objects.Length; i++)
        {
            var renderer = objects[i].GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
#if true // 動かなくして中央に置きたい時もある
            block.SetColor(propertyId, new Color(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                1f));
            renderer.SetPropertyBlock(block);
            objects[i].transform.localPosition = new Vector3(
                Random.Range(-worldSize, worldSize),
                Random.Range(-worldSize, worldSize),
                Random.Range(-worldSize, worldSize));
            objects[i].transform.localRotation = Quaternion.Euler(
                Random.Range(-180f, 180f),
                Random.Range(-180f, 180f),
                Random.Range(-180f, 180f));
            velocities[i] = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f));
            angularVelocities[i] = new Vector3(
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f));
#endif
        }
        // 以下ベンチマーク準備
        times = new float[60];
        fillRenderer.ManualStart();
        logText.text = "Start Finished.";
    }

    IEnumerator CoSetupSlack()
    {
        secretData = new Kayac.SecretData();
        yield return secretData.CoLoad();
        var token = secretData.Get("slackToken");
        if (token != null)
        {
            slack = new Kayac.DebugSlack(token, "unity-debug");
        }
    }

    public void OnClickSendRtButton()
    {
        StartCoroutine(CoSendRenderTargets());
    }

    IEnumerator CoSendRenderTargets()
    {
        yield return new WaitForEndOfFrame();
        yield return slack.CoPostTextures(
            postProcess.EnumerateRenderTexturesForDebug());
    }

    public void OnClickSaveRtButton()
    {
        StartCoroutine(CoSaveRenderTargets());
    }

    public void OnClickSendLogButton()
    {
        StartCoroutine(slack.CoPostBinary(log.GetBytes(), "log.txt"));
    }

    public void OnClickColorVfxButton()
    {
        StartCoroutine(CoColorVfx());
    }

    IEnumerator CoColorVfx()
    {
        var offset = new Vector3(0.4f, 0.2f, 0f);
        var scale = new Vector3(1.2f, 0.6f, 0.3f);
        var saturation = 0f;
        for (int i = 0; i < 120; i++)
        {
            postProcess.SetColorFilter(offset, scale, saturation);
            yield return null;
            offset += -offset * 0.02f;
            scale += (Vector3.one - scale) * 0.02f;
            saturation += (1f - saturation) * 0.02f;
        }
        postProcess.SetColorFilter(Vector3.zero, Vector3.one, 1f);
    }

    IEnumerator CoSaveRenderTargets()
    {
        yield return new WaitForEndOfFrame();
        foreach (var rt in postProcess.EnumerateRenderTexturesForDebug())
        {
            Debug.Log("Saving: " + rt.name);
            var bytesList = Kayac.TextureUtil.ConvertAllLevelToFile(rt);
            for (int level = 0; level < bytesList.Length; level++)
            {
                var name = rt.name;
                if (bytesList.Length > 1)
                {
                    name += "_" + level;
                }
                name += ".png";
                System.IO.File.WriteAllBytes(name, bytesList[level]);
            }
        }
        Debug.Log("Done");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            StartCoroutine(slack.CoPostScreenshot(null, null, null, null, 0, false, Kayac.TextureUtil.FileType.Jpeg));
        }

        if (Input.GetKeyDown(KeyCode.F1))
        {
            postProcess.BloomCombineStartLevel -= 1;
            Debug.Log(postProcess.BloomCombineStartLevel);
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            postProcess.BloomCombineStartLevel += 1;
            Debug.Log(postProcess.BloomCombineStartLevel);
        }


        postProcess.enabled = enableToggle.isOn;
        stdPostProcessLayer.enabled = enableStdToggle.isOn;
        float speed = speedSlider.value;
        speed = Mathf.Pow(10f, (speed - 0.5f) * 3);
        float dt = Time.deltaTime * speed;
        for (int i = 0; i < objects.Length; i++)
        {
            Update(i, dt);
        }
        // 以下ベンチマーク
        times[timeIndex] = Time.realtimeSinceStartup;
        timeIndex++;
        if (timeIndex >= times.Length)
        {
            timeIndex = 0;
        }
        var latest = ((timeIndex - 1) < 0) ? (times.Length - 1) : (timeIndex - 1);
        var avg = (times[latest] - times[timeIndex]) / (times.Length - 1);
        text.text = "FrameTime: " + (avg * 1000f).ToString("N2") + "\nCount: " + count.ToString("N2") + " " + postProcess.BloomCombineStartLevel;

        // ベンチマーク中は物描かない
        objectsRoot.gameObject.SetActive(!benchmarkToggle.isOn);
        if (benchmarkToggle.isOn)
        {
            var targetMs = fps24Toggle.isOn ? (1000f / 24f) : (1000f / 40f);
            var accel = (((targetMs * 0.001f) - Time.unscaledDeltaTime) * (count + 1f) * 0.1f) - (countVelocity * 0.5f);
            countVelocity += accel;
            count += countVelocity;
            count = Mathf.Clamp(count, 0f, 10000f);
        }
        else
        {
            count = 0f;
            countVelocity = 0f;
        }
        fillRenderer.SetCount(count);
        fillRenderer.ManualUpdate();
        if (logToggle.isOn)
        {
            logText.text = log.Tail(10);
        }
        logText.enabled = logToggle.isOn;
    }

    void Update(int i, float dt)
    {
        var o = objects[i];
        var pos = o.transform.localPosition;
        pos += velocities[i] * dt;
        o.transform.localPosition = pos;
        velocities[i].x = Bound(velocities[i].x, pos.x);
        velocities[i].y = Bound(velocities[i].y, pos.y);
        velocities[i].z = Bound(velocities[i].z, pos.z);
        o.transform.localScale = new Vector3(scaleSlider.value, scaleSlider.value, scaleSlider.value);

        var rot = o.transform.localRotation.eulerAngles;
        rot += angularVelocities[i] * dt;
        o.transform.localRotation = Quaternion.Euler(rot);
    }

    float Bound(float velocity, float position)
    {
        if (position > worldSize)
        {
            return -Mathf.Abs(velocity);
        }
        else if (position < -worldSize)
        {
            return Mathf.Abs(velocity);
        }
        else
        {
            return velocity;
        }
    }
}
