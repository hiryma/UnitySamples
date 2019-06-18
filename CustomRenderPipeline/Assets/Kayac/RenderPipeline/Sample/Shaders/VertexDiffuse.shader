Shader "Kayac/BasicRenderPipeline/VertexDiffuse"
{
	Properties
	{
		_Color ("Color", Color) = (1, 1, 1, 1)
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" }

		Pass
		{
			Name "Main"
			Tags { "LightMode" = "Main" }
			Blend [_SrcBlend] [_DstBlend]
			Cull [_Cull]
			ZWrite [_ZWrite]

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float dotNL : TEXCOORD0;
			};

			float3 _MainLightDirection;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.dotNL = saturate(dot(_MainLightDirection, v.normal));
				return o;
			}

			fixed4 _Color;
			fixed3 _MainLightColor;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 color;
				color.xyz = _Color.xyz * _MainLightColor.xyz * i.dotNL;
				color.w = _Color.w;
				return color;
			}
			ENDCG
		}
	}
}
