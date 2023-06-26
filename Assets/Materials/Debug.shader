Shader "Unlit/Debug"
{
    Properties
    {
        _DebugMap("Texture", 2D) = "white" {}
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
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 position : SV_POSITION;
            };

            sampler2D _DebugMap;
            float4 _DebugMap_ST;

            v2f vert (appdata v)
            {
                v2f o;
                //float height = tex2Dlod(_HeightMap, float4(v.uv, 0.0, 0.0)).x;
                //v.position.y += height;
                o.position = UnityObjectToClipPos(v.position);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_DebugMap, i.uv);
                
                return col;
            }
            ENDCG
        }
    }
}
