Shader "Kayac/Diff"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _CompareTex ("Texture", 2D) = "white" {}
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
            sampler2D _CompareTex;
            float4 _CompareTex_TexelSize;
            
            float4 frag (v2f i) : SV_Target
            {
                float4 t0 = tex2D(_MainTex, i.uv);
                float4 t1 = tex2D(_CompareTex, i.uv);
                float4 d = t0 - t1;
                return d;
            }
            ENDCG
        }
    }
}
