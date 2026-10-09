//RealToon HDRP - RayTraLigLo
//MJQStudioWorks

//=========================

#ifndef REALTOON_HDRP_RAYTRALIGLO_HLSL
#define REALTOON_HDRP_RAYTRALIGLO_HLSL

#include "Assets/RealToon/RealToon Shaders/RealToon Core/HDRP/RT_HDRP_Lit_Func_RT.hlsl"

//=========================

void RT_RAYTLIG(

#if SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT || SHADERPASS == SHADERPASS_RAYTRACING_FORWARD
	float3 viewWS, float3 reflected,
#endif

#if SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT
	float3 refdif,
#endif

#if SHADERPASS == SHADERPASS_RAYTRACING_FORWARD
	out float Trans_Val_Out,
#endif

FragInputs fragInput, PositionInputs posInput, SurfaceData surfaceData, BuiltinData builtinData, float3 normalWS, float3 tangentWS, float3 bitangentWS, float3 normalDirection, float3 viewDirection, float3 viewReflectDirection, out float4 fi_out)
{
	
    float SWEXPO = saturate(GetCurrentExposureMultiplier());
	float3 smoNorm = float3(0.0, 0.0, 0.0);

	
	#if N_F_SON_ON
		smoNorm = GetAbsolutePositionWS(fragInput.positionRWS);
	#endif

	
	float isFrontFace = (fragInput.isFrontFace >= 0 ? 1 : 0 );
    float4 objPos = mul(GetObjectToWorldMatrix(), float4(0.0, 0.0, 0.0, 1.0));
	
	#if SHADERPASS == SHADERPASS_RAYTRACING_FORWARD
		float2 RTD_OB_VP_CAL = distance(objPos.xyz, GetCurrentViewPosition());
		float2 RTD_TC_TP_OO = fragInput.texCoord0.xy;
	#else
		float4 projPos = ComScrPos( TransformWorldToHClip( WorldRayOrigin() ) );
		float2 sceneUVs = (projPos.xy / projPos.w);
		float2 RTD_OB_VP_CAL = distance(objPos.xyz, GetCurrentViewPosition());
		float2 RTD_VD_Cal = -(float2((sceneUVs.x * 2.0 - 1.0)*(_ScreenParams.r/_ScreenParams.g), sceneUVs.y * 2.0 - 1.0).xy * RTD_OB_VP_CAL );
		float2 RTD_TC_TP_OO = lerp( fragInput.texCoord0.xy, RTD_VD_Cal, _TexturePatternStyle );
	#endif

	float3 _RTD_MVCOL = lerp(1, fragInput.color.rgb, _MVCOL);


	//=========//
	//=========//
	float3 color = float3(0.0,0.0,0.0);
	float3 D_L_O = float3(0.0,0.0,0.0);
	float3 A_L_O = float3(0.0,0.0,0.0);
	float3 Ar_L_O = float3(0.0,0.0,0.0);
	
	uint i=0;
	uint i_en=0;
	uint cellIndex;

	#ifdef USE_LIGHT_CLUSTER
		float3 actualWSPos = posInput.positionWS;
	#endif

	LightLoopContext context;
	context.shadowContext  = InitShadowContext();
	context.shadowValue = 1;			
	context.sampleReflection = 0;
	#ifdef APPLY_FOG_ON_SKY_REFLECTIONS
		context.positionWS = posInput.positionWS;
	#endif

	InitContactShadow(posInput, context);

	#ifdef SHADOWS_SHADOWMASK
		float4 shaMask = SampleShadowMask(posInput.positionWS, fragInput.texCoord0.xy.xy);
		builtinData.shadowMask0 = shaMask.x;
		builtinData.shadowMask1 = shaMask.y;
		builtinData.shadowMask2 = shaMask.z;
		builtinData.shadowMask3 = shaMask.w;
	#endif

	#if UNITY_VERSION >= 202310
		builtinData.renderingLayers = GetMeshRenderingLayerMask();
	#else
		builtinData.renderingLayers = _EnableLightLayers ? asuint(unity_RenderingLayer.x) : DEFAULT_LIGHT_LAYERS;
	#endif
	//=========//
	//=========//

	
	#ifdef N_F_TP_ON
		float4 _MainTex_var = RT_Tripl_Default(_MainTex, sampler_MainTex, fragInput.positionRWS, normalDirection);
	#else
		float4 _MainTex_var = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex , TRANSFORM_TEX(RTD_TC_TP_OO, _MainTex) );
	#endif
	
	
	//RT_DC
    RT_DC(posInput, isFrontFace, _MainTex_var, normalDirection);
	//==
	
	
	//
	#ifdef UNITY_COLORSPACE_GAMMA
		_OverallShadowColor = float4(LinearToGamma22(_OverallShadowColor.rgb), _OverallShadowColor.a);
	#endif
	float3 RTD_OSC = (_OverallShadowColor.rgb*_OverallShadowColorPower);
	//


	//RT_MCAP
	float3 MCapOutP = RT_MCAP( fragInput.texCoord0.xy , normalDirection );
	//==

	
	//
	#ifdef UNITY_COLORSPACE_GAMMA
		_MainColor = float4(LinearToGamma22(_MainColor.rgb),_MainColor.a);
	#endif
	//
	
	
	//RT_MCAP_SUB1
	float3 RTD_TEX_COL;
	float3 RTD_MCIALO_IL = RT_MCAP_SUB1( MCapOutP , _MainTex_var, _RTD_MVCOL , RTD_TEX_COL);
	//==


	//RT_TRANS_CO
	float RTD_TRAN_OPA_Sli;
	bool bo_co_val;
	float RTD_CO;
	float3 GLO_OUT = (float3)0.0;
	RT_TRANS_CO( fragInput.texCoord0.xy, _MainTex_var, RTD_TRAN_OPA_Sli, RTD_CO, bo_co_val, true, fragInput.positionRWS.xyz, normalDirection, fragInput.positionSS.xy, GLO_OUT);
	//==
	
	
	//RT_SON
	float3 RTD_SON_CHE_1;
	float3 RTD_SON = RT_SON( fragInput.color , smoNorm , normalDirection , RTD_SON_CHE_1);
	//==
	
	
	//RT_RELGI
	float3 RTD_GI_FS_OO = RT_RELGI( RTD_SON );
	//==
	
	
	//RT_SCT
	float3 RTD_SCT = RT_SCT( fragInput.texCoord0.xy, fragInput.positionRWS.xyz, normalDirection, RTD_MCIALO_IL );
	//==
	
	
	//RT_PT
	float3 RTD_PT_COL;
	#if SHADERPASS == SHADERPASS_RAYTRACING_FORWARD
		float RTD_PT = 1.0; //RT_PT( RTD_VD_Cal , RTD_PT_COL );
	#else
		float RTD_PT = RT_PT( RTD_VD_Cal , RTD_PT_COL );
	#endif
	//==
	

	//
	#ifdef UNITY_COLORSPACE_GAMMA
		_SelfShadowRealTimeShadowColor = float4(LinearToGamma22(_SelfShadowRealTimeShadowColor.rgb), _SelfShadowRealTimeShadowColor.a);
	#endif
	float3 ss_col = lerp( RTD_PT_COL, (_SelfShadowRealTimeShadowColor.rgb * _SelfShadowRealTimeShadowColorPower) * RTD_OSC * RTD_SCT, RTD_PT);
	//
	
	#if SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER
		//RT_REF_ENV
		float3 EnvRef = RT_REF_ENV_RT(builtinData, normalDirection, viewReflectDirection, posInput, context, float3(0.0, 0.0, 0.0), true);
		//==
	#else
		//RT_REF_ENV_RT
		float3 EnvRef = RT_REF_ENV_RT(builtinData, normalDirection, viewReflectDirection, posInput, context, reflected, true);
		//==
	#endif
	
	//=========//
	//=========//
	
	float3 RTD_SL_CHE_1;
	float RTD_RT_GI_Sha_FO = 1.0;

	if (LIGHTFEATUREFLAGS_DIRECTIONAL)
	{
		float4 DirLigCol = float4(0.0,0.0,0.0,0.0);
		float3 DirLigDir = float3(0.0,0.0,0.0);
		float DirSha = 1.0;
		float DirLigDim = 0.0;
		float DirDifDim = 1.0;
		float DirSpecDim = 1.0;
	
		for (i = 0; i < _DirectionalLightCount; ++i)
		{
			DirectionalLightData light = _DirectionalLightDatas[i];
							
			if (IsMatchingLightLayer(light.lightLayers, builtinData.renderingLayers))
			{
				//RT_DL_RT
				RT_DL_RT( context, light , DirLigCol , DirLigDir , DirSha , DirLigDim , DirDifDim , DirSpecDim , posInput );
	
				#if N_F_NLASOBF_ON
					float3 lightColor = lerp(float3(0.0,0.0,0.0),DirLigCol.rgb * DirDifDim,isFrontFace);
				#else
					float3 lightColor = DirLigCol.rgb * DirDifDim;
				#endif

				#if N_F_HDLS_ON
					float attenuation = 1.0; 
				#else
					float dlshmin = lerp(0.0,0.6,_ShadowHardness);
					float dlshmax = lerp(1.0,0.6,_ShadowHardness);

					#if N_F_NLASOBF_ON
						float FB_Check = lerp(1.0,DirSha,isFrontFace);
					#else
						float FB_Check = DirSha;
					#endif

					float attenuation = smoothstep(dlshmin,dlshmax,FB_Check);
				#endif
	
				float3 lightDirection = DirLigDir;
				float3 halfDirection = normalize(viewDirection + lightDirection);
	
				float3 lig_col_int = (_LightIntensity * lightColor.rgb);
				float RTD_LVLC = RTD_LVLC_F(lightColor.rgb * 0.2);
			
				#if SHADERPASS == SHADERPASS_RAYTRACING_FORWARD
					float3 RTD_LAS = lerp((ss_col * 7) * RTD_LVLC, (ss_col * lig_col_int), _LightAffectShadow);
				#else
					float3 RTD_LAS = lerp(( ss_col * lerp( GetInverseCurrentExposureMultiplier(), 7.0, SWEXPO) ) * RTD_LVLC, (ss_col * lig_col_int), _LightAffectShadow);
				#endif

				//
				#ifdef UNITY_COLORSPACE_GAMMA
					_HighlightColor = float4(LinearToGamma22(_HighlightColor.rgb), _HighlightColor.a);
				#endif
				float3 RTD_HL = (_HighlightColor.rgb*_HighlightColorPower+_DirectionalLightIntensity);
				//


				float3 RTD_MCIALO = lerp(RTD_TEX_COL , lerp( lerp( (RTD_TEX_COL * (_MainColor.rgb * _MaiColPo)), (RTD_TEX_COL + (_MainColor.rgb * _MaiColPo)), _SPECMODE) , _MainTex_var.rgb * MCapOutP * _RTD_MVCOL * 0.7 , clamp((RTD_LVLC*1.0),0.0,1.0) ) , _MCIALO );

				//cc
				//RT_GLO
				float RTD_GLO; 
				float3 RTD_GLO_COL;
				
                #if SHADERPASS == SHADERPASS_RAYTRACING_FORWARD
					RT_GLO(fragInput.texCoord0.xy, fragInput.texCoord1.xy, fragInput.texCoord0.xy, halfDirection, bitangentWS, normalDirection, viewDirection, fragInput.positionRWS.xyz, DirSpecDim, RTD_GLO, RTD_GLO_COL);
				#else
					RT_GLO(fragInput.texCoord0.xy, fragInput.texCoord1.xy, RTD_VD_Cal, halfDirection, bitangentWS, normalDirection, viewDirection, fragInput.positionRWS.xyz, DirSpecDim, RTD_GLO, RTD_GLO_COL );
				#endif
				
				float3 RTD_GLO_OTHERS = RTD_GLO;
				//
	
	
				//RT_RL
				float3 RTD_RL_LARL_OO;
				float RTD_RL_MAIN;
				float RTD_RL_CHE_1 = RT_RL( viewDirection , normalDirection , lightColor, RTD_RL_LARL_OO , RTD_RL_MAIN );
				//==
	
	
				//RT_CLD
				float3 RTD_CLD = RT_CLD( lightDirection );
				//==
	
	
				float3 RTD_ST_SS_AVD_OO = lerp( RTD_CLD, viewDirection, _SelfShadowShadowTAtViewDirection );
				float RTD_NDOTL = 0.5*dot(RTD_ST_SS_AVD_OO, float3(RTD_SON.x, RTD_SON.y * (1 - _LigIgnoYNorDir), RTD_SON.z))+0.5;

	
				//RT_ST
				float3 RTD_SHAT_COL;
				float RTD_STIAL;
				float RTD_ST_IS;
				float3 RTD_ST_LAF;
				
				//cc
				float2 RTD_SDF_UV;
				#if N_F_UUONESDF_ON
					RTD_SDF_UV = fragInput.texCoord1.xy;
				#else
					RTD_SDF_UV = fragInput.texCoord0.xy;
				#endif
				
                float RTD_ST = RT_ST(RTD_SDF_UV, fragInput.positionRWS.xyz, normalDirection, RTD_NDOTL, attenuation, lightDirection, RTD_LVLC, RTD_PT_COL, lig_col_int, RTD_SCT, RTD_OSC, RTD_PT, RTD_SHAT_COL, RTD_STIAL, RTD_ST_IS, RTD_ST_LAF);
				//==
	
                #if SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER || SHADERPASS == SHADERPASS_RAYTRACING_FORWARD
					//RT_RELGI_SUB1
					float ref_int_val;
					float3 RTD_SL_OFF_OTHERS = RT_RELGI_SUB1(posInput, viewReflectDirection, viewDirection, RTD_GI_FS_OO, RTD_SHAT_COL, RTD_MCIALO, RTD_STIAL, RTD_RT_GI_Sha_FO, ref_int_val, (float3) 0.0, true);
					//==
				#else
					//RT_RELGI_SUB1
					float ref_int_val;
					float3 RTD_SL_OFF_OTHERS = RT_RELGI_SUB1(posInput, viewReflectDirection, viewDirection, RTD_GI_FS_OO , RTD_SHAT_COL , RTD_MCIALO , RTD_STIAL , RTD_RT_GI_Sha_FO , ref_int_val , refdif , false);
					//==
				#endif
	
	
				//RT_SS
				float RTD_SS = RT_SS( fragInput.color , RTD_NDOTL , attenuation , DirLigDim );
				//==		

	
				float3 RTD_RL_LIG;
				float3 RTD_RL_SHA;
				#ifdef N_F_RLIS_ON
					RTD_RL_LIG = lerp( RTD_MCIALO_IL * RTD_HL , RTD_GLO_COL , RTD_GLO_OTHERS );
					RTD_RL_SHA = lerp (lerp( RTD_ST_LAF, RTD_LAS, RTD_ST_IS) * RTD_RT_GI_Sha_FO,RTD_RL_LARL_OO,RTD_RL_CHE_1 * RTD_LVLC );
				#else
                    RTD_RL_LIG = lerp( lerp( RTD_MCIALO_IL * RTD_HL , RTD_GLO_COL , RTD_GLO_OTHERS) , RTD_RL_LARL_OO , RTD_RL_CHE_1 );
                    RTD_RL_SHA = lerp( RTD_ST_LAF, RTD_LAS, RTD_ST_IS) * RTD_RT_GI_Sha_FO;
				#endif
				
                float3 RTD_R_OFF_OTHERS = lerp(RTD_RL_SHA, lerp(RTD_ST_LAF, RTD_RL_LIG * lightColor.rgb, RTD_ST), RTD_SS);
				

				//RT_R
				float3 RTD_R = RT_R( fragInput.texCoord0.xy, viewDirection, normalDirection, fragInput.positionRWS.xyz, EnvRef, RTD_TEX_COL, RTD_R_OFF_OTHERS );
				//==
	
	
				//RT_SL
				float3 RTD_SL = RT_SL( fragInput.texCoord0.xy, fragInput.positionRWS.xyz, normalDirection, RTD_SL_OFF_OTHERS, RTD_TEX_COL, RTD_R, RTD_SL_CHE_1 );
				//==
	
	
				//RT_RL_SUB1
				float3 RTD_RL = RT_RL_SUB1( RTD_SL_CHE_1 , RTD_RL_LARL_OO , RTD_RL_MAIN);
				//==
	
	
				float3 RTD_CA_OFF_OTHERS = (RTD_RL + RTD_SL);
				float3 main_light_output = RTD_CA_OFF_OTHERS;
	
				#if N_F_USETLB_ON
					D_L_O += main_light_output;
				#else
					D_L_O = max (main_light_output,D_L_O);
				#endif
	
			} //cc
			else
            {
				
				float3 RTD_MCIALO = lerp(RTD_TEX_COL , lerp( lerp( (RTD_TEX_COL * (_MainColor.rgb * _MaiColPo)), (RTD_TEX_COL + (_MainColor.rgb * _MaiColPo)), _SPECMODE) , _MainTex_var.rgb * MCapOutP * _RTD_MVCOL * 0.7 , clamp((0*1.0),0.0,1.0) ) , _MCIALO );


				//RT_RL
				float3 RTD_RL_LARL_OO;
				float RTD_RL_MAIN;
				float RTD_RL_CHE_1 = RT_RL( viewDirection , normalDirection , 0, RTD_RL_LARL_OO , RTD_RL_MAIN );
				//==
	
		
				#if SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER || SHADERPASS == SHADERPASS_RAYTRACING_FORWARD
					//RT_RELGI_SUB1
					float ref_int_val;
					float3 RTD_SL_OFF_OTHERS = RT_RELGI_SUB1(posInput, viewReflectDirection, viewDirection, RTD_GI_FS_OO, 0, RTD_MCIALO, 1, RTD_RT_GI_Sha_FO, ref_int_val, (float3) 0.0, true);
					//==
				#else
					//RT_RELGI_SUB1
					float ref_int_val;
					float3 RTD_SL_OFF_OTHERS = RT_RELGI_SUB1(posInput, viewReflectDirection, viewDirection, RTD_GI_FS_OO , 0 , RTD_MCIALO , 1 , RTD_RT_GI_Sha_FO , ref_int_val , refdif , false);
					//==
				#endif
	

				//RT_R
				float3 RTD_R = RT_R( fragInput.texCoord0.xy, viewDirection, normalDirection, fragInput.positionRWS.xyz, EnvRef, RTD_TEX_COL, 0 );
				//==
	
	
				//RT_SL
				float3 RTD_SL = RT_SL( fragInput.texCoord0.xy, fragInput.positionRWS.xyz, normalDirection, RTD_SL_OFF_OTHERS, RTD_TEX_COL, RTD_R, RTD_SL_CHE_1 );
				//==
	
	
				//RT_RL_SUB1
				float3 RTD_RL = RT_RL_SUB1( RTD_SL_CHE_1 , RTD_RL_LARL_OO , RTD_RL_MAIN);
				//==
	
	
				float3 RTD_CA_OFF_OTHERS = (RTD_RL + RTD_SL);
				D_L_O = RTD_CA_OFF_OTHERS;
            }

		}

	}
    if (_DirectionalLightCount == 0)
    {

		float3 RTD_MCIALO = lerp(RTD_TEX_COL , lerp( lerp( (RTD_TEX_COL * (_MainColor.rgb * _MaiColPo)), (RTD_TEX_COL + (_MainColor.rgb * _MaiColPo)), _SPECMODE) , _MainTex_var.rgb * MCapOutP * _RTD_MVCOL * 0.7 , clamp((0*1.0),0.0,1.0) ) , _MCIALO );


		//RT_RL
		float3 RTD_RL_LARL_OO;
		float RTD_RL_MAIN;
		float RTD_RL_CHE_1 = RT_RL( viewDirection , normalDirection , 0, RTD_RL_LARL_OO , RTD_RL_MAIN );
		//==
	
		
		#if SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER || SHADERPASS == SHADERPASS_RAYTRACING_FORWARD
			//RT_RELGI_SUB1
			float ref_int_val;
			float3 RTD_SL_OFF_OTHERS = RT_RELGI_SUB1(posInput, viewReflectDirection, viewDirection, RTD_GI_FS_OO, 0, RTD_MCIALO, 1, RTD_RT_GI_Sha_FO, ref_int_val, (float3) 0.0, true);
			//==
		#else
			//RT_RELGI_SUB1
			float ref_int_val;
			float3 RTD_SL_OFF_OTHERS = RT_RELGI_SUB1(posInput, viewReflectDirection, viewDirection, RTD_GI_FS_OO , 0 , RTD_MCIALO , 1 , RTD_RT_GI_Sha_FO , ref_int_val , refdif , false);
			//==
		#endif
	

		//RT_R
		float3 RTD_R = RT_R( fragInput.texCoord0.xy, viewDirection, normalDirection, fragInput.positionRWS.xyz, EnvRef, RTD_TEX_COL, 0 );
		//==
	
	
		//RT_SL
		float3 RTD_SL = RT_SL( fragInput.texCoord0.xy, fragInput.positionRWS.xyz, normalDirection, RTD_SL_OFF_OTHERS, RTD_TEX_COL, RTD_R, RTD_SL_CHE_1 );
		//==
	
	
		//RT_RL_SUB1
		float3 RTD_RL = RT_RL_SUB1( RTD_SL_CHE_1 , RTD_RL_LARL_OO , RTD_RL_MAIN);
		//==
	
	
		float3 RTD_CA_OFF_OTHERS = (RTD_RL + RTD_SL);
		D_L_O = RTD_CA_OFF_OTHERS;
	
    }
	
	//=========//
	//=========//
	#if N_F_PAL_ON

		if (LIGHTFEATUREFLAGS_PUNCTUAL)
		{

			float4 PunLigCol = float4(1.0,1.0,1.0,1.0);
			float3 PuncLigDir = float3(0.0,0.0,0.0);
			float PuncSha = 1.0;
			float PuncLF = 1.0;

			i=0;
			i_en=0;

			#ifdef USE_LIGHT_CLUSTER
				GetLightCountAndStartCluster(actualWSPos, LIGHTCATEGORY_PUNCTUAL, i, i_en, cellIndex);
			#else
				i = 0;
				i_en = _WorldPunctualLightCount;
			#endif

			for (i = 0; i < i_en; ++i)
			{
				
				//#if SHADERPASS == SHADERPASS_RAYTRACING_FORWARD
					//LightData Plight = _WorldLightDatas[i];
				//#endif
	
				#ifdef USE_LIGHT_CLUSTER
					LightData Plight = FetchClusterLightIndex(cellIndex, i);
				#else
					LightData Plight = _WorldLightDatas[i];
				#endif

				if (IsMatchingLightLayer(Plight.lightLayers, builtinData.renderingLayers))
				{
	
					//RT_PL_RT
					RT_PL_RT( context , Plight , PunLigCol , PuncLigDir , PuncSha , PuncLF , posInput );

					#if N_F_NLASOBF_ON
						float3 lightColor = lerp((float3)0.0,PunLigCol.rgb * Plight.diffuseDimmer,isFrontFace);
					#else
						float3 lightColor = PunLigCol.rgb * Plight.diffuseDimmer;
					#endif

					#if N_F_HPSAS_ON
						float attenuation = 1.0;
					#else
						float dlshmin = lerp(0.0,0.6,_ShadowHardness);
						float dlshmax = lerp(1.0,0.6,_ShadowHardness);

						#if N_F_NLASOBF_ON
							float FB_Check = lerp(1.0,PuncSha,isFrontFace);
						#else
							float FB_Check = PuncSha;
						#endif

						float attenuation = smoothstep(dlshmin, dlshmax ,FB_Check);
					#endif

					float3 lightDirection = PuncLigDir;
					float3 halfDirection = normalize(viewDirection+lightDirection);

					float RTD_LVLC = RTD_LVLC_F(lightColor.rgb * 0.2);

					float lightfos = smoothstep(0, _LightFalloffSoftness ,PuncLF);

					float3 lig_col_int = (_LightIntensity * lightColor.rgb);

					#if SHADERPASS == SHADERPASS_RAYTRACING_FORWARD
						float3 RTD_LAS = lerp ((ss_col * 7) * RTD_LVLC, ( ss_col * lig_col_int ), _LightAffectShadow);
					#else
						float3 RTD_LAS = lerp((ss_col * lerp(GetInverseCurrentExposureMultiplier(), 7.0, SWEXPO)) * RTD_LVLC, (ss_col * lig_col_int), _LightAffectShadow);
					#endif
	
					float3 RTD_HL = (_HighlightColor.rgb*_HighlightColorPower+_PointSpotlightIntensity);

					float3 RTD_MCIALO = lerp(RTD_TEX_COL , lerp( lerp( (RTD_TEX_COL * (_MainColor.rgb * _MaiColPo)), (RTD_TEX_COL + (_MainColor.rgb * _MaiColPo)), _SPECMODE) , _MainTex_var.rgb * MCapOutP * _RTD_MVCOL * 0.7 , clamp((RTD_LVLC*1.0),0.0,1.0) ) , _MCIALO );

	
					//RT_GLO
					float RTD_GLO; 
					float3 RTD_GLO_COL;

	                #if SHADERPASS == SHADERPASS_RAYTRACING_FORWARD
						RT_GLO( fragInput.texCoord0.xy, fragInput.texCoord1.xy, fragInput.texCoord0.xy, halfDirection, bitangentWS, normalDirection, viewDirection, fragInput.positionRWS.xyz, Plight.specularDimmer, RTD_GLO, RTD_GLO_COL );
					#else
						RT_GLO( fragInput.texCoord0.xy, fragInput.texCoord1.xy, RTD_VD_Cal, halfDirection, bitangentWS, normalDirection, viewDirection, fragInput.positionRWS.xyz, Plight.specularDimmer, RTD_GLO, RTD_GLO_COL );
					#endif
	
					float3 RTD_GLO_OTHERS = RTD_GLO;

					//RT_RL
					float3 RTD_RL_LARL_OO;
					float RTD_RL_MAIN;
					float RTD_RL_CHE_1 = RT_RL( viewDirection , normalDirection , lightColor , RTD_RL_LARL_OO , RTD_RL_MAIN );

					//RT_CLD
					float3 RTD_CLD = RT_CLD( lightDirection );

					float3 RTD_ST_SS_AVD_OO = lerp( RTD_CLD, viewDirection, _SelfShadowShadowTAtViewDirection );
					float RTD_NDOTL = 0.5*dot(RTD_ST_SS_AVD_OO, float3(RTD_SON.x, RTD_SON.y * (1 - _LigIgnoYNorDir), RTD_SON.z))+0.5;

					//RT_ST
					float3 RTD_SHAT_COL;
					float RTD_STIAL;
					float RTD_ST_IS;
					float3 RTD_ST_LAF;
	
					//cc
					float2 RTD_SDF_UV;
					#if N_F_UUONESDF_ON
						RTD_SDF_UV = fragInput.texCoord1.xy;
					#else
						RTD_SDF_UV = fragInput.texCoord0.xy;
					#endif
	
					float RTD_ST = RT_ST(RTD_SDF_UV, fragInput.positionRWS.xyz, normalDirection, RTD_NDOTL, lightfos, lightDirection, RTD_LVLC, RTD_PT_COL, lig_col_int, RTD_SCT, RTD_OSC, RTD_PT, RTD_SHAT_COL, RTD_STIAL, RTD_ST_IS, RTD_ST_LAF);

					//RT_SS
					float RTD_SS = RT_SS( fragInput.color , RTD_NDOTL , attenuation , Plight.shadowDimmer );

	
					float3 RTD_RL_LIG;
					float3 RTD_RL_SHA;
					#ifdef N_F_RLIS_ON
						RTD_RL_LIG = lerp( RTD_MCIALO_IL * RTD_HL , RTD_GLO_COL , RTD_GLO_OTHERS );
						RTD_RL_SHA = lerp (lerp( RTD_ST_LAF, RTD_LAS, RTD_ST_IS) * RTD_RT_GI_Sha_FO,RTD_RL_LARL_OO,RTD_RL_CHE_1 * RTD_LVLC );
					#else
						RTD_RL_LIG = lerp( lerp( RTD_MCIALO_IL * RTD_HL , RTD_GLO_COL , RTD_GLO_OTHERS) , RTD_RL_LARL_OO , RTD_RL_CHE_1 );
						RTD_RL_SHA = lerp( RTD_ST_LAF, RTD_LAS, RTD_ST_IS) * RTD_RT_GI_Sha_FO;
					#endif

					float3 RTD_R_OFF_OTHERS = lerp( RTD_RL_SHA, lerp( RTD_ST_LAF, RTD_RL_LIG * lightColor.rgb, RTD_ST) , RTD_SS );
	

					//RT_R
					float3 RTD_R = RT_R( fragInput.texCoord0.xy, viewDirection, normalDirection, fragInput.positionRWS.xyz, EnvRef, RTD_TEX_COL, RTD_R_OFF_OTHERS );
					
					//RT_SL
					//float3 RTD_SL_CHE_1;
					float3 RTD_SL = RT_SL( fragInput.texCoord0.xy, fragInput.positionRWS.xyz, normalDirection, (float3)0.0, RTD_TEX_COL, RTD_R, RTD_SL_CHE_1 );

					//RT_RL_SUB1
					float3 RTD_RL = RT_RL_SUB1( RTD_SL_CHE_1 , RTD_RL_LARL_OO , RTD_RL_MAIN);

					float3 RTD_CA_OFF_OTHERS = (RTD_RL + RTD_SL);
					float3 punctual_light_output = RTD_CA_OFF_OTHERS * lightfos;


					#if N_F_USETLB_ON
						A_L_O += punctual_light_output;
					#else
						A_L_O = max (punctual_light_output,A_L_O);
					#endif

				}

			}
			
		}

	#endif
	//=========//
	//=========//

	
	//=========//
	//=========//
	#if SHADEROPTIONS_AREA_LIGHTS

		#if N_F_AL_ON

			if (LIGHTFEATUREFLAGS_AREA)
			{

				float4 ALigCol = float4(1.0,1.0,1.0,1.0);
				float3 ALigDir = float3(0.0,0.0,0.0);
				float ASha = 1.0;
				float ALF = 1.0;
				float ALF2 = 0.0;
				float3 ALCk = float3(1.0,1.0,1.0);

				uint NPLC = 0;
				uint NALC = 0;
					
				#ifdef USE_LIGHT_CLUSTER
					GetLightCountAndStartCluster(actualWSPos, LIGHTCATEGORY_AREA, NPLC, NALC, cellIndex);
				#else
					NPLC = _WorldPunctualLightCount;
					NALC = _WorldAreaLightCount;
				#endif

				if (NALC > 0)
				{

					i=0;

					uint last = NALC - 1;

					#ifdef USE_LIGHT_CLUSTER
						LightData Alight = FetchClusterLightIndex(cellIndex, NPLC+i);
					#else
						LightData Alight = _WorldLightDatas[NPLC+i];
					#endif

					while (i <= last )
					{

						if(IsMatchingLightLayer(Alight.lightLayers, builtinData.renderingLayers))
						{
							//RT_AL_RT
							RT_AL_RT( context , Alight , posInput , viewDirection , normalDirection , objPos , ALigCol , ALigDir , ASha , ALF , ALF2 , ALCk );

							#if N_F_NLASOBF_ON
								float3 lightColor = lerp((float3)0.0,ALigCol.rgb * Alight.diffuseDimmer ,isFrontFace);
							#else
								float3 lightColor = ALigCol.rgb * Alight.diffuseDimmer;
							#endif

							#if N_F_HPSAS_ON
								float attenuation = 1.0; 
							#else
								float dlshmin = lerp(0.0,0.6,_ShadowHardness);
								float dlshmax = lerp(1.0,0.6,_ShadowHardness);

								#if N_F_NLASOBF_ON
									float FB_Check = lerp(1.0,ASha,isFrontFace);
								#else
									float FB_Check = ASha;
								#endif

								float attenuation = smoothstep(dlshmin, dlshmax ,FB_Check);
							#endif

							float RTD_LVLC = RTD_LVLC_F(lightColor.rgb * 0.2);
							float3 lightDirection = ALigDir;
							float3 halfDirection = normalize(viewDirection+lightDirection);
							float lightfos = smoothstep(0, _LightFalloffSoftness ,ALF * ALF2); 
							float3 lig_col_int = (_LightIntensity * lightColor.rgb);
	
							#if SHADERPASS == SHADERPASS_RAYTRACING_FORWARD
								float3 RTD_LAS = lerp ((ss_col * 7) * RTD_LVLC, ( ss_col * lig_col_int ), _LightAffectShadow);
							#else
								float3 RTD_LAS = lerp((ss_col * lerp(GetInverseCurrentExposureMultiplier(), 7.0, SWEXPO)) * RTD_LVLC, (ss_col * lig_col_int), _LightAffectShadow);
							#endif
	
							float3 RTD_HL = (_HighlightColor.rgb*_HighlightColorPower+_ALIntensity);
							float3 RTD_MCIALO = lerp(RTD_TEX_COL , lerp( lerp( (RTD_TEX_COL * (_MainColor.rgb * _MaiColPo)), (RTD_TEX_COL + (_MainColor.rgb * _MaiColPo)), _SPECMODE) , _MainTex_var.rgb * MCapOutP * _RTD_MVCOL * 0.7 , clamp((RTD_LVLC*1.0),0.0,1.0) ) , _MCIALO );

							//cc
							//RT_GLO
							float RTD_GLO; 
							float3 RTD_GLO_COL;

							#if SHADERPASS == SHADERPASS_RAYTRACING_FORWARD
								RT_GLO( fragInput.texCoord0.xy, fragInput.texCoord1.xy, fragInput.texCoord0.xy, halfDirection, bitangentWS, normalDirection, viewDirection, fragInput.positionRWS.xyz, Alight.specularDimmer, RTD_GLO, RTD_GLO_COL );
							#else
								RT_GLO( fragInput.texCoord0.xy, fragInput.texCoord1.xy, RTD_VD_Cal, halfDirection, bitangentWS, normalDirection, viewDirection, fragInput.positionRWS.xyz, Alight.specularDimmer, RTD_GLO, RTD_GLO_COL ); 
							#endif
	
							//==
	
	
							float RTD_GLO_OTHERS = RTD_GLO;

	
							//RT_RL
							float3 RTD_RL_LARL_OO;
							float RTD_RL_MAIN;
							float RTD_RL_CHE_1 = RT_RL( viewDirection , normalDirection , lightColor , RTD_RL_LARL_OO , RTD_RL_MAIN );
							//==
	
	
							//RT_CLD
							float3 RTD_CLD = RT_CLD( lightDirection );
							//==

	
							float3 RTD_ST_SS_AVD_OO = lerp( RTD_CLD, viewDirection, _SelfShadowShadowTAtViewDirection );
							float RTD_NDOTL = 0.5*dot(RTD_ST_SS_AVD_OO, float3(RTD_SON.x, RTD_SON.y * (1 - _LigIgnoYNorDir), RTD_SON.z))+0.5;

	
							//RT_ST
							float3 RTD_SHAT_COL;
							float RTD_STIAL;
							float RTD_ST_IS;
							float3 RTD_ST_LAF;
	
							//cc
							float2 RTD_SDF_UV;
							#if N_F_UUONESDF_ON
								RTD_SDF_UV = fragInput.texCoord1.xy;
							#else
								RTD_SDF_UV = fragInput.texCoord0.xy;
							#endif
	
							float RTD_ST = RT_ST (RTD_SDF_UV, fragInput.positionRWS.xyz, normalDirection, RTD_NDOTL, lightfos, lightDirection, RTD_LVLC, RTD_PT_COL, lig_col_int, RTD_SCT, RTD_OSC, RTD_PT, RTD_SHAT_COL, RTD_STIAL, RTD_ST_IS, RTD_ST_LAF );
							//==

	
							//RT_SS
							float RTD_SS = RT_SS( fragInput.color , RTD_NDOTL , attenuation , Alight.shadowDimmer );
							//==

	
							float3 RTD_RL_LIG;
							float3 RTD_RL_SHA;
							#ifdef N_F_RLIS_ON
								RTD_RL_LIG = lerp( RTD_MCIALO_IL * RTD_HL , RTD_GLO_COL , RTD_GLO_OTHERS );
								RTD_RL_SHA = lerp (lerp( RTD_ST_LAF, RTD_LAS, RTD_ST_IS) * RTD_RT_GI_Sha_FO,RTD_RL_LARL_OO,RTD_RL_CHE_1 * RTD_LVLC );
							#else
								RTD_RL_LIG = lerp( lerp( RTD_MCIALO_IL * RTD_HL , RTD_GLO_COL , RTD_GLO_OTHERS) , RTD_RL_LARL_OO , RTD_RL_CHE_1 );
								RTD_RL_SHA = lerp( RTD_ST_LAF, RTD_LAS, RTD_ST_IS) * RTD_RT_GI_Sha_FO;
							#endif
	
							float3 RTD_R_OFF_OTHERS = lerp( RTD_RL_SHA, lerp( RTD_ST_LAF, RTD_RL_LIG * lightColor.rgb, RTD_ST) , RTD_SS );

	
							//RT_R
							float3 RTD_R = RT_R( fragInput.texCoord0.xy, viewDirection, normalDirection, fragInput.positionRWS.xyz, EnvRef, RTD_TEX_COL, RTD_R_OFF_OTHERS );
							//==

	
							//RT_SL
							float3 RTD_SL_CHE_1;
							float3 RTD_SL = RT_SL( fragInput.texCoord0.xy, fragInput.positionRWS.xyz, normalDirection, (float3)0.0, RTD_TEX_COL, RTD_R, RTD_SL_CHE_1 );
							//==

	
							//RT_RL_SUB1
							float3 RTD_RL = RT_RL_SUB1( RTD_SL_CHE_1 , RTD_RL_LARL_OO , RTD_RL_MAIN);
							//==

	
							float3 RTD_CA_OFF_OTHERS = (RTD_RL + RTD_SL);
							float3 area_light_output = RTD_CA_OFF_OTHERS * lightfos;

							#if N_F_USETLB_ON
								Ar_L_O += area_light_output;
							#else
								Ar_L_O = max (area_light_output,Ar_L_O);
							#endif

						}

						#ifdef USE_LIGHT_CLUSTER
							Alight = FetchClusterLightIndex(cellIndex, NPLC+(min(++i, last)));
						#else
							Alight = _WorldLightDatas[NPLC+(min(++i, last))];
						#endif

					}
				}
			}

		#endif

	#endif
	//=========//
	//=========//

	#if N_F_USETLB_ON
		color = D_L_O + A_L_O + Ar_L_O;
	#else
		color = max(max(D_L_O ,A_L_O), Ar_L_O);
	#endif


	#if N_F_TRANS_ON

		float Trans_Val = 1.0;
					
		#ifndef N_F_CO_ON

			Trans_Val = RTD_TRAN_OPA_Sli;

		#endif
					
	#else

		float Trans_Val = 1.0;

	#endif

	#if SHADERPASS == SHADERPASS_RAYTRACING_FORWARD
		Trans_Val_Out = Trans_Val;
	#endif
	
	//RT_CA
	float3 RTD_CA = RT_CA(color + GLO_OUT);
	//==
	
	#if SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER
		fi_out = float4(RTD_CA * RTD_SON_CHE_1,1.0);
	#else
		fi_out = ApplyBlendMode(RTD_CA * RTD_SON_CHE_1, Trans_Val);
	#endif
}

#endif

