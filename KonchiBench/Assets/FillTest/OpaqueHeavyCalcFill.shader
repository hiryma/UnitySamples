Shader "Benchmark/OpaqueHeavyCalcFill"
{
	Properties
	{
		_Time ("time", Float) = 0.0
	}
	SubShader
	{
		Tags { "Queue" = "Geometry" }
		LOD 100
		ZTest Always
		ZWrite Off
//		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			void vert (appdata v, out float4 vertex : SV_POSITION)
			{
				vertex = UnityObjectToClipPos(v.vertex);
			}

			float3 getNormal(float3 pos)
			{
				float x = sin(pos.x * 16.0) + (0.5 * sin(pos.y * 32.0)) + (0.25 * sin(pos.x * 64.0));
				float y = sin(pos.y * 16.0) + (0.5 * sin(pos.x * 32.0)) + (0.25 * sin(pos.y * 64.0));
				float3 t = float3(x, y, -1.0);
				return normalize(t);
			}

			float schlick(float r0, float cos)
			{
				float t = 1.0 - cos;
				float t2 = t * t;
				float t4 = t2 * t2;
				return r0 + (1.0 - r0) * t4 * t;
			}

			fixed4 frag (UNITY_VPOS_TYPE vpos : VPOS) : SV_Target
			{
				float rnd = frac(sin(dot(vpos.xy, float2(12.9898, 78.233))) * 43758.5453);
				// constants
				float pi = 3.14159;
				float3 diffuseColor = float3(0.8, 0.6, 0.7);
				float3 specularColor = float3(0.3, 0.5, 0.9);
				float viewConeHalfAngle = pi * 0.5 * 0.5;
				float shininess = rnd * 128.0;
				float3 ambientGround = float3(0.1, 0.3, 0.1);
				float3 ambientSky = float3(0.1, 0.3, 0.6);
				float reflection0 = 0.04;

				float3 lightVector;
				lightVector.x = cos(_Time * 16.0) * cos(_Time * 32.0);
				lightVector.y = cos(_Time * 16.0) * sin(_Time * 32.0);
				lightVector.z = -abs(sin(_Time * 16.0));

				float screenVMax = max(_ScreenParams.x, _ScreenParams.y);
				float2 screenPosition = ((vpos.xy / screenVMax) - 0.5) * 2.0;
				float theta = sqrt(dot(screenPosition, screenPosition)) * viewConeHalfAngle;
				float phi = atan2(screenPosition.y, screenPosition.x);
				float3 viewVector = float3(
					sin(phi) * sin(theta),
					cos(phi) * sin(theta),
					cos(theta));
				float3 halfVector = normalize(lightVector - viewVector);
				float3 hitPos = float3(viewVector.xy / viewVector.z, 1.0);
				float3 hitNormal = getNormal(hitPos);
				float nl = saturate(dot(hitNormal, lightVector));
				float nh = saturate(dot(hitNormal, halfVector));
				float ambient = ((ambientGround - ambientSky) * hitNormal.z) + ambientSky;
				float fresnel = schlick(reflection0, nh);
				float3 lighted = (diffuseColor * nl) + (specularColor * pow(nh, shininess) * ((shininess + 2.0) / (2.0 * pi)) * fresnel) + (ambient * diffuseColor);
				return float4(lighted, 0.5);
			}
			ENDCG
		}
	}
}