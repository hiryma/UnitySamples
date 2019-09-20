Shader "Custom/Sky"
{
	Properties {
		_Axis ("Axis", Vector) = (0, 1, 0, 0)
		_UpColor ("UpColor", Color) = (1, 1, 1, 1)
		_DownColor ("DownColor", Color) = (0, 0, 0, 1)
	}
	SubShader
	{

		Tags
		{
			"RenderType"="Background"
			"Queue"="Background"
			"PreviewType"="SkyBox"
		}

		Pass
		{
			ZWrite Off
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct appdata
			{
				float4 vertex : POSITION;
				float3 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 texcoord : TEXCOORD0;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = v.texcoord;
				return o;
			}

			fixed4 _UpColor;
			fixed4 _DownColor;
			fixed3 _Axis;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed dp = dot(_Axis, i.texcoord);
				fixed t = (dp * 0.5) + 0.5;
				return lerp(_DownColor, _UpColor, t);
			}
			ENDCG
		}
	}
}