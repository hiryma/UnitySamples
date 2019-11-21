Shader "Kayac/Show"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ShowAlpha ("_ShowAlpha", Float) = 0.0
        _Scale ("_Scale", Float) = 1.0
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
            float _Scale;
            float _ShowAlpha;
            
            float4 frag (v2f i) : SV_Target
            {
                float4 t = tex2D(_MainTex, i.uv) * _Scale;
                if (_ShowAlpha >= 0.5)
                {
                    t.xyz = t.w;
                }
                t.w = 1.0;
                t = abs(t);
                return t;
            }
            ENDCG
        }
    }
}
