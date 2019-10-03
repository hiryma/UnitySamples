using System.Collections.Generic;
using UnityEngine;

namespace Kayac
{
    public class LightPostProcessor : MonoBehaviour
    {
        [SerializeField] LightPostProcessorAsset asset;

        [SerializeField, Header("Color Filter Settings")] Vector3 colorOffset = new Vector3(0f, 0f, 0f);
        [SerializeField] Vector3 colorScale = new Vector3(1f, 1f, 1f);
        [SerializeField] float saturation = 1f;

        [SerializeField, Header("Bloom Settings")] float bloomPixelThreshold = 0;
        [SerializeField] float bloomStrength = 1f;
        [SerializeField] float bloomStrengthMultiplier = 2f;

        [SerializeField, Header("Advanced Settings")] int bloomStartLevel = 2;
        [SerializeField] int bloomCombineStartLevel = 1;
        [SerializeField] int maxBloomLevelCount = 7;
        [SerializeField] int minBloomLevelSize = 16;
        [SerializeField] float bloomSigmaInPixel = 3f;

        Material extractionMaterial;
        Material blurMaterial;
        Material combineMaterial;
        Material compositionMaterial;
        RenderTexture prevSource;
        RenderTexture brightness;
        RenderTexture bloomX;
        RenderTexture bloomXY;
        RenderTexture bloomCombined;
        List<BloomRect> bloomRects;
        BloomSample[] bloomSamples;
        bool first = true; // クリアが必要かどうかのために初期化直後かどうかを記録
        readonly Color clearColor = new Color(0f, 0f, 0f, 1f);

        public float BloomPixelThreshold { set { bloomPixelThreshold = value; } }
        public float BloomStrength { set { bloomStrength = value; } }

        public int BloomCombineStartLevel
        {
            get
            {
                return bloomCombineStartLevel;
            }
            set
            {
                bloomCombineStartLevel = value;
            }
        }

        public IEnumerable<RenderTexture> EnumerateRenderTexturesForDebug()
        {
            yield return brightness;
            yield return bloomX;
            yield return bloomXY;
            yield return bloomCombined;
        }

        void Start()
        {
            if (blurMaterial == null)
            {
                blurMaterial = new Material(asset.GaussianBlurShader);
            }
            if (extractionMaterial == null)
            {
                extractionMaterial = new Material(asset.BrightnessExtractionShader);
            }
            if (combineMaterial == null)
            {
                combineMaterial = new Material(asset.BloomCombineShader);
            }
            if (compositionMaterial == null)
            {
                compositionMaterial = new Material(asset.CompositionShader);
                SetColorTransform();
            }
            bloomSamples = new BloomSample[4];
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            maxBloomLevelCount = System.Math.Min(maxBloomLevelCount, 7); // 最大7。シェーダ的な都合で。

            SetupRenderTargets(source);

            GL.PushMatrix();
            GL.LoadIdentity();
            GL.LoadOrtho();

            bool clear = first;
            int brightnessNetWidth = source.width >> bloomStartLevel;
            int brightnessNetHeight = source.height >> bloomStartLevel;
            int brightnessOffsetX = (brightness.width - brightnessNetWidth) / 2; // 中央に配置する。端に置くと次のgaussianで末端がおかしくなる
            int brightnessOffsetY = (brightness.height - brightnessNetHeight) / 2;
            ExtractBrightness(
                source,
                brightnessOffsetX,
                brightnessOffsetY,
                brightnessNetWidth,
                brightnessNetHeight,
                clear: true); // とりあえずクリア。TODO: 初回のみで良いGPUでは無駄にクリアしたくない
                              // 係数再計算
            CalcGaussianSamples(bloomSigmaInPixel);
            BlurX( // _gaussAの所定の場所へ_brightnessの各レベルからX方向ガウシアンブラー
                brightnessOffsetX,
                brightnessOffsetY,
                brightnessNetWidth,
                brightnessNetHeight);
            BlurY(); // _bloomX -> _bloomXY Y方向ガウシアンブラー
            if (bloomCombineStartLevel < (bloomRects.Count + bloomStartLevel))
            {
                CombineBloom();
            }
            Composite(source, destination); // 最終合成
            GL.PopMatrix();
            first = false;
        }

        void ExtractBrightness(
            RenderTexture source,
            int brightnessOffsetX,
            int brightnessOffsetY,
            int brightnessNetWidth,
            int brightnessNetHeight,
            bool clear)
        {
            /* 輝度抽出
			color' = (color - threshold) / (1 - threshold)
			とするのだが、高速に計算するために式を展開して積和の形にしておく
			  = (1/(1-threshold)) * color + (-threshold)/(1-threshold)
			*/
            extractionMaterial.SetTexture("_MainTex", source);
            if (bloomPixelThreshold <= 0f)
            {
                extractionMaterial.EnableKeyword("PASS_THROUGH");
            }
            else
            {
                extractionMaterial.DisableKeyword("PASS_THROUGH");
                var colorTransform = new Vector4(
                    1f / (1f - bloomPixelThreshold), // 乗算項
                    -bloomPixelThreshold / (1f - bloomPixelThreshold), // 加算項
                    0f,
                    0f);
                extractionMaterial.SetVector("_ColorTransform", colorTransform);
            }
            extractionMaterial.SetPass(0);
            Blit(
                source,
                0,
                0,
                source.width,
                source.height,
                brightness,
                brightnessOffsetX,
                brightnessOffsetY,
                brightnessNetWidth,
                brightnessNetHeight,
                clear,
                clearColor);
        }

        void BlurX(
            int brightnessOffsetX,
            int brightnessOffsetY,
            int brightnessNetWidth,
            int brightnessNetHeight)
        {
            // _gaussAの所定の場所へ_brightnessの各レベルからX方向ガウシアンブラー
            brightness.filterMode = FilterMode.Bilinear; // バイリニアが必要
            blurMaterial.SetTexture("_MainTex", brightness);
            blurMaterial.SetFloat(
                "_InvertOffsetScale01",
                (bloomSamples[0].offset * 2f) / Mathf.Abs(bloomSamples[0].offset - bloomSamples[1].offset));
            //Debug.Log((_bloomSamples[0].offset * 2f) / Mathf.Abs(_bloomSamples[0].offset - _bloomSamples[1].offset));
            blurMaterial.SetPass(0);
            bloomX.DiscardContents();
            Graphics.SetRenderTarget(bloomX);
            GL.Clear(false, true, clearColor);
            int w = brightness.width; // 各ミップレベルの幅
            GL.Begin(GL.QUADS);
            for (int i = 0; i < bloomRects.Count; i++)
            {
                var rect = bloomRects[i];
                AddBlurQuads(
                    brightness,
                    brightnessOffsetX,
                    brightnessOffsetY,
                    brightnessNetWidth,
                    brightnessNetHeight,
                    1f / (float)w,
                    bloomX,
                    rect.x,
                    rect.y,
                    rect.width,
                    rect.height,
                    forX: true);
                w /= 2;
            }
            GL.End();
        }

        void BlurY()
        {
            bloomXY.DiscardContents();
            Graphics.SetRenderTarget(bloomXY);
            GL.Clear(false, true, clearColor);
            bloomX.filterMode = FilterMode.Bilinear; // バイリニアが必要
            blurMaterial.SetTexture("_MainTex", bloomX);
            blurMaterial.SetPass(0);
            GL.Begin(GL.QUADS);
            for (int i = 0; i < bloomRects.Count; i++)
            {
                var rect = bloomRects[i];
                AddBlurQuads(
                    bloomX,
                    rect.x,
                    rect.y,
                    rect.width,
                    rect.height,
                    1f / (float)bloomX.height,
                    bloomXY,
                    rect.x,
                    rect.y,
                    rect.width,
                    rect.height,
                    forX: false);
            }
            GL.End();
        }

        // _bloomXYの各解像度を事前に縮小バッファ上で合成して、最終段を高速化する
        void CombineBloom()
        {
            bloomCombined.DiscardContents();
            Graphics.SetRenderTarget(bloomCombined);
            GL.Clear(false, true, clearColor);
            bloomXY.filterMode = FilterMode.Bilinear;

            int combinePassCount = (bloomRects.Count + bloomStartLevel) - bloomCombineStartLevel;
            combinePassCount = Mathf.Clamp(combinePassCount, 0, bloomRects.Count);
            if ((combinePassCount & 0x4) != 0)
            {
                combineMaterial.EnableKeyword("SAMPLE_4");
            }
            else
            {
                combineMaterial.DisableKeyword("SAMPLE_4");
            }
            if ((combinePassCount & 0x2) != 0)
            {
                combineMaterial.EnableKeyword("SAMPLE_2");
            }
            else
            {
                combineMaterial.DisableKeyword("SAMPLE_2");
            }
            if ((combinePassCount & 0x1) != 0)
            {
                combineMaterial.EnableKeyword("SAMPLE_1");
            }
            else
            {
                combineMaterial.DisableKeyword("SAMPLE_1");
            }
            combineMaterial.SetTexture("_MainTex", bloomXY);
            var toLevel = bloomRects.Count - combinePassCount; //合成する枚数を引く。7レベルあって7枚なら0
            var toRect = bloomRects[toLevel];

            // 正規化係数を計算する
            float strength = 1f;
            float strengthSum = 0f;
            for (int i = 0; i < combinePassCount; i++)
            {
                strengthSum += strength;
                strength *= bloomStrengthMultiplier;
            }
            float normalizeFactor = 1f / strengthSum;

            strength = 1f;
            for (int i = 0; i < combinePassCount; i++)
            {
                var fromLevel = toLevel + i;
                var fromRect = bloomRects[fromLevel];

                combineMaterial.SetFloat("_Weight" + i, strength * normalizeFactor);

                var uvTransform = new Vector4();
                // toRect.x -> fromRect.x
                // toRect.y -> fromRect.y
                // toRect.x + toRect.width -> fromRect.x + fromRect.width
                // toRect.y + toRect.height -> fromRect.y + fromRect.height
                // と変換されるような、xy' = (xy * transform.xy) + transform.zwのtransformを求める。
                uvTransform.x = (float)fromRect.width / (float)toRect.width;
                uvTransform.y = (float)fromRect.height / (float)toRect.height;
                uvTransform.z = ((float)fromRect.x - ((float)toRect.x * uvTransform.x)) / (float)bloomXY.width;
                uvTransform.w = ((float)fromRect.y - ((float)toRect.y * uvTransform.y)) / (float)bloomXY.height;
                combineMaterial.SetVector("_UvTransform" + i, uvTransform);
                strength *= bloomStrengthMultiplier;
            }
            combineMaterial.SetPass(0);

            float x0 = (float)toRect.x / (float)bloomCombined.width;
            float x1 = (float)(toRect.x + toRect.width) / (float)bloomCombined.width;
            float y0 = (float)toRect.y / (float)bloomCombined.height;
            float y1 = (float)(toRect.y + toRect.height) / (float)bloomCombined.height;

            GL.Begin(GL.QUADS);
            GL.Vertex3(x0, y0, 0f);
            GL.Vertex3(x0, y1, 0f);
            GL.Vertex3(x1, y1, 0f);
            GL.Vertex3(x1, y0, 0f);
            GL.End();
        }

        void Composite(RenderTexture source, RenderTexture destination)
        {
            if (destination != null)
            {
                destination.DiscardContents();
            }
            Graphics.SetRenderTarget(destination);

            // カラーフィルターの有効無効設定
            if ((colorOffset == Vector3.zero)
                && (colorScale == Vector3.one)
                && (saturation == 1f))
            {
                compositionMaterial.DisableKeyword("COLOR_FILTER");
            }
            else
            {
                compositionMaterial.EnableKeyword("COLOR_FILTER");
            }

            int combinePassCount = (bloomRects.Count + bloomStartLevel) - bloomCombineStartLevel;
            combinePassCount = Mathf.Clamp(combinePassCount, 0, bloomRects.Count);
            int bloomRectCount = bloomRects.Count - combinePassCount;
            if (combinePassCount > 0)
            {
                compositionMaterial.EnableKeyword("BLOOM_COMBINED");
            }
            else
            {
                compositionMaterial.DisableKeyword("BLOOM_COMBINED");
            }

            if ((bloomRectCount & 0x4) != 0)
            {
                compositionMaterial.EnableKeyword("BLOOM_4");
            }
            else
            {
                compositionMaterial.DisableKeyword("BLOOM_4");
            }
            if ((bloomRectCount & 0x2) != 0)
            {
                compositionMaterial.EnableKeyword("BLOOM_2");
            }
            else
            {
                compositionMaterial.DisableKeyword("BLOOM_2");
            }
            if ((bloomRectCount & 0x1) != 0)
            {
                compositionMaterial.EnableKeyword("BLOOM_1");
            }
            else
            {
                compositionMaterial.DisableKeyword("BLOOM_1");
            }

            source.filterMode = FilterMode.Point; // ポイントで良い
            compositionMaterial.SetTexture("_MainTex", source);
            bloomXY.filterMode = FilterMode.Bilinear; // バイリニアが必要
            compositionMaterial.SetTexture("_BloomTex", bloomXY);
            bloomCombined.filterMode = FilterMode.Bilinear; // バイリニアが必要
            compositionMaterial.SetTexture("_BloomCombinedTex", bloomCombined);

            // 強度定数を計算する
            float strength = 1f;
            float strengthSum = 0f;
            for (int i = 0; i < bloomRects.Count; i++)
            {
                strengthSum += strength;
                strength *= bloomStrengthMultiplier;
            }
            float strengthBase = bloomStrength / strengthSum;
            strength = strengthBase;

            // 素で_bloomXYを読む枚数
            for (int i = 0; i < bloomRectCount; i++)
            {
                var rect = bloomRects[i];
                compositionMaterial.SetFloat(rect.weightShaderPropertyId, strength);
                Vector4 uvTransform;
                uvTransform.x = (float)rect.width / (float)bloomXY.width;
                uvTransform.y = (float)rect.height / (float)bloomXY.height;
                uvTransform.z = (float)rect.x / (float)bloomXY.width;
                uvTransform.w = (float)rect.y / (float)bloomXY.height;
                compositionMaterial.SetVector(rect.uvTransformShaderPropertyId, uvTransform);
                strength *= bloomStrengthMultiplier;
            }

            if (combinePassCount > 0)
            {
                var combinedStrength = 1f;
                var combinedStrengthSum = 0f;
                for (int i = 0; i < combinePassCount; i++)
                {
                    combinedStrengthSum += combinedStrength;
                    combinedStrength *= bloomStrengthMultiplier;
                }
                var rect = bloomRects[bloomRects.Count - combinePassCount];
                float weight = strength * combinedStrengthSum;
                compositionMaterial.SetFloat("_BloomWeightCombined", weight); // バッファ内は和で除して正規化されているので、和を乗じて戻す。
                Vector4 uvTransform;
                uvTransform.x = (float)rect.width / (float)bloomXY.width;
                uvTransform.y = (float)rect.height / (float)bloomXY.height;
                uvTransform.z = (float)rect.x / (float)bloomXY.width;
                uvTransform.w = (float)rect.y / (float)bloomXY.height;
                compositionMaterial.SetVector("_BloomUvTransformCombined", uvTransform);
            }
            compositionMaterial.SetPass(0);

            GL.Begin(GL.QUADS);
            GL.Vertex3(0f, 0f, 0f);
            GL.Vertex3(0f, 1f, 0f);
            GL.Vertex3(1f, 1f, 0f);
            GL.Vertex3(1f, 0f, 0f);
            GL.End();
        }

        void AddBlurQuads(
            RenderTexture from,
            int fromX,
            int fromY,
            int fromWidth,
            int fromHeight,
            float offsetScale,
            RenderTexture to,
            int toX,
            int toY,
            int toWidth,
            int toHeight,
            bool forX)
        {
            float x0 = (float)toX / (float)to.width;
            float x1 = (float)(toX + toWidth) / (float)to.width;
            float y0 = (float)toY / (float)to.height;
            float y1 = (float)(toY + toHeight) / (float)to.height;

            float u0 = (float)fromX / (float)from.width;
            float u1 = (float)(fromX + fromWidth) / (float)from.width;
            float v0 = (float)fromY / (float)from.height;
            float v1 = (float)(fromY + fromHeight) / (float)from.height;

            float uOffset0 = bloomSamples[0].offset * offsetScale;
            float vOffset0 = bloomSamples[0].offset * offsetScale;
            float uOffset1 = bloomSamples[1].offset * offsetScale;
            float vOffset1 = bloomSamples[1].offset * offsetScale;
            float uOffset2 = bloomSamples[2].offset * offsetScale;
            float vOffset2 = bloomSamples[2].offset * offsetScale;
            float uOffset3 = bloomSamples[3].offset * offsetScale;
            float vOffset3 = bloomSamples[3].offset * offsetScale;
            if (forX)
            {
                vOffset0 = vOffset1 = vOffset2 = vOffset3 = 0f;
            }
            else
            {
                uOffset0 = uOffset1 = uOffset2 = uOffset3 = 0f;
            }
            //Debug.Log(from.name + " -> " + to.name + " " + x0 + "," + y0 + " - " + x1 + "," + y1 + " uv: " + u0 + "," + v0 + " - " + u1 + "," + v1 + " size: " + toWidth + "x" + toHeight + " / " + to.width + "x" + to.height);

            GL.MultiTexCoord3(0, u0 + uOffset0, v0 + vOffset0, bloomSamples[0].weight);
            GL.MultiTexCoord3(1, u0 + uOffset1, v0 + vOffset1, bloomSamples[1].weight);
            GL.MultiTexCoord3(2, u0 + uOffset2, v0 + vOffset2, bloomSamples[2].weight);
            GL.MultiTexCoord3(3, u0 + uOffset3, v0 + vOffset3, bloomSamples[3].weight);
            GL.Vertex3(x0, y0, 0f);

            GL.MultiTexCoord3(0, u0 + uOffset0, v1 + vOffset0, bloomSamples[0].weight);
            GL.MultiTexCoord3(1, u0 + uOffset1, v1 + vOffset1, bloomSamples[1].weight);
            GL.MultiTexCoord3(2, u0 + uOffset2, v1 + vOffset2, bloomSamples[2].weight);
            GL.MultiTexCoord3(3, u0 + uOffset3, v1 + vOffset3, bloomSamples[3].weight);
            GL.Vertex3(x0, y1, 0f);

            GL.MultiTexCoord3(0, u1 + uOffset0, v1 + vOffset0, bloomSamples[0].weight);
            GL.MultiTexCoord3(1, u1 + uOffset1, v1 + vOffset1, bloomSamples[1].weight);
            GL.MultiTexCoord3(2, u1 + uOffset2, v1 + vOffset2, bloomSamples[2].weight);
            GL.MultiTexCoord3(3, u1 + uOffset3, v1 + vOffset3, bloomSamples[3].weight);
            GL.Vertex3(x1, y1, 0f);

            GL.MultiTexCoord3(0, u1 + uOffset0, v0 + vOffset0, bloomSamples[0].weight);
            GL.MultiTexCoord3(1, u1 + uOffset1, v0 + vOffset1, bloomSamples[1].weight);
            GL.MultiTexCoord3(2, u1 + uOffset2, v0 + vOffset2, bloomSamples[2].weight);
            GL.MultiTexCoord3(3, u1 + uOffset3, v0 + vOffset3, bloomSamples[3].weight);
            GL.Vertex3(x1, y0, 0f);
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
            int toHeight,
            bool clear,
            Color clearColor)
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
                to.DiscardContents(); // 刺す前に必ずDiscard
                Graphics.SetRenderTarget(to);
            }
            if (clear)
            {
                GL.Clear(false, true, clearColor);
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

        public void SetColorFilter(Vector3 colorOffset, Vector3 colorScale, float saturation)
        {
            this.colorOffset = colorOffset;
            this.colorScale = colorScale;
            this.saturation = saturation;
            SetColorTransform();
        }

        void SetupRenderTargets(RenderTexture source)
        {
            if (prevSource != null)
            {
                if ((source != null)
                    && (source.width == prevSource.width)
                    && (source.height == prevSource.height))
                {
                    return;
                }
            }
            if (maxBloomLevelCount == 0)
            {
                return;
            }
#if UNITY_EDITOR
            bloomStartLevelOnSetup = bloomStartLevel;
            maxBloomLevelCountOnSetup = maxBloomLevelCount;
            minBloomLevelSizeOnSetup = minBloomLevelSize;
#endif
            var format = RenderTextureFormat.ARGB32;

#if false // 手持ちの京セラS2にて、GLES2でビルドすると絵が出なくなる。そもそもtrueを返すなよ...。ver2018.3.9
			if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB2101010))
			{
				format = RenderTextureFormat.ARGB2101010;
			}
#endif
            int topBloomWidth = source.width >> bloomStartLevel;
            int topBloomHeight = source.height >> bloomStartLevel;
            brightness = new RenderTexture(
                TextureUtil.ToPow2RoundUp(topBloomWidth), //2羃でないとミップマップを作れる保証がないので2羃
                TextureUtil.ToPow2RoundUp(topBloomHeight),
                0,
                format);
            brightness.name = "brightness";
            brightness.useMipMap = true;
            brightness.filterMode = FilterMode.Bilinear;
            bloomRects = new List<BloomRect>();
            int bloomWidth, bloomHeight;
            CalcBloomRenderTextureArrangement(
                out bloomWidth,
                out bloomHeight,
                bloomRects,
                topBloomWidth,
                topBloomHeight,
                16, // TODO: 調整可能にするか?
                maxBloomLevelCount);
            Debug.Log("LightPostProcessor.SetupRenderTargets(): create RTs. " + brightness.width + "x" + brightness.height + " + " + bloomWidth + "x" + bloomHeight + " levels:" + bloomRects.Count);
            bloomX = new RenderTexture(bloomWidth, bloomHeight, 0, format);
            bloomX.name = "bloomX";
            bloomX.filterMode = FilterMode.Bilinear;
            bloomXY = new RenderTexture(bloomWidth, bloomHeight, 0, format);
            bloomXY.name = "bloomXY";
            bloomXY.filterMode = FilterMode.Bilinear;
            // TODO: combineする枚数が起動時に来まれば、その時点で最適な解像度に変更できる
            bloomCombined = new RenderTexture(bloomWidth, bloomHeight, 0, format);
            bloomCombined.name = "bloomCombined";
            prevSource = source;
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
            //Debug.Log(w01 + " " + w23 + " " + w45 + " " + w67);
            //Debug.Log(x01 + " " + x23 + " " + x45 + " " + x67);
            SetGaussSample(0, x01, w01);
            SetGaussSample(1, x23, w23);
            SetGaussSample(2, x45, w45);
            SetGaussSample(3, x67, w67);
        }

        void SetGaussSample(int index, float offset, float weight)
        {
            var sample = bloomSamples[index];
            sample.offset = offset;
            sample.weight = weight;
            bloomSamples[index] = sample;
        }

        static float Gauss(float sigma, float x)
        {
            float sigma2 = sigma * sigma;
            return Mathf.Exp(-(x * x) / (2f * sigma2));
        }

        struct BloomSample
        {
            public float offset;
            public float weight;
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
                if ((width < minBloomLevelSize) || (height < minBloomLevelSize))
                {
                    break;
                }
                levelCount--;
            }
            widthOut = maxX;
            heightOut = maxY;
            first = true;
        }

        void SetColorTransform()
        {
            if (compositionMaterial == null)
            {
                return;
            }
            var scaleOffsetTransform = Matrix4x4.Translate(new Vector3(colorOffset.x, colorOffset.y, colorOffset.z))
                * Matrix4x4.Scale(new Vector3(colorScale.x, colorScale.y, colorScale.z));

            var toYuv = new Matrix4x4();
            toYuv.SetRow(0, new Vector4(0.299f, 0.587f, 0.114f, 0f));
            toYuv.SetRow(1, new Vector4(-0.169f, -0.331f, 0.5f, 0f));
            toYuv.SetRow(2, new Vector4(0.5f, -0.419f, -0.081f, 0f));
            toYuv.SetRow(3, new Vector4(0f, 0f, 0f, 1f));

            var saturationTransform = Matrix4x4.Scale(new Vector3(1f, saturation, saturation));

            var fromYuv = new Matrix4x4();
            fromYuv.SetRow(0, new Vector4(1f, 0f, 1.402f, 0f));
            fromYuv.SetRow(1, new Vector4(1f, -0.344f, -0.714f, 0f));
            fromYuv.SetRow(2, new Vector4(1f, 1.772f, 0f, 0f));
            fromYuv.SetRow(3, new Vector4(0f, 0f, 0f, 1f));

            Matrix4x4 t = scaleOffsetTransform * fromYuv * saturationTransform * toYuv;
            compositionMaterial.SetVector("_ColorTransformR", t.GetRow(0));
            compositionMaterial.SetVector("_ColorTransformG", t.GetRow(1));
            compositionMaterial.SetVector("_ColorTransformB", t.GetRow(2));
        }

#if UNITY_EDITOR

        int bloomStartLevelOnSetup;
        int maxBloomLevelCountOnSetup;
        int minBloomLevelSizeOnSetup;

        void OnValidate()
        {
            if ((bloomStartLevelOnSetup != bloomStartLevel)
                || (maxBloomLevelCountOnSetup != maxBloomLevelCount)
                || (minBloomLevelSizeOnSetup != minBloomLevelSize))
            {
                bloomStartLevelOnSetup = bloomStartLevel;
                maxBloomLevelCountOnSetup = maxBloomLevelCount;
                minBloomLevelSizeOnSetup = minBloomLevelSize;
                prevSource = null; // これで再初期化が走る
            }
            SetColorTransform();
        }
#endif
    }
}
