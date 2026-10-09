//RealToon HDRP RT - Indir
//MJQStudioWorks

#include "Assets/RealToon/RealToon Shaders/RealToon Core/HDRP/RT_HDRP_Core.hlsl"
#include "Assets/RealToon/RealToon Shaders/RealToon Core/HDRP/RT_HDRP_RAYTRALIGLO.hlsl"

[shader("closesthit")]
void ClosestHitMain(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
	UNITY_XR_ASSIGN_VIEW_INDEX(DispatchRaysIndex().z);

	IntersectionVertex currentVertex;
	FragInputs fragInput;
	
    GetCurrentVertexAndBuildFragInputs(attributeData, currentVertex, fragInput);
    PositionInputs posInput = GetPositionInput(rayIntersection.pixelCoord, _ScreenSize.zw, fragInput.positionRWS);

	float3 viewWS = -WorldRayDirection();
	float3 pointWSPos = fragInput.positionRWS;
    float SWEXPO = saturate(GetCurrentExposureMultiplier());

	rayIntersection.t = RayTCurrent();
	rayIntersection.cone.width += rayIntersection.t * rayIntersection.cone.spreadAngle;

	SurfaceData surfaceData;
	BuiltinData builtinData;
	bool isVisible;
	GetSurfaceAndBuiltinData(fragInput, viewWS, posInput, surfaceData, builtinData, currentVertex, rayIntersection.cone, isVisible);
	
	#if _UVSET_UV0
		fragInput.texCoord0 = currentVertex.texCoord0;
	#elif _UVSET_UV1
		fragInput.texCoord0 = fragInput.texCoord1;
	#endif

#ifdef HAS_LIGHTLOOP

	float3 reflected = float3(0.0, 0.0, 0.0);
	float3 refdif = float3(0.0, 0.0, 0.0);
	float reflectedWeight = 0.0;

	#ifdef MULTI_BOUNCE_INDIRECT

	if (rayIntersection.remainingDepth < _RaytracingMaxRecursion)
	{
		float2 sample = float2(0.0, 0.0);
		sample.x = GetBNDSequenceSample(rayIntersection.pixelCoord, rayIntersection.sampleIndex, rayIntersection.remainingDepth * 2);
		sample.y = GetBNDSequenceSample(rayIntersection.pixelCoord, rayIntersection.sampleIndex, rayIntersection.remainingDepth * 2 + 1);

		float3 sampleDir;
		if (_RayTracingDiffuseLightingOnly)
		{
			sampleDir = SampleHemisphereCosine(sample.x, sample.y, surfaceData.normalWS);
		}

		RayDesc rayDescriptor;
		rayDescriptor.Origin = pointWSPos + surfaceData.normalWS * _RayTracingRayBias;
		rayDescriptor.Direction = sampleDir;
		rayDescriptor.TMin = 0.0f;
		rayDescriptor.TMax = _RaytracingRayMaxLength;

		RayIntersection reflectedIntersection;
		reflectedIntersection.color = float3(0.0, 0.0, 0.0);
		reflectedIntersection.t = -1.0f;
		reflectedIntersection.remainingDepth = rayIntersection.remainingDepth + 1;
		reflectedIntersection.pixelCoord = rayIntersection.pixelCoord;
		reflectedIntersection.sampleIndex = rayIntersection.sampleIndex;     
		reflectedIntersection.cone.spreadAngle = rayIntersection.cone.spreadAngle;
		reflectedIntersection.cone.width = rayIntersection.cone.width;

		bool launchRay = true;
		if (!_RayTracingDiffuseLightingOnly)
			launchRay = dot(sampleDir, surfaceData.normalWS) > 0.0;

		if (launchRay)
			TraceRay(_RaytracingAccelerationStructure
				, RAY_FLAG_CULL_BACK_FACING_TRIANGLES
				, _RayTracingDiffuseLightingOnly ? RAYTRACINGRENDERERFLAG_GLOBAL_ILLUMINATION : RAYTRACINGRENDERERFLAG_REFLECTION
				, 0, 1, 0, rayDescriptor, reflectedIntersection);

		if (_RayTracingDiffuseLightingOnly)
			builtinData.bakeDiffuseLighting = reflectedIntersection.color;
		else
		{
			reflected = reflectedIntersection.color;
			reflectedWeight = 1.0;
		}

	}
	#endif

	//RT_light cal

	float3 normalWS = normalize(mul(currentVertex.normalOS, (float3x3)WorldToObject3x4()));
	float3 tangentWS = normalize(mul(currentVertex.tangentOS.xyz, (float3x3)WorldToObject3x4()));
	float3 bitangentWS = cross(normalWS, tangentWS) * ( sign(currentVertex.tangentOS.w) * GetOddNegativeScale() );
	
	
	//RT_NM
	float3 normalLocal = RT_NM( fragInput.texCoord0.xy, fragInput.positionRWS.xyz, fragInput.tangentToWorld, surfaceData.normalWS );
	//==
	
	
	float3 normalDirection = SafeNormalize(TransformTangentToWorld( normalLocal, fragInput.tangentToWorld ));
	float3 viewDirection = viewWS;
    float3 viewReflectDirection = reflect( -viewDirection, normalDirection );
	
	float4 finalRGBA; 
	RT_RAYTLIG( viewWS, reflected, refdif, fragInput, posInput, surfaceData, builtinData, normalWS, tangentWS, bitangentWS, normalDirection, viewDirection, viewReflectDirection, finalRGBA);
	rayIntersection.color = finalRGBA.rgb;

#else

	rayIntersection.color = float3(1.0,1.0,1.0);

#endif

	ApplyFogAttenuation(WorldRayOrigin(), WorldRayDirection(), rayIntersection.t, rayIntersection.color, true);

}

			
[shader("anyhit")]
void AnyHitMain(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{

	#ifndef N_F_CO_ON
		IgnoreHit();
	#else

	UNITY_XR_ASSIGN_VIEW_INDEX(DispatchRaysIndex().z);

	IntersectionVertex currentVertex;

	FragInputs fragInput;
	GetCurrentVertexAndBuildFragInputs(attributeData, currentVertex, fragInput);

	float3 viewWS = -WorldRayDirection();

	rayIntersection.t = RayTCurrent();

	PositionInputs posInput;
	posInput.positionWS = fragInput.positionRWS;
	posInput.positionSS = rayIntersection.pixelCoord;

	bool isVisible;

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
		float4 _MainTex_var = RT_Tripl_Default(_MainTex, sampler_MainTex, fragInput.positionRWS.xyz, SafeNormalize( mul( float4(0.0,0.0,1.0,0.0), fragInput.tangentToWorld) )  );
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