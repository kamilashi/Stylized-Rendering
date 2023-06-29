Shader "Unlit/foliageSurface"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color1("Color 1", Color) = (0.34, 0.72, 0.27, 1)
        //_Color0("Color 0", Color) = (0.34, 0.72, 0.27, 1)
        _SwingFreq("Swinging Speed",  Range(0.0, 30)) = 1
        _SwingAmp("Swinging Amplitude",  Range(0.0, 20)) = 1
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

        #pragma surface surf Lambert vertex:vert alpha:fade
        #include "foliage.cginc"



        ENDCG
    }

        Fallback "Diffuse"
}
