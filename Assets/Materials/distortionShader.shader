// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/distortionShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _AlphaTex ("Alpha Texture", 2D) = "white" {}
         _ScaleX("Scale X", Float) = 1.0
         _ScaleY("Scale Y", Float) = 1.0
    }
    SubShader
    {
        Tags {
            "DistortionType" = "Default"
            "RenderType"="Transparent" }
        LOD 100
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "../Scripts/Noises.cginc"
            #include "../Scripts/HelperFunctions.cginc"
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
            sampler2D _AlphaTex;
            float4 _MainTex_ST;
            float _ScaleX;
            float _ScaleY;

            v2f vert (appdata v)
            {
                v2f o;
                float4 clipPosition = UnityObjectToClipPos(v.vertex);

                o.vertex = mul(UNITY_MATRIX_P, mul(UNITY_MATRIX_MV, float4(0.0, 0.0, 0.0, 1.0))  + float4(v.vertex.x, v.vertex.y, 0.0, 0.0) * float4(_ScaleX, _ScaleY, 1.0, 1.0));

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float2 uvCentered = i.uv *2.0f - 1.0f;
                float windSpeed = 10.5f;

                float2 randomOffset = float2(1.0f,1.0f);
                float2 perlinUV = i.uv;

                float perlinNoiseScale = 4.5f;
                //float perlinMask = GradientNoise(perlinUV - float2(0.0f, windSpeed / (10.0f) * _Time), perlinNoiseScale);
                float perlinMask = GradientNoise(perlinUV, perlinNoiseScale);
                //GradientNoise(perlinUV, perlinNoiseScale, perlinMask);

                //float alpha = sin(i.uv.y * 10.0f * (uvCentered.x * uvCentered.x) - windSpeed * _Time - (1 - uvCentered.x * uvCentered.x) + (perlinMask - 0.5) + randomOffset.y);
                //i.uv.x += perlinMask;
                //i.uv.y -= perlinMask;
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed alphaText = tex2D(_AlphaTex, i.uv).x;
                col.w = col.w * 0.7;
                return col;
            }
            ENDCG
        }
    }
}
