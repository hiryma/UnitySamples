using UnityEngine;
using UnityEngine.UI;
using System;

public class Main : MonoBehaviour
{
    [SerializeField] RawImage image;
    [SerializeField] Slider slider;
    [SerializeField] Text sizeText;
    [SerializeField] Text fpsText;
    [SerializeField] Text coresText;
    [SerializeField] Toggle gpuToggle;
    [SerializeField] ComputeShader computeShader;

    RenderTexture[] renderTextures;
    int writeBufferIndex;
    Texture2D texture2d;
    Color32[] cpuData;
    float avgDeltaTime;
    DateTime prevFrameTime;
    Kayac.SimpleThreadPool threadPool;
    int sizeLog;
    enum Mode
    {
        Cpu,
        Gpu,
    }
    Mode mode;

    void Start()
    {
        threadPool = new Kayac.SimpleThreadPool();
        coresText.text = "Core:" + threadPool.threadCount.ToString();
        mode = Mode.Gpu;
        Reset();
        prevFrameTime = DateTime.Now;
    }

    void Update()
    {
        var now = DateTime.Now;
        var deltaTime = (float)(now - prevFrameTime).Ticks / (float)TimeSpan.TicksPerSecond;
        prevFrameTime = now;
        avgDeltaTime *= 0.9f;
        avgDeltaTime += deltaTime * 0.1f;
        var fps = 1f / avgDeltaTime;
        fpsText.text = fps.ToString("F1");
        bool changed = false;
        if (gpuToggle.isOn && (mode == Mode.Cpu))
        {
            changed = true;
            mode = Mode.Gpu;
        }
        else if (!gpuToggle.isOn && (mode == Mode.Gpu))
        {
            changed = true;
            mode = Mode.Cpu;
        }
        var newSizeLog = Mathf.RoundToInt(slider.value);
        if (newSizeLog != sizeLog)
        {
            sizeLog = newSizeLog;
            changed = true;
        }
        if (changed)
        {
            Reset();
        }
        UpdateTexture();
    }

    void UpdateTexture()
    {
        if (mode == Mode.Cpu)
        {
            UpdateTextureCpu();
        }
        else if (mode == Mode.Gpu)
        {
            UpdateTextureGpu();
        }
    }

    static void XorShift32(ref Color32 c)
    {
        uint x = ((uint)c.a << 24) | ((uint)c.r << 16) | ((uint)c.g << 8) | c.r;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        c.a = (byte)(x >> 24);
        c.r = (byte)((x >> 16) & 0xff);
        c.g = (byte)((x >> 8) & 0xff);
        c.b = (byte)(x & 0xff);
    }

    static void Job(Color32[] data, int begin, int count)
    {
        for (int i = 0; i < count; i++)
        {
            XorShift32(ref data[begin + i]);
        }
    }

    void UpdateTextureCpu()
    {
        const int jobCount = 64;
        int begin = 0;
        int rest = cpuData.Length;
        int unit = (cpuData.Length + jobCount - 1) / jobCount;
        for (int i = 0; i < jobCount; i++)
        {
            int beginCaptured = begin;
            int countCaptured = (unit <= rest) ? unit : rest;
            threadPool.AddJob(() => Job(cpuData, beginCaptured, countCaptured));
            begin += countCaptured;
        }
        threadPool.Wait();
        texture2d.SetPixels32(cpuData);
        texture2d.Apply();
        image.texture = texture2d;
    }

    void UpdateTextureGpu()
    {
        var size = 1 << sizeLog;
        var kernelIndex = computeShader.FindKernel("CSMain");
        computeShader.SetTexture(kernelIndex, "Source", renderTextures[1 - writeBufferIndex]);
        computeShader.SetTexture(kernelIndex, "Destination", renderTextures[writeBufferIndex]);
        uint sizeX, sizeY, sizeZ;
        computeShader.GetKernelThreadGroupSizes(
            kernelIndex,
            out sizeX,
            out sizeY,
            out sizeZ
        );
        if (SystemInfo.supportsComputeShaders)
        {
            computeShader.Dispatch(
                kernelIndex,
                (size + (int)sizeX - 1) / (int)sizeX,
                (size + (int)sizeY - 1) / (int)sizeY,
                (1 + (int)sizeZ - 1) / (int)sizeZ);
        }
        image.texture = renderTextures[writeBufferIndex]; // これ同期待たないとダメでしょ感
        writeBufferIndex = 1 - writeBufferIndex;
    }

    void CreateSeedTexture(int size)
    {
        texture2d = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture2d.name = "2D";
        cpuData = new Color32[size * size];
        sizeText.text = size.ToString();
        for (int i = 0; i < cpuData.Length; i++)
        {
            cpuData[i].r = (byte)UnityEngine.Random.Range(0, 256);
            cpuData[i].g = (byte)UnityEngine.Random.Range(0, 256);
            cpuData[i].b = (byte)UnityEngine.Random.Range(0, 256);
            cpuData[i].a = (byte)UnityEngine.Random.Range(0, 256);
        }
        texture2d.SetPixels32(cpuData);
        texture2d.Apply();
    }

    void Reset()
    {
        var size = 1 << sizeLog;
        CreateSeedTexture(size);
        if (mode == Mode.Cpu)
        {
        }
        else if (mode == Mode.Gpu)
        {
            renderTextures = new RenderTexture[2];
            for (int i = 0; i < renderTextures.Length; i++)
            {
                renderTextures[i] = new RenderTexture(size, size, 1, RenderTextureFormat.ARGB32);
                renderTextures[i].name = "RT" + i.ToString();
                renderTextures[i].enableRandomWrite = true;
                renderTextures[i].Create();
            }
            Graphics.Blit(texture2d, renderTextures[1]);
        }
    }
}
