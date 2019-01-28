Shader "Benchmark/IncoherentTextureFill"
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
				float4 uv01 : TEXCOORD0;
				float4 uv23 : TEXCOORD1;
				float4 uv45 : TEXCOORD2;
				float4 uv67 : TEXCOORD3;
				float4 vertex : SV_POSITION;
			};

			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				float2 uv = TRANSFORM_TEX(v.uv, _MainTex) * 16.0;
				float ox = _MainTex_TexelSize.x;// * 16.0;
				float oy = _MainTex_TexelSize.y;// * 16.0;
				o.uv01.xy = uv + float2(-ox, -oy);
				o.uv01.zw = uv + float2(-ox, 0.0);
				o.uv23.xy = uv + float2(-ox, oy);
				o.uv23.zw = uv + float2(0.0, -oy);
				o.uv45.xy = uv + float2(0.0, oy);
				o.uv45.zw = uv + float2(ox, -oy);
				o.uv67.xy = uv + float2(ox, 0.0);
				o.uv67.zw = uv + float2(ox, oy);
				return o;
			}

			sampler2D _MainTex;

			fixed4 frag (v2f i) : SV_Target
			{
				half4 c = tex2D(_MainTex, i.uv01.xy);
				c += tex2D(_MainTex, i.uv01.zw);
				c += tex2D(_MainTex, i.uv23.xy);
				c += tex2D(_MainTex, i.uv23.zw);
				c += tex2D(_MainTex, i.uv45.xy);
				c += tex2D(_MainTex, i.uv45.zw);
				c += tex2D(_MainTex, i.uv67.xy);
				c += tex2D(_MainTex, i.uv67.zw);
				c.a = 0.5;
				return c;
			}
			ENDCG
		}
	}
}
