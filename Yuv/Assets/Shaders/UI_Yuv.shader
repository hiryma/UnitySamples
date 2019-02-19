Shader "UI/Yuv"
{
	Properties
	{
		_MainTex ("MainTexture", 2D) = "white" {}
		_UvTex ("UvTexture", 2D) = "white" {}
		_AlphaTex ("AlphaTexture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "Queue"="Transparent" }
		LOD 100

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ HAS_ALPHA

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uvY : TEXCOORD0;
				float2 uvU : TEXCOORD1;
				float2 uvV : TEXCOORD2;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			sampler2D _UvTex;
			sampler2D _AlphaTex;
			float4 _MainTex_ST;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uvY = TRANSFORM_TEX(v.uv, _MainTex);
				o.uvU = float2(o.uvY.x * 0.5, o.uvY.y);
				o.uvV = float2(o.uvU.x + 0.5, o.uvU.y);
				return o;
			}

			half3 yuvToRgb(half3 yuv)
			{
/*
				const float yr = 0.299;
				const float yb = 0.114;
				const float uScale = 0.5 / (1.0 - yb);
				const float vScale = 0.5 / (1.0 - yr);
				const float yg = 1.0 - yr - yb;

				half3 rgb;
				rgb.b = ((yuv.g - 0.5) / uScale) + yuv.r;
				rgb.r = ((yuv.b - 0.5) / vScale) + yuv.r;
				rgb.g = (yuv.r - (yr * rgb.r) - (yb * rgb.b)) / yg;
*/
				// 以下の書き方の方が速いかもしれない。
				half3 rgb;
				rgb.r = yuv.r + (1.402 * yuv.b) - 0.701;
				rgb.g = yuv.r - (0.344 * yuv.g) - (0.714 * yuv.b) + 0.529;
				rgb.b = yuv.r + (1.772 * yuv.g) - 0.886;
				return saturate(rgb);
			}

			fixed4 frag (v2f i) : SV_Target
			{
				half3 yuv;
				yuv.x = tex2D(_MainTex, i.uvY).a;
				yuv.y = tex2D(_UvTex, i.uvU).a;
				yuv.z = tex2D(_UvTex, i.uvV).a;
				half4 rgba;
				rgba.xyz = yuvToRgb(yuv);
#ifdef HAS_ALPHA
				rgba.a = tex2D(_AlphaTex, i.uvY).a;
#else
				rgba.a = 1.0;
#endif
				return rgba;
			}
			ENDCG
		}
	}
}
