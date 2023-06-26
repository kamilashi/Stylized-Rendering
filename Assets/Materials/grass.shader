Shader "Unlit/grass"
{
	Properties
	{
		_Color1("Color 1", Color) = (0.34, 0.72, 0.27, 1)
		_Color2("Color 2", Color) = (0.49,0.63,0.07, 1)
		_TipHightlightColor("Tip Highlight Color", Color) = (0.49,0.63,0.07, 1)
		_Fade("Gradient Slider", Range(0,1)) = 0.0
		_HeightMap("Texture", 2D) = "black" {}
	}
		SubShader
	{
		Tags{ "RenderType" = "Opaque" }

		LOD 200
		Cull Off

		Pass{
			CGPROGRAM
			// Physically based Standard lighting model, and enable shadows on all light types   
			//#pragma surface surf Standard vertex:vert addshadow fullforwardshadows
			//#pragma instancing_options procedural:setup 
			#pragma target 4.5
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma multi_compile_fwdbase
			#include "HLSLSupport.cginc"
			#include "UnityCG.cginc"
			//#include "UnityCustomRenderTexture.cginc"

			sampler2D _MainTex;
			sampler2D _HeightMap;
			float4 _HeightMap_ST;
			float _planeSideSize;

			struct appdata
			{
				float4 position : POSITION;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
				float height : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			half _Glossiness;
			half _Metallic;
			float4 _Color1;
			float4 _Color2;
			float4 _TipHightlightColor;
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

			//#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				struct GrassBlade
				{
					float3 position;
					float lean;
					float noise;
					float fade;
					float tallness;
				};
				StructuredBuffer<GrassBlade> bladesBuffer;
			//#endif


			/*void setup()
			{
				#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
					GrassBlade blade = bladesBuffer[unity_InstanceID];
					_Matrix = create_matrix(blade.position, blade.lean);
					_Position = blade.position;
					_Tallness = blade.tallness;
				#endif
			}*/

			v2f vert(appdata v)
			{
				//#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				#ifdef UNITY_INSTANCING_ENABLED
					UNITY_SETUP_INSTANCE_ID(v);

					GrassBlade blade = bladesBuffer[unity_InstanceID];
					_Matrix = create_matrix(blade.position, blade.lean);
					_Position = blade.position;
					_Tallness = blade.tallness;

					//float4 rotatedVertex = mul(_Matrix, v.position);
					//v.position.xyz *= _Scale;
					//v.position.y *= _Tallness;
					v.position.x = (float)unity_InstanceID;
					v.position.xyz += _Position;
				    //v.vertex = lerp(v.vertex, rotatedVertex, v.uv.y);
				#endif

				v2f o;

				#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				UNITY_TRANSFER_INSTANCE_ID(v, o); 
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				#endif

				o.position = UnityObjectToClipPos(v.position);
			    o.height = v.position.y;
				o.uv = v.uv;

				return o;
			}

			float4 frag(v2f i) : SV_Target
			{ 
				UNITY_SETUP_INSTANCE_ID(i);

				float4 col = lerp(_Color1, _Color2, i.height);
				col = lerp(col, _TipHightlightColor, i.uv.y);

				#ifdef UNITY_INSTANCING_ENABLED	
				col = float4(1, 1, 1, 1);
				#endif
				return col;
			}


			//void surf(Input IN, inout SurfaceOutputStandard o)
			//{
			//	// Albedo comes from a texture tinted by color
			//	//fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color * _Fade;
			//   // float height = tex2D(_HeightMap,(IN.uv)).x + 0.5;

			//	o.Albedo = lerp(_Color1, _Color2, IN.height).xyz;
			//	o.Albedo = lerp(o.Albedo, _TipHightlightColor.xyz, IN.uv.y);
			//	// Metallic and smoothness come from slider variables
			//	o.Metallic = _Metallic;
			//	o.Smoothness = _Glossiness;
			//}



			ENDCG
		}

	}
}
