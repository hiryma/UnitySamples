Shader "Sample/FogFragmentExpHeightExp"
{
	Properties
	{
		_FogDensity ("FogDensity", float) = 1.0
		_FogDensityAttenuation ("FogDensityAttenuation", float) = 1.0
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
			#include "Fog.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 lightmapUv : TEXCOORD1;
			};

			struct v2f
			{
				float2 lightmapUv : TEXCOORD1;
				float3 worldPosition : TEXCOORD2;
				float4 vertex : SV_POSITION;
			};

			float _FogDensity;
			float _FogDensityAttenuation;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.lightmapUv = (v.lightmapUv * unity_LightmapST.xy) + unity_LightmapST.zw;
				o.worldPosition = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float fog = calcFogHeightExp(i.worldPosition, _WorldSpaceCameraPos, _FogDensity, _FogDensityAttenuation);
//return fixed4(fog, frac(fog), -fog, 1);
				fixed4 col = fixed4(1.0, 1.0, 1.0, 1.0);
				col.xyz = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lightmapUv));
				col.xyz = lerp(unity_FogColor.xyz, col.xyz, fog);
				return col;
			}
			ENDCG
		}
	}
}
