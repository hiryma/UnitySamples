// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Miura/DebugPrimitiveTextured"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Cull Off
		ZWrite Off
		ZTest Always
		BlendOp Add
		Blend SrcAlpha OneMinusSrcAlpha
		Tags
		{
			 "Queue" = "Transparent"
			 "RenderType" = "Transparent"
			 "DisableBatching" = "True"
		}
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
				fixed4 color : COLOR0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR0;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
//return fixed4(1,0,0,1);
//return fixed4(i.uv.xy * 8.0, 0, 1);
//return fixed4(0,0,0,1);
//return i.color;

				// フォントテクスチャはアルファだけしか入ってない
				fixed4 ret = i.color;
				ret *= tex2D(_MainTex, i.uv);
				return ret;
			}
			ENDCG
		}
	}
}
