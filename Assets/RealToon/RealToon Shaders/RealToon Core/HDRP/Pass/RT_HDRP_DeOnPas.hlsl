//RealToon HDRP - DeOnPas
//MJQStudioWorks

#include "Assets/RealToon/RealToon Shaders/RealToon Core/HDRP/RT_HDRP_Other.hlsl"

#if (SHADERPASS != SHADERPASS_DEPTH_ONLY && SHADERPASS != SHADERPASS_SHADOWS)
	#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
	VaryingsType varyingsType;

#if (SHADERPASS == SHADERPASS_DEPTH_ONLY) && defined(HAVE_RECURSIVE_RENDERING) && !defined(SCENESELECTIONPASS) && !defined(SCENEPICKINGPASS)

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

#if defined(WRITE_NORMAL_BUFFER) && defined(WRITE_MSAA_DEPTH)
	#define SV_TARGET_DECAL SV_Target2
#elif defined(WRITE_NORMAL_BUFFER) || defined(WRITE_MSAA_DEPTH)
	#define SV_TARGET_DECAL SV_Target1
#else
	#define SV_TARGET_DECAL SV_Target0
#endif
			
void Frag( PackedVaryingsToPS packedInput
            #if defined(SCENESELECTIONPASS) || defined(SCENEPICKINGPASS)
            , out float4 outColor : SV_Target0
            #else
                #ifdef WRITE_MSAA_DEPTH
                , out float4 depthColor : SV_Target0
                    #ifdef WRITE_NORMAL_BUFFER
                    , out float4 outNormalBuffer : SV_Target1
                    #endif
                #else
                    #ifdef WRITE_NORMAL_BUFFER
                    , out float4 outNormalBuffer : SV_Target0
                    #endif
                #endif
            #endif

			#if (defined(WRITE_DECAL_BUFFER) && !defined(_DISABLE_DECALS)) || defined(WRITE_RENDERING_LAYER)
			, out float4 outDecalBuffer : SV_TARGET_DECAL
			#endif

            #if defined(_DEPTHOFFSET_ON) && !defined(SCENEPICKINGPASS)
            , out float outputDepth : DEPTH_OFFSET_SEMANTIC
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
				
	SurfaceData surfaceData;
	BuiltinData builtinData;
	GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

	
	//RT_NM
	#if N_F_NM_ON
		float3 normalTS = float3(0.0,0.0,0.0);
		normalTS = RT_NM(input.texCoord0.xy, input.positionRWS.xyz, input.tangentToWorld, surfaceData.normalWS );	
		surfaceData.perceptualSmoothness = _Smoothness;
		surfaceData.normalWS = SafeNormalize(TransformTangentToWorld(normalTS, input.tangentToWorld));
	#else
		surfaceData.normalWS = input.tangentToWorld[2];
	#endif
	//==
	
		
	//RT CO ONLY
    RT_CO_ONLY(input.texCoord0.xy, input.positionRWS.xyz, surfaceData.normalWS, input.positionSS.xy);
	//==

	
	#if defined(_DEPTHOFFSET_ON) && !defined(SCENEPICKINGPASS)
		outputDepth = posInput.deviceDepth;

		//Remove just now
		//#if SHADERPASS == SHADERPASS_SHADOWS
			//float bias = max(abs(ddx(posInput.deviceDepth)), abs(ddy(posInput.deviceDepth))) * _SlopeScaleDepthBias;
			//outputDepth += bias;
		//#endif
	#endif

	#ifdef SCENESELECTIONPASS
		outColor = float4(_ObjectId, _PassValue, 1.0, 1.0);
	#elif defined(SCENEPICKINGPASS)
		outColor = unity_SelectionID;
	#else
		#ifdef WRITE_MSAA_DEPTH
			depthColor = packedInput.vmesh.positionCS.z;
		#endif

		#if defined(WRITE_NORMAL_BUFFER)
			EncodeIntoNormalBuffer(ConvertSurfaceDataToNormalData(surfaceData), outNormalBuffer);
		#endif
	
		#if (defined(WRITE_DECAL_BUFFER) && !defined(_DISABLE_DECALS)) || defined(WRITE_RENDERING_LAYER)
			DecalPrepassData decalPrepassData;
		#ifdef _DISABLE_DECALS
			ZERO_INITIALIZE(DecalPrepassData, decalPrepassData);
		#else
			decalPrepassData.geomNormalWS = surfaceData.geomNormalWS;
		#endif
			decalPrepassData.renderingLayerMask = GetMeshRenderingLayerMask();
			EncodeIntoDecalPrepassBuffer(decalPrepassData, outDecalBuffer);
		#endif
	
	#endif
	
	
	//RT_NFD
	#ifdef N_F_NFD_ON
		RT_NFD(packedInput.vmesh.positionCS.xy);
	#endif
	//==
	

}