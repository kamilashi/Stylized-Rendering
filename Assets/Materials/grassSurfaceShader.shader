Shader "Custom/grassSurfaceShader"
{
	Properties
	{
		_Color1("Color 1", Color) = (0.34, 0.72, 0.27, 1)
		_Color2("Color 2", Color) = (0.49,0.63,0.07, 1)
		_OutlineMapColor1("Outline Map Color 1", Color) = (0.34, 0.72, 0.27, 1)
		_OutlineMapColor2("Outline Map Color 2", Color) = (0.49,0.63,0.07, 1)
		_Fade("Fade", Range(0,5)) = 0.5
		_HeightMap("Texture", 2D) = "black" {}
		_MainTex("Main Text", 2D) = "black" {}
	}
		//regular pass
			SubShader
		{
			Tags{
				
				"OutlineType" = "GrassShader"
				"RenderType" = "Opaque" }

			LOD 200
			Cull Off
			CGPROGRAM
			// Physically based Standard lighting model, and enable shadows on all light types   
			#pragma surface surf Standard vertex:vert //addshadow fullforwardshadows
			#pragma instancing_options procedural:setup
			 //#include "UnityCustomRenderTexture.cginc"

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

			half _Glossiness;
			half _Metallic;
			float4 _Color1;
			float4 _Color2;
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

				float4 landscapeGradient = lerp(_Color1, _Color2, IN.height);
				//o.Albedo = lerp(landscapeGradient, _TipHightlightColor, IN.texcoord.y);
				o.Albedo = landscapeGradient;
				// Metallic and smoothness come from slider variables
				o.Metallic = _Metallic;
				o.Smoothness = _Glossiness;
			}
			ENDCG

		}
		FallBack "Diffuse"
}
