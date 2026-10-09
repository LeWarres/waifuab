//RealToon HDRP RT - Forwa
//MJQStudioWorks

#include "Assets/RealToon/RealToon Shaders/RealToon Core/HDRP/RT_HDRP_Core.hlsl"
#include "Assets/RealToon/RealToon Shaders/RealToon Core/HDRP/RT_HDRP_RAYTRALIGLO.hlsl"

[shader("closesthit")]
void ClosestHitForward(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
	UNITY_XR_ASSIGN_VIEW_INDEX(DispatchRaysIndex().z);

	IntersectionVertex currentVertex;
	FragInputs fragInput;
	
    GetCurrentVertexAndBuildFragInputs(attributeData, currentVertex, fragInput);
    PositionInputs posInput = GetPositionInput(rayIntersection.pixelCoord, _ScreenSize.zw, fragInput.positionRWS);
	
	float3 incidentDirection = WorldRayDirection();
	float3 viewWS = -incidentDirection;
	float3 pointWSPos = fragInput.positionRWS;

    rayIntersection.t = RayTCurrent();
    rayIntersection.cone.width += rayIntersection.t * abs(rayIntersection.cone.spreadAngle);

	SurfaceData surfaceData;
	BuiltinData builtinData;
	bool isVisible;
	GetSurfaceAndBuiltinData(fragInput, viewWS, posInput, surfaceData, builtinData, currentVertex, rayIntersection.cone, isVisible);
	BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);
	
	#if _UVSET_UV0
		fragInput.texCoord0 = currentVertex.texCoord0;
	#elif _UVSET_UV1
		fragInput.texCoord0 = fragInput.texCoord1;
	#endif
   
#ifdef HAS_LIGHTLOOP
	float3 reflected = float3(0.0, 0.0, 0.0);
	uint additionalRayCount = 0;
	float3 transmitted = float3(0.0, 0.0, 0.0);

#if N_F_TRANS_ON

	if (rayIntersection.remainingDepth > 0)
	{
		const float biasSign = sign(dot(fragInput.tangentToWorld[2], -viewWS));

		RayDesc transmittedRay;
		transmittedRay.Origin = pointWSPos + biasSign * fragInput.tangentToWorld[2] * _RayTracingRayBias;
		transmittedRay.Direction = -viewWS;
		transmittedRay.TMin = 0;
		transmittedRay.TMax = _RaytracingRayMaxLength;

		RayIntersection transmittedIntersection;
		transmittedIntersection.color = float3(0.0, 0.0, 0.0);
		transmittedIntersection.t = 0.0f;
		transmittedIntersection.remainingDepth = rayIntersection.remainingDepth - 1;
		transmittedIntersection.rayCount = 1;
		transmittedIntersection.pixelCoord = rayIntersection.pixelCoord;
		transmittedIntersection.cone.spreadAngle = rayIntersection.cone.spreadAngle;
		transmittedIntersection.cone.width = rayIntersection.cone.width;

		TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, RAYTRACINGRENDERERFLAG_RECURSIVE_RENDERING, 0, 1, 0, transmittedRay, transmittedIntersection);

		transmitted = transmittedIntersection.color;
		additionalRayCount += transmittedIntersection.rayCount;
	}

#endif

	if (rayIntersection.remainingDepth > 0 && RecursiveRenderingReflectionPerceptualSmoothness(bsdfData) >= _RaytracingReflectionMinSmoothness)
	{

		float3 reflectedDir = reflect(-viewWS, surfaceData.normalWS);
		const float biasSign = sign(dot(fragInput.tangentToWorld[2], reflectedDir));

		RayDesc reflectedRay;
		reflectedRay.Origin = pointWSPos + biasSign * fragInput.tangentToWorld[2] * _RayTracingRayBias;
		reflectedRay.Direction = reflectedDir;
		reflectedRay.TMin = 0;
		reflectedRay.TMax = _RaytracingRayMaxLength;

		RayIntersection reflectedIntersection;
		reflectedIntersection.color = float3(0.0, 0.0, 0.0);
		reflectedIntersection.t = 0.0f;
		reflectedIntersection.remainingDepth = rayIntersection.remainingDepth - 1;
		reflectedIntersection.rayCount = 1;
		reflectedIntersection.pixelCoord = rayIntersection.pixelCoord;
		reflectedIntersection.cone.spreadAngle = rayIntersection.cone.spreadAngle;
		reflectedIntersection.cone.width = rayIntersection.cone.width;

		TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, RAYTRACINGRENDERERFLAG_RECURSIVE_RENDERING, 0, 1, 0, reflectedRay, reflectedIntersection);

		reflected = reflectedIntersection.color;
		additionalRayCount += reflectedIntersection.rayCount;

	}

	//light cal

	float3 normalWS = normalize(mul(currentVertex.normalOS, (float3x3)WorldToObject3x4()));
	float3 tangentWS = normalize(mul(currentVertex.tangentOS.xyz, (float3x3)WorldToObject3x4()));
	float3 bitangentWS = cross(normalWS, tangentWS) * ( sign(currentVertex.tangentOS.w) * GetOddNegativeScale() );
	
	
	//RT_NM
	float3 normalLocal = RT_NM( fragInput.texCoord0.xy, fragInput.positionRWS.xyz, fragInput.tangentToWorld, float3(0.0, 0.0, 1.0) );
	//==
	
	
	float3 normalDirection = SafeNormalize(TransformTangentToWorld( normalLocal, fragInput.tangentToWorld ));
	float3 viewDirection = viewWS;
    float3 viewReflectDirection = reflect( -viewDirection, normalDirection );
	
	float4 finalRGBA;
	float Trans_Val_Out;
	RT_RAYTLIG( viewWS, reflected, Trans_Val_Out, fragInput, posInput, surfaceData, builtinData, normalWS, tangentWS, bitangentWS, normalDirection, viewDirection, viewReflectDirection, finalRGBA);

	rayIntersection.color = finalRGBA.rgb;
	rayIntersection.rayCount += additionalRayCount;

	#ifdef N_F_TRANS_ON
		rayIntersection.color = lerp(transmitted, rayIntersection.color, Trans_Val_Out);
	#endif

#else
	rayIntersection.color = float3(1.0,1.0,1.0);
#endif
	ApplyFogAttenuation(WorldRayOrigin(), WorldRayDirection(), rayIntersection.t, rayIntersection.color, true);
}

[shader("anyhit")]
void AnyHitMain(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
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
	float2 RTD_OB_VP_CAL = distance(objPos.xyz, GetCurrentViewPosition());
	float2 RTD_TC_TP_OO = fragInput.texCoord0.xy;

	#ifdef N_F_TP_ON
		float4 _MainTex_var = RT_Tripl_Default(_MainTex, sampler_MainTex, fragInput.positionRWS.xyz, SafeNormalize( mul(float3(0.0,0.0,1.0), fragInput.tangentToWorld) ) );
	#else
		float4 _MainTex_var = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex , TRANSFORM_TEX(RTD_TC_TP_OO, _MainTex) );
	#endif
	

	//RT_TRANS_CO
	float RTD_TRAN_OPA_Sli;
	bool bo_co_val;
	float RTD_CO;	
	float3 GLO_OUT = (float3)0.0;
    RT_TRANS_CO(fragInput.texCoord0.xy, _MainTex_var, RTD_TRAN_OPA_Sli, RTD_CO, bo_co_val, true, fragInput.positionRWS.xyz, SafeNormalize(mul(float3(0.0, 0.0, 1.0), fragInput.tangentToWorld)), fragInput.positionSS.xy, GLO_OUT);
	//==
	
	
	if (!bo_co_val)
	{
		IgnoreHit();
	}
}