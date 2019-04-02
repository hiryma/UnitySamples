Shader "Hidden/LightPostProcessorBloomCombine"
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
				float3 sample0 : TEXCOORD0; //x:u y:v z:weight
				float3 sample1 : TEXCOORD1;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 sample0 : TEXCOORD0; //x:u y:v z:weight
				float3 sample1 : TEXCOORD1;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.sample0 = v.sample0;
				o.sample1 = v.sample1;
				return o;
			}

			sampler2D _MainTex;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 c;
				c = tex2D(_MainTex, i.sample0.xy) * i.sample0.z;
				c += tex2D(_MainTex, i.sample1.xy) * i.sample1.z;
				return c;
			}
			ENDCG
		}
	}
}
