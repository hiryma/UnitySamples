using UnityEngine;
using UnityEngine.UI;
using System;
using Unity.Collections;
using Unity.Jobs;

public class Main : MonoBehaviour
{
    [SerializeField] RawImage image;
    [SerializeField] Slider sizeSlider;
    [SerializeField] Text sizeText;
    [SerializeField] Text fpsText;
    [SerializeField] Toggle gpuToggle;
    [SerializeField] ComputeShader computeShader;

    int writeBufferIndex;

    // GPU用
    RenderTexture[] renderTextures;

    // CPU用
    Texture2D texture2d; 
    NativeArray<Color32>[] cpuData;
    Color32[] cpuDataCopy;

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
        var newSizeLog = Mathf.RoundToInt(sizeSlider.value);
        if (newSizeLog != sizeLog)
        {
            sizeLog = newSizeLog;
            changed = true;
        }
        if (changed)
        {
            Reset();
        }

        if (mode == Mode.Cpu)
        {
            UpdateCpu();
        }
        else if (mode == Mode.Gpu)
        {
            UpdateGpu();
        }
        writeBufferIndex = 1 - writeBufferIndex;
    }

    struct Job : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Color32> inArray;
        public NativeArray<Color32> outArray;

        public void Execute(int i)
        {
            outArray[i] = XorShift32(inArray[i]);
        }

        public static Color32 XorShift32(Color32 c)
        {
            uint x = (uint)c.r | ((uint)c.g << 8) | ((uint)c.b << 16) | ((uint)c.r << 24);
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            c.r = (byte)(x & 0xff);
            c.g = (byte)((x >> 8) & 0xff);
            c.b = (byte)((x >> 16) & 0xff);
            c.a = (byte)((x >> 24) & 0xff);
            return c;
        }
    }

    void UpdateCpu()
    {
        Job job;
        job.inArray = cpuData[1 - writeBufferIndex];
        job.outArray = cpuData[writeBufferIndex];
        var handle = job.Schedule(cpuDataCopy.Length, 1);
        handle.Complete();
        UpdateCpuTexture();
        image.texture = texture2d;
    }

    void UpdateGpu()
    {
        var size = 1 << sizeLog;
        var kernelIndex = computeShader.FindKernel("CSMain");
        computeShader.SetTexture(kernelIndex, "Source", renderTextures[1 - writeBufferIndex]);
        computeShader.SetTexture(kernelIndex, "Destination", renderTextures[writeBufferIndex]);
        uint sizeX, sizeY, sizeZUnused;
        computeShader.GetKernelThreadGroupSizes(
            kernelIndex,
            out sizeX,
            out sizeY,
            out sizeZUnused);
        Debug.Assert(sizeZUnused == 1); // 1である前提
        if (SystemInfo.supportsComputeShaders)
        {
            computeShader.Dispatch(
                kernelIndex,
                size / (int)sizeX,
                size / (int)sizeY,
                1);
        }
        image.texture = renderTextures[writeBufferIndex]; // これ同期待たないとダメでしょ感
    }

    void CreateSeedTexture(int size)
    {
        texture2d = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture2d.name = "2D";
        if (cpuData != null)
        {
            for (int i = 0; i < cpuData.Length; i++)
            {
                cpuData[i].Dispose();
            }
        }
        cpuData = new NativeArray<Color32>[2];
        int texelCount = size * size;
        for (int i = 0; i < 2; i++)
        {
            cpuData[i] = new NativeArray<Color32>(
                texelCount,
                Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
        }
        sizeText.text = size.ToString();
        var dst = cpuData[writeBufferIndex];
        for (int i = 0; i < texelCount; i++)
        {
            var c = new Color32(
                (byte)UnityEngine.Random.Range(0, 256),
                (byte)UnityEngine.Random.Range(0, 256),
                (byte)UnityEngine.Random.Range(0, 256),
                (byte)UnityEngine.Random.Range(0, 256));
            dst[i] = c;
        }
        //通常配列にコピーして
        cpuDataCopy = new Color32[size * size];
        UpdateCpuTexture();
        writeBufferIndex = 1 - writeBufferIndex;
    }

    void UpdateCpuTexture()
    {
        var src = cpuData[writeBufferIndex];
        for (int i = 0; i < src.Length; i++)
        {
            cpuDataCopy[i] = src[i];
        }
        texture2d.SetPixels32(cpuDataCopy);
        texture2d.Apply();
    }

    void Reset()
    {
        var size = 1 << sizeLog;
        writeBufferIndex = 0;
        CreateSeedTexture(size);
        if (mode == Mode.Cpu)
        {
            // 特にやることない
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

    void OnDestroy()
    {
        for (int i = 0; i < cpuData.Length; i++)
        {
            cpuData[i].Dispose();
        }
    }
}
