Shader "UI/YuvBilinear"
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

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// UV決定
				half2 texcoord = i.uv * _MainTex_TexelSize.zw; // ピクセル単位座標に変換
				texcoord -= 0.5; // i.uvはピクセル中心なので整数単位に直すと0.5がついている。これを減ずる
				half2 texcoordFrac = frac(texcoord);
				half2 uv00 = (texcoord - texcoordFrac + 0.5) * _MainTex_TexelSize.xy; //さっき減じた0.5を戻す
				half2 uv11 = half2(
					uv00.x + _MainTex_TexelSize.x,
					uv00.y + _MainTex_TexelSize.y); // 1ピクセル分のUVを追加
				half2 uv01 = half2(uv00.x, uv11.y);
				half2 uv10 = half2(uv11.x, uv00.y);
				// インデクス取得
				half index00 = tex2D(_MainTex, uv00).a;
				half index01 = tex2D(_MainTex, uv01).a;
				half index10 = tex2D(_MainTex, uv10).a;
				half index11 = tex2D(_MainTex, uv11).a;
				// 色取得
				fixed4 col00 = tex2D(_TableTex, index00.xx);
				fixed4 col01 = tex2D(_TableTex, index01.xx);
				fixed4 col10 = tex2D(_TableTex, index10.xx);
				fixed4 col11 = tex2D(_TableTex, index11.xx);
				// バイリニア
				fixed4 col00_01 = lerp(col00, col01, texcoordFrac.y);
				fixed4 col10_11 = lerp(col10, col11, texcoordFrac.y);
				fixed4 col = lerp(col00_01, col10_11, texcoordFrac.x);
				return col;
			}
			ENDCG
		}
	}
}
