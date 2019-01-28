Shader "Benchmark/AlphaTestColorFill"
{
	Properties
	{
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
			#pragma target 3.0

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			void vert (appdata v, out float4 vertex : SV_POSITION)
			{
				vertex = UnityObjectToClipPos(v.vertex);
			}

			fixed4 frag (UNITY_VPOS_TYPE vpos : VPOS) : SV_Target
			{
				vpos.xy *= 1.0 / 128.0;
				if (frac(dot(vpos, half2(1.0, 1.0))) < 0.5)
				{
					discard;
				}
				return fixed4(1.0, 0.0, 0.0, 0.5);
			}
			ENDCG
		}
	}
}