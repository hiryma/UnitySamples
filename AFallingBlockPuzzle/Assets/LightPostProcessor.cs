using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LightPostProcessor : MonoBehaviour
{
	[SerializeField]
	Shader _compositionShader;
	[SerializeField]
	Shader _extractionShader;
	[SerializeField]
	Shader _copyShader;
	[SerializeField]
	Shader _gaussianShader;
	[SerializeField]
	float _extractionThreshold;
	[SerializeField]
	Vector3 _colorOffset = new Vector3(0f, 0f, 0f);
	[SerializeField]
	Vector3 _colorScale = new Vector3(1f, 1f, 1f);
	[SerializeField]
	float _saturation = 1f;
	[SerializeField]
	int _maxGaussianLevelCount = 7;
	[SerializeField]
	float _bloomStrength = 0.01f;

	Material _compositionMaterial;
	Material _extractionMaterial;
	Material _copyMaterial;
	RenderTexture _level1;
	RenderTexture _level2;
	RenderTexture _level3;
	RenderTexture _level4;
	RenderTexture _level5;
	RenderTexture _prevSource;
	RenderTexture _sourceWithMip;
	RenderTexture _gaussA;
	RenderTexture _gaussB;
	int _gaussianStartLevel = 1;
	List<GaussRect> _gaussRects;

	void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		_maxGaussianLevelCount = System.Math.Min(_maxGaussianLevelCount, 7); // 最大7。シェーダ的な都合で。
		if (_copyMaterial == null)
		{
			_copyMaterial = new Material(_copyShader);
		}
		if (_extractionMaterial == null)
		{
			_extractionMaterial = new Material(_extractionShader);
		}
		if (_compositionMaterial == null)
		{
			_compositionMaterial = new Material(_compositionShader);
			SetColorTransform();
		}
		SetupRenderTargets(source);

		GL.PushMatrix();
		GL.LoadIdentity();
		GL.LoadOrtho();
		_copyMaterial.SetTexture("_MainTex", source);
		_copyMaterial.SetPass(0);
		int toWidth = source.width >> _gaussianStartLevel;
		int toHeight = source.height >> _gaussianStartLevel;
		int toX = (_sourceWithMip.width - toWidth) / 2; // 中央に配置する。端に置くと次のgaussianで末端がおかしくなる
		int toY = (_sourceWithMip.height - toHeight) / 2;
		Blit(
			source,
			0,
			0,
			source.width,
			source.height,
			_sourceWithMip,
			toX,
			toY,
			toWidth,
			toHeight);

		_sourceWithMip.filterMode = FilterMode.Point; // 後で換える
		_copyMaterial.SetTexture("_MainTex", _sourceWithMip);
		_copyMaterial.SetPass(0);

		// _gaussAの所定の場所へ各レベルをコピー(後でgaussしながらに変更する)
		for (int i = 0; i < _gaussRects.Count; i++)
		{
			var rect = _gaussRects[i];
			Blit(
				_sourceWithMip,
				toX,
				toY,
				toWidth,
				toHeight,
				_gaussA,
				rect.x,
				rect.y,
				rect.width,
				rect.height);
		}

		// _gaussA -> _gaussB
		_copyMaterial.SetTexture("_MainTex", _gaussA);
		_copyMaterial.SetPass(0);
		for (int i = 0; i < _gaussRects.Count; i++)
		{
			var rect = _gaussRects[i];
			Blit(
				_gaussA,
				_gaussB,
				rect.x,
				rect.y,
				rect.width,
				rect.height);
		}

		Composite(source, destination);

		GL.PopMatrix();
		if (Time.frameCount == 100)
		{
			StartCoroutine(CoSaveRenderTargets());
			Debug.Log("Save");
		}
		/*
				Graphics.Blit(source, _level1, _extractionMaterial);
				Graphics.Blit(_level1, _level2);
				Graphics.Blit(_level2, _level3);
				Graphics.Blit(_level3, _level4);
				Graphics.Blit(_level4, _level5);
				Graphics.Blit(source, destination, _compositionMaterial);
		*/
	}

	void Composite(RenderTexture source, RenderTexture destination)
	{
		_compositionMaterial.SetTexture("_MainTex", source);
		_compositionMaterial.SetPass(0);
		RenderTexture.active = destination;
		GL.Begin(GL.QUADS);
		float strength = _bloomStrength;
		for (int i = 0; i < _gaussRects.Count; i++)
		{
			var rect = _gaussRects[i];
			float u0 = (float)rect.x / (float)_gaussB.width;
			float v0 = (float)rect.y / (float)_gaussB.height;
			GL.MultiTexCoord3(1 + i, u0, v0, strength);
			strength *= 2f;
		}
		GL.MultiTexCoord2(0, 0f, 0f);
		GL.Vertex3(0f, 0f, 0f);
		strength = _bloomStrength;
		for (int i = 0; i < _gaussRects.Count; i++)
		{
			var rect = _gaussRects[i];
			float u0 = (float)rect.x / (float)_gaussB.width;
			float v1 = (float)(rect.y + rect.height) / (float)_gaussB.height;
			GL.MultiTexCoord3(1 + i, u0, v1, strength);
			strength *= 2f;
		}
		GL.MultiTexCoord2(0, 0f, 1f);
		GL.Vertex3(0f, 1f, 0f);
		strength = _bloomStrength;
		for (int i = 0; i < _gaussRects.Count; i++)
		{
			var rect = _gaussRects[i];
			float u1 = (float)(rect.x + rect.width) / (float)_gaussB.width;
			float v1 = (float)(rect.y + rect.height) / (float)_gaussB.height;
			GL.MultiTexCoord3(1 + i, u1, v1, strength);
			strength *= 2f;
		}
		GL.MultiTexCoord2(0, 1f, 1f);
		GL.Vertex3(1f, 1f, 0f);
		strength = _bloomStrength;
		for (int i = 0; i < _gaussRects.Count; i++)
		{
			var rect = _gaussRects[i];
			float u1 = (float)(rect.x + rect.width) / (float)_gaussB.width;
			float v0 = (float)rect.y / (float)_gaussB.height;
			GL.MultiTexCoord3(1 + i, u1, v0, strength);
			strength *= 2f;
		}
		GL.MultiTexCoord2(0, 1f, 0f);
		GL.Vertex3(1f, 0f, 0f);
		GL.End();
	}

	// 拡縮なし、同位置版
	void Blit(
		RenderTexture from,
		RenderTexture to,
		int x,
		int y,
		int width,
		int height)
	{
		Blit(from, x, y, width, height, to, x, y, width, height);
	}

	void Blit(
		RenderTexture from,
		int fromX,
		int fromY,
		int fromWidth,
		int fromHeight,
		RenderTexture to,
		int toX,
		int toY,
		int toWidth,
		int toHeight)
	{
		float x0 = (float)toX / (float)to.width;
		float x1 = (float)(toX + toWidth) / (float)to.width;
		float y0 = (float)toY / (float)to.height;
		float y1 = (float)(toY + toHeight) / (float)to.height;

		float u0 = (float)fromX / (float)from.width;
		float u1 = (float)(fromX + fromWidth) / (float)from.width;
		float v0 = (float)fromY / (float)from.height;
		float v1 = (float)(fromY + fromHeight) / (float)from.height;
		//Debug.Log(from.name + " -> " + to.name + " " + x0 + "," + y0 + " - " + x1 + "," + y1 + " uv: " + u0 + "," + v0 + " - " + u1 + "," + v1 + " size: " + toWidth + "x" + toHeight + " / " + to.width + "x" + to.height);

		if (RenderTexture.active != to)
		{
			RenderTexture.active = to;
		}
		GL.Begin(GL.QUADS);
		GL.TexCoord2(u0, v0);
		GL.Vertex3(x0, y0, 0f);
		GL.TexCoord2(u0, v1);
		GL.Vertex3(x0, y1, 0f);
		GL.TexCoord2(u1, v1);
		GL.Vertex3(x1, y1, 0f);
		GL.TexCoord2(u1, v0);
		GL.Vertex3(x1, y0, 0f);
		GL.End();
	}

	public float extractionThreshold
	{
		set
		{
			_extractionThreshold = value;
			if (_extractionMaterial != null)
			{
				_extractionMaterial.SetFloat("_Threshold", _extractionThreshold);
			}
		}
	}

	public void SetColorFilter(Vector3 colorOffset, Vector3 colorScale, float saturation)
	{
		_colorOffset = colorOffset;
		_colorScale = colorScale;
		_saturation = saturation;
		SetColorTransform();
	}

	static int ToPow2RoundUp(int x)
	{
		if (x == 0)
		{
			return 0;
		}
		x--;
		x |= x >> 1; // 上2bitが1になる
		x |= x >> 2; // 上4bitが1になる
		x |= x >> 4; // 上8bitが1になる
		x |= x >> 8; // 上16bitが1になる
		x |= x >> 16; // 上32bitが1になる
		return x + 1;
	}

	void SetupRenderTargets(RenderTexture source)
	{
		if ((source != null) && (_prevSource != null) && (source.width == _prevSource.width) && (source.height == _prevSource.height))
		{
			return;
		}
		var format = RenderTextureFormat.Default;
		if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB2101010))
		{
			format = RenderTextureFormat.ARGB2101010;
		}
		int topGaussWidth = source.width >> _gaussianStartLevel;
		int topGaussHeight = source.height >> _gaussianStartLevel;
		_sourceWithMip = new RenderTexture(
			ToPow2RoundUp(topGaussWidth), //2羃でないとミップマップを作れる保証がないので2羃
			ToPow2RoundUp(topGaussHeight),
			0,
			format);
		_sourceWithMip.name = "sourceWithMip";
		_sourceWithMip.useMipMap = true;
		_gaussRects = new List<GaussRect>();
		int gaussWidth, gaussHeight;
		CalcGaussianRenderTextureArrangement(
			out gaussWidth,
			out gaussHeight,
			_gaussRects,
			topGaussWidth,
			topGaussHeight,
			16, // TODO: 調整可能にするか?
			_maxGaussianLevelCount);
		_gaussA = new RenderTexture(gaussWidth, gaussHeight, 0, format);
		_gaussA.name = "gaussA";
		_gaussB = new RenderTexture(gaussWidth, gaussHeight, 0, format);
		_gaussB.name = "gaussB";

		_level1 = new RenderTexture(source.width >> 1, source.height >> 1, 0);
		_level2 = new RenderTexture(source.width >> 2, source.height >> 2, 0);
		_level3 = new RenderTexture(source.width >> 3, source.height >> 3, 0);
		_level4 = new RenderTexture(source.width >> 4, source.height >> 4, 0);
		_level5 = new RenderTexture(source.width >> 5, source.height >> 5, 0);
		_compositionMaterial.SetTexture("_Level1Tex", _level1);
		_compositionMaterial.SetTexture("_Level2Tex", _level2);
		_compositionMaterial.SetTexture("_Level3Tex", _level3);
		_compositionMaterial.SetTexture("_Level4Tex", _level4);
		_compositionMaterial.SetTexture("_Level5Tex", _level5);
		_compositionMaterial.SetTexture("_GaussTex", _gaussB);
		_prevSource = source;
	}

	struct GaussRect
	{
		public GaussRect(int x, int y, int w, int h)
		{
			this.x = x;
			this.y = y;
			this.width = w;
			this.height = h;
		}
		public int x, y, width, height;
	}

	void CalcGaussianRenderTextureArrangement(
		out int widthOut,
		out int heightOut,
		List<GaussRect> gaussRects,
		int width,
		int height,
		int padding,
		int levelCount)
	{
		bool isRight = (height > width); // 縦長なら右配置から始める
		int x = padding;
		int y = padding;
		int maxX = 0;
		int maxY = 0;
		while ((levelCount > 0) && (width > 0) && (height > 0))
		{
			gaussRects.Add(new GaussRect(x, y, width, height));
			maxX = System.Math.Max(maxX, x + width + padding);
			maxY = System.Math.Max(maxY, y + height + padding);
			if (isRight)
			{
				x += width + padding;
			}
			else
			{
				y += height + padding;
			}
			isRight = !isRight;
			width >>= 1;
			height >>= 1;
			levelCount--;
		}
		widthOut = maxX;
		heightOut = maxY;
	}

	void SetColorTransform()
	{
		if (_compositionMaterial == null)
		{
			return;
		}
		var scaleOffsetTransform = Matrix4x4.Translate(new Vector3(_colorOffset.x, _colorOffset.y, _colorOffset.z))
			* Matrix4x4.Scale(new Vector3(_colorScale.x, _colorScale.y, _colorScale.z));

		var toYuv = new Matrix4x4();
		toYuv.SetRow(0, new Vector4(0.299f, 0.587f, 0.114f, 0f));
		toYuv.SetRow(1, new Vector4(-0.169f, -0.331f, 0.5f, 0f));
		toYuv.SetRow(2, new Vector4(0.5f, -0.419f, -0.081f, 0f));
		toYuv.SetRow(3, new Vector4(0f, 0f, 0f, 1f));

		var saturationTransform = Matrix4x4.Scale(new Vector3(1f, _saturation, _saturation));

		var fromYuv = new Matrix4x4();
		fromYuv.SetRow(0, new Vector4(1f, 0f, 1.402f, 0f));
		fromYuv.SetRow(1, new Vector4(1f, -0.344f, -0.714f, 0f));
		fromYuv.SetRow(2, new Vector4(1f, 1.772f, 0f, 0f));
		fromYuv.SetRow(3, new Vector4(0f, 0f, 0f, 1f));

		Matrix4x4 t = scaleOffsetTransform * fromYuv * saturationTransform * toYuv;
		_compositionMaterial.SetVector("_ColorTransformR", t.GetRow(0));
		_compositionMaterial.SetVector("_ColorTransformG", t.GetRow(1));
		_compositionMaterial.SetVector("_ColorTransformB", t.GetRow(2));
	}

	IEnumerator CoSaveRenderTargets()
	{
		yield return new WaitForEndOfFrame();

		int w = _sourceWithMip.width;
		int h = _sourceWithMip.height;
		int level = 0;
		while (true)
		{
			SaveRenderTarget(_sourceWithMip, level, "sourceWithMip" + level + ".png");
			w >>= 1;
			h >>= 1;
			if ((w < 1) || (h < 1))
			{
				break;
			}
			level++;
		}
		SaveRenderTarget(_gaussA, 0, "gaussA.png");
		SaveRenderTarget(_gaussB, 0, "gaussB.png");
	}

	void SaveRenderTarget(RenderTexture source, int mipLevel, string path)
	{
		int w = source.width >> mipLevel;
		int h = source.height >> mipLevel;
		if ((w == 0) || (h == 0))
		{
			Debug.Assert(false, "no such level. mipLevel: " + mipLevel + " size: " + source.width + "x" + source.height);
		}
		var tmpRt = new RenderTexture(source.width >> mipLevel, source.height >> mipLevel, 0);

		// フィルタを一時的にPointに
		var filterBackup = source.filterMode;
		source.filterMode = FilterMode.Point;
		Graphics.Blit(source, tmpRt);
		source.filterMode = filterBackup;

		// 読み出し用テクスチャを生成して差し換え
		var tex2d = new Texture2D(tmpRt.width, tmpRt.height, TextureFormat.RGBA32, false);
		RenderTexture.active = tmpRt;
		tex2d.ReadPixels(new Rect(0, 0, tex2d.width, tex2d.height), 0, 0);
		var pngBytes = tex2d.EncodeToPNG();
		System.IO.File.WriteAllBytes(path, pngBytes);
	}

#if UNITY_EDITOR
	void OnValidate()
	{
		this.extractionThreshold = _extractionThreshold;
		SetColorTransform();
	}
#endif
}
