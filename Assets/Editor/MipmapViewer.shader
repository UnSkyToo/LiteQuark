Shader "Lite/MipmapViewer"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 mipuv : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.mipuv = v.uv * _MainTex_TexelSize.zw / 8.0;
                
                return o;
            }

            float4 GetCurMipColor(float miplevel)
            {
                if (miplevel == 0)
                {
                    return float4(0, 0, 1, 1);
                }
                else
                {
                    if (miplevel < 1)
                    {
                        return lerp(float4(0, 0, 1, 1), float4(0, 0, 1, 0.8), miplevel);
                    }
                    else if (miplevel < 2)
                    {
                        return lerp(float4(0, 0, 1, 0.8), float4(0, 0.5, 1, 0.4), miplevel - 1);
                    }
                    else if (miplevel < 3)
                    {
                        return lerp(float4(0, 0.5, 1, 0.4), float4(1, 1, 1, 0), miplevel - 2);
                    }
                    else if (miplevel < 4)
                    {
                        return lerp(float4(1, 1, 1, 0), float4(1, 0.7, 0, 0.2), miplevel - 3);
                    }
                    else if (miplevel < 5)
                    {
                        return lerp(float4(1, 0.7, 0, 0.2), float4(1, 0.3, 0, 0.6), miplevel - 4);
                    }
                    else if (miplevel < 6)
                    {
                        return lerp(float4(1, 0.3, 0, 0.6), float4(1, 0, 0, 0.8), miplevel - 5);
                    }
                    else
                    {
                        return float4(1, 0, 0, 0.8);
                    }
                }
            }

            float4 frag (v2f i) : SV_Target
            {
                float dx = ddx(i.mipuv);
                float dy = ddy(i.mipuv);
                float px = 32 * dx;
                float py = 32 * dy;
                float lod = 0.5 * log2(max(dot(px, px), dot(py, py)));
                
                float4 baseCol = tex2D(_MainTex, i.uv);
                float4 debugCol = GetCurMipColor(lod);

                float4 col = lerp(baseCol, debugCol, debugCol.a);
                col.a = 1;
                
                return col;
            }
            ENDCG
        }
    }
}
