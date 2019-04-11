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

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 position : TEXCOORD0;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				float aspect = _ScreenParams.x / _ScreenParams.y;
				o.position = v.vertex; // [0, 1]が入っている
				o.position -= 0.5; // -1/2, 1/2に変換
				o.position.x *= aspect; // x方向をアスペクト比を乗じる [-aspect/2, aspect/2], [-1/2, 1/2]
				o.position *= 2.0; // [-aspect, aspect],[-1,1]
				return o;
			}

			sampler2D _MainTex;
			float _TanSrcHalfFovY;
			float _DstHalfFovY;

			fixed4 frag (v2f i) : SV_Target
			{
				float2 p = i.position;
				float dstR = sqrt((p.x * p.x) + (p.y * p.y));
				float theta = dstR * _DstHalfFovY;
#ifdef VIEWPORT_DEBUG
				if (theta > (3.14159 * 0.5))
				{
					return fixed4(0.0, 1.0, 0.0, 1.0);
				}
#endif
				float srcR = (dstR == 0.0) ? 0.0 : (tan(theta) / (_TanSrcHalfFovY * dstR));
				p *= srcR;
				p.x *= _ScreenParams.y / _ScreenParams.x;
				p *= 0.5;
				p += 0.5;

#ifdef VIEWPORT_DEBUG
				if ((p.x < 0.0) || (p.x > 1.0) || (p.y < 0.0) || (p.y > 1.0))
				{
					return fixed4(1.0, 0.0, 0.0, 1.0);
				}
#endif
				return tex2D(_MainTex, p);
			}
			ENDCG
		}
	}
}
