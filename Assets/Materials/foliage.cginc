#ifndef FOLIAGE
#define FOLIAGE

#include "../Scripts/Noises.cginc"
#include "../Scripts/HelperFunctions.cginc"
#include "UnityCG.cginc"

struct Input
{
    float4 vertex : SV_POSITION;
    //float4 color : COLOR;
    float2 uv_MainTex;
};

sampler2D _MainTex;
float _UseTexture;
float4 _Color1;
float _TransparencyRamp;
float _SwingFreq;
float _SwingAmp;

float4 _OutlineMapColor1;
//float4 _OutlineMapColor0;
//float _OutlineMapDepthThreshold;

void vert(inout appdata_full v, out Input IN)
{
    float4 clipSpacePos = UnityObjectToClipPos(v.vertex);
    float xOffset = sin(_Time * _SwingFreq) * _SwingAmp;
    v.vertex.x += xOffset;
    IN.vertex = v.vertex;
    IN.uv_MainTex = v.texcoord;
}

void surf(Input IN, inout SurfaceOutput o)
{
    float4 clipSpacePos = UnityObjectToClipPos(IN.vertex);
    float4 worldSpacePos = mul(unity_ObjectToWorld, IN.vertex);
    
    // TO-DO: Make scaling proportional to?
    float perlinMask = GradientNoise(IN.uv_MainTex + worldSpacePos.xy, 3);
    float alphaMask = radialAlpha(IN.uv_MainTex);
    alphaMask += perlinMask;
    alphaMask = saturate(alphaMask);

    #ifdef OUTLINEPASS
         o.Albedo = _OutlineMapColor1.xyz;
		 o.Alpha = step(_TransparencyRamp, alphaMask);
    #else
    if (_UseTexture)
    {
        o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
    }
    else
    { //o.Albedo = lerp(_Color0, _Color1, IN.vertex.y).rgb;
        o.Albedo = _Color1.rgb;
    }
    o.Alpha = step(_TransparencyRamp, alphaMask);
    #endif
}

#endif