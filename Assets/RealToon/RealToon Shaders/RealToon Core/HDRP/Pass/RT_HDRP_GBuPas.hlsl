//RealToon HDRP - GBuPas
//MJQStudioWorks

#include "Assets/RealToon/RealToon Shaders/RealToon Core/HDRP/RT_HDRP_Other.hlsl"

#if SHADERPASS != SHADERPASS_GBUFFER
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
	VaryingsType varyingsType;

#if defined(HAVE_RECURSIVE_RENDERING)

	if (_EnableRecursiveRayTracing && _RecurRen > 0.0)
	{
		ZERO_INITIALIZE(VaryingsType, varyingsType);
	}
	else
#endif
	{
		
        varyingsType.vmesh = VertMesh(inputMesh);
		
		
		#if _UVSET_UV0
			varyingsType.vmesh.texCoord0 = inputMesh.uv0;
		#elif _UVSET_UV1
			varyingsType.vmesh.texCoord0 = inputMesh.uv1;
		#endif
		
		
		//RT_SE
		#if N_F_SE_ON
			inputMesh.positionOS = RT_SE(varyingsType.vmesh.positionRWS).xyz;
		#endif
		//==
		
		
        varyingsType.vmesh.positionCS += (float4(0, 0, _ObjePosiZCS, 0.0) * 0.0001);
		
		
		//RT_PA
		#if N_F_PA_ON
			varyingsType.vmesh.positionCS = mul(RT_PA(-TransformWorldToView(varyingsType.vmesh.positionRWS).z), float4(inputMesh.positionOS.xyz, 1.0));
		#endif
		//==
		
		
	}

	return PackVaryingsType(varyingsType);
}

void Frag(PackedVaryingsToPS packedInput  
			,out GBufferType0 GBT0 : SV_Target0
			,out GBufferType1 GBT1 : SV_Target1
			,out GBufferType2 GBT2 : SV_Target2
			,out GBufferType3 GBT3 : SV_Target3
		#ifdef _DEPTHOFFSET_ON
			,out float outputDepth : DEPTH_OFFSET_SEMANTIC
		#endif
)
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
	
	FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput);
	PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

	#ifdef VARYINGS_NEED_POSITION_WS
		float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
	#else
		float3 V = float3(1.0, 1.0, 1.0);
	#endif

	ZERO_INITIALIZE(GBufferType0,GBT0);
	ZERO_INITIALIZE(GBufferType1,GBT1);
	ZERO_INITIALIZE(GBufferType2,GBT2);
	ZERO_INITIALIZE(GBufferType3,GBT3);

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);
	
    
	//RT_NM
	#if N_F_NM_ON
		float3 normalTS = float3(0.0,0.0,0.0);
		normalTS = RT_NM(input.texCoord0.xy, input.positionRWS.xyz, input.tangentToWorld, surfaceData.normalWS);	
		surfaceData.perceptualSmoothness = _Smoothness;
		surfaceData.normalWS = SafeNormalize(TransformTangentToWorld(normalTS, input.tangentToWorld));
	#else
		surfaceData.normalWS = input.tangentToWorld[2];
	#endif
	//==
	

	#ifndef N_F_R_ON
		surfaceData.perceptualSmoothness = 0.0;
	#elif N_F_R_ON
		if (_ReflectionIntensity == 0.0)
		{
			surfaceData.perceptualSmoothness = 0.0;
		}
	#elif N_F_FR_ON
		if (_ReflectionIntensity == 0.0)
		{
			surfaceData.perceptualSmoothness = 0.0;
		}
	#endif

	
	//RT CO ONLY
    RT_CO_ONLY(input.texCoord0.xy, input.positionRWS.xyz, surfaceData.normalWS, input.positionSS.xy);
	//==
	
	
	//RT_NFD
	#ifdef N_F_NFD_ON
		RT_NFD(packedInput.vmesh.positionCS.xy);
	#endif
	//==
	
	
	//==================================================================================================

	GBT0 = float4(1.0,1.0,1.0,0.0);
	EncodeIntoNormalBuffer(ConvertSurfaceDataToNormalData(surfaceData), posInput.positionSS, GBT1);
	GBT2 = float4(0.0,0.0,0.0,0.0);
	GBT3 = float4(1.0,1.0,1.0,0.0);

	//==================================================================================================

	#ifdef _DEPTHOFFSET_ON
		outputDepth = posInput.deviceDepth;
	#endif
}