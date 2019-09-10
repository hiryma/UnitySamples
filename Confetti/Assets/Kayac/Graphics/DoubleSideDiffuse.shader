Shader "Kayac/DoubleSideDiffuse"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Cull Off
		ZWrite On
		Tags
		{
			"LightMode" = "ForwardBase"
			"Queue" = "Geometry"
			"RenderType" = "Opaque"
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				half3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				half3 worldNormal : TEXCOORD1;
				fixed3 ambient : TEXCOORD2;
				UNITY_FOG_COORDS(1)
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _LightColor0;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.ambient = ShadeSH9(half4(o.worldNormal, 1.0));
				UNITY_TRANSFER_FOG(o, o.vertex);
				return o;
			}

			fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target
			{
				half3 n = (facing > 0.0) ? i.worldNormal : -i.worldNormal;
				half dp = saturate(dot(_WorldSpaceLightPos0, n));
				fixed3 color = _LightColor0.xyz * dp;
				color += i.ambient;
				color *= tex2D(_MainTex, i.uv);
				UNITY_APPLY_FOG(i.fogCoord, ret);
				return fixed4(color, 1.0);
			}
			ENDCG
		}
	}
}
