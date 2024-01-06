Shader "Unlit/FlatColortWithSilhouetteOutline"
{
    Properties
    {
        _Color("Main Color", Color) = (0.34, 0.72, 0.27, 1)
        _OutlineMapColor("Outline Map Color", Color) = (0.34, 0.72, 0.27, 1)
    }
    SubShader
    {
        Tags { "OutlineType" = "SilhouetteOutline"  "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc" // for _LightColor0

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

            float4 _Color;
            //float4 _OutlineMapColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = _Color;
                return col;
            }
            ENDCG
        }
    }
}
