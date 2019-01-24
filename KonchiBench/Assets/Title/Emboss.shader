Shader "Benchmark/Emboss"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_SpecularColor ("SpecularColor", Color) = (1, 1, 1, 1)
	}
	SubShader
	{
		Tags { "Queue" = "Transparent" }
		LOD 100
		ZTest Always
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ FOR_TEXT

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR0;
			};

			float4 _MainTex_ST;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				return o;
			}

			sampler2D _MainTex;
			float3 _SpecularColor;

			float schlick(float r0, float cos)
			{
				float t = 1.0 - cos;
				float t2 = t * t;
				float t4 = t2 * t2;
				return r0 + (1.0 - r0) * t4 * t;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float preRnd = sin(dot(i.uv.xy, float2(12.9898, 78.233))) * 43758.5453;
				float rnd = frac(preRnd);
				// constants
				float pi = 3.14159;
				float shininess = rnd * 128.0;
				float3 ambient = float3(0.5, 0.5, 0.5);
				float reflection0 = 0.04;

				float3 l; // light vector
				float t = _Time * 32.0;
				float t_2pi = t / (2.0 * pi);
				float cosT = cos(t_2pi);
				l.x = cosT * cos(t);
				l.y = cosT * sin(t);
				l.z = sin(t_2pi) - 2.0;
				l = normalize(l);

				float3 e = normalize(float3(0.5 - i.uv.xy, -1.0)); // eye vector
				float3 h = normalize(l + e);

				float4 texel = tex2D(_MainTex, i.uv);
#ifdef FOR_TEXT
				float3 diffuseColor = i.color.xyz;
				float3 n = float3(ddx(texel.w), ddy(texel.w), -1); // normal
#else
				float3 diffuseColor = texel.xyz * i.color.xyz;
				float3 n = float3(ddx(texel.x), ddy(texel.y), -1); // normal
#endif
				n = normalize(n);
				float nl = saturate(dot(n, l));
				float nh = saturate(dot(n, h));
				float f = schlick(reflection0, nh); // fresnel
				float3 specular = (_SpecularColor.xyz * pow(nh, shininess) * ((shininess + 2.0) / (2.0 * pi)) * f);
				float3 lighted = (diffuseColor * nl) + specular;// + (ambient * diffuseColor);

				float alpha = texel.w * i.color.w;
				return float4(lighted, alpha);
			}
			ENDCG
		}
	}
}

