Shader "UI/Diff"
{
	Properties
	{
		_MainTex ("MainTexture", 2D) = "white" {}
		_RefTex ("RefTexture", 2D) = "white" {}
		_Scale ("Scale", float) = 16.0
		_Blend ("Blend", float) = -1.0
	}
	SubShader
	{
		Tags { "Queue"="Transparent" }
		LOD 100

		Pass
		{
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ HAS_ALPHA

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

			sampler2D _MainTex;
			sampler2D _RefTex;
			float4 _MainTex_ST;
			float _Scale;
			float _Blend;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				half4 c;
				if (_Blend < 0.0)
				{
					c = tex2D(_MainTex, i.uv) - tex2D(_RefTex, i.uv);
					c = abs(c);
					c *= _Scale;
					c.a = 1.0;
				}
				else
				{
					c = lerp(tex2D(_MainTex, i.uv), tex2D(_RefTex, i.uv), _Blend);
				}
				return c;
			}
			ENDCG
		}
	}
}
