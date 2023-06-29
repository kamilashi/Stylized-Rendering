Shader "Unlit/outlineShader"
{
	Properties
	{
	}
		SubShader
	{
		Tags{
			"OutlineType" = "GrassShader"
			"RenderType" = "Opaque" }
		LOD 200
		Cull Off
		CGPROGRAM
		#pragma surface surf Lambert vertex:vert //addshadow fullforwardshadows
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

		//float4 _Color1;
		//float4 _Color2;
		float _Tallness;
		float _Scale;
		float _Fade;
		float4x4 _Matrix;
		float3 _Position;

		float4 _OutlineMapColor1;
		float4 _OutlineMapColor2;
		float _OutlineGradientSlider;

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
				_Tallness = blade.tallness;// *float4 (0.34, 0.72, 0.27, 1);
			#endif
		}

		void surf(Input IN, inout SurfaceOutput o)
		{
			o.Albedo = lerp(_OutlineMapColor1, _OutlineMapColor2, (IN.texcoord.y - _OutlineGradientSlider));
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

			//float4 _Color;

			sampler2D _HeightMap;
			float4 _HeightMap_ST;
			float _PlaneScale;
			float _HeightModifier;
			float4 _OutlineMapColor;

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
				float4 col = _OutlineMapColor;//ground color for now

				return col;
			}
			ENDCG
		}
	}

		SubShader
			{
				Tags { "OutlineType" = "UnlitOutline" "RenderType" = "Opaque" }
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
						float4 position : SV_POSITION;
						float2 uv : TEXCOORD0;
					};

					//float4 _Color;
					float4 _OutlineMapColor;

					v2f vert(appdata v)
					{
						v2f o;
						o.position = UnityObjectToClipPos(v.position);
						o.uv = v.uv;
						return o;
					}

					float4 frag(v2f i) : SV_Target
					{
						float4 col = _OutlineMapColor; 
						return col;
					}
					ENDCG
				}
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

						float4 _OutlineMapColor1;
						//float4 _OutlineMapColor0;
						//float _OutlineMapDepthThreshold;
						float _DistortionSpeed;
						float _TransparencyRamp;

						void surf(Input IN, inout SurfaceOutput o) {
							

							float4 clipSpacePos = UnityObjectToClipPos(IN.vertex);
							//float4 worldSpacePos = mul(unity_ObjectToWorld, IN.vertex);

							float xOffset = sin(_Time * _DistortionSpeed);
							IN.vertex.x += xOffset;

							float randomSeed = clipSpacePos.x * clipSpacePos.y;
							float perlinMask = GradientNoise(IN.uv_MainTex + clipSpacePos.xy, 5);
							float alphaMask = radialAlpha(IN.uv_MainTex);
							alphaMask += perlinMask;
							alphaMask = saturate(alphaMask);

							 o.Albedo = _OutlineMapColor1.xyz;
							 o.Alpha = step(_TransparencyRamp, alphaMask);
						}

						ENDCG
					}
FallBack "Diffuse"

}
