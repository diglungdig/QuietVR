// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Stencils/ClearDepthBuffer"
{
	//from http://wiki.popcornfx.com/index.php/PostFx_Workaround_(Unity_Plugin)
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		// No culling or depth
		ZTest Always
		ZWrite On

		// Writes to a single-component texture (TextureFormat.Depth)
		Pass
	{
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"

		struct appdata
	{
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
	};

	struct v2f
	{
		fixed2 uv : TEXCOORD0;
		float4 vertex : SV_POSITION;
	};

	sampler2D    _MainTex;

	v2f vert(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;
		return o;
	}

#if !defined(SHADER_API_D3D9) && !defined(SHADER_API_D3D11_9X)
	fixed frag(v2f i) : SV_Depth
	{
		fixed4 col = tex2D(_MainTex, i.uv);
	return 0;
	}
#else
	void frag(v2f i, out float4 dummycol:COLOR, out float depth : DEPTH)
	{
		fixed4 col = tex2D(_MainTex, i.uv);
		dummycol = col;

		depth = 0;
	}
#endif
	ENDCG
	}
	}
}