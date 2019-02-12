Shader "UI/Grb"
{
	Properties
	{
		_MainTex ("MainTexture", 2D) = "white" {}
		_RbTex ("RbTexture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "Queue"="Transparent" }
		LOD 100

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			ZWrite Off

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
				float2 uvG : TEXCOORD0;
				float2 uvR : TEXCOORD1;
				float2 uvB : TEXCOORD2;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			sampler2D _RbTex;
			float4 _MainTex_ST;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uvG = TRANSFORM_TEX(v.uv, _MainTex);
				o.uvR = float2(o.uvG.x * 0.5, o.uvG.y);
				o.uvB = float2(o.uvR.x + 0.5, o.uvR.y);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				half4 c;
				c.g = tex2D(_MainTex, i.uvG).a;
				c.r = tex2D(_RbTex, i.uvR).a;
				c.b = tex2D(_RbTex, i.uvB).a;
				c.a = 1.0;
				return c;
			}
			ENDCG
		}
	}
}
