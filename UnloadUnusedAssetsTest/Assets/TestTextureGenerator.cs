using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TestTextureGenerator
{
    public const int Count = 100;
#if UNITY_EDITOR
	[MenuItem("Sample/GenerateTextures")]
    public static void GenerateTextures()
    {
        for (int i = 0; i < Count; i++)
        {
            GenerateTexture(i);
        }
    }
#endif
    static void GenerateTexture(int i)
    {
        var tex = new Texture2D(4096, 4096);
        var bytes = ImageConversion.EncodeToPNG(tex);
		var appDir = System.IO.Path.GetFullPath(".");
        var path = string.Format("{0}/TestData/{0}.png", appDir, i);
        System.IO.File.WriteAllBytes(path, bytes);
        Debug.Log(i);
    }
}
