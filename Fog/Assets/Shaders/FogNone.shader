Shader "Sample/FogNone"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 lightmapUv : TEXCOORD1;
			};

			struct v2f
			{
				float2 lightmapUv : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.lightmapUv = (v.lightmapUv * unity_LightmapST.xy) + unity_LightmapST.zw;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = fixed4(1.0, 1.0, 1.0, 1.0);
				col.xyz = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lightmapUv));
				return col;
			}
			ENDCG
		}
	}
}
