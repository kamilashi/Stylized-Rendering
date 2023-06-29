Shader "Unlit/foliageSurface"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color1("Color 1", Color) = (0.34, 0.72, 0.27, 1)
        //_Color0("Color 0", Color) = (0.34, 0.72, 0.27, 1)
        _DistortionSpeed("Distortion Speed", Float) = 1
        [Toggle(USE_TEXTURE)] _UseTexture("Use Texture", Float) = 0


        _OutlineMapColor1("Outline Map Color 1", Color) = (0.34, 0.72, 0.27, 1)
        //_OutlineMapColor0("Outline Map Color 0", Color) = (0.34, 0.72, 0.27, 1)
        //_OutlineMapDepthThreshold("Depth Threshold",  Range(0.1, 10)) = 10.0
        _TransparencyRamp("Ramp Threshold",  Range(0.01, 20)) = 10.0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent"  "OutlineType" = "Foliage" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        CGPROGRAM

        #pragma surface surf Lambert alpha:fade
        #include "../Scripts/Noises.cginc"
        #include "../Scripts/HelperFunctions.cginc"
        #include "UnityCG.cginc"

         struct Input {
          float4 vertex : SV_POSITION;
          float4 color : COLOR;
          float2 uv_MainTex;
         };

        sampler2D _MainTex;
        float _UseTexture;
        float4 _Color1;
        //float4 _Color0;
        float _TransparencyRamp;
        float _DistortionSpeed;

        void vert(inout appdata_full v, out Input data)
        {
            data.height = v.vertex.y;
            data.texcoord = v.texcoord;
        }

        void surf(Input IN, inout SurfaceOutput o) {

            float4 clipSpacePos = UnityObjectToClipPos(IN.vertex);
            //float4 worldSpacePos = mul(unity_ObjectToWorld, IN.vertex);

            float xOffset = sin(_Time* _DistortionSpeed);
            IN.vertex.x += xOffset;

            float randomSeed = clipSpacePos.x * clipSpacePos.y;
            float perlinMask = GradientNoise(IN.uv_MainTex + clipSpacePos.xy, 5);
            float alphaMask = radialAlpha(IN.uv_MainTex);
            alphaMask += perlinMask;
            alphaMask = saturate(alphaMask);


            if (_UseTexture) {
                o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
            }
            else {
                //o.Albedo = lerp(_Color0, _Color1, IN.vertex.y).rgb;
                o.Albedo = _Color1.rgb;
                //o.Albedo = worldSpacePos.rgb;
                //o.Albedo = float3(perlinMask.xxx);
            }
          o.Alpha = step(_TransparencyRamp , alphaMask);
        }

        ENDCG
    }

        Fallback "Diffuse"
}
