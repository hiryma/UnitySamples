Shader "Hidden/AzimuthalEquidistantProjectionFragment"
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
			#pragma multi_compile _ VIEWPORT_DEBUG

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			void vert (appdata v, out float4 vertex : SV_POSITION)
			{
				vertex = UnityObjectToClipPos(v.vertex);
			}

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			float _RcpTanSrcHalfFovY;
			float _DstHalfFovY;

			fixed4 frag (UNITY_VPOS_TYPE vpos : VPOS) : SV_Target
			{
				// yは[-1,1]、xは[-aspect,aspect]に変換
				float2 p = (vpos.xy - (_ScreenParams.xy * 0.5)) / (_ScreenParams.y * 0.5);
				float srcR = sqrt((p.x * p.x) + (p.y * p.y));
				float theta = srcR * _DstHalfFovY;
#ifdef VIEWPORT_DEBUG
				if (theta > (3.14159 * 0.5))
				{
					return fixed4(0.0, 1.0, 0.0, 1.0);
				}
#endif
				float2 uv;
				uv.x = p.x;
				uv.y = p.y;
				float r = (srcR == 0.0) ? 0.0 : tan(theta) * _RcpTanSrcHalfFovY / srcR;
				uv.xy *= r;
				uv.x *= _ScreenParams.y / _ScreenParams.x;
				uv.xy *= 0.5;
				uv.xy += 0.5;

#ifdef VIEWPORT_DEBUG
				if ((uv.x < 0.0) || (uv.x > 1.0) || (uv.y < 0.0) || (uv.y > 1.0))
				{
					return fixed4(1.0, 0.0, 0.0, 1.0);
				}
#endif
				return tex2D(_MainTex, uv);
			}
			ENDCG
		}
	}
}
