Shader "UI/PackedYuvPoint"
{
	Properties
	{
		_MainTex ("MainTexture", 2D) = "white" {}
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

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;
			sampler2D _TableTex;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			half3 yuvToRgb(half3 yuv)
			{
				yuv.yz -= (128.0 / 255.0);
				half3 rgb = half3(
					dot(half3(1.0, 0.0, 1.402), yuv),
					dot(half3(1.0, -0.344, -0.714), yuv),
					dot(half3(1.0, 1.772, 0.0), yuv));
				return saturate(rgb);
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// 左画素なのか右画素なのかを判定
				half texcoordX = i.uv.x * _MainTex_TexelSize.z; // ピクセル単位座標に変換(幅半分のテクスチャでの)
				half4 encoded = tex2D(_MainTex, i.uv);
				half y = (frac(texcoordX) < 0.49) ? encoded.x : encoded.y; // 非2羃テクスチャで誤差が出た時のために少し甘めに見ておく
				half3 rgb = yuvToRgb(half3(y, encoded.zw));
				return fixed4(rgb, 1.0);
			}
			ENDCG
		}
	}
}
