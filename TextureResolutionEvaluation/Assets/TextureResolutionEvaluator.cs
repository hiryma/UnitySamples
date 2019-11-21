using UnityEngine;
using UnityEngine.UI;

public class TextureResolutionEvaluator : MonoBehaviour
{
    [SerializeField] Texture2D originalTexture;
    [SerializeField] RawImage rawImage;
    [SerializeField] Material lanczosMaterial;
    [SerializeField] Material diffMaterial;
    [SerializeField] Material showMaterial;
    [SerializeField] Material sumMaterial;
    float scaleLevelFloat;
    float diffScale;
    float newDiffScale = 1f;
    int scaleLevel;
    Texture2D shrinked;
    Texture2D diff;
    Texture2D currentOriginalTexture;
    string scaleText;
    enum Mode
    {
        None,
        Original,
        Shrinked,
        Diff,
    }
    Mode mode;
    Mode newMode = Mode.Original;

    void UpdateView()
    {
        if (mode == Mode.Original)
        {
            rawImage.texture = originalTexture;
        }
        else if (mode == Mode.Shrinked)
        {
            rawImage.texture = shrinked;
        }
        else if (mode == Mode.Diff)
        {
            rawImage.texture = diff;
        }
    }

    void Update()
    {
        var newScaleLevel = (int)(scaleLevelFloat + 0.5f);
        if ((currentOriginalTexture != originalTexture)
            || (newScaleLevel != scaleLevel))
        {
            scaleLevel = newScaleLevel;
            currentOriginalTexture = originalTexture;
            UpdateTransform();
            Shrink();
            Diff();
            SumDiff();
            mode = Mode.None;
        }
        if (mode != newMode)
        {
            mode = newMode;
            if (mode == Mode.Diff)
            {
                showMaterial.SetFloat("_Scale", diffScale);
            }
            else
            {
                showMaterial.SetFloat("_Scale", 1f);
            }
            UpdateView();
        }
        if (newDiffScale != diffScale)
        {
            diffScale = newDiffScale;
            if (mode == Mode.Diff)
            {
                showMaterial.SetFloat("_Scale", diffScale);
            }
        }
    }

    void UpdateDiffScale()
    {
        diffMaterial.SetFloat("_Scale", diffScale);
    }

    int ToPowerOf2(int x)
    {
        int t = 1;
        while ((t + t) <= x)
        {
            t += t;
        }
        return t;
    }

    void Shrink()
    {
        var srcW = currentOriginalTexture.width;
        var srcH = currentOriginalTexture.height;
        var potW = ToPowerOf2(srcW);
        var potH = ToPowerOf2(srcH);
        var dstW = potW >> scaleLevel;
        var dstH = potH >> scaleLevel;
        dstW = (dstW < 1) ? 1 : dstW;
        dstH = (dstH < 1) ? 1 : dstH;
        scaleText = string.Format(dstW + "x" + dstH);
        var dst = new Texture2D(dstW, dstH);
        var lanczosRadius = 2f;
        var rt = new RenderTexture(dstW, dstH, 0, RenderTextureFormat.ARGB32);

        var radiusX = Mathf.CeilToInt(lanczosRadius * (float)srcW / (float)dstW);
        var radiusY = Mathf.CeilToInt(lanczosRadius * (float)srcW / (float)dstH);
        lanczosMaterial.SetInt("_KernelRadiusX", radiusX);
        lanczosMaterial.SetInt("_KernelRadiusY", radiusY);
        lanczosMaterial.SetInt("_LanczosType", (int)lanczosRadius);
        Graphics.Blit(currentOriginalTexture, rt, lanczosMaterial);
        RenderTexture.active = rt;
        dst.ReadPixels(new Rect(0, 0, dstW, dstH), 0, 0);
        dst.Apply();
        shrinked = dst;
    }

    void Diff()
    {
        var w = currentOriginalTexture.width;
        var h = currentOriginalTexture.height;
        var dst = new Texture2D(w, h, TextureFormat.RGBAFloat, false);
        var rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGBFloat);

        diffMaterial.SetTexture("_CompareTex", shrinked);
        Graphics.Blit(currentOriginalTexture, rt, diffMaterial);
        RenderTexture.active = rt;
        dst.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        dst.Apply();
        diff = dst;
    }

    Color SumDiff()
    {
        var w = currentOriginalTexture.width;
        var h = currentOriginalTexture.height;
        var dst = new Texture2D(1, h, TextureFormat.RGBAFloat, false);
        var rt = new RenderTexture(1, h, 0, RenderTextureFormat.ARGBFloat);

        Graphics.Blit(diff, rt, sumMaterial);
        RenderTexture.active = rt;
        dst.ReadPixels(new Rect(0, 0, 1, h), 0, 0);
        dst.Apply();

        var r = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < h; y++)
        {
            var c = dst.GetPixel(0, y);
            r.r += c.r;
            r.g += c.g;
            r.b += c.b;
            r.a += c.a;
        }
        float rcpPixelCount = 1f / (w * h);
        r.r *= rcpPixelCount;
        r.g *= rcpPixelCount;
        r.b *= rcpPixelCount;
        r.a *= rcpPixelCount;
        r.r = Mathf.Sqrt(r.r);
        r.g = Mathf.Sqrt(r.g);
        r.b = Mathf.Sqrt(r.b);
        r.a = Mathf.Sqrt(r.a);
        Debug.LogFormat("ERROR: ({0}, {1}, {2}, {3})", r.r, r.g, r.b, r.a);
        return r;
    }


    void UpdateTransform()
    {
        if (currentOriginalTexture == null)
        {
            return;
        }
        var screenWidth = (float)Screen.width;
        var screenHeight = (float)Screen.height;
        var screenAspect = screenWidth / screenHeight;
        var texWidth = (float)currentOriginalTexture.width;
        var texHeight = (float)currentOriginalTexture.height;
        var texAspect = texWidth / texHeight;
        var transform = rawImage.rectTransform;
        if (screenAspect > texAspect) // 画面が横長
        {
            var half = 0.5f * (texAspect / screenAspect);
            transform.anchorMin = new Vector2(0.5f - half, 0f);
            transform.anchorMax = new Vector2(0.5f + half, 1f);
        }
        else // 画面が縦長
        {
            float half = 0.5f * (screenAspect / texAspect);
            transform.anchorMin = new Vector2(0f, 0.5f - half);
            transform.anchorMax = new Vector2(1f, 0.5f + half);
        }
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(0f, 0f, 100f, 50f), "ShowOriginal"))
        {
            newMode = Mode.Original;
        }
        if (GUI.Button(new Rect(100f, 0f, 100f, 50f), "ShowShrinked"))
        {
            newMode = Mode.Shrinked;
        }
        if (GUI.Button(new Rect(200f, 0f, 100f, 50f), "ShowDiff"))
        {
            newMode = Mode.Diff;
        }
        GUI.Label(new Rect(0f, 50f, 100f, 50f), scaleText);
        scaleLevelFloat = GUI.HorizontalSlider(new Rect(100f, 50f, 300f, 50f), scaleLevelFloat, 0f, 5f);
        GUI.Label(new Rect(0f, 100f, 100f, 50f), diffScale.ToString("F2"));
        var t = GUI.HorizontalSlider(new Rect(100f, 100f, 300f, 50f), Mathf.Log10(diffScale), -1f, 2f);
        newDiffScale = Mathf.Pow(10f, t);
    }
}
