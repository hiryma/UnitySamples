Shader "Sample/Back"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
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
                float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
				return o;
			}

			float4 _Color;
            sampler2D _MainTex;

			fixed4 frag (v2f i) : SV_Target
			{
				return tex2D(_MainTex, i.uv) * _Color;
			}
			ENDCG
		}
	}
}
