//RealToon HDRP - Other
//MJQStudioWorks

#ifndef REALTOON_HDRP_OTHER_HLSL
#define REALTOON_HDRP_OTHER_HLSL

float4 ComputeScreenPos(float4 positionCS)
{
	float4 o = positionCS * 0.5f;
	o.xy = float2(o.x, o.y * _ProjectionParams.x) + o.w;
	o.zw = positionCS.zw;
	return o;
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
half4 RT_Tripl_Default(TEXTURE2D_PARAM(tex, samp), float3 positionWS, float3 normalWS)
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
		16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
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

//=========================


//RT NM
float3 RT_NM(float2 uv, float3 positionWS, float3x3 tangentToWorld, float3 normalWS)
{
	#if N_F_NM_ON

		#ifdef N_F_TP_ON
			
			float3 UV = GetAbsolutePositionWS(positionWS) * _TriPlaTile; //Tile Var
    
			float3 Blend = pow(abs(normalWS), _TriPlaBlend); //Blend Var
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

//RT TRANS CO
void RT_TRANS_CO( float2 uv, float4 _MainTex_var, out float RTD_TRAN_OPA_Sli, float RTD_CO, out bool bo_co_val, bool is_rt, float3 positionWS, float3 normalDirection, float2 positionCS )
{
	RTD_TRAN_OPA_Sli = 1.0;
	bo_co_val = false;

	#if N_F_TRANS_ON

		#if N_F_CO_ON
	
			#ifdef N_F_TP_ON
				float4 _SecondaryCutout_var = RT_Tripl_Default(_SecondaryCutout, sampler_SecondaryCutout, positionWS, normalDirection); 
			#else
				float4 _SecondaryCutout_var = SAMPLE_TEXTURE2D(_SecondaryCutout, sampler_SecondaryCutout , TRANSFORM_TEX(uv,_SecondaryCutout));
			#endif
	
			float RTD_CO_ON = (float)lerp( (lerp((_MainTex_var.r*_SecondaryCutout_var.r),_SecondaryCutout_var.r,_UseSecondaryCutoutOnly)+lerp(0.5,(-1.0),_Cutout)), saturate(( (1.0 - _Cutout) > 0.5 ? (1.0-(1.0-2.0*((1.0 - _Cutout)-0.5))*(1.0-lerp((_MainTex_var.a*_SecondaryCutout_var.r),_SecondaryCutout_var.a,_UseSecondaryCutoutOnly))) : (2.0*(1.0 - _Cutout)*lerp((_MainTex_var.a*_SecondaryCutout_var.r),_SecondaryCutout_var.a,_UseSecondaryCutoutOnly)) )), _AlphaBasedCutout );

			RTD_CO = RTD_CO_ON;

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
				bo_co_val = RTD_CO >= 0.5;
			}
            
		#else
	
			#ifdef N_F_TP_ON
				float4 _MaskTransparency_var = RT_Tripl_Default(_MaskTransparency, sampler_MaskTransparency, positionWS, normalDirection);
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

//RT CO ONLY
void RT_CO_ONLY( float2 uv, float3 positionRWS, float3 normalDirection, float2 positionCS )
{
	#if N_F_TRANS_ON

		#if N_F_CO_ON
	
			#ifdef N_F_TP_ON
				float4 _MainTex_var = RT_Tripl_Default(_MainTex, sampler_MainTex, positionRWS, normalDirection);
			#else
				float4 _MainTex_var = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(uv.xy, _MainTex));
			#endif
	
			#ifdef N_F_TP_ON
				float4 _SecondaryCutout_var = RT_Tripl_Default(_SecondaryCutout, sampler_SecondaryCutout, positionRWS, normalDirection);
			#else
				float4 _SecondaryCutout_var = SAMPLE_TEXTURE2D(_SecondaryCutout, sampler_SecondaryCutout, TRANSFORM_TEX(uv.xy, _SecondaryCutout));
			#endif
	
			float RTD_CO_ON = (float)lerp( (lerp((_MainTex_var.r*_SecondaryCutout_var.r),_SecondaryCutout_var.r,_UseSecondaryCutoutOnly)+lerp(0.5,(-1.0),_Cutout)), saturate(( (1.0 - _Cutout) > 0.5 ? (1.0-(1.0-2.0*((1.0 - _Cutout)-0.5))*(1.0-lerp((_MainTex_var.a*_SecondaryCutout_var.r),_SecondaryCutout_var.a,_UseSecondaryCutoutOnly))) : (2.0*(1.0 - _Cutout)*lerp((_MainTex_var.a*_SecondaryCutout_var.r),_SecondaryCutout_var.a,_UseSecondaryCutoutOnly)) )), _AlphaBasedCutout );		

			float RTD_CO = RTD_CO_ON;

			#ifdef N_F_SCO_ON
				clip( -(RT_Dither_Out(positionCS) - RTD_CO));
			#else
				clip(RTD_CO - 0.5);
			#endif

		#endif

	#endif
}

//RT_RELGI
float3 RT_RELGI( float3 RTD_SON )
{
	#if N_F_RELGI_ON

		float3 RTD_GI_ST_Sli = (RTD_SON*_GIShadeThreshold);
		float3 RTD_GI_FS_OO = lerp( RTD_GI_ST_Sli, float3(smoothstep(0.0, 0.01, RTD_SON.r * _GIShadeThreshold), 0.0, smoothstep(0.0, 0.01, RTD_SON.b * _GIShadeThreshold)), _GIFlatShade );
		//float3 RTD_GI_FS_OO = lerp( RTD_GI_ST_Sli, float3(smoothstep( float2(0.0,0.0), float2(0.01,0.01), (RTD_SON.rb*_GIShadeThreshold) ),0.0), _GIFlatShade ); //old

		return RTD_GI_FS_OO;

	#else

		float3 RTD_GI_FS_OO = RTD_SON;

		return RTD_GI_FS_OO;

	#endif
}

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
    float4x4 VPM_Mod = float4x4(VPM_Mul[0][0], VPM_Mul[0][1], VPM_Mul[0][2], VPM_Mul[0][3],  VPM_Mul[1][0], VPM_Mul[1][1], VPM_Mul[1][2], VPM_Mul[1][3],  VPM_Mul[2][0] * (_ClipAdju * 5), VPM_Mul[2][1] * (_ClipAdju * 5), VPM_Mul[2][2] * (_ClipAdju * 5), VPM_Mul[2][3],  VPM_Mul[3][0] * _PresAdju, VPM_Mul[3][1], VPM_Mul[3][2] * _PresAdju, VPM_Mul[3][3] * limit);

    return VPM_Mod;
}

//RT_SE
float haS(float n) { return frac(sin(n) * 43758.5453); }
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

