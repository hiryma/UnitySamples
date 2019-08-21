Shader "Kayac/Sample/PrecisionEmulation"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_MantissaBits ("MantissaBits", float) = 23.0
		_UvScale ("UvScale", float) = 1.0
		_ShowDerivative ("ShowDerivative", float) = 0.0
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
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float _MantissaBits;
			float _UvScale;
			float _ShowDerivative;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv * _UvScale;
				return o;
			}

			float2 emulateQuantize(float2 uv, float mantissaBits){
				// uvを[1,2)に正規化する
				float2 logUv = log2(uv);
				logUv = floor(logUv);
				float2 scale = exp2(logUv); // [1,2)なら1、[0.5,1)なら0.5
				float2 uvNormalized = uv / scale;
				// 小数部を取り出し
				float2 uvFrac = uvNormalized - 1.0;
				// 仮数部の最大値を乗じる
				mantissaBits = floor(mantissaBits); // 整数化
				float mantissaScale = exp2(mantissaBits); //8bitなら256が返る
				float2 ret = uvFrac * mantissaScale; //8bitなら[256,511]になる
				// 整数化して劣化させる。ここでは四捨五入固定だが、そこはGPU依存
				ret = round(ret);
				ret /= mantissaScale; //8bitなら[1,2]になる
				ret += 1.0;
				return ret * scale;
			}
			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = emulateQuantize(i.uv, _MantissaBits);
				if (_ShowDerivative){
					return fixed4(abs(ddx(uv.x)) * 512, abs(ddy(uv.y)) * 512, 0, 1);
				}else{
					return tex2D(_MainTex, uv);
				}
			}
			ENDCG
		}
	}
}
