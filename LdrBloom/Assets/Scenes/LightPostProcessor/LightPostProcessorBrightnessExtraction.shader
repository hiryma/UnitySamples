Shader "Hidden/LightPostProcessorBrightnessExtraction"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
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
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;
			float3 _ColorTransform; // xを乗算、yを加算して結果を出す

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
#if 0
				fixed brightness = (col.r * 0.299) + (col.g * 0.587) + (col.b * 0.114);
				brightness = pow(brightness, _ColorTransform.z); // 真っ白でないとほぼほぼ光らない
				col.xyz *= brightness;
#else
				col.xyz *= _ColorTransform.x;
				col.xyz += _ColorTransform.y;
#endif
				return col;
			}
			ENDCG
		}
	}
}
