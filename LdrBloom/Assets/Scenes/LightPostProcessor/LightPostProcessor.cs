using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LightPostProcessor : MonoBehaviour
{
	[SerializeField]
	Shader _extractionShader;
	[SerializeField]
	Shader _convolutionShader;
	[SerializeField]
	Shader _compositionShader;

	[SerializeField]
	Vector3 _colorOffset = new Vector3(0f, 0f, 0f);
	[SerializeField]
	Vector3 _colorScale = new Vector3(1f, 1f, 1f);
	[SerializeField]
	float _saturation = 1f;
	[SerializeField]
	float _bloomPixelThreshold = 0;
	[SerializeField]
	int _bloomStartLevel = 2;
	[SerializeField]
	int _maxBloomLevelCount = 7;
	[SerializeField]
	int _minBloomLevelSize = 16;
	[SerializeField]
	float _bloomStrength = 1f;
	[SerializeField]
	float _bloomStrengthMultiplier = 2f;
	[SerializeField]
	float _bloomSigmaInPixel = 3f;

	Material _extractionMaterial;
	Material _convolutionMaterial;
	Material _compositionMaterial;
	RenderTexture _prevSource;
	RenderTexture _sourceWithMip;
	RenderTexture _bloomX;
	RenderTexture _bloomXY;
	List<BloomRect> _bloomRects;
	BloomSample[] _bloomSamples;
	bool _first = true;

	void Awake()
	{
		_maxBloomLevelCount = System.Math.Min(_maxBloomLevelCount, 7); // 最大7。シェーダ的な都合で。
		if (_convolutionMaterial == null)
		{
			_convolutionMaterial = new Material(_convolutionShader);
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
		_bloomSamples = new BloomSample[8];
		for (int i = 0; i < 8; i++)
		{
			var sample = _bloomSamples[i];
			sample.shaderPropertyId = Shader.PropertyToID("_Sample" + i);
			_bloomSamples[i] = sample;
		}
	}

#if UNITY_EDITOR
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.F1))
		{
			StartCoroutine(CoSaveRenderTargets());
			Debug.Log("Save");
		}
	}
#endif

	void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		SetupRenderTargets(source);

		GL.PushMatrix();
		GL.LoadIdentity();
		GL.LoadOrtho();

		/* 輝度抽出
		color' = (color - threshold) / (1 - threshold)
		とするのだが、高速に計算するために式を展開して積和の形にしておく
		  = (1/(1-threshold)) * color + (-threshold)/(1-threshold)
		*/
		_extractionMaterial.SetTexture("_MainTex", source);
		var colorTransform = new Vector4(
			1f / (1f - _bloomPixelThreshold), // 乗算項
			-_bloomPixelThreshold / (1f - _bloomPixelThreshold), // 加算項
			Mathf.Pow(2f, _bloomPixelThreshold),
			0f);
		_extractionMaterial.SetVector("_ColorTransform", colorTransform);
		_extractionMaterial.SetPass(0);
		int toWidth = source.width >> _bloomStartLevel;
		int toHeight = source.height >> _bloomStartLevel;
		int toX = (_sourceWithMip.width - toWidth) / 2; // 中央に配置する。端に置くと次のgaussianで末端がおかしくなる
		int toY = (_sourceWithMip.height - toHeight) / 2;
		RenderTexture.active = _sourceWithMip;
		_sourceWithMip.DiscardContents();
		bool first = _first;
		if (first)
		{
			GL.Clear(false, true, new Color(0f, 0f, 0f, 1f));
		}
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

		// 係数再計算
		CalcGaussianSamples(_bloomSigmaInPixel);
		// _gaussAの所定の場所へ_sourceWithMipの各レベルからX方向ガウシアンブラー
		_convolutionMaterial.SetTexture("_MainTex", _sourceWithMip);
		RenderTexture.active = _bloomX;
		_bloomX.DiscardContents();
		if (first)
		{
			GL.Clear(false, true, new Color(0f, 0f, 0f, 1f));
		}
		int w = _sourceWithMip.width; // 各ミップレベルの幅
		for (int rectIndex = 0; rectIndex < _bloomRects.Count; rectIndex++)
		{
			for (int sampleIndex = 0; sampleIndex < _bloomSamples.Length; sampleIndex++)
			{
				var sample = _bloomSamples[sampleIndex];
				_convolutionMaterial.SetVector(
					sample.shaderPropertyId,
					new Vector4(sample.offset / (float)w, 0f, sample.weight, 0f));
//Debug.Log(sampleIndex + " " + w + " " + sample.offset + " " + (sample.offset / (float)w) + " " + ((float)w / sample.offset) + " " + sample.weight);
			}
			_convolutionMaterial.SetPass(0);

			var rect = _bloomRects[rectIndex];
			Blit(
				_sourceWithMip,
				toX,
				toY,
				toWidth,
				toHeight,
				_bloomX,
				rect.x,
				rect.y,
				rect.width,
				rect.height);
			w /= 2;
		}

		// _bloomX -> _bloomXY Y方向ガウシアンブラー
		RenderTexture.active = _bloomXY;
		_bloomXY.DiscardContents();
		if (first)
		{
			GL.Clear(false, true, new Color(0f, 0f, 0f, 1f));
		}
		_convolutionMaterial.SetTexture("_MainTex", _bloomX);
		for (int rectIndex = 0; rectIndex < _bloomRects.Count; rectIndex++)
		{
			for (int sampleIndex = 0; sampleIndex < _bloomSamples.Length; sampleIndex++)
			{
				var sample = _bloomSamples[sampleIndex];
				_convolutionMaterial.SetVector(
					sample.shaderPropertyId,
					new Vector4(0f, sample.offset / (float)_bloomX.height, sample.weight, 0f));
			}
			_convolutionMaterial.SetPass(0);

			var rect = _bloomRects[rectIndex];
			Blit(
				_bloomX,
				_bloomXY,
				rect.x,
				rect.y,
				rect.width,
				rect.height);
		}

		// 最終合成
		Composite(source, destination);
		GL.PopMatrix();
		_first = false;
	}

	void Composite(RenderTexture source, RenderTexture destination)
	{
		// カラーフィルターの有効無効設定
		if ((_colorOffset == Vector3.zero)
			&& (_colorScale == Vector3.one)
			&& (_saturation == 1f))
		{
			_compositionMaterial.DisableKeyword("COLOR_FILTER");
		}
		else
		{
			_compositionMaterial.EnableKeyword("COLOR_FILTER");
		}
		_compositionMaterial.SetTexture("_MainTex", source);
		// 強度定数を計算する
		float s = 1f;
		float sSum = 0f;
		for (int i = 0; i < _bloomRects.Count; i++)
		{
			sSum += s;
			s *= _bloomStrengthMultiplier;
		}
		float strengthBase = _bloomStrength / sSum;
		float strength = strengthBase;
		for (int i = 0; i < _bloomRects.Count; i++)
		{
			var rect = _bloomRects[i];
			_compositionMaterial.SetFloat(rect.weightShaderPropertyId, strength);
			Vector4 uvTransform;
			uvTransform.x = (float)rect.width / (float)_bloomXY.width;
			uvTransform.y = (float)rect.height / (float)_bloomXY.height;
			uvTransform.z = (float)rect.x / (float)_bloomXY.width;
			uvTransform.w = (float)rect.y / (float)_bloomXY.height;
			_compositionMaterial.SetVector(rect.uvTransformShaderPropertyId, uvTransform);
			strength *= _bloomStrengthMultiplier;
		}
		_compositionMaterial.SetPass(0);

		RenderTexture.active = destination;
		GL.Begin(GL.QUADS);
		GL.TexCoord2(0f, 0f);
		GL.Vertex3(0f, 0f, 0f);
		GL.TexCoord2(0f, 1f);
		GL.Vertex3(0f, 1f, 0f);
		GL.TexCoord2(1f, 1f);
		GL.Vertex3(1f, 1f, 0f);
		GL.TexCoord2(1f, 0f);
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
			to.DiscardContents(); // 刺したら必ずDiscard
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

	public float bloomPixelThreshold
	{
		set
		{
			_bloomPixelThreshold = value;
		}
	}

	public float bloomStrength
	{
		set
		{
			_bloomStrength = value;
		}
	}

	public void SetColorFilter(Vector3 colorOffset, Vector3 colorScale, float saturation)
	{
		_colorOffset = colorOffset;
		_colorScale = colorScale;
		_saturation = saturation;
		SetColorTransform();
	}

	void SetupRenderTargets(RenderTexture source)
	{
		if (_prevSource != null)
		{
			if ((source != null)
				&& (source.width == _prevSource.width)
				&& (source.height == _prevSource.height))
			{
				return;
			}
		}
		if (_maxBloomLevelCount == 0)
		{
			return;
		}
#if UNITY_EDITOR
		_bloomStartLevelOnSetup = _bloomStartLevel;
		_maxBloomLevelCountOnSetup = _maxBloomLevelCount;
		_minBloomLevelSizeOnSetup = _minBloomLevelSize;
#endif
		var format = RenderTextureFormat.Default;
		if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB2101010))
		{
			format = RenderTextureFormat.ARGB2101010;
		}
		int topBloomWidth = source.width >> _bloomStartLevel;
		int topBloomHeight = source.height >> _bloomStartLevel;
		_sourceWithMip = new RenderTexture(
			ToPow2RoundUp(topBloomWidth), //2羃でないとミップマップを作れる保証がないので2羃
			ToPow2RoundUp(topBloomHeight),
			0,
			format);
		_sourceWithMip.name = "sourceWithMip";
		_sourceWithMip.useMipMap = true;
		_sourceWithMip.filterMode = FilterMode.Bilinear;
		_bloomRects = new List<BloomRect>();
		int bloomWidth, bloomHeight;
		CalcBloomRenderTextureArrangement(
			out bloomWidth,
			out bloomHeight,
			_bloomRects,
			topBloomWidth,
			topBloomHeight,
			16, // TODO: 調整可能にするか?
			_maxBloomLevelCount);
		if ((_bloomRects.Count & 0x4) != 0)
		{
			_compositionMaterial.EnableKeyword("BLOOM_4");
		}
		else
		{
			_compositionMaterial.DisableKeyword("BLOOM_4");
		}
		if ((_bloomRects.Count & 0x2) != 0)
		{
			_compositionMaterial.EnableKeyword("BLOOM_2");
		}
		else
		{
			_compositionMaterial.DisableKeyword("BLOOM_2");
		}
		if ((_bloomRects.Count & 0x1) != 0)
		{
			_compositionMaterial.EnableKeyword("BLOOM_1");
		}
		else
		{
			_compositionMaterial.DisableKeyword("BLOOM_1");
		}
		Debug.Log("LightPostProcessor.SetupRenderTargets(): create RTs. " + _sourceWithMip.width + "x" + _sourceWithMip.height + " + " + bloomWidth + "x" + bloomHeight + " levels:" + _bloomRects.Count);
		_bloomX = new RenderTexture(bloomWidth, bloomHeight, 0, format);
		_bloomX.name = "bloomX";
		_bloomX.filterMode = FilterMode.Bilinear;
		_bloomXY = new RenderTexture(bloomWidth, bloomHeight, 0, format);
		_bloomXY.name = "bloomXY";
		_bloomXY.filterMode = FilterMode.Bilinear;
		_compositionMaterial.SetTexture("_BloomTex", _bloomXY);

		_prevSource = source;
	}

	void CalcGaussianSamples(float sigma)
	{
		// 0: 0と1
		// 1: -1と0
		// 2: 2と3
		// 3: -3と-2
		// 4: 4と5
		// 5: -5と-4
		// 6: 6と7
		// 7: -7と-6
		float w0 = Gauss(sigma, 0f) * 0.5f; // 2回参照されるので半分
		float w1 = Gauss(sigma, 1f);
		float w2 = Gauss(sigma, 2f);
		float w3 = Gauss(sigma, 3f);
		float w4 = Gauss(sigma, 4f);
		float w5 = Gauss(sigma, 5f);
		float w6 = Gauss(sigma, 6f);
		float w7 = Gauss(sigma, 7f);

		float w01 = w0 + w1;
		float x01 = 0f + (w1 / w01);
		float w23 = w2 + w3;
		float x23 = 2f + (w3 / w23);
		float w45 = w4 + w5;
		float x45 = 4f + (w5 / w45);
		float w67 = w6 + w7;
		float x67 = 6f + (w7 / w67);
		float wSum = (w01 + w23 + w45 + w67) * 2f;
		// 和が1になるように正規化
		w01 /= wSum;
		w23 /= wSum;
		w45 /= wSum;
		w67 /= wSum;
//Debug.Log(x01 + " " + w01);
//Debug.Log(x23 + " " + w23);
//Debug.Log(x45 + " " + w45);
//Debug.Log(x67 + " " + w67);

		SetGaussSample(0, x01, w01);
		SetGaussSample(1, -x01, w01);
		SetGaussSample(2, x23, w23);
		SetGaussSample(3, -x23, w23);
		SetGaussSample(4, x45, w45);
		SetGaussSample(5, -x45, w45);
		SetGaussSample(6, x67, w67);
		SetGaussSample(7, -x67, w67);
	}

	void SetGaussSample(int index, float offset, float weight)
	{
		var sample = _bloomSamples[index];
		sample.offset = offset;
		sample.weight = weight;
		_bloomSamples[index] = sample;
	}

	float Gauss(float sigma, float x)
	{
		float sigma2 = sigma * sigma;
		return Mathf.Exp(-(x * x) / (2f * sigma2));
	}

	struct BloomSample
	{
		public float offset;
		public float weight;
		public int shaderPropertyId;
	}

	struct BloomRect
	{
		public int x;
		public int y;
		public int width;
		public int height;
		public int uvTransformShaderPropertyId;
		public int weightShaderPropertyId;
	}

	void CalcBloomRenderTextureArrangement(
		out int widthOut,
		out int heightOut,
		List<BloomRect> rects,
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
			BloomRect rect;
			rect.x = x;
			rect.y = y;
			rect.width = width;
			rect.height = height;
			rect.uvTransformShaderPropertyId = Shader.PropertyToID("_BloomUvTransform" + rects.Count);
			rect.weightShaderPropertyId = Shader.PropertyToID("_BloomWeight" + rects.Count);
			rects.Add(rect);
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


			// 4で割れなくなるとサンプリング点がズレて汚なくなるので抜ける。TODO: 理由を明らかにせよ。奇数になるとどうも汚ない。
			if (((width % 4) != 0) || ((height % 4) != 0))
			{
//				break;
			}
			width /= 2;
			height /= 2;
			// 指定サイズ以下なら作らない
			if ((width < _minBloomLevelSize) || (height < _minBloomLevelSize))
			{
				break;
			}


			levelCount--;
		}
		widthOut = maxX;
		heightOut = maxY;
		_first = true;
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

#if UNITY_EDITOR
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
		SaveRenderTarget(_bloomX, 0, "bloomX.png");
		SaveRenderTarget(_bloomXY, 0, "bloomXY.png");
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

	int _bloomStartLevelOnSetup;
	int _maxBloomLevelCountOnSetup;
	int _minBloomLevelSizeOnSetup;

	void OnValidate()
	{
		if ((_bloomStartLevelOnSetup != _bloomStartLevel)
			|| (_maxBloomLevelCountOnSetup != _maxBloomLevelCount)
			|| (_minBloomLevelSizeOnSetup != _minBloomLevelSize))
		{
			_bloomStartLevelOnSetup = _bloomStartLevel;
			_maxBloomLevelCountOnSetup = _maxBloomLevelCount;
			_minBloomLevelSizeOnSetup = _minBloomLevelSize;
			_prevSource = null; // これで再初期化が走る
		}
		SetColorTransform();
	}
#endif
}
