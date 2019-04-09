Shader "Hidden/AzimuthalEquidistantProjectionVertex"
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

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			float _RcpTanSrcHalfFovY;
			float _DstHalfFovY;

			v2f vert (appdata v)
			{
				float2 p = v.vertex; // [-1, 1]
				p *= _ScreenParams.xy; // [-w, w], [-h, h]
				p /= _ScreenParams.y; // [-aspect, aspect], [-1, 1]
				float srcR = sqrt((p.x * p.x) + (p.y * p.y));
				float theta = srcR * _DstHalfFovY;
				float2 uv;
				uv.x = p.x;
#if UNITY_UV_STARTS_AT_TOP
				uv.y = -p.y;
#else
				uv.y = p.y;
#endif
				float r = (srcR == 0.0) ? 0.0 : tan(theta) * _RcpTanSrcHalfFovY / srcR;
				uv.xy *= r;
				uv.x *= _ScreenParams.y / _ScreenParams.x;
				uv.xy *= 0.5;
				uv.xy += 0.5;

				v2f o;
				o.vertex = v.vertex;
				o.uv = uv;
				return o;
			}

			sampler2D _MainTex;

			fixed4 frag (v2f i) : SV_Target
			{
//return fixed4(i.vertex.xy / _ScreenParams.xy, 0, 1);
//	return fixed4(1, 0, 0, 1);
//return fixed4(i.uv, 0, 1);
				float2 uv = i.uv;
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
