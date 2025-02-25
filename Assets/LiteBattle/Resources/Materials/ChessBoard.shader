Shader "Lite/ChessBoard"
{
    Properties
    {
        _BoardSize ("Board Size", Range(2, 128)) = 2
        _ColorFactor ("Color Factor", Range(0, 1)) = 1
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
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _BoardSize;
            float _ColorFactor;

            v2f vert (appdata v)
            {
                v2f o;
                o.positionCS = UnityObjectToClipPos(v.positionOS);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 v2 = floor(i.uv * _BoardSize) / 2;
                float v = frac(v2.x + v2.y) * 2 + _ColorFactor;
                float4 col = float4(v, v, v, 1);
                return col;
            }
            ENDCG
        }
    }
}
