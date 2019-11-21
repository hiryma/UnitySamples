Shader "Kayac/Lanczos"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _KernelRadiusX ("KernelRadiusX", int) = 0
        _KernelRadiusY ("KernelRadiusY", int) = 0
        _LanczosType ("LanzcosType", int) = 2
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
            int _KernelRadiusX;
            int _KernelRadiusY;
            int _LanczosType;

            float4 lanczos(float2 uv)
            {
                float2 srcUvStep = _MainTex_TexelSize.xy;
                int radiusX = _KernelRadiusX;
                int radiusY = _KernelRadiusY;
                float pi = 3.141592653589793;
                float pi2 = pi * pi;
                float2 srcUv;
                float4 sum = float4(0.0, 0.0, 0.0, 0.0);
                float weightSum = 0.0;
                float lanczosN = _LanczosType;
                float xScale = lanczosN / radiusX;
                float yScale = lanczosN / radiusY;
                for (int xIndex = -radiusX; xIndex <= radiusX; xIndex++)
                {
                    float x = xIndex * xScale;
                    srcUv.x = uv.x + (xIndex * srcUvStep.x);
                    float weightX;
                    if (x == 0.0)
                    {   
                        weightX = 1.0;
                    }
                    else
                    {
                        weightX = lanczosN * sin(pi * x) * sin(pi * x / lanczosN) / (pi2 * x * x);
                    }
                    float4 lineSum = float4(0.0, 0.0, 0.0, 0.0);
                    float lineWeightSum = 0.0;
                    for (int yIndex = -radiusY; yIndex <= radiusY; yIndex++)
                    {
                        float y = yIndex * yScale;
                        srcUv.y = uv.y + (yIndex * srcUvStep.y);
                        float weightY;
                        if (y == 0.0)
                        {   
                            weightY = 1.0;
                        }
                        else
                        {
                            weightY = lanczosN * sin(pi * y) * sin(pi * y / lanczosN) / (pi2 * y * y);
                        }
                        float4 texel = tex2D(_MainTex, srcUv);
                        lineSum += texel * weightY;
                        lineWeightSum += weightY;
                    }
                    sum += lineSum * weightX;
                    weightSum += lineWeightSum * weightX;
                }
                float4 ret = sum / weightSum;
                return ret;
            }


            fixed4 frag (v2f i) : SV_Target
            {
                return lanczos(i.uv);
            }
            ENDCG
        }
    }
}
