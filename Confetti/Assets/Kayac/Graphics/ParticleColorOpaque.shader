// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Kayac/ParticleColorOpaque"
{
	Properties
	{
	}
	SubShader
	{
		Cull Off
		ZWrite On
		Tags
		{
			"Queue" = "Geometry"
			"RenderType" = "Opaque"
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR0;
				UNITY_FOG_COORDS(1)
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				UNITY_TRANSFER_FOG(o, o.vertex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 ret = i.color;
				UNITY_APPLY_FOG(i.fogCoord, ret);
				return ret;
			}
			ENDCG
		}
	}
}
