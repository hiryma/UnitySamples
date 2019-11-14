using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TextureListMaker
{
    struct Item
    {
        public override string ToString()
        {
            return string.Format("{0}\t{1}x{2}\t{3}\tmips={4}\tfaces={5}\treadable={6}\t{7}", byteSize, width, height, format, mipmapCount, faceCount, readable, name);
        }
        public string name;
        public int width;
        public int height;
        public TextureFormat format;
        public int mipmapCount;
        public int faceCount;
        public bool readable;
        public int byteSize;
    }

    [MenuItem("Kayac/MakeTextureList")]
    public static void MakeTextureList()
    {
        var guids = AssetDatabase.FindAssets("t: texture");
        var items = new List<Item>();
        EditorUtility.DisplayProgressBar("テクスチャ検索中", null, 0f);
        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                EditorUtility.DisplayProgressBar("テクスチャ検索中", null, (float)i / (float)guids.Length);
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.StartsWith("Assets/", System.StringComparison.Ordinal))
                {
                    var item = GetInfo(path);
                    items.Add(item);
                }
            }
            EditorUtility.DisplayProgressBar("ソート中", null, 1f);
            items.Sort((a, b) => b.byteSize.CompareTo(a.byteSize));

            EditorUtility.DisplayProgressBar("書き込み中", null, 1f);
            var writer = new System.IO.StreamWriter("textureList.txt");
            long bytesSum = 0;
            foreach (var item in items)
            {
                writer.WriteLine(item.ToString());
                bytesSum += item.byteSize;
            }
            writer.Close();
            Debug.LogFormat("{0} textures. total size: {1} ({2}MB)", items.Count, bytesSum, ((float)bytesSum / (1024 * 1024)).ToString("F2"));
        }
        catch (System.Exception e) // どこかで例外こくとプログレスバーが出しっぱになって操作不能になるのでケアが必要
        {
            Debug.LogException(e);
        }
        EditorUtility.ClearProgressBar();
    }

    static Item GetInfo(string path)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture>(path);
        Item item;
        item.name = path.Remove(0, "Assets/".Length);
        item.width = texture.width;
        item.height = texture.height;
        item.readable = texture.isReadable;
        item.mipmapCount = 1;
        item.faceCount = 1;
        item.format = TextureFormat.ARGB32; // とりあえず仮定
        item.byteSize = 0;
        var tex2d = texture as Texture2D;
        if (tex2d != null)
        {
            item.mipmapCount = tex2d.mipmapCount;
            item.format = tex2d.format;
        }
        var texCube = texture as Cubemap;
        if (texCube != null)
        {
            item.mipmapCount = texCube.mipmapCount;
            item.faceCount = 6;
            item.format = texCube.format;
        }
        item.byteSize = CalcTextureByteSize(item.width, item.height, item.faceCount, item.mipmapCount, item.format);
        return item;
    }

    static int CalcTextureByteSize(int width, int height, int faceCount, int mipmapCount, TextureFormat format)
    {
        var pixelsPerFaceLevel = width * height;
        var bitsPerFace = pixelsPerFaceLevel;
        switch (format)
        {
            case TextureFormat.PVRTC_RGB2:
            case TextureFormat.PVRTC_RGBA2:
                bitsPerFace *= 2; break;
            case TextureFormat.BC4:
            case TextureFormat.DXT1:
            case TextureFormat.EAC_R:
            case TextureFormat.EAC_R_SIGNED:
            case TextureFormat.ETC2_RGB:
            case TextureFormat.ETC2_RGBA1:
            case TextureFormat.ETC_RGB4:
            case TextureFormat.PVRTC_RGB4:
            case TextureFormat.PVRTC_RGBA4:
                bitsPerFace *= 4; break;
            case TextureFormat.Alpha8:
            case TextureFormat.ASTC_RGBA_4x4:
            case TextureFormat.ASTC_RGB_4x4:
            case TextureFormat.BC5:
            case TextureFormat.BC7:
            case TextureFormat.DXT5:
            case TextureFormat.EAC_RG:
            case TextureFormat.EAC_RG_SIGNED:
            case TextureFormat.ETC2_RGBA8:
            case TextureFormat.R8:
                bitsPerFace *= 8; break;
            case TextureFormat.ARGB4444:
            case TextureFormat.BC6H:
            case TextureFormat.R16:
            case TextureFormat.RGB565:
            case TextureFormat.RGBA4444:
            case TextureFormat.RHalf:
                bitsPerFace *= 16; break;
            case TextureFormat.ARGB32:
            case TextureFormat.BGRA32:
            case TextureFormat.RFloat:
            case TextureFormat.RG16:
            case TextureFormat.RGB24: // どうせメモリの中で32bitにふくれるだろうからこの扱い
            case TextureFormat.RGB9e5Float:
            case TextureFormat.RGBA32:
            case TextureFormat.RGHalf:
                bitsPerFace *= 32; break;
            case TextureFormat.RGBAHalf:
            case TextureFormat.RGFloat:
                bitsPerFace *= 64; break;
            case TextureFormat.RGBAFloat:
                bitsPerFace *= 128; break;
            case TextureFormat.ASTC_RGBA_5x5:
            case TextureFormat.ASTC_RGB_5x5:
                bitsPerFace *= 128;
                bitsPerFace /= 5 * 5;
                break;
            case TextureFormat.ASTC_RGBA_6x6:
            case TextureFormat.ASTC_RGB_6x6:
                bitsPerFace *= 128;
                bitsPerFace /= 6 * 6;
                break;
            case TextureFormat.ASTC_RGBA_8x8:
            case TextureFormat.ASTC_RGB_8x8:
                bitsPerFace *= 128;
                bitsPerFace /= 8 * 8;
                break;
            case TextureFormat.ASTC_RGBA_10x10:
            case TextureFormat.ASTC_RGB_10x10:
                bitsPerFace *= 128;
                bitsPerFace /= 10 * 10;
                break;
            case TextureFormat.ASTC_RGBA_12x12:
            case TextureFormat.ASTC_RGB_12x12:
                bitsPerFace *= 128;
                bitsPerFace /= 12 * 12;
                break;
            default:
                bitsPerFace *= 32; break; // 雑
        }
        var bytesPerFace = bitsPerFace / 8;
        if (mipmapCount > 1) // 何枚あろうが大差ないので1.333倍固定でいいよね
        {
            bytesPerFace *= 4;
            bytesPerFace /= 3;
        }
        return bytesPerFace * faceCount;
    }
}
