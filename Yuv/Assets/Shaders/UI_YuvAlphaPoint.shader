Shader "UI/YuvAlphaPoint"
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
				/*
				x: y0が6bit、u上位2bit
				y: y1が6bit、v上位2bit
				z: u下位4bit、v下位4bit
				w: a0上位4bit、a1上位4bit
				*/
				// 整数化
				half4 encodedInt = encoded * 255.01;
				//xyからy0,y1,u上位、v上位を抜き出す
				half2 encodedIntXy_2 = encodedInt.xy / 4.0; //[0/4, 255/4]
				half2 uvHigh = frac(encodedIntXy_2); //[0, 3/4]
				half2 y01Int = (encodedIntXy_2 - uvHigh) * 4.0; // 上位6bit [0, 252/4]
				half2 uvHighInt = uvHigh * 256.0; //[0, 192]

				//zからuv下位、wからa0,a1を取り出す
				half2 encodedIntZw_4 = encodedInt.zw / 16.0; //整数部がu、小数部がv
				half2 zwLow = frac(encodedIntZw_4); //[0/16, 15/16]
				half2 zwHigh = (encodedIntZw_4 - zwLow) / 16.0; //[0, 15/16]
				half3 yuvInt;
				yuvInt.x = (frac(texcoordX) < 0.49) ? y01Int.x : y01Int.y;
				yuvInt.yz = uvHighInt.xy + (half2(zwHigh.x, zwLow.x) * 64.0);

				// 上位2ビットを下位2ビットにコピー
				half3 yuvHigh2 = floor(yuvInt / 64.0);
				yuvInt += yuvHigh2;
				half3 rgb = yuvToRgb(yuvInt / 255.0);

				half a = (frac(texcoordX) < 0.49) ? zwHigh.y : zwLow.y;
				a += a / 16.0;
				return fixed4(rgb, a);
			}
			ENDCG
		}
	}
}
