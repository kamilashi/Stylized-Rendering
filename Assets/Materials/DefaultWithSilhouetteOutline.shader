Shader "Unlit/DefaultWithSilhouetteOutline"
{
    Properties
    {
        _Color("Main Color", Color) = (0.34, 0.72, 0.27, 1)
        _HasGlobalShadow("Global Shadow", Float) = 1
        //_ShadowColor("Shadow Color", Color) = (0.0, 0.0, 0.0, 1)
        _MainTex("Texture", 2D) = "white" {}

        _OutlineMapColor("Outline Map Color", Color) = (0.34, 0.72, 0.27, 1)
    }
    SubShader
    {
        Tags { "OutlineType" = "SilhouetteOutline"  "RenderType" = "Opaque" "LightMode" = "ForwardBase" }
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
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                fixed4 diff : COLOR0; // diffuse lighting color
                float4 vertex : SV_POSITION;
            };

            float4 _Color;
            float _HasGlobalShadow;
            float4 _ShadowColor;
            sampler2D _MainTex;
            //float4 _OutlineMapColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                // get vertex normal in world space
                half3 worldNormal = UnityObjectToWorldNormal(v.normal);

                // dot product between normal and light direction for
                // standard diffuse (Lambert) lighting
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));

                float4 finalColor = lerp(_ShadowColor, _Color, nl);

                // factor in the light color
                o.diff = finalColor * _LightColor0;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = i.diff;
                // sample texture
                col *= tex2D(_MainTex, i.uv);
                // multiply by lighting
                // col *= i.diff;
                
                return col;
            }
            ENDCG
        }

        // shadow caster rendering pass, implemented manually
        // using macros from UnityCG.cginc
        Pass
        {
            Tags {"LightMode" = "ShadowCaster"}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct v2f {
                V2F_SHADOW_CASTER;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
