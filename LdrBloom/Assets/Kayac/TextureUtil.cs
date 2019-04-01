using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kayac
{
	public static class TextureUtil
	{
		public enum FileType
		{
			Png,
			Jpeg, // 厳密にピクセル単位の正確性が必要ないならjpg推奨。容量が全然違う
		}

		public static IEnumerator CoGetScreenshot(
			CoRetVal<byte[]> retVal,
			bool withAlpha = true,
			int waitFrameCount = 0, // デバグUIを消すなどがすぐに済まない場合、ここにフレーム数を指定
			FileType fileType = FileType.Png)
		{
			for (int i = 0; i < waitFrameCount; i++)
			{
				yield return null;
			}
			yield return new WaitForEndOfFrame();

			var width = Screen.width;
			var height = Screen.height;
			var format = withAlpha ? TextureFormat.RGBA32 : TextureFormat.RGB24;
			var texture = new Texture2D(width, height, format, mipChain: false);
			texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);

			var bytes = ConvertReadableTextureToFile(texture, fileType);
			retVal.Succeed(bytes);
		}

		public static byte[] ConvertToFile(
			Texture texture,
			int mipLevel = 0,
			FileType fileType = FileType.Png)
		{
			var texture2d = texture as Texture2D;
			int w = System.Math.Max(1, texture.width >> mipLevel);
			int h = System.Math.Max(1, texture.height >> mipLevel);
#if UNITY_2018_1_OR_NEWER
			if ((texture2d == null) || !texture2d.isReadable || (mipLevel != 0)) // 2Dでない場合及び、そのまま読めない場合、レベル0でない場合
#else
			if (true) // 2018より前にはisReadableがなく判定できないため、常にRenderTextureを経由する
#endif
			{
				var renderTexture = texture as RenderTexture;
				// 来たのがRenderTextureでないならTexture2D。RenderTexture生成してそこにコピー
				if ((texture2d != null)  // 2DならRT経由
					|| ((renderTexture.format != RenderTextureFormat.ARGB32) && (renderTexture.format != RenderTextureFormat.BGRA32)) // 32bitRGBAでないなら、やはりRT経由
					|| (mipLevel != 0)) // 京セラS2にて、レベル1をSetRenderTargetしてReadPixelsするとcrashするので回避。ver2018.3.9
				{
					if ((texture2d != null) && (mipLevel >= texture2d.mipmapCount)) // そんなレベルはない
					{
						return null;
					}
					var filterModeBack = texture.filterMode;
					texture.filterMode = FilterMode.Point;
					renderTexture = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
					Graphics.Blit(texture, renderTexture); // TODO: これで本当に正しいmipLevelが完全に正しく抜けるのか?は不明
					texture.filterMode = filterModeBack;
					Graphics.SetRenderTarget(renderTexture, 0);
				}
				else if (renderTexture != null)
				{
					Graphics.SetRenderTarget(renderTexture, mipLevel);
				}
				// 読み出し用テクスチャを生成して差し換え
				texture2d = new Texture2D(w, h, TextureFormat.RGBA32, false);
				texture2d.ReadPixels(new Rect(0, 0, w, h), destX: 0, destY: 0);
			}
			return ConvertReadableTextureToFile(texture2d, fileType);
		}

		public static byte[][] ConvertAllLevelToFile(Texture texture, FileType fileType = FileType.Png)
		{
			int mipmapCount = GetMipmapCount(texture);
			var ret = new byte[mipmapCount][];
			for (int i = 0; i < mipmapCount; i++)
			{
				ret[i] = ConvertToFile(texture, i, fileType);
			}
			return ret;
		}

		public static int GetMipmapCount(Texture texture)
		{
			// ミップマップの枚数がTextureでは取れないので、実際の型にして調べる
			var texture2d = texture as Texture2D;
			var renderTexture = texture as RenderTexture;
			int ret = 1;
			if (texture2d != null)
			{
				ret = texture2d.mipmapCount;
			}
			else if (renderTexture != null)
			{
				if (renderTexture.useMipMap)
				{
					ret = CountMipLevel(renderTexture.width, renderTexture.height);
				}
			}
			return ret;
		}

		public static int ToPow2RoundUp(int x)
		{
			if (x == 0)
			{
				return 0;
			}
			x--;
			x = MakeAll1UnderMsb(x);
			return x + 1;
		}

		// ---------- 以下外から使いそうにないのでprivate -----------------

		static byte[] ConvertReadableTextureToFile(Texture2D readableTexture2d, FileType fileType)
		{
			byte[] bytes = null;
			if (fileType == FileType.Png)
			{
				bytes = ImageConversion.EncodeToPNG(readableTexture2d);
			}
			else if (fileType == FileType.Jpeg)
			{
				bytes = ImageConversion.EncodeToJPG(readableTexture2d);
			}
			return bytes;
		}

		static int CountMipLevel(int width, int height)
		{
			if ((width <= 0) || (height <= 0)) // どちらか0以下なら0返す
			{
				return 0;
			}
			int x = System.Math.Max(width, height); // 大きい方で見る
			x = MakeAll1UnderMsb(x); // 全部1にする。例えば8-15は15になる
			return CountBitOne(x); // ビット数を数えればそれがレベル数
		}

		static int CountBitOne(int x)
		{
			x = (x & 0x55555555) + ((x >> 1) & 0x55555555);
			x = (x & 0x33333333) + ((x >> 2) & 0x33333333);
			x = (x & 0x0f0f0f0f) + ((x >> 4) & 0x0f0f0f0f);
			x = (x & 0x00ff00ff) + ((x >> 8) & 0x00ff00ff);
			x = (x & 0x0000ffff) + ((x >> 16) & 0x0000ffff);
			return x;
		}

		static int MakeAll1UnderMsb(int x)
		{
			x |= x >> 1; // 上2bitが1になる
			x |= x >> 2; // 上4bitが1になる
			x |= x >> 4; // 上8bitが1になる
			x |= x >> 8; // 上16bitが1になる
			x |= x >> 16; // 上32bitが1になる
			return x;
		}
	}
}
