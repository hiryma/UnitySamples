Shader "Hidden/LightPostProcessorComposition"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_BloomTex ("Bloom", 2D) = "white" {}
		_ColorTransformR ("ColorTransformR", Vector) = (1, 0, 0, 0)
		_ColorTransformG ("ColorTransformG", Vector) = (0, 1, 0, 0)
		_ColorTransformB ("ColorTransformB", Vector) = (0, 0, 1, 0)
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
			#pragma multi_compile _ BLOOM_1
			#pragma multi_compile _ BLOOM_2
			#pragma multi_compile _ BLOOM_4
			#pragma multi_compile _ COLOR_FILTER

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
				float2 bloomUv0 : TEXCOORD1;
				float2 bloomUv1 : TEXCOORD2;
				float2 bloomUv2 : TEXCOORD3;
				float2 bloomUv3 : TEXCOORD4;
				float2 bloomUv4 : TEXCOORD5;
				float2 bloomUv5 : TEXCOORD6;
				float2 bloomUv6 : TEXCOORD7;
			};

			float4 _BloomUvTransform0;
			float4 _BloomUvTransform1;
			float4 _BloomUvTransform2;
			float4 _BloomUvTransform3;
			float4 _BloomUvTransform4;
			float4 _BloomUvTransform5;
			float4 _BloomUvTransform6;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.bloomUv0 = (v.uv * _BloomUvTransform0.xy) + _BloomUvTransform0.zw;
				o.bloomUv1 = (v.uv * _BloomUvTransform1.xy) + _BloomUvTransform1.zw;
				o.bloomUv2 = (v.uv * _BloomUvTransform2.xy) + _BloomUvTransform2.zw;
				o.bloomUv3 = (v.uv * _BloomUvTransform3.xy) + _BloomUvTransform3.zw;
				o.bloomUv4 = (v.uv * _BloomUvTransform4.xy) + _BloomUvTransform4.zw;
				o.bloomUv5 = (v.uv * _BloomUvTransform5.xy) + _BloomUvTransform5.zw;
				o.bloomUv6 = (v.uv * _BloomUvTransform6.xy) + _BloomUvTransform6.zw;
				return o;
			}

			sampler2D _MainTex;
			sampler2D _BloomTex;
			fixed4 _ColorTransformR;
			fixed4 _ColorTransformG;
			fixed4 _ColorTransformB;
			float _BloomWeight0;
			float _BloomWeight1;
			float _BloomWeight2;
			float _BloomWeight3;
			float _BloomWeight4;
			float _BloomWeight5;
			float _BloomWeight6;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
#ifdef BLOOM_4
				col += tex2D(_BloomTex, i.bloomUv0) * _BloomWeight0;
				col += tex2D(_BloomTex, i.bloomUv1) * _BloomWeight1;
				col += tex2D(_BloomTex, i.bloomUv2) * _BloomWeight2;
				col += tex2D(_BloomTex, i.bloomUv3) * _BloomWeight3;
	#ifdef BLOOM_2
				col += tex2D(_BloomTex, i.bloomUv4) * _BloomWeight4;
				col += tex2D(_BloomTex, i.bloomUv5) * _BloomWeight5;
		#ifdef BLOOM_1 // 7
				col += tex2D(_BloomTex, i.bloomUv6) * _BloomWeight6;
		#endif // else 6
	#elif BLOOM_1 // 5
				col += tex2D(_BloomTex, i.bloomUv4) * _BloomWeight4;
	#endif // else 4
#elif BLOOM_2
				col += tex2D(_BloomTex, i.bloomUv0) * _BloomWeight0;
				col += tex2D(_BloomTex, i.bloomUv1) * _BloomWeight1;
	#ifdef BLOOM_1 // 3
				col += tex2D(_BloomTex, i.bloomUv2) * _BloomWeight2;
	#endif // else 2
#elif BLOOM_1 //1
				col += tex2D(_BloomTex, i.bloomUv0) * _BloomWeight0;
#endif // else 0
// return tex2D(_BloomTex, i.bloomUv2) * _BloomWeight2;

#ifdef COLOR_FILTER
				fixed3 t;
				fixed4 colA1 = fixed4(col.xyz, 1.0);
				t.r = dot(_ColorTransformR, colA1);
				t.g = dot(_ColorTransformG, colA1);
				t.b = dot(_ColorTransformB, colA1);
				col.rgb = t;
#endif
				return col;
			}
			ENDCG
		}
	}
}
