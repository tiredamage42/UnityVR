﻿// Purpose: Used for the teleport markers
// UNITY_SHADER_NO_UPGRADE
Shader "Valve/VR/Highlight"
{
	Properties
	{
		_TintColor( "Tint Color", Color ) = ( 1, 1, 1, 1 )
		_SeeThru( "SeeThru", Range( 0.0, 1.0 ) ) = 0.25
		_Darken( "Darken", Range( 0.0, 1.0 ) ) = 0.0
		_MainTex( "MainTex", 2D ) = "white" {}
	}

	CGINCLUDE

		//uncomment if not working on nico's comp
		
		//#pragma target 5.0
		//#pragma only_renderers d3d11 vulkan glcore
		//#pragma exclude_renderers gles

		#include "UnityCG.cginc"

		struct VertexInput
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
			fixed4 color : COLOR;
		};
		
		struct VertexOutput
		{
			float2 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
			fixed4 color : COLOR;
		};

		sampler2D _MainTex;
		float4 _MainTex_ST;
		float4 _TintColor;
		float _SeeThru;
		float _Darken;
				
		VertexOutput MainVS( VertexInput i )
		{
			VertexOutput o;
			o.vertex = UnityObjectToClipPos(i.vertex);
			o.uv = TRANSFORM_TEX( i.uv, _MainTex );
			o.color = i.color;			
			return o;
		}
		
		float4 MainPS( VertexOutput i ) : SV_Target {
			float4 vTexel = tex2D( _MainTex, i.uv ).rgba;
			float4 vColor = vTexel.rgba * _TintColor.rgba * i.color.rgba;
			vColor.rgba = saturate( 2.0 * vColor.rgba );
			float flAlpha = vColor.a;
			vColor.rgb *= vColor.a;
			vColor.a = lerp( 0.0, _Darken, flAlpha );
			return vColor.rgba;
		}

		// MainPs ---------------------------------------------------------------------------------------------------------------------------------------------------
		float4 SeeThruPS( VertexOutput i ) : SV_Target {
			float4 vTexel = tex2D( _MainTex, i.uv ).rgba;
			float4 vColor = vTexel.rgba * _TintColor.rgba * i.color.rgba * _SeeThru;
			vColor.rgba = saturate( 2.0 * vColor.rgba );
			float flAlpha = vColor.a;
			vColor.rgb *= vColor.a;
			vColor.a = lerp( 0.0, _Darken, flAlpha * _SeeThru );
			return vColor.rgba;
		}

	ENDCG

	SubShader
	{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 100

		// Behind Geometry ---------------------------------------------------------------------------------------------------------------------------------------------------
		Pass
		{
			Blend One OneMinusSrcAlpha
			Cull Off
			ZWrite Off
			ZTest Greater

			CGPROGRAM
				#pragma vertex MainVS
				#pragma fragment SeeThruPS
			ENDCG
		}

		Pass
		{
			Blend One OneMinusSrcAlpha
			Cull Off
			ZWrite Off
			ZTest LEqual
			CGPROGRAM
				#pragma vertex MainVS
				#pragma fragment MainPS
			ENDCG
		}
	}
}
