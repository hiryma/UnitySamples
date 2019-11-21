Shader "Kayac/Sum"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off 
        ZWrite Off 
        ZTest Always

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            
            float4 frag (v2f i) : SV_Target
            {
                float4 s = float4(0.0, 0.0, 0.0, 0.0);
                float2 uv;
                uv.x = (0.5 / _MainTex_TexelSize.z);
                uv.y = i.uv.y;
                for (int j = 0; j < _MainTex_TexelSize.z; j++)
                {
                    float4 c = tex2D(_MainTex, uv);
                    s += c * c;
                    uv.x += 1.0 / _MainTex_TexelSize.z;
                }
                return s;
            }
            ENDCG
        }
    }
}
