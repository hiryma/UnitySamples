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
			#pragma multi_compile _ SAMPLE_1
			#pragma multi_compile _ SAMPLE_2
			#pragma multi_compile _ SAMPLE_4

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float2 uv2 : TEXCOORD2;
				float2 uv3 : TEXCOORD3;
				float2 uv4 : TEXCOORD4;
				float2 uv5 : TEXCOORD5;
				float2 uv6 : TEXCOORD6;
			};

			float4 _UvTransform0;
			float4 _UvTransform1;
			float4 _UvTransform2;
			float4 _UvTransform3;
			float4 _UvTransform4;
			float4 _UvTransform5;
			float4 _UvTransform6;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv0 = (v.vertex * _UvTransform0.xy) + _UvTransform0.zw;
				o.uv1 = (v.vertex * _UvTransform1.xy) + _UvTransform1.zw;
				o.uv2 = (v.vertex * _UvTransform2.xy) + _UvTransform2.zw;
				o.uv3 = (v.vertex * _UvTransform3.xy) + _UvTransform3.zw;
				o.uv4 = (v.vertex * _UvTransform4.xy) + _UvTransform4.zw;
				o.uv5 = (v.vertex * _UvTransform5.xy) + _UvTransform5.zw;
				o.uv6 = (v.vertex * _UvTransform6.xy) + _UvTransform6.zw;
				return o;
			}

			sampler2D _MainTex;
			float _Weight0;
			float _Weight1;
			float _Weight2;
			float _Weight3;
			float _Weight4;
			float _Weight5;
			float _Weight6;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 c = tex2D(_MainTex, i.uv0) * _Weight0; // 最低1タップはある
#ifdef SAMPLE_4
				c += tex2D(_MainTex, i.uv1) * _Weight1;
				c += tex2D(_MainTex, i.uv2) * _Weight2;
				c += tex2D(_MainTex, i.uv3) * _Weight3;
	#ifdef SAMPLE_2
				c += tex2D(_MainTex, i.uv4) * _Weight4;
				c += tex2D(_MainTex, i.uv5) * _Weight5;
		#ifdef SAMPLE_1 // 7
				c += tex2D(_MainTex, i.uv6) * _Weight6;
		#endif // else 6
	#elif SAMPLE_1 // 5
				c += tex2D(_MainTex, i.uv4) * _Weight4;
	#endif // else 4
#elif SAMPLE_2
				c += tex2D(_MainTex, i.uv1) * _Weight1;
	#ifdef SAMPLE_1 // 3
				c += tex2D(_MainTex, i.uv2) * _Weight2;
	#endif // else 2
#elif SAMPLE_1 // 1
				// 一番上で足してあるのでやることない
#else // 0
				return fixed4(1.0, 0.0, 1.0, 1.0); // 0は不正。紫にしてすぐわかるようにする。
#endif
				return c;
			}
			ENDCG
		}
	}
}
