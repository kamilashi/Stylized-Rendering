Shader "Unlit/outlineShader"
{
	Properties
	{
		//grass
		_Color1("Color 1", Color) = (0.34, 0.72, 0.27, 1)
		_Color2("Color 2", Color) = (0.49,0.63,0.07, 1)
		_OutlineMapColor1("Outline Map Color 1", Color) = (0.34, 0.72, 0.27, 1)
		_OutlineMapColor2("Outline Map Color 2", Color) = (0.49,0.63,0.07, 1)
		_Fade("Fade", Range(0,5)) = 0.5
		_HeightMap("Texture", 2D) = "black" {}
		_MainTex("Main Text", 2D) = "black" {}

		//ground
		_Color("Color", Color) = (0.34, 0.72, 0.27, 1)
		_PlaneScale("Plane Scale",  Range(0.01, 50)) = 10.0
		_HeightModifier("Height Modifier",  Range(0.1, 10)) = 10.0
	}
		SubShader
	{
		Tags{
			"OutlineType" = "GrassShader"
			"RenderType" = "Opaque" }

		LOD 200
		Cull Off
		// Outline Pass
		CGPROGRAM
		#pragma surface surf Standard vertex:vert //addshadow fullforwardshadows
		#pragma instancing_options procedural:setup

		sampler2D _MainTex;
		float4 _MainTex_ST;
		sampler2D _HeightMap;
		float4 _HeightMap_ST;
		float _planeSideSize;

		struct Input
		{
			float2 texcoord;
			float height;
		};

		float4 _Color1;
		float4 _Color2;
		float4 _OutlineMapColor1;
		float4 _OutlineMapColor2;
		float _Tallness;
		float _Scale;
		float _Fade;
		float4x4 _Matrix;
		float3 _Position;

		float4x4 create_matrix(float3 pos, float theta) {
			float c = cos(theta);
			float s = sin(theta);
			return float4x4(
				c,-s, 0, pos.x,
				s, c, 0, pos.y,
				0, 0, 1, pos.z,
				0, 0, 0, 1
			);
		}

		#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			struct GrassBlade
			{
				float3 position;
				float lean;
				float noise;
				float fade;
				float tallness;
			};
			StructuredBuffer<GrassBlade> bladesBuffer;
		#endif

		void vert(inout appdata_full v, out Input data)
		{
			UNITY_INITIALIZE_OUTPUT(Input, data);

			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED

				float4 rotatedVertex = mul(_Matrix, v.vertex);
				v.vertex.xyz *= _Scale;
				v.vertex.y *= _Tallness;
				//v.vertex.x *= (1-v.texcoord.y)*2.0f;
				v.vertex.xyz += _Position;
			   v.vertex = lerp(v.vertex, rotatedVertex, v.texcoord.y);
			   data.height = v.vertex.y;
			#endif
			data.texcoord = v.texcoord;
		}

		void setup()
		{
			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				GrassBlade blade = bladesBuffer[unity_InstanceID];
				_Matrix = create_matrix(blade.position, blade.lean);
				_Position = blade.position;
				//_Fade = blade.fade;
				//_Fade = 0.0f;
				_Tallness = blade.tallness;// *float4 (0.34, 0.72, 0.27, 1);
			#endif
		}

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			// Albedo comes from a texture tinted by color
			//fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color * _Fade;
		   // float height = tex2D(_HeightMap,(IN.uv)).x + 0.5;

			o.Albedo = lerp(_OutlineMapColor1, _OutlineMapColor2, IN.texcoord.y);
			// Metallic and smoothness come from slider variables
			o.Metallic = 0;
			o.Smoothness = 0;
		}
		ENDCG
	}

	SubShader
	{
		Tags { "OutlineType" = "GroundShader" "RenderType" = "Transparent" }
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

			v2f vert(appdata v)
			{
				v2f o;
				float height = tex2Dlod(_HeightMap, float4(v.uv, 0.0, 0.0)).x;
				v.position.y = height * _HeightModifier;
				o.position = UnityObjectToClipPos(v.position);
				o.uv = v.uv;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				// sample the texture
				//fixed4 col = tex2D(_HeightMap, i.uv);
				float4 col = _Color;//ground color for now

				return col;
			}
			ENDCG
		}
	}
FallBack "Diffuse"
}
