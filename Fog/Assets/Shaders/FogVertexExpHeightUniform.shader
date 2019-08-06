Shader "Sample/FogVertexExpHeightUniform"
{
	Properties
	{
		_FogDensity ("Fog", float) = 1.0
		_FogEndHeight ("FogEndHeight", float) = 3.0
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
				float fog : TEXCOORD2;
				float4 vertex : SV_POSITION;
			};

			float _FogDensity;
			float _FogEndHeight;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.lightmapUv = (v.lightmapUv * unity_LightmapST.xy) + unity_LightmapST.zw;
				float3 worldPosition = mul(unity_ObjectToWorld, v.vertex);
				o.fog = calcFogHeightUniform(worldPosition, _WorldSpaceCameraPos, _FogDensity, _FogEndHeight);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = fixed4(1.0, 1.0, 1.0, 1.0);
				col.xyz = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.lightmapUv));
				col.xyz = lerp(col.xyz, unity_FogColor.xyz, i.fog);
				return col;
			}
			ENDCG
		}
	}
}
