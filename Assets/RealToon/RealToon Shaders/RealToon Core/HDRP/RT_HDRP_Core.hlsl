//RealToon HDRP - Core
//MJQStudioWorks

//=========================

#ifndef REALTOON_HDRP_CORE_HLSL
#define REALTOON_HDRP_CORE_HLSL

float RTD_LVLC_F( float3 Light_Color_f3 )
{
	float4 node_3149_k = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
	float4 node_3149_p = lerp(float4(float4(Light_Color_f3.rgb,0.0).zy, node_3149_k.wz), float4(float4(Light_Color_f3.rgb,0.0).yz, node_3149_k.xy), step(float4(Light_Color_f3.rgb,0.0).z, float4(Light_Color_f3.rgb,0.0).y));
	float4 node_3149_q = lerp(float4(node_3149_p.xyw, float4(Light_Color_f3.rgb,0.0).x), float4(float4(Light_Color_f3.rgb,0.0).x, node_3149_p.yzx), step(node_3149_p.x, float4(Light_Color_f3.rgb,0.0).x));
	float node_3149_d = node_3149_q.x - min(node_3149_q.w, node_3149_q.y);
	float node_3149_e = 1.0e-10;
	float3 node_3149 = float3(abs(node_3149_q.z + (node_3149_q.w - node_3149_q.y) / (6.0 * node_3149_d + node_3149_e)), node_3149_d / (node_3149_q.x + node_3149_e), node_3149_q.x);

	return saturate(node_3149.b);
}

float4 EL_AT_SC(PositionInputs posInput, float3 V, float4 inputColor)
{
	float4 result = inputColor;

	#if defined(N_F_TRANS_ON) && !defined(N_F_CO_ON)
		
		float3 volColor, volOpacity;
		EvaluateAtmosphericScattering(posInput, V, volColor, volOpacity);

		result.rgb = result.rgb * (1.0 - volOpacity) + volColor * result.a;
				
	#endif

	return result;
}

float4 ComScrPos(float4 positionCS)
{
	float4 o = positionCS * 0.5f;
	o.xy = float2(o.x, o.y * _ProjectionParams.x) + o.w;
	o.zw = positionCS.zw;
	return o;
}

//Temporary Solution
real Po_Ir(real4x3 L)
{
	real3 L4 = real3(0,0,0);

	#ifdef APPROXIMATE_POLY_LIGHT_AS_SPHERE_LIGHT
		L[0] = SafeNormalize(L[0]);
		L[1] = SafeNormalize(L[1]);
		L[2] = SafeNormalize(L[2]);

		switch (4)
		{
			case 3:
				L[3] = L[0];
				break;
			case 4:
				L[3] = SafeNormalize(L[3]);
				L4   = L[0];
				break;
			case 5:
				L[3] = SafeNormalize(L[3]);
				L4   = SafeNormalize(L4);
				break;
		}

		real3 F  = ComputeEdgeFactor(L[0], L[1]);
			  F += ComputeEdgeFactor(L[1], L[2]);
			  F += ComputeEdgeFactor(L[2], L[3]);
		if (4 >= 4)
			  F += ComputeEdgeFactor(L[3],L4);
		if (4 == 5)
			  F += ComputeEdgeFactor(L4, L[0]);

		float l = length(F);
		return max(0.0, (l * l + F.z) / (l + 1.0  * (0.16 * 0.02) ) );

	#endif
}

real3 AL_GI( float3 N )
{
	
	//will remove soon if not use
	/*
	real4 SHCoefficients[7];
	SHCoefficients[0] = unity_SHAr;
	SHCoefficients[1] = unity_SHAg;
	SHCoefficients[2] = unity_SHAb;
	SHCoefficients[3] = unity_SHBr;
	SHCoefficients[4] = unity_SHBg;
	SHCoefficients[5] = unity_SHBb;
	SHCoefficients[6] = unity_SHC;

	return max(float3(0.0, 0.0, 0.0), SampleSH9(SHCoefficients, N));
	*/
	
    real3 res = SHEvalLinearL0L1(N, unity_SHAr, unity_SHAg, unity_SHAb);
    res += SHEvalLinearL2(N, unity_SHBr, unity_SHBg, unity_SHBb, unity_SHC);
	
    return res;
	
}

void EncIntNormBu(NormalData normalData, uint2 positionSS, out float4 outNormalBuffer0)
{
    float2 octNormalWS = PackNormalOctQuadEncode(normalData.normalWS);
    float3 packNormalWS = PackFloat2To888(saturate(octNormalWS * 0.5 + 0.5));

    outNormalBuffer0 = float4(packNormalWS, 1.0);
}

float2 PixPos(float2 positionCS)
{
	#if UNITY_UV_STARTS_AT_TOP
		return float2(positionCS.x, (_ProjectionParams.x < 0) ? (_ScreenParams.y - positionCS.y) : positionCS.y);
	#else
		return float2(positionCS.x, (_ProjectionParams.x > 0) ? (_ScreenParams.y - positionCS.y) : positionCS.y);
	#endif
}

//RT_Tripl_Default
half4 RT_Tripl_Default(TEXTURE2D_PARAM( tex, samp), float3 positionWS, float3 normalWS)
{
	float3 UV = GetAbsolutePositionWS(positionWS) * _TriPlaTile;
    
	float3 Blend = pow(abs(normalWS), _TriPlaBlend);
	Blend /= dot(Blend, 1.0);
    
	float4 X = SAMPLE_TEXTURE2D(tex, samp, UV.zy);
	float4 Y = SAMPLE_TEXTURE2D(tex, samp, UV.xz);
	float4 Z = SAMPLE_TEXTURE2D(tex, samp, UV.xy);

	return X * Blend.x + Y * Blend.y + Z * Blend.z;
}

//Dither_Setup
void Dither_Float(float In, float4 ScreenPosition, out float Out)
{
	float2 uv = ScreenPosition.xy * _ScreenParams.xy;
	float DITHER_THRESHOLDS[16] =
	{
		1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
		13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
		4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
		16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0,
    };
	uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
	
	Out = In - DITHER_THRESHOLDS[index];
}

//Dither_Out
float RT_Dither_Out(float2 positionCS)
{
    float2 PixelPositions = PixPos(positionCS);

    float2 NDCPositions;
    NDCPositions = PixelPositions.xy / _ScreenParams.xy;
    NDCPositions.y = 1.0f - NDCPositions.y;
        
    float dither_out;
    Dither_Float(1, float4(NDCPositions.xy, 0, 0), dither_out);
    return dither_out;
}

//RT_ViewVecWorl
float3 RT_ViewVecWorl(float3 WorldSpacePosition)
{
    float3 sub = _WorldSpaceCameraPos.xyz - GetAbsolutePositionWS(WorldSpacePosition);
    if (!IsPerspectiveProjection())
    {
        sub = GetViewForwardDir() * dot(sub, GetViewForwardDir());
    }
	
    return sub;
}

//RT_Anis_Shift
float3 RTShifTan(float3 bitangentWS, float3 normalDirection, float NoiTex)
{
    float3 ShifTan = bitangentWS + NoiTex * normalDirection;
    return normalize(ShifTan);
}

//RT_Anis_Stra       
float StraSpec(float3 bitangentWS, float3 viewDirection, float3 halfDirection, float StraPow)
{
    float3 H = lerp(viewDirection, halfDirection, _GlossTextureFollowLight);
    float dotTH = dot(bitangentWS, H);
    float sinTH = sqrt(1 - dotTH * dotTH);
    float dirAtten = smoothstep(-1, 0, dotTH);
    return dirAtten * pow(sinTH, StraPow);
}

//=========================


//EdgDet/SSOL
float SampleClampedDepth(float2 uv)
{
    return SampleCameraDepth(clamp(uv, _ScreenSize.zw, 1.0 - _ScreenSize.zw)).r;
}

float EdgDet(float2 uv, float4 PosCS)
{
    float2 _ScrSi = _OutlineWidth / float2(1920, 1080);
	
    float depth = SampleClampedDepth(uv); //LoadCameraDepth(PosCS.xy);
	float obj_only = depth != UNITY_RAW_FAR_CLIP_VALUE;

	float halfScaleFloor = floor(_OutlineWidth * 0.5);
	float halfScaleCeil = ceil(_OutlineWidth * 0.5);

    float2 bottomLeftUV = uv - float2(_ScrSi.x, _ScrSi.y) * halfScaleFloor;
    float2 topRightUV = uv + float2(_ScrSi.x, _ScrSi.y) * halfScaleCeil;
    float2 bottomRightUV = uv + float2(_ScrSi.x * halfScaleCeil, -_ScrSi.y * halfScaleFloor);
    float2 topLeftUV = uv + float2(-_ScrSi.x * halfScaleFloor, _ScrSi.y * halfScaleCeil);

    float depth0 = SampleClampedDepth(bottomLeftUV);
    float depth1 = SampleClampedDepth(topRightUV);
    float depth2 = SampleClampedDepth(bottomRightUV);
    float depth3 = SampleClampedDepth(topLeftUV);

	float depthDerivative0 = depth1 - depth0;
	float depthDerivative1 = depth3 - depth2;

	float edgeDepth = sqrt(pow(depthDerivative0, 2.0) + pow(depthDerivative1, 2.0)) * 100;
    edgeDepth = (edgeDepth > (depth0 * (_DepthThreshold * 0.01)) ? 1 : 0) - Linear01Depth(SampleClampedDepth(uv), _ZBufferParams);
    edgeDepth *= obj_only;
	
	NormalData normalData0, normalData1, normalData2, normalData3;
    DecodeFromNormalBuffer(_ScreenSize.xy * bottomLeftUV, normalData0);
    DecodeFromNormalBuffer(_ScreenSize.xy * topRightUV, normalData1);
    DecodeFromNormalBuffer(_ScreenSize.xy * bottomRightUV, normalData2);
    DecodeFromNormalBuffer(_ScreenSize.xy * topLeftUV, normalData3);

	float3 normalFiniteDifference0 = (normalData1.normalWS - normalData0.normalWS);
	float3 normalFiniteDifference1 = (normalData3.normalWS - normalData2.normalWS);

	float edgeNormal = sqrt(dot(normalFiniteDifference0, normalFiniteDifference0) + dot(normalFiniteDifference1, normalFiniteDifference1));
	edgeNormal = smoothstep(_NormalMin, _NormalMax, edgeNormal * _NormalThreshold);
	edgeNormal *= obj_only;

	#ifdef N_F_CO_ON
		return max(edgeNormal, edgeDepth);
	#elif !N_F_CO_ON && N_F_TRANS_ON
		return 0.0;
	#else
		return max(edgeNormal, edgeDepth);
	#endif
}
//

float RT_SSRLig(float2 uv, float4 PosCS)
{
    float2 _ScrSi = ( (1.0 - _RimLightUnfill + 5) / float2(1920, 1080) + ((_RimLigPosi.xy - 1.0) * 0.01) );
	
    float depth = SampleClampedDepth(uv); //LoadCameraDepth(PosCS.xy);

    float halfScaleFloor = floor((1.0 - _RimLightUnfill + 5) * 0.5);
    float halfScaleCeil = ceil((1.0 - _RimLightUnfill + 5) * 0.5);

    float2 bottomLeftUV = uv - float2(_ScrSi.x, _ScrSi.y) * halfScaleFloor;
    float2 topRightUV = uv + float2(_ScrSi.x, _ScrSi.y) * halfScaleCeil;
    float2 bottomRightUV = uv + float2(_ScrSi.x * halfScaleCeil, -_ScrSi.y * halfScaleFloor);
    float2 topLeftUV = uv + float2(-_ScrSi.x * halfScaleFloor, _ScrSi.y * halfScaleCeil);

    float depth0 = SampleClampedDepth(bottomLeftUV);
    float depth1 = SampleClampedDepth(topRightUV);
    float depth2 = SampleClampedDepth(bottomRightUV);
    float depth3 = SampleClampedDepth(topLeftUV);

	float depthDerivative0 = depth1 - depth0;
	float depthDerivative1 = depth3 - depth2;

	float edgeDepth = sqrt(pow(depthDerivative0, 2.0) + pow(depthDerivative1, 2.0)) * 100;
    edgeDepth = (edgeDepth > (depth0 * (_RimLighThres * 0.01)) ? 1 : 0) - Linear01Depth(SampleClampedDepth(uv), _ZBufferParams);
	
	#ifdef N_F_CO_ON
		return edgeDepth;
	#elif !N_F_CO_ON && N_F_TRANS_ON
		return 0.0;
	#else
		return edgeDepth;
	#endif
}
//

//RT NM
float3 RT_NM(float2 uv, float3 positionWS, float3x3 tangentToWorld, float3 normalWS)
{
	#if N_F_NM_ON

		#ifdef N_F_TP_ON
			
			float3 UV = GetAbsolutePositionWS(positionWS) * _TriPlaTile;
    
			float3 Blend = pow(abs(normalWS), _TriPlaBlend);
			Blend /= dot(Blend, 1.0);
    
			float3 X = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, UV.zy));
			float3 Y = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, UV.xz));
			float3 Z = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, UV.xy));

			float3 _NormalMap_var = X * Blend.x + Y * Blend.y + Z * Blend.z;
	
		#else
	
			float3 _NormalMap_var = UnpackNormal( SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap , TRANSFORM_TEX(uv, _NormalMap) ) );
	
		#endif
	
		float3 normalLocal = lerp(float3(0.0,0.0,1.0),_NormalMap_var.rgb,_NormalScale);

		return normalLocal;

	#else

		return float3(0.0,0.0,1.0);

	#endif
}
//

//RT_MCAP
float3 RT_MCAP( float2 uv, float3 normalDirection )
{
	#if N_F_MC_ON 
            
		float2 MUV = (mul( UNITY_MATRIX_V, float4(normalDirection,0.0) ).xyz.rgb.rg * 0.5 + 0.5); 
		float4 _MatCap_var = SAMPLE_TEXTURE2D(_MCap, sampler_MCap , TRANSFORM_TEX(MUV, _MCap));
		float4 _MCapMask_var = SAMPLE_TEXTURE2D(_MCapMask, sampler_MCapMask , TRANSFORM_TEX(uv, _MCapMask));
		float3 MCapOutP = lerp( lerp((float3)1.0,(float3)0.0, _SPECMODE), lerp( lerp((float3)1.0,(float3)0.0, _SPECMODE) ,_MatCap_var.rgb,_MCapIntensity) ,_MCapMask_var.rgb ); 

		return MCapOutP;

	#else
            
		return (float3)1.0;

	#endif
}

//RT_MCAP_SUB1
float3 RT_MCAP_SUB1( float3 MCapOutP, float4 _MainTex_var, float3 _RTD_MVCOL, out float3 RTD_TEX_COL )
{
	#if N_F_MC_ON 

		float3 SPECMode_Sel = lerp( lerp( ((_MainColor.rgb * _MaiColPo) * MCapOutP), ((_MainColor.rgb * _MaiColPo) + (MCapOutP * _SPECIN) ), _SPECMODE) ,  lerp( ( MCapOutP), ( (MCapOutP * _SPECIN) ), _SPECMODE), _MCIALO );
		RTD_TEX_COL = _MainTex_var.rgb * SPECMode_Sel * _RTD_MVCOL;
			
		float3 RTD_MCIALO_IL = RTD_TEX_COL;

		return RTD_MCIALO_IL;

	#else

		RTD_TEX_COL = _MainTex_var.rgb * (_MainColor.rgb * _MaiColPo) * MCapOutP * _RTD_MVCOL;
		float3 RTD_MCIALO_IL = lerp( RTD_TEX_COL , _MainTex_var.rgb * MCapOutP * _RTD_MVCOL, _MCIALO);

		return RTD_MCIALO_IL;

	#endif
}
//

//RT TRANS CO
void RT_TRANS_CO( float2 uv , float4 _MainTex_var , out float RTD_TRAN_OPA_Sli , float RTD_CO , out bool bo_co_val, bool is_rt, float3 positionWS, float3 normalDirection, float2 positionCS, inout float3 GLO_OUT) 
{
	RTD_TRAN_OPA_Sli = 1.0;
	bo_co_val = false;

	#if N_F_TRANS_ON

		#if N_F_CO_ON
	
			#ifdef N_F_TP_ON
				float4 _SecondaryCutout_var = RT_Tripl_Default(_SecondaryCutout, sampler_SecondaryCutout, GetAbsolutePositionWS(positionWS), normalDirection);
			#else
				float4 _SecondaryCutout_var = SAMPLE_TEXTURE2D(_SecondaryCutout, sampler_SecondaryCutout ,TRANSFORM_TEX(uv,_SecondaryCutout));
			#endif
			
			float RTD_CO_ON = (float)lerp( (lerp((_MainTex_var.r*_SecondaryCutout_var.r),_SecondaryCutout_var.r,_UseSecondaryCutoutOnly)+lerp(0.5,(-1.0),_Cutout)), saturate(( (1.0 - _Cutout) > 0.5 ? (1.0-(1.0-2.0*((1.0 - _Cutout)-0.5))*(1.0-lerp((_MainTex_var.a*_SecondaryCutout_var.r),_SecondaryCutout_var.a,_UseSecondaryCutoutOnly))) : (2.0*(1.0 - _Cutout)*lerp((_MainTex_var.a*_SecondaryCutout_var.r),_SecondaryCutout_var.a,_UseSecondaryCutoutOnly)) )), _AlphaBasedCutout );
			RTD_CO = RTD_CO_ON;

			//GLOW
			#ifdef N_F_COEDGL_ON

				float _Glow_Edge_Width_Val = (1.0 - _Glow_Edge_Width);
				float _Glow_Edge_Width_Add_Input_Value = (_Glow_Edge_Width_Val + RTD_CO);
				float _Remapping = (_Glow_Edge_Width_Add_Input_Value * 8.0 + -4.0);
				float _Pre_Output = (1.0 - saturate(_Remapping));
				 
				#if SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT || SHADERPASS == SHADERPASS_RAYTRACING_FOWARD || SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER

					float3 _Final_Output = (_Pre_Output * lerp(0.0, _Glow_Color.rgb * 6000.0, saturate(_Cutout * 200.0)));
					GLO_OUT = _Final_Output;

				#else

					float3 _Final_Output = (_Pre_Output * lerp(0.0, _Glow_Color.rgb, saturate(_Cutout * 200.0)));
					GLO_OUT = _Final_Output;

				#endif

			#endif

			if (!is_rt)
			{
				#ifdef N_F_SCO_ON
					clip( -( RT_Dither_Out(positionCS) - RTD_CO ));
				#else
					clip(RTD_CO - 0.5);
				#endif
			}
			else
			{
				#ifdef N_F_SCO_ON
					bo_co_val = RTD_CO >= RT_Dither_Out(positionCS);
				#else
					bo_co_val = RTD_CO >= 0.5;
				#endif
			}
            
		#else
	
			#ifdef N_F_TP_ON
				float4 _MaskTransparency_var = RT_Tripl_Default(_MaskTransparency, sampler_MaskTransparency, GetAbsolutePositionWS(positionWS), normalDirection);
			#else
				float4 _MaskTransparency_var = SAMPLE_TEXTURE2D(_MaskTransparency, sampler_MaskTransparency ,TRANSFORM_TEX(uv,_MaskTransparency));
			#endif

			//Backup (Old)
			//float RTD_TRAN_MAS = (smoothstep(clamp(-20.0,1.0,_TransparentThreshold),1.0,_MainTex_var.a) *_MaskTransparency_var.r);
			//RTD_TRAN_OPA_Sli = lerp( RTD_TRAN_MAS, smoothstep(clamp(-20.0,1.0,_TransparentThreshold) , 1.0, _MainTex_var.a)  ,_Opacity);

			#ifdef N_F_SIMTRANS_ON
				RTD_TRAN_OPA_Sli = _MainTex_var.a * _Opacity; 
			#else
				RTD_TRAN_OPA_Sli = lerp(smoothstep(clamp(-20.0, 1.0, _TransparentThreshold), 1.0, _MainTex_var.a) * _Opacity, 1.0, _MaskTransparency_var.r);
			#endif

		#endif

	#endif
}
//

//RT SON
float3 RT_SON( float4 vertexColor , float3 calNorm , float3 normalDirection , out float3 RTD_SON_CHE_1)
{
	RTD_SON_CHE_1 = float3(1.0,1.0,1.0);

	#if N_F_SON_ON

		float RTD_SON_VCBCSON_OO = lerp( _SmoothObjectNormal, _SmoothObjectNormal * vertexColor.r, _VertexColorRedControlSmoothObjectNormal ); 

		float3 RTD_SON_ON_OTHERS = lerp(normalDirection, -normalize(_XYZPosition.xyz - calNorm), RTD_SON_VCBCSON_OO);

		float3 RTD_SNorm_OO = lerp((float3)1.0, smoothstep(0.0, 0.01, RTD_SON_ON_OTHERS), _ShowNormal);
		RTD_SON_CHE_1 = RTD_SNorm_OO;

		float3 RTD_SON = RTD_SON_ON_OTHERS;

		return RTD_SON;
            
	#else
            
		float3 RTD_SON = normalDirection;
		
		return RTD_SON;
            
	#endif
}
//

//RT_RELGI
float3 RT_RELGI( float3 RTD_SON )
{
	#if N_F_RELGI_ON

		float3 RTD_GI_ST_Sli = (RTD_SON*_GIShadeThreshold);
		float3 RTD_GI_FS_OO = lerp( RTD_GI_ST_Sli, float3(smoothstep(0.0, 0.01, RTD_SON.r * _GIShadeThreshold), 0.0, smoothstep(0.0, 0.01, RTD_SON.b * _GIShadeThreshold)), _GIFlatShade );
		//float3 RTD_GI_FS_OO = lerp( RTD_GI_ST_Sli, float3(smoothstep( float2(0.0,0.0), float2(0.01,0.01), (RTD_SON.rb*_GIShadeThreshold) ),0.0), _GIFlatShade );//old

		return RTD_GI_FS_OO;

	#else

		float3 RTD_GI_FS_OO = RTD_SON;

		return RTD_GI_FS_OO;

	#endif
}

//RT_RELGI_SUB1
float3 RT_RELGI_SUB1( PositionInputs posInput , float3 viewReflectDirection, float3 viewDirection , float3 RTD_GI_FS_OO , float3 RTD_SHAT_COL , float3 RTD_MCIALO , float RTD_STIAL , out float RTD_RT_GI_Sha_FO , out float ref_int_val , in float3 ref_dif , bool isNonRT)
{
	RTD_RT_GI_Sha_FO = 1.0;
	ref_int_val = 1.0;
	float3 RTD_SL_OFF_OTHERS = float3(1.0,1.0,1.0);
	float3 IDT = float3(1.0, 1.0, 1.0);
	float4 IDTex = LOAD_TEXTURE2D_X(_IndirectDiffuseTexture, posInput.positionSS);
    float3 ALGI = EvaluateAmbientProbe(lerp(float3(0.0, 0.0, 0.0), float3(1.0, 1.0, 1.0), RTD_GI_FS_OO)); //AL_GI(lerp(float3(0.0, 0.0, 0.0), float3(1.0, 1.0, 1.0), RTD_GI_FS_OO));
    float ADVAL = 7;
	
	#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
		if (_EnableProbeVolumes)
		{

			BuiltinData tempBuiltinData;
			ZERO_INITIALIZE(BuiltinData, tempBuiltinData);

			float3 lightInReflDir = float3(-0, -0, -0);

			EvaluateAdaptiveProbeVolume(GetAbsolutePositionWS(posInput.positionWS),
				RTD_GI_FS_OO,
				-RTD_GI_FS_OO,
				viewReflectDirection,
				viewDirection,
				posInput.positionSS,
				tempBuiltinData.bakeDiffuseLighting,
				tempBuiltinData.backBakeDiffuseLighting,
				RTD_GI_FS_OO);


				ALGI = tempBuiltinData.bakeDiffuseLighting;
		}
		else 
		{
			ALGI = EvaluateAmbientProbe(lerp(float3(0.0, 0.0, 0.0), float3(1.0, 1.0, 1.0), RTD_GI_FS_OO));
		}

	#else
	
		ALGI = EvaluateAmbientProbe(lerp(float3(0.0, 0.0, 0.0), float3(1.0, 1.0, 1.0), RTD_GI_FS_OO));
	
	#endif

	#if N_F_RELGI_ON 

		#if N_F_R_ON

			ref_int_val = _ReflectionIntensity;

		#else

			ref_int_val = 1.0;

		#endif

		if(!isNonRT)
		{

			#if N_F_ESSGI_ON

				if (_IndirectDiffuseMode == INDIRECTDIFFUSEMODE_RAY_TRACED)
				{ 
					#if SHADERPASS == SHADERPASS_FORWARD && !defined(N_F_TRANS_ON) || SHADERPASS == SHADERPASS_FORWARD && defined(N_F_CO_ON)

						if (_IndirectDiffuseMode == INDIRECTDIFFUSEMODE_RAY_TRACED)
						{
							IDT = IDTex.xyz + ref_dif;
							RTD_RT_GI_Sha_FO = smoothstep( -0.01 , _RTGIShaFallo ,(float)lerp(0.0, 1.0 , IDT * (_EnvironmentalLightingIntensity + ADVAL)) ); 
							RTD_SL_OFF_OTHERS = lerp( RTD_SHAT_COL , RTD_MCIALO , RTD_STIAL) * IDT * (_EnvironmentalLightingIntensity + ADVAL);
						}
						else
						{
							RTD_SL_OFF_OTHERS = lerp( RTD_SHAT_COL , RTD_MCIALO , RTD_STIAL) * (ALGI * (((_EnvironmentalLightingIntensity + ADVAL))*GetCurrentExposureMultiplier()));
						}

					#else

						#if defined(N_F_TRANS_ON) && !defined(N_F_CO_ON)
							RTD_SL_OFF_OTHERS = lerp(RTD_SHAT_COL, RTD_MCIALO, RTD_STIAL) * (ALGI * (((_EnvironmentalLightingIntensity + ADVAL))*GetCurrentExposureMultiplier()));
						#else
							RTD_SL_OFF_OTHERS = lerp(RTD_SHAT_COL, RTD_MCIALO, RTD_STIAL) * (ALGI * (((_EnvironmentalLightingIntensity + ADVAL))));
						#endif

					#endif
				}
				else
				{

					#if !defined(N_F_TRANS_ON) || defined(N_F_CO_ON)

						if (_IndirectDiffuseMode != INDIRECTDIFFUSEMODE_OFF)
						{
							RTD_SL_OFF_OTHERS = lerp(RTD_SHAT_COL, RTD_MCIALO, RTD_STIAL) * lerp(ALGI, (IDTex.xyz * 2.0) * GetInverseCurrentExposureMultiplier(), IDTex.w )  *  GetCurrentExposureMultiplier()  * (_EnvironmentalLightingIntensity + ADVAL);

						}
						else if (_IndirectDiffuseMode == INDIRECTDIFFUSEMODE_OFF)
						{
	
							#if SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT
	
								RTD_SL_OFF_OTHERS = lerp(RTD_SHAT_COL, RTD_MCIALO, RTD_STIAL) * ( ALGI * (_EnvironmentalLightingIntensity + ADVAL) );
	
							#else

								RTD_SL_OFF_OTHERS = lerp(RTD_SHAT_COL, RTD_MCIALO, RTD_STIAL) * (ALGI * ( ((_EnvironmentalLightingIntensity + ADVAL) * GetCurrentExposureMultiplier() ) ) ); //ALGI * ( _EnvironmentalLightingIntensity * GetCurrentExposureMultiplier() ); 
	
							#endif

						}

					#else

						RTD_SL_OFF_OTHERS = lerp(RTD_SHAT_COL, RTD_MCIALO, RTD_STIAL) * (ALGI * (((_EnvironmentalLightingIntensity + ADVAL))*GetCurrentExposureMultiplier()));

					#endif

				}

			#else

				RTD_SL_OFF_OTHERS = lerp(RTD_SHAT_COL, RTD_MCIALO, RTD_STIAL) * (ALGI * (((_EnvironmentalLightingIntensity + ADVAL))*GetCurrentExposureMultiplier()));

			#endif

		}
		else
		{
	
			#if SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER
	
				RTD_SL_OFF_OTHERS = lerp(RTD_SHAT_COL, RTD_MCIALO, RTD_STIAL) * (ALGI * ((_EnvironmentalLightingIntensity * 6)));
	
			#else

				RTD_SL_OFF_OTHERS = lerp(RTD_SHAT_COL, RTD_MCIALO, RTD_STIAL) * (ALGI * (((_EnvironmentalLightingIntensity + ADVAL))));
	
			#endif

		}

		return RTD_SL_OFF_OTHERS;

	#else

		RTD_SL_OFF_OTHERS = float3(0.0,0.0,0.0);

		return RTD_SL_OFF_OTHERS;

	#endif
}
//

//RT_SCT
float3 RT_SCT( float2 uv, float3 positionWS, float3 normalDirection, float3 RTD_MCIALO_IL )
{
	#if N_F_SCT_ON
	
		#ifdef N_F_TP_ON
			float4 _ShadowColorTexture_var = RT_Tripl_Default(_ShadowColorTexture, sampler_ShadowColorTexture, positionWS, normalDirection); 
		#else
			float4 _ShadowColorTexture_var = SAMPLE_TEXTURE2D(_ShadowColorTexture, sampler_ShadowColorTexture ,TRANSFORM_TEX(uv,_ShadowColorTexture)); 
		#endif

		float3 RTD_SCT_ON = lerp(_ShadowColorTexture_var.rgb,(_ShadowColorTexture_var.rgb*_ShadowColorTexture_var.rgb),_ShadowColorTexturePower);

		float3 RTD_SCT = RTD_SCT_ON * lerp((_MainColor.rgb * _MaiColPo),1.0,_MCIALO);

		return RTD_SCT;
            
	#else
            
		float3 RTD_SCT = RTD_MCIALO_IL;

		return RTD_SCT;
            
	#endif
}
//

//RT_PT
float RT_PT( float2 RTD_VD_Cal , out float3 RTD_PT_COL )
{
	RTD_PT_COL = float3(1.0,1.0,1.0);

	#if N_F_PT_ON

		float4 _PTexture_var = SAMPLE_TEXTURE2D(_PTexture, sampler_PTexture ,TRANSFORM_TEX(RTD_VD_Cal,_PTexture));  
		float RTD_PT_ON = lerp((1.0 - _PTexturePower),1.0,_PTexture_var.r);
		RTD_PT_COL = _PTCol.rgb;
            
		float RTD_PT = RTD_PT_ON;

		return RTD_PT;
            
	#else
            
		float RTD_PT = 1.0;

		return RTD_PT;
            
	#endif
}
//

//RT_GLO
void RT_GLO(float2 uv, float2 uv1, float2 RTD_VD_Cal, float3 halfDirection, float3 bitangentWS, float3 normalDirection, float3 viewDirection, float3 positionWS, float dim_val, out float RTD_GLO, out float3 RTD_GLO_COL)
{
	
	float2 RTD_UV;
	#if N_F_UUONEANIS_ON
		RTD_UV = uv1;
	#else
		RTD_UV = uv;
	#endif
	
	#if N_F_GLO_ON

		#ifdef N_F_TP_ON
			float4 _MaskGloss_var = RT_Tripl_Default(_MaskGloss, sampler_MaskGloss, GetAbsolutePositionWS(positionWS), normalDirection);
		#else
			float4 _MaskGloss_var = SAMPLE_TEXTURE2D(_MaskGloss, sampler_MaskGloss , TRANSFORM_TEX(uv, _MaskGloss) );
		#endif

	
		//
		#ifdef UNITY_COLORSPACE_GAMMA
			_GlossColor = float4(LinearToGamma22(_GlossColor.rgb), _GlossColor.a);
		#endif

		RTD_GLO_COL = (_GlossColor.rgb*_GlossColorPower); 
		//
	
	
		#if N_F_GLOT_ON

			#if N_F_ANIS_ON

				float4 _GlossTexture_var = SAMPLE_TEXTURE2D(_GlossTexture, sampler_GlossTexture , TRANSFORM_TEX(RTD_UV,_GlossTexture) );
	
				float RTD_ANIS_SHIF = ( _GlossTexture_var.r -_ShifAnis );
				float RTD_ANIS = StraSpec( RTShifTan( bitangentWS , normalDirection , lerp(0.0, RTD_ANIS_SHIF ,_NoisTexInten * 0.1) ), viewDirection, halfDirection , ((_StraWidt * 200) * 0.1) * lerp(1.0,(1.0 - _GlossTexture_var.r ),_NoiTexAffStraWidt)  );
				float RTD_ANIS_SOF = smoothstep( 0.1, lerp(0.1,1.0, _GlossSoftness) , RTD_ANIS);
				float RTD_GT_ON = RTD_ANIS_SOF;
	
			#else
			
				float _5992_ang = _GlossTextureRotate;
				float _5992_spd = 1.0;
				float _5992_cos = cos(_5992_spd*_5992_ang);
				float _5992_sin = sin(_5992_spd*_5992_ang);
				float2 _5992_piv = float2(0.5,0.5);

				float3 RTD_GT_FL_Sli = lerp(viewDirection,halfDirection,_GlossTextureFollowLight);
				float3 RefGlo = reflect(-RTD_GT_FL_Sli,normalDirection);

				float3 RTD_GT_FOR_OO = lerp( RefGlo, mul( GetWorldToObjectMatrix() , float4(RefGlo,0.0) ).xyz, _GlossTextureFollowObjectRotation );

				float2 glot_rot_cal = (mul(float2((RTD_GT_FOR_OO.r),RTD_GT_FOR_OO.g)-_5992_piv,float2x2( _5992_cos, -_5992_sin, _5992_sin, _5992_cos))+_5992_piv);
				float2 glot_rot_out = (glot_rot_cal*0.5+0.5);
	
				float4 _GlossTexture_var = SAMPLE_TEXTURE2D_LOD(_GlossTexture, sampler_GlossTexture , TRANSFORM_TEX(lerp(glot_rot_out,RTD_VD_Cal,_PSGLOTEX),_GlossTexture) , _GlossTextureSoftness );
				float RTD_GT_ON = _GlossTexture_var.r;

			#endif

			float RTD_GT = RTD_GT_ON;
            
		#else

			float RTD_GLO_MAIN_Sof_Sli = lerp(0.1,1.0,_GlossSoftness);
			float RTD_NDOTH = saturate(dot(halfDirection, normalDirection));
			float RTD_GLO_MAIN = smoothstep( 0.1, RTD_GLO_MAIN_Sof_Sli, pow(RTD_NDOTH,exp2(lerp(-2.0,15.0,_Glossiness))) );

			float RTD_GT = RTD_GLO_MAIN;
            
		#endif

		float RTD_GLO_I_Sli = lerp(0.0, RTD_GT,_GlossIntensity);
		float RTD_GLO_MAS = lerp( 0.0, RTD_GLO_I_Sli ,_MaskGloss_var.r);
	
		RTD_GLO = RTD_GLO_MAS * dim_val;

	#else

		RTD_GLO_COL = (float3)1.0;
		RTD_GLO = 0.0;
  
	#endif
}
//

//RT_RL
float RT_RL(float3 viewDirection, float3 normalDirection, float3 lightColor,

#if SHADERPASS == SHADERPASS_FORWARD
	float2 uv,
	float4 PosCS,
#endif

out float3 RTD_RL_LARL_OO, out float RTD_RL_MAIN)
{
	RTD_RL_MAIN = 0.0;
	
	#if N_F_RL_ON

	
		//
		#ifdef UNITY_COLORSPACE_GAMMA
			_RimLightColor = float4(LinearToGamma22(_RimLightColor.rgb), _RimLightColor.a);
		#endif

		#if SHADERPASS == SHADERPASS_FORWARD
			RTD_RL_LARL_OO = lerp( _RimLightColor.rgb ,lerp(float3(0.0,0.0,0.0),_RimLightColor.rgb, lightColor * _LigAffRiLigColInte ), _LightAffectRimLightColor ) * _RimLightColorPower;
		#else
			RTD_RL_LARL_OO = lerp( lerp( _RimLightColor.rgb  * 2000, _RimLightColor.rgb , _RimLightInLight ) ,lerp(float3(0.0,0.0,0.0),_RimLightColor.rgb, lightColor * _LigAffRiLigColInte ), _LightAffectRimLightColor ) * _RimLightColorPower;
		#endif
		//


		float RTD_RL_S_Sli = lerp(1.70,0.29,_RimLightSoftness);
	
		#if N_F_SSRRL_ON
		
			#if SHADERPASS == SHADERPASS_FORWARD
				RTD_RL_MAIN = RT_SSRLig(uv,PosCS);
			#else
				RTD_RL_MAIN = lerp(0.0, 1.0 ,smoothstep( 1.71, RTD_RL_S_Sli, pow(abs( 1.0-max(0,dot(normalDirection, float3(viewDirection.x + (1.0 - _RimLigPosi.x),viewDirection.y + (1.0 - _RimLigPosi.y),viewDirection.z + (1.0 - _RimLigPosi.z)) ) ) ), (1.0 - _RimLightUnfill) ) ) );
			#endif
	
		#else
	
			RTD_RL_MAIN = lerp(0.0, 1.0 ,smoothstep( 1.71, RTD_RL_S_Sli, pow(abs( 1.0-max(0,dot(normalDirection, float3(viewDirection.x + (1.0 - _RimLigPosi.x),viewDirection.y + (1.0 - _RimLigPosi.y),viewDirection.z + (1.0 - _RimLigPosi.z)) ) ) ), (1.0 - _RimLightUnfill) ) ) );
	
		#endif
					
		float RTD_RL_IL_OO = lerp( 0.0, RTD_RL_MAIN, _RimLigInt);

		float RTD_RL_CHE_1 = RTD_RL_IL_OO;

		return RTD_RL_CHE_1;
              
	#else
					
		RTD_RL_LARL_OO = (float3)1.0;

		float RTD_RL_CHE_1 = 0.0;

		return RTD_RL_CHE_1;
            
	#endif
}
//

//RT_RL_SUB1
float3 RT_RL_SUB1( float3 RTD_SL_CHE_1 , float3 RTD_RL_LARL_OO , float3 RTD_RL_MAIN)
{
	#if N_F_RL_ON

		#ifdef N_F_RLIS_ON
			float3 RTD_RL_ON = RTD_SL_CHE_1;
		#else
			float3 RTD_RL_ON = lerp(RTD_SL_CHE_1 ,lerp( (lerp(RTD_SL_CHE_1, RTD_RL_LARL_OO, RTD_RL_MAIN) ), RTD_SL_CHE_1, _RimLightInLight) , _RimLigInt);
		#endif

		float3 RTD_RL = RTD_RL_ON;

		return RTD_RL;
            
	#else
            
		float3 RTD_RL = RTD_SL_CHE_1;

		return RTD_RL;
            
	#endif
}
//

//RT_CLD
float3 RT_CLD( float3 lightDirection )
{
	#if N_F_CLD_ON

        float3 RTD_CLD_CLDFOR_OO = lerp( _CustomLightDirection.rgb, TransformObjectToWorldDir( _CustomLightDirection.xyz ), _CustomLightDirectionFollowObjectRotation );
		float3 RTD_CLD_CLDI_Sli = lerp(lightDirection,RTD_CLD_CLDFOR_OO,_CustomLightDirectionIntensity); 
		float3 RTD_CLD = RTD_CLD_CLDI_Sli;

		return RTD_CLD;
            
	#else
            
		float3 RTD_CLD = lightDirection;

		return RTD_CLD;
            
	#endif
}
//

//RT_ST
float RT_ST(float2 uv, float3 positionWS, float3 normalDirection, float3 RTD_NDOTL, float attenuation, float3 DirLig, float RTD_LVLC, float3 RTD_PT_COL, float3 lightColint, float3 RTD_SCT, float3 RTD_OSC, float RTD_PT, out float3 RTD_SHAT_COL, out float RTD_STIAL, out float RTD_ST_IS, out float3 RTD_ST_LAF)
{
	
    RTD_ST_LAF = 1.0;
    RTD_STIAL = 1.0;
    RTD_ST_IS = 1.0;
	
	#if N_F_ST_ON
	
		#ifdef N_F_STSDFM_ON
			float3 HF = _ObjectForward;
			float2 HF_RB = float2(HF[0],HF[2]);
			float2 HF_RB_Norma = normalize(HF_RB);
	
			float2 DirLig_RB = float2(DirLig[0],DirLig[2] *_ShadowTLightThreshold*0.01);
			float2 DirLig_RB_Norma = normalize(DirLig_RB);

			float DirLig_HF_Dot = dot(HF_RB_Norma,DirLig_RB_Norma);
			float Ste_DirLig_HF_Dot = step(float(0),DirLig_HF_Dot);

			float3 HR = _ObjectRight;
			float2 HR_RB = float2(HR[0],HR[2]);
			float2 HR_RB_Norma = normalize(HR_RB);

			float DirLig_HR_Dot = dot(HR_RB_Norma,DirLig_RB_Norma);
			float Comp_DirLig_HR_Dot = DirLig_HR_Dot > float(0) ? 1 : 0;
	
			float2 UV_Mod_R = float2( (1 - uv.r) , uv.g);
			float2 Bran_uv = Comp_DirLig_HR_Dot ? UV_Mod_R : uv;
		#else
			float2 Bran_uv = uv;
		#endif
	
		#ifdef N_F_TP_ON
			float4 _ShadowT_var = RT_Tripl_Default(_ShadowT, sampler_ShadowT, positionWS, normalDirection);
		#else
        	float4 _ShadowT_var = SAMPLE_TEXTURE2D(_ShadowT, sampler_ShadowT , TRANSFORM_TEX(Bran_uv,_ShadowT));
		#endif
	

		//
		#ifdef UNITY_COLORSPACE_GAMMA
			_ShadowTColor = float4(LinearToGamma22(_ShadowTColor.rgb), _ShadowTColor.a);
		#endif

		RTD_SHAT_COL = lerp( RTD_PT_COL, (_ShadowTColor.rgb*_ShadowTColorPower) * RTD_SCT * RTD_OSC, RTD_PT);
		//
	

		#if SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT || SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER
			float SWEXPO = saturate(GetCurrentExposureMultiplier());
			RTD_ST_LAF = lerp((RTD_SHAT_COL * lerp(GetInverseCurrentExposureMultiplier(), 7.0, SWEXPO)) * RTD_LVLC, (RTD_SHAT_COL * lightColint), _LightAffectShadow);
		#else
			RTD_ST_LAF = lerp((RTD_SHAT_COL * 7) * RTD_LVLC, (RTD_SHAT_COL * lightColint), _LightAffectShadow);
		#endif

		#if !defined(N_F_STSDFM_ON)
			float RTD_ST_H_Sli = lerp(0.0,0.22,_ShadowTHardness);
			float RTD_ST_IS_ON = (float)smoothstep( RTD_ST_H_Sli, 0.22, (_ShowInAmbientLightShadowThreshold*_ShadowT_var.rgb) ); 

			#if N_F_STIAL_ON
				float RTD_ST_ALI_Sli = lerp(1.0,RTD_ST_IS_ON,_ShowInAmbientLightShadowIntensity);
				float RTD_STIAL_ON = (float)lerp(RTD_ST_ALI_Sli,float3(1.0,1.0,1.0),clamp((RTD_LVLC*8.0),0.0,1.0));

				RTD_STIAL = RTD_STIAL_ON;          
			#else
				RTD_STIAL = 1.0;
			#endif

			#if N_F_STIS_ON
				RTD_ST_IS = lerp(1.0,RTD_ST_IS_ON,_ShowInAmbientLightShadowIntensity);
			#else
				RTD_ST_IS = 1.0;
			#endif

			float RTD_ST_LFAST_OO = (float)lerp(lerp( RTD_NDOTL, (attenuation*RTD_NDOTL), _LightFalloffAffectShadowT ) , 1.0 , _STIL );
			float RTD_ST_In_Sli = lerp( 1.0 ,smoothstep( RTD_ST_H_Sli, 0.22, ((_ShadowT_var.r*(1.0 - _ShadowTShadowThreshold))*(RTD_ST_LFAST_OO *_ShadowTLightThreshold*0.01)) ),_ShadowTIntensity);
		#endif
	
		#ifdef N_F_STSDFM_ON
			float Pi_Cons = 3.141593;
			float DirLig_HR_Dot_acos = acos(DirLig_HR_Dot);
			float acos_pi_div = DirLig_HR_Dot_acos/Pi_Cons;
			float acos_pi_div_mul_val = acos_pi_div * 2;
	
			float RoundMinuOn = 1 - acos_pi_div_mul_val;
			float RoundRev = -1 * RoundMinuOn;
			float Bran_Sphe = Comp_DirLig_HR_Dot ? RoundMinuOn : RoundRev;
			
			float SmooLo_SDF = lerp(_ShadowT_var.r, float(1), (1 - _ShadowTHardness) );
			float SmooSte = smoothstep(_ShadowT_var.r, SmooLo_SDF, Bran_Sphe * distance(DirLig_RB_Norma,HF_RB_Norma) );
			float SmooSte_mi_one = 1 - SmooSte.r;
	
			float SDF_Final = Ste_DirLig_HF_Dot * SmooSte_mi_one;
			float RTD_ST_In_Sli = lerp( 1.0 , SDF_Final ,_ShadowTIntensity);
		#endif

		float RTD_ST_ON = RTD_ST_In_Sli;
		float RTD_ST = RTD_ST_ON;
		return RTD_ST;
            
	#else
            
		float RTD_ST = 1.0;
		RTD_SHAT_COL = 1.0;
		RTD_ST_LAF = 1.0;
		RTD_STIAL = 1.0;
		RTD_ST_IS = 1.0;

		return RTD_ST;
            
	#endif
}
//

//RT_SS
float RT_SS( float4 vertexColor , float3 RTD_NDOTL , float attenuation , float dim_val )
{
	#if N_F_SS_ON
 
		float RTD_SS_SSH_Sil = lerp(0.3,1.0,_SelfShadowHardness);
		float RTD_SS_SSTH_Sli = lerp(-1.0, 1.0, _SelfShadowThreshold);

		float RTD_SS_VCGCSSS_OO = lerp( RTD_SS_SSTH_Sli, RTD_SS_SSTH_Sli * vertexColor.g, _VertexColorGreenControlSelfShadowThreshold); 

		float RTD_SS_SST = smoothstep( RTD_SS_SSH_Sil, 1.0, ((float)RTD_NDOTL * lerp(7.0, RTD_SS_VCGCSSS_OO ,RTD_SS_VCGCSSS_OO)) );
		float RTD_SS_SSABLSS_OO = lerp( RTD_SS_SST, lerp(RTD_SS_SST,1.0, (1.0 - dim_val)  ), _SelfShadowAffectedByLightShadowStrength );
		float RTD_SS_ON = lerp(1.0,(RTD_SS_SSABLSS_OO*attenuation),_SelfShadowRealtimeShadowIntensity);

		float RTD_SS = RTD_SS_ON;

		return RTD_SS;
            
	#else
            
		float RTD_SS_OFF = lerp(1.0,attenuation,_SelfShadowRealtimeShadowIntensity);

		float RTD_SS = RTD_SS_OFF;

		return RTD_SS;
            
	#endif
}
//

//RT_R
float3 RT_R( float2 uv, float3 viewDirection, float3 normalDirection, float3 positionWS, float3 EnvRef, float3 RTD_TEX_COL, float3 RTD_R_OFF_OTHERS )
{
	#if N_F_R_ON

		float3 RTD_FR_OFF_OTHERS = EnvRef;

		#if N_F_FR_ON
            
			float2 ref_cal = reflect(viewDirection,normalDirection).rg;
			float2 ref_cal_out = (float2(ref_cal.r,(-1.0*ref_cal.g))*0.5+0.5);
			float4 _FReflection_var = SAMPLE_TEXTURE2D_LOD(_FReflection, sampler_FReflection, TRANSFORM_TEX(ref_cal_out, _FReflection), _Smoothness);
			float3 RTD_FR_ON = _FReflection_var.rgb;

			float3 RTD_FR = RTD_FR_ON;
            
		#else
            
			float3 RTD_FR = RTD_FR_OFF_OTHERS;

		#endif

		#ifdef N_F_TP_ON
			float4 _MaskReflection_var = RT_Tripl_Default(_MaskReflection, sampler_MaskReflection, positionWS, normalDirection); 
		#else
			float4 _MaskReflection_var = SAMPLE_TEXTURE2D(_MaskReflection, sampler_MaskReflection , TRANSFORM_TEX(uv, _MaskReflection));
		#endif
	
		float3 RTD_R_MET_Sli = lerp((float3)1.0,(9.0 * (RTD_TEX_COL - (9.0 * 0.005) ) ) , _RefMetallic);
		float3 RTD_R_MAS = lerp(RTD_R_OFF_OTHERS, (RTD_FR * RTD_R_MET_Sli) ,_MaskReflection_var.r);
		float3 RTD_R_ON = lerp(RTD_R_OFF_OTHERS, RTD_R_MAS ,_ReflectionIntensity);

		float3 RTD_R = RTD_R_ON;

		return RTD_R;
            
	#else
            
		float3 RTD_R = RTD_R_OFF_OTHERS;
        
		return RTD_R;

	#endif
}
//

//RT_RF
half4 RT_RF(float3 normalDirection, float3 viewReflectDirection, half4 _MainTX, float4 ScrUVPos, float2 positionCS, float2 uv, float3 positionWS)
{
	#if defined(N_F_RF_ON) && defined(N_F_TRANS_ON) && !N_F_CO_ON
	
		#if N_F_TP_ON
			half4 _RefrMask_var = RT_Tripl_Default(_RefrMask, sampler_RefrMask, positionWS, normalDirection);
		#else
			half4 _RefrMask_var = SAMPLE_TEXTURE2D(_RefrMask, sampler_RefrMask, TRANSFORM_TEX(uv, _RefrMask));
		#endif	
	
		float3 VRefDir_Norm = TransformWorldToViewDir(viewReflectDirection);

		float fresnel = pow(saturate(1.0 - VRefDir_Norm.z) , (_FresnelEdge * 0.01));
		float2 offset = VRefDir_Norm.xy * ( (_DistRef * 0.01) * (1.0 + fresnel * 2.0));
                
		float2 screenUV = ScrUVPos.xy / ScrUVPos.w;
		float2 baseUV = screenUV + offset;

		float noise = frac(sin(dot(positionCS, float2(12.9898, 78.233))) * 43758.5453);
		float sinAngle, cosAngle;
		sincos(noise * 6.2831853, sinAngle, cosAngle);
		float2x2 rotMatrix = float2x2(cosAngle, -sinAngle, sinAngle, cosAngle);

		float2 blurDirs[4] =
		{
			float2(-0.5, -0.5), float2(0.5, -0.5), float2(-0.5, 0.5), float2(0.5, 0.5)
		};

		half3 accumulatedColor = half3(0, 0, 0);

		[unroll]
		for (int tap = 0; tap < 4; tap++)
		{
			float2 rotatedOffset = mul(rotMatrix, blurDirs[tap]) * (_Frost * 0.01);
			float2 sampleUV = baseUV + rotatedOffset;

			half Abr = (_Aberration * 0.01);
			accumulatedColor.r += SampleCameraColor(sampleUV + (offset * Abr)).r;
			accumulatedColor.g += SampleCameraColor(sampleUV).g;
			accumulatedColor.b += SampleCameraColor(sampleUV + (offset * Abr)).b;
		}

		half3 MixMaiTexRefr_var = half3(1,1,1);
		if(_MixMaiTexRefr)
		{
			MixMaiTexRefr_var = _MainTX.rgb;
		}
	
		half3 RT_RF = ( (accumulatedColor * _RefracColor.rgb * MixMaiTexRefr_var) * 0.25);
		half4 final_ref_output = lerp(_MainTX, half4(RT_RF ,_MainTX.a), _RefracInten * _RefrMask_var.r);
	
		return final_ref_output;

	#else
	
		return _MainTX;
	
	#endif
}
//

//RT_EPRF
void RT_EPRF(float3 tangentOSxyz, float tangentOSw, float3 normalWS, float3 viewDirection, float2 uv, out float2 uv_out)
{
	float3x3 worldToTangent = float3x3(tangentOSxyz, cross(normalWS, tangentOSxyz) * tangentOSw, normalWS);
	float3 viewDir = normalize(mul(worldToTangent, viewDirection));

    float2 offset = viewDir.xy * _RefracDepth;
    float2 RefracUV = (uv - 0.5) * _TexSca + 0.5 + offset;
	uv_out = RefracUV;
}

//RT_SL
float3 RT_SL( float2 uv, float3 positionWS, float3 normalDirection, float3 RTD_SL_OFF_OTHERS, float3 RTD_TEX_COL, float3 RTD_R, out float3 RTD_SL_CHE_1 )
{
	#if N_F_SL_ON

		#ifdef N_F_TP_ON
			float4 _MaskSelfLit_var = RT_Tripl_Default(_MaskSelfLit, sampler_MaskSelfLit, positionWS, normalDirection); 
		#else
			float4 _MaskSelfLit_var = SAMPLE_TEXTURE2D(_MaskSelfLit, sampler_MaskSelfLit ,TRANSFORM_TEX(uv, _MaskSelfLit)); 
		#endif


		//
		#ifdef UNITY_COLORSPACE_GAMMA
			_SelfLitColor = float4(LinearToGamma22(_SelfLitColor.rgb), _SelfLitColor.a);
		#endif

		#if SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT || SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER || SHADERPASS == SHADERPASS_RAYTRACING_FORWARD
			float SL_IRT_GICEM = GetInverseCurrentExposureMultiplier();
		#else
			float SL_IRT_GICEM = 1.0;
		#endif
		//
				

		#ifdef N_F_SLMM_ON
			float3 RTD_SL_MAS = lerp(RTD_SL_OFF_OTHERS, ((_SelfLitColor.rgb * SL_IRT_GICEM)*_SelfLitPower),_MaskSelfLit_var.rgb);
			float3 RTD_R_SEL = lerp(RTD_R,lerp(RTD_R,RTD_TEX_COL*_TEXMCOLINT,_MaskSelfLit_var.rgb),_SelfLitIntensity);
		#else
	
			float3 RTD_SL_HC_OO;
			if (!_SelfLitHighContrast)
			{
				RTD_SL_HC_OO = (float3)1.0;
			}
			else
			{
				RTD_SL_HC_OO = RTD_TEX_COL;
			}
	
			float3 RTD_SL_MAS = lerp(RTD_SL_OFF_OTHERS, ((_SelfLitColor.rgb * RTD_TEX_COL * RTD_SL_HC_OO * SL_IRT_GICEM)*_SelfLitPower),_MaskSelfLit_var.r);
			float3 RTD_R_SEL = lerp(RTD_R,lerp(RTD_R,RTD_TEX_COL*_TEXMCOLINT,_MaskSelfLit_var.r),_SelfLitIntensity);
		#endif			
	
		float3 RTD_SL_ON = lerp(RTD_SL_OFF_OTHERS,RTD_SL_MAS,_SelfLitIntensity);

		float3 RTD_SL = RTD_SL_ON;
		RTD_SL_CHE_1 = RTD_R_SEL;

		return RTD_SL;
            
	#else
            
		float3 RTD_SL = RTD_SL_OFF_OTHERS;
		RTD_SL_CHE_1 = RTD_R;

		return RTD_SL;
     
	#endif
}
//

//RT_CA
float3 RT_CA( float3 color )
{
	#if N_F_CA_ON
            
		float3 RTD_CA_ON = lerp(color,dot(color,float3(0.3,0.59,0.11)),(1.0 - _Saturation));
		float3 RTD_CA = RTD_CA_ON;

		return RTD_CA;
            
	#else

		float3 RTD_CA = color;

		return RTD_CA;
            
	#endif
}

//RT_SSAO
float3 RT_SSAO(float2 positionSS)
{
	float3 RT_SSAmOc = 0.0;

	float3 SSAmOc = GetScreenSpaceAmbientOcclusion(positionSS);
	RT_SSAmOc = lerp(_SSAOColor.rgb, 1.0, SSAmOc);

	#ifdef N_F_ESSAO_ON
		return RT_SSAmOc;
	#else
		return 1.0;
	#endif
}
//

//RT_DC
void RT_DC(PositionInputs posInput, float isFrontFace, inout float4 _MainTex_var, inout float3 normalLocal)
{
	#if !defined(_DISABLE_DECALS)
		#if SHADERPASS == SHADERPASS_FORWARD || SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT || SHADERPASS == SHADERPASS_RAYTRACING_FORWARD || SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER

			float alpha = 1.0; //Temporary
			DecalSurfaceData decalSurfaceData;

			#if USE_CLUSTERED_DECALLIST
				GetDecalSurfaceDataFromCluster(posInput, normalLocal, GetMeshRenderingLayerMask(), alpha, decalSurfaceData);
			#else
				GetDecalSurfaceDataFromDBuffer(posInput, decalSurfaceData);
			#endif
		
			decalSurfaceData.normalWS.xy *= isFrontFace;
			ApplyDecalToSurfaceNormal(decalSurfaceData, normalLocal);

			_MainTex_var.xyz = _MainTex_var.xyz * decalSurfaceData.baseColor.w + decalSurfaceData.baseColor.xyz;
	
		#endif
	#endif
}
//

//RT_NFD
void RT_NFD(float2 positionCS)
{
    float distanceFromCamera = distance(GetAbsolutePositionWS(UNITY_MATRIX_M._m03_m13_m23), _WorldSpaceCameraPos);
    clip(-(RT_Dither_Out(positionCS) - saturate((distanceFromCamera - _MinFadDistance) / _MaxFadDistance)));
}

//RT_PA
float4x4 RT_PA(float Dep)
{
    float Neg = Dep - float(1.0) * (_PADist * 0.1);
    float limit = smoothstep(((1 - _PASmooTrans) * 0.1), 1, clamp(Neg, (1 - _PASize), float(1.0)));

    float4x4 VPM_Mul = mul(GetWorldToHClipMatrix(), GetObjectToWorldMatrix());
    float4x4 VPM_Mod = float4x4(VPM_Mul[0][0], VPM_Mul[0][1], VPM_Mul[0][2], VPM_Mul[0][3], VPM_Mul[1][0], VPM_Mul[1][1], VPM_Mul[1][2], VPM_Mul[1][3], VPM_Mul[2][0] * (_ClipAdju * 5), VPM_Mul[2][1] * (_ClipAdju * 5), VPM_Mul[2][2] * (_ClipAdju * 5), VPM_Mul[2][3], VPM_Mul[3][0] * _PresAdju, VPM_Mul[3][1], VPM_Mul[3][2] * _PresAdju, VPM_Mul[3][3] * limit);

    return VPM_Mod;
}

//RT_SE
float haS(float n)
{
    return frac(sin(n) * 43758.5453);
}
float4 RT_SE(float3 positionWS)
{
    float4 WSPos = float4(positionWS, 1.0);
    float3 WSOff = _ObjPosi.xyz - _PrevPosition.xyz;
    float3 OSOff = WSPos.xyz - _ObjPosi.xyz;
    float3 NoiSi = WSPos.xyz * _NoiseSize;
    float3 p = floor(NoiSi);
    float3 f = frac(NoiSi);
    float n = p.x + p.y * 57.0 + 113.0 * p.z;
	
    f = f * f * (3.0 - 2.0 * f);
    float Noi = lerp(lerp(lerp(haS(n + 0.0), haS(n + 1.0), f.x), lerp(haS(n + 57.0), haS(n + 58.0), f.x), f.y), lerp(lerp(haS(n + 113.0), haS(n + 114.0), f.x), lerp(haS(n + 170.0), haS(n + 171.0), f.x), f.y), f.z);
    float DirDotComp = dot(normalize(WSOff), normalize(OSOff));
    float3 NoiSizAdj = (float3) _TrailSize;
	
    WSOff = clamp(WSOff, NoiSizAdj * -1, NoiSizAdj);
    WSOff *= -clamp(DirDotComp, -1, 0) * lerp(1, 0, step(length(WSOff), 0));
    WSPos.xyz += -WSOff.xyz * lerp(1, Noi, step(0, _NoiseSize));
    return mul(GetWorldToObjectMatrix(), WSPos);
}

//=========================

#endif

