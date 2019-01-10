Shader "UI/Indexed16Bilinear"
{
	Properties
	{
		_MainTex ("MainTexture", 2D) = "white" {}
		_TableTex ("TableTexture", 2D) = "white" {}
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

			fixed4 frag (v2f i) : SV_Target
			{
				// 4ピクセルタップ
				half2 texcoord = i.uv * _MainTex_TexelSize.zw; // ピクセル単位座標に変換
				texcoord -= 0.5; // 左下ピクセルは0.5づつずたした場所
				half2 texcoordFrac = frac(texcoord);
				half2 uv00 = (texcoord - texcoordFrac) * _MainTex_TexelSize.xy;
				half2 uv11 = half2(
					uv00.x + _MainTex_TexelSize.x,
					uv00.y + _MainTex_TexelSize.y); // 1ピクセル分のUVを追加
				half2 uv01 = half2(uv00.x, uv11.y);
				half2 uv10 = half2(uv11.x, uv00.y);
				half indexEncoded00 = tex2D(_MainTex, uv00).a * (255.01 / 16);
				half indexEncoded01 = tex2D(_MainTex, uv01).a * (255.01 / 16);
				half indexEncoded10 = tex2D(_MainTex, uv10).a * (255.01 / 16);
				half indexEncoded11 = tex2D(_MainTex, uv11).a * (255.01 / 16);
				// 左インデクスと右インデクスに分離
				half index00r = frac(indexEncoded00);
				half index00l = (indexEncoded00 - index00r) / 16;
				half index01r = frac(indexEncoded01);
				half index01l = (indexEncoded01 - index01r) / 16;
				half index10r = frac(indexEncoded10); // 最終的には使わない
				half index10l = (indexEncoded10 - index10r) / 16;
				half index11r = frac(indexEncoded11); // 最終的には使わない
				half index11l = (indexEncoded11 - index11r) / 16;

				// 2倍して「元の幅」にし、これを0.5倍してfracが0.5なら右で、0なら左。2倍して0.5倍なので、そのまま。
				bool isLeft = (frac(texcoord.x) < 0.49); // 非2羃テクスチャでの誤差に配慮して甘くしておく

				// インデクス決定
				half index00 = isLeft ? index00l : index00r;
				half index01 = isLeft ? index01l : index01r;
				half index10 = isLeft ? index00r : index10l;
				half index11 = isLeft ? index01r : index11l;
				// 色取得
				fixed4 col00 = tex2D(_TableTex, index00.xx);
				fixed4 col01 = tex2D(_TableTex, index01.xx);
				fixed4 col10 = tex2D(_TableTex, index10.xx);
				fixed4 col11 = tex2D(_TableTex, index11.xx);
				// バイリニア
				fixed4 col00_01 = lerp(col00, col01, texcoordFrac.y);
				fixed4 col10_11 = lerp(col10, col11, texcoordFrac.y);
				// texcoordFracは「幅半分」でのピクセル単位小数部なので、
				// オリジナルではさらに倍してその小数部で見る。
				half xFactor = frac(texcoordFrac * 2);
				fixed4 col = lerp(col00_01, col10_11, xFactor);
				return col;
			}
			ENDCG
		}
	}
}
