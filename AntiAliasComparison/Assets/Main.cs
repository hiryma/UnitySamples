using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Main : MonoBehaviour
{
    [SerializeField] Camera aaCamera;
    [SerializeField] GameObject fillPrefab;
    [SerializeField] Slider fillSlider;
    [SerializeField] Text fillCountText;
    [SerializeField] Transform fillObjectsRoot;
    [SerializeField] Text fpsText;
    [SerializeField] Text resoText;

    List<GameObject> fillObjects;
    int fillCount = -1;
    float frameTime;
    System.DateTime time;

    MonoBehaviour fxaaComponent;

    public void Start()
    {
        resoText.text = string.Format("{0}x{1}", Screen.width, Screen.height);
        fillObjects = new List<GameObject>();
        var component = aaCamera.gameObject.GetComponent("FXAA");
        if (component != null)
        {
            Debug.Log("FXAA Component Found.");
            fxaaComponent = component as MonoBehaviour;
        }
        else
        {
            Debug.Log("FXAA Component Not Found.");
        }
        OnClickNoAA(); // 最初はナシで
        fillSlider.onValueChanged.AddListener(OnFillSliderChange);
        time = System.DateTime.Now;
        OnFillSliderChange(fillSlider.value); // 初回強制呼び出し
    }

    void Update()
    {
        var now = System.DateTime.Now;
        var deltaTime = (float)(now - time).TotalSeconds;
        frameTime *= 0.95f;
        frameTime += deltaTime * 0.05f;
        time = now;
        fpsText.text = "FPS: " + (1f / frameTime).ToString("F2");
    }

    public void OnClickNoAA()
    {
        QualitySettings.SetQualityLevel(0, true);
        if (fxaaComponent != null)
        {
            fxaaComponent.enabled = false;
        }
    }

    public void OnClickMsaa2()
    {
        QualitySettings.SetQualityLevel(1, true);
        if (fxaaComponent != null)
        {
            fxaaComponent.enabled = false;
        }
    }

    public void OnClickMsaa4()
    {
        QualitySettings.SetQualityLevel(2, true);
        if (fxaaComponent != null)
        {
            fxaaComponent.enabled = false;
        }
    }

    public void OnClickMsaa8()
    {
        QualitySettings.SetQualityLevel(3, true);
        if (fxaaComponent != null)
        {
            fxaaComponent.enabled = false;
        }
    }

    public void OnClickFxaa()
    {
        QualitySettings.SetQualityLevel(0, true);
        if (fxaaComponent != null)
        {
            fxaaComponent.enabled = true;
        }
    }

    void OnFillSliderChange(float value)
    {
        var newCount = Mathf.FloorToInt(Mathf.Pow(2f, value));
        if (newCount != fillCount)
        {
            if (fillCount < 0)
            {
                fillCount = 0;
            }
            fillCountText.text = "Fill: " + newCount.ToString();
            // 多すぎる分破棄
            if (fillCount > newCount)
            {
                for (int i = newCount; i < fillCount; i++)
                {
                    Destroy(fillObjects[i]);
                }
                fillObjects.RemoveRange(newCount, fillCount - newCount);
            }
            else
            {
                // 足りない分追加
                for (int i = fillCount; i < newCount; i++)
                {
                    fillObjects.Add(Instantiate(fillPrefab, fillObjectsRoot, false));
                }
            }
            fillCount = newCount;
        }
    }
}
