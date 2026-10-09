//RealToon HDRP RT - GBuPas
//MJQStudioWorks

#include "Assets/RealToon/RealToon Shaders/RealToon Core/HDRP/RT_HDRP_Core.hlsl"
#include "Assets/RealToon/RealToon Shaders/RealToon Core/HDRP/RT_HDRP_RAYTRALIGLO.hlsl"

[shader("closesthit")]
void ClosestHitGBuffer(inout RayIntersectionGBuffer rayIntersectionGbuffer : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
	UNITY_XR_ASSIGN_VIEW_INDEX(DispatchRaysIndex().z);

	IntersectionVertex currentVertex;
    FragInputs fragInput;
    GetCurrentVertexAndBuildFragInputs(attributeData, currentVertex, fragInput);

	const float3 incidentDir = WorldRayDirection();

	PositionInputs posInput;
	posInput.positionWS = fragInput.positionRWS;
	posInput.positionSS = uint2(0, 0);

	SurfaceData surfaceData;
	BuiltinData builtinData;
	bool isVisible;
	RayCone cone;
	cone.width = 0.0;
	cone.spreadAngle = 0.0;
	GetSurfaceAndBuiltinData(fragInput, -incidentDir, posInput, surfaceData, builtinData, currentVertex, cone, isVisible);
	
	#if _UVSET_UV0
		fragInput.texCoord0 = currentVertex.texCoord0;
	#elif _UVSET_UV1
		fragInput.texCoord0 = fragInput.texCoord1;
	#endif
	
	
#ifdef HAS_LIGHTLOOP
	//light cal

	float3 normalWS = normalize(mul(currentVertex.normalOS, (float3x3)WorldToObject3x4()));
	float3 tangentWS = normalize(mul(currentVertex.tangentOS.xyz, (float3x3)WorldToObject3x4()));
	float3 bitangentWS = cross(normalWS, tangentWS) * ( sign(currentVertex.tangentOS.w) * GetOddNegativeScale() );
	
	
	//RT_NM
	float3 normalLocal = RT_NM( fragInput.texCoord0.xy, fragInput.positionRWS.xyz, fragInput.tangentToWorld, surfaceData.normalWS );
	//==
	
	
	float3 normalDirection = SafeNormalize(TransformTangentToWorld( normalLocal, fragInput.tangentToWorld ));
	float3 viewDirection = -incidentDir;
    float3 viewReflectDirection = reflect( -viewDirection, normalDirection );
	
	float4 finalRGBA; 
	RT_RAYTLIG(fragInput, posInput, surfaceData, builtinData, normalWS, tangentWS, bitangentWS, normalDirection, viewDirection, viewReflectDirection, finalRGBA);

	//===================================================================

	rayIntersectionGbuffer.gbuffer0 = 0.0; //float4(finalRGBA.rgb,1.0); //Futher Checking

	NormalData normalData;
	normalData.normalWS = normalDirection;
	normalData.perceptualRoughness = 1.0;
	EncIntNormBu(normalData, uint2(0, 0), rayIntersectionGbuffer.gbuffer1);

	rayIntersectionGbuffer.gbuffer2 = float4(0.0,0.0,0.0,0.0);
	rayIntersectionGbuffer.gbuffer3 = float4(finalRGBA.rgb, 1.0) * GetCurrentExposureMultiplier();

	#ifdef LIGHT_LAYERS
		OUT_GBUFFER_LIGHT_LAYERS = float4(0.0, 0.0, 0.0, standardBSDFData.renderingLayers / 255.0);
	#endif

	//===================================================================

	#if N_F_SL_ON
		rayIntersectionGbuffer.t = -1; //-RayTCurrent()
	#else
		rayIntersectionGbuffer.t = RayTCurrent();
	#endif

	//===================================================================

#else

	rayIntersectionGbuffer.gbuffer3 = float4(1.0, 1.0, 1.0, 1.0);

#endif

}

[shader("anyhit")]
void AnyHitGBuffer(inout RayIntersectionGBuffer rayIntersectionGbuffer : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
	#ifndef N_F_TRANS_ON
		IgnoreHit();
	#else
	
		UNITY_XR_ASSIGN_VIEW_INDEX(DispatchRaysIndex().z);

		IntersectionVertex currentVertex;
		FragInputs fragInput;
		GetCurrentVertexAndBuildFragInputs(attributeData, currentVertex, fragInput);

		const float3 incidentDir = WorldRayDirection();

		PositionInputs posInput;
		posInput.positionWS = fragInput.positionRWS;
		posInput.positionSS = uint2(0, 0);

		SurfaceData surfaceData;
		BuiltinData builtinData;
		bool isVisible;
		RayCone cone;
		cone.width = 0.0;
		cone.spreadAngle = 0.0;
		GetSurfaceAndBuiltinData(fragInput, -incidentDir, posInput, surfaceData, builtinData, currentVertex, cone, isVisible);

		#if _UVSET_UV0
			fragInput.texCoord0 = currentVertex.texCoord0;
		#elif _UVSET_UV1
			fragInput.texCoord0 = fragInput.texCoord1;
		#endif
	
		float4 objPos = mul ( GetObjectToWorldMatrix(), float4(0.0,0.0,0.0,1.0) );
		float4 projPos = ComScrPos( TransformWorldToHClip( WorldRayOrigin() ) );
		float2 sceneUVs = (projPos.xy / projPos.w);

		float2 RTD_OB_VP_CAL = distance(objPos.xyz, GetCurrentViewPosition());
		float2 RTD_VD_Cal = -(float2((sceneUVs.x * 2.0 - 1.0)*(_ScreenParams.r/_ScreenParams.g), sceneUVs.y * 2.0 - 1.0).xy * RTD_OB_VP_CAL );
		float2 RTD_TC_TP_OO = lerp( fragInput.texCoord0.xy, RTD_VD_Cal, _TexturePatternStyle );
	
		#ifdef N_F_TP_ON
			float4 _MainTex_var = RT_Tripl_Default(_MainTex, sampler_MainTex, fragInput.positionRWS.xyz, SafeNormalize( mul( float3(0.0,0.0,1.0), fragInput.tangentToWorld) ));
		#else
			float4 _MainTex_var = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex , TRANSFORM_TEX(RTD_TC_TP_OO, _MainTex) );
		#endif

	
		//RT_TRANS_CO
		float RTD_TRAN_OPA_Sli;
		bool bo_co_val;
		float RTD_CO;	
		float3 GLO_OUT = (float3)0.0;
		RT_TRANS_CO(fragInput.texCoord0.xy, _MainTex_var, RTD_TRAN_OPA_Sli, RTD_CO, bo_co_val, true, fragInput.positionRWS.xyz, SafeNormalize( mul( float3(0.0,0.0,1.0), fragInput.tangentToWorld) ), fragInput.positionSS.xy, GLO_OUT);
		//==
	
	
		if(!bo_co_val)
		{
			IgnoreHit();
		}

	#endif
}