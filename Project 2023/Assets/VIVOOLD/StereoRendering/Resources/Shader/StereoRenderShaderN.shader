// 注意：这些修改需要在Shader的开头进行
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl";
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl";
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInstancing.hlsl";

using UnityEngine.Rendering.Universal;

Shader "Custom/StereoRenderShaderN"
{
	Properties
	{
		_LeftEyeTexture("Left Eye Texture", 2D) = "white" {}
		_RightEyeTexture("Right Eye Texture", 2D) = "white" {}
	}

		CGINCLUDE
#include "UnityCG.cginc"
#include "UnityInstancing.cginc"
		ENDCG

		SubShader
	{
		Tags{ "RenderType" = "Opaque" "UniversalPipeline" }

		Cull OFF

		CGPROGRAM
		#pragma surface surf Lambert

		#pragma multi_compile __ STEREO_RENDER
		#pragma target 3.0

		sampler2D _LeftEyeTexture;
		sampler2D _RightEyeTexture;

		struct Input
		{
			float2 uv_MainTex;
			UNITY_POSITION_TYPE_POSITION worldPos; // 修改为UNITY_POSITION_TYPE_POSITION类型
		};

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			float4 worldPos = mul(unity_ObjectToWorld, IN.worldPos);
			float4 clipPos = TransformWorldToHClip(worldPos);

			float2 screenUV = clipPos.xy / clipPos.w;

			#ifdef SHADERGRAPH_PREVIEW
				screenUV = IN.uv_MainTex;
			#endif

				// ...

				if (unity_StereoEyeIndex == 0)
				{
					fixed4 color = tex2D(_LeftEyeTexture, screenUV);
					o.Albedo = color.xyz;
				}
				else
				{
					fixed4 color = tex2D(_RightEyeTexture, screenUV);
					o.Albedo = color.xyz;
				}
			}

			ENDCG
	}

		Fallback "UniversalForward"
}