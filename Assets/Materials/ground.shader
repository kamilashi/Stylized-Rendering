Shader "Unlit/ground"
{
    Properties
    {
        _HeightMap("Texture", 2D) = "black" {}
        _Color("Color", Color) = (0.34, 0.72, 0.27, 1)
        _PlaneScale("Plane Scale",  Range(0.01, 50)) = 10.0
        _HeightModifier("Height Modifier",  Range(0.1, 10)) = 10.0
    }
    SubShader
    {
        Tags { "OutlineType" = "GroundShader" "RenderType"="Transparent" }
        LOD 100
        Cull OFF

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

            sampler2D _HeightMap;
            float4 _HeightMap_ST;
            float4 _Color;
            float _PlaneScale;
            float _HeightModifier;

            v2f vert (appdata v)
            {
                v2f o;
                float height = tex2Dlod(_HeightMap, float4(v.uv, 0.0, 0.0)).x;
                v.position.y = height*_HeightModifier;
                o.position = UnityObjectToClipPos(v.position);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // sample the texture
                //fixed4 col = tex2D(_HeightMap, i.uv);
                float4 col = _Color;//ground color for now
                
                return col;
            }
            ENDCG
        }
    }
}
