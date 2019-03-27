Shader "PostProcess/LightPostProcessorComposition"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_GaussTex ("Gauss", 2D) = "white" {}
		_Level1Tex ("Level1", 2D) = "white" {}
		_Level2Tex ("Level2", 2D) = "white" {}
		_Level3Tex ("Level3", 2D) = "white" {}
		_Level4Tex ("Level4", 2D) = "white" {}
		_Level5Tex ("Level5", 2D) = "white" {}
		_ColorTransformR ("ColorTransformR", Vector) = (1, 0, 0, 0)
		_ColorTransformG ("ColorTransformG", Vector) = (0, 1, 0, 0)
		_ColorTransformB ("ColorTransformB", Vector) = (0, 0, 1, 0)
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

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
				float3 gauss0 : TEXCOORD1;
				float3 gauss1 : TEXCOORD2;
				float3 gauss2 : TEXCOORD3;
				float3 gauss3 : TEXCOORD4;
				float3 gauss4 : TEXCOORD5;
				float3 gauss5 : TEXCOORD6;
				float3 gauss6 : TEXCOORD7;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 gauss0 : TEXCOORD1;
				float3 gauss1 : TEXCOORD2;
				float3 gauss2 : TEXCOORD3;
				float3 gauss3 : TEXCOORD4;
				float3 gauss4 : TEXCOORD5;
				float3 gauss5 : TEXCOORD6;
				float3 gauss6 : TEXCOORD7;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.gauss0 = v.gauss0;
				o.gauss1 = v.gauss1;
				o.gauss2 = v.gauss2;
				o.gauss3 = v.gauss3;
				o.gauss4 = v.gauss4;
				o.gauss5 = v.gauss5;
				o.gauss6 = v.gauss6;
				return o;
			}

			sampler2D _MainTex;
			sampler2D _GaussTex;
			sampler2D _Level1Tex;
			sampler2D _Level2Tex;
			sampler2D _Level3Tex;
			sampler2D _Level4Tex;
			sampler2D _Level5Tex;
			fixed4 _ColorTransformR;
			fixed4 _ColorTransformG;
			fixed4 _ColorTransformB;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
#if 1
				col += tex2D(_GaussTex, i.gauss0.xy) * i.gauss0.z;
				col += tex2D(_GaussTex, i.gauss1.xy) * i.gauss1.z;
				col += tex2D(_GaussTex, i.gauss2.xy) * i.gauss2.z;
				col += tex2D(_GaussTex, i.gauss3.xy) * i.gauss3.z;
				col += tex2D(_GaussTex, i.gauss4.xy) * i.gauss4.z;
				col += tex2D(_GaussTex, i.gauss5.xy) * i.gauss5.z;
				col += tex2D(_GaussTex, i.gauss6.xy) * i.gauss6.z;
#else
				col += tex2D(_Level1Tex, i.uv) * (1.0 / 1.0);
				col += tex2D(_Level2Tex, i.uv) * (1.0 / 1.0);
				col += tex2D(_Level3Tex, i.uv) * (1.0 / 1.0);
				col += tex2D(_Level4Tex, i.uv) * (1.0 / 1.0);
				col += tex2D(_Level5Tex, i.uv) * (1.0 / 1.0);
#endif
				fixed4 colA1 = fixed4(col.rgb, 1.0);
				fixed4 t;
				t.r = dot(_ColorTransformR, colA1);
				t.g = dot(_ColorTransformG, colA1);
				t.b = dot(_ColorTransformB, colA1);
				t.a = col.a;
				return t;
//col = tex2D(_Level4Tex, i.uv);
				return col;
			}
			ENDCG
		}
	}
}
