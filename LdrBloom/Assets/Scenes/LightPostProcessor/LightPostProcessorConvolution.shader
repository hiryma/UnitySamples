Shader "Hidden/LightPostProcessorConvolution"
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
				float4 vertex : SV_POSITION;
				float3 sample0 : TEXCOORD0; //x:u y:v z:weight
				float3 sample1 : TEXCOORD1;
				float3 sample2 : TEXCOORD2;
				float3 sample3 : TEXCOORD3;
				float3 sample4 : TEXCOORD4;
				float3 sample5 : TEXCOORD5;
				float3 sample6 : TEXCOORD6;
				float3 sample7 : TEXCOORD7;
			};

			float3 _Sample0;
			float3 _Sample1;
			float3 _Sample2;
			float3 _Sample3;
			float3 _Sample4;
			float3 _Sample5;
			float3 _Sample6;
			float3 _Sample7;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.sample0 = float3(v.uv + _Sample0.xy, _Sample0.z);
				o.sample1 = float3(v.uv + _Sample1.xy, _Sample1.z);
				o.sample2 = float3(v.uv + _Sample2.xy, _Sample2.z);
				o.sample3 = float3(v.uv + _Sample3.xy, _Sample3.z);
				o.sample4 = float3(v.uv + _Sample4.xy, _Sample4.z);
				o.sample5 = float3(v.uv + _Sample5.xy, _Sample5.z);
				o.sample6 = float3(v.uv + _Sample6.xy, _Sample6.z);
				o.sample7 = float3(v.uv + _Sample7.xy, _Sample7.z);
//o.sample0 = float3(v.uv, 1.0);
				return o;
			}

			sampler2D _MainTex;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 c;
				c = tex2D(_MainTex, i.sample0.xy) * i.sample0.z;
				c += tex2D(_MainTex, i.sample1.xy) * i.sample1.z;
				c += tex2D(_MainTex, i.sample2.xy) * i.sample2.z;
				c += tex2D(_MainTex, i.sample3.xy) * i.sample3.z;
				c += tex2D(_MainTex, i.sample4.xy) * i.sample4.z;
				c += tex2D(_MainTex, i.sample5.xy) * i.sample5.z;
				c += tex2D(_MainTex, i.sample6.xy) * i.sample6.z;
				c += tex2D(_MainTex, i.sample7.xy) * i.sample7.z;
				return c;
			}
			ENDCG
		}
	}
}
