Shader "Benchmark/DependentTextureFill"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "Queue" = "Transparent" }
		LOD 100
		ZTest Always
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
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

			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			sampler2D _MainTex;

			fixed4 frag (v2f i) : SV_Target
			{
				float ox = _MainTex_TexelSize.x;
				float oy = _MainTex_TexelSize.y;
				half4 c = tex2D(_MainTex, frac(i.uv + float2(-ox, -oy)));
				c += tex2D(_MainTex, frac(i.uv + float2(-ox, 0.0)));
				c += tex2D(_MainTex, frac(i.uv + float2(-ox, oy)));
				c += tex2D(_MainTex, frac(i.uv + float2(0.0, -oy)));
				c += tex2D(_MainTex, frac(i.uv + float2(0.0, oy)));
				c += tex2D(_MainTex, frac(i.uv + float2(ox, -oy)));
				c += tex2D(_MainTex, frac(i.uv + float2(ox, 0.0)));
				c += tex2D(_MainTex, frac(i.uv + float2(ox, oy)));
				c.rgb *= 0.125;
				c.a = 0.5;
				return c;
			}
			ENDCG
		}
	}
}
