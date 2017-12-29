
Shader "Stencil/StencilRevealByReference"
{
	Properties{
		_StencilMask("Stencil Mask", Int) = 0

		_Color("Color", Color) = (1,1,1,1)

		//_MainTex("Albedo (RGB)", 2D) = "white" {}

	//_Glossiness("Smoothness", Range(0,1)) = 0.5
		//_Metallic("Metallic", Range(0,1)) = 0.0
	}
		SubShader{
		Tags
	{
		"RenderType" = "Opaque"
		"Queue" = "Geometry"
	}
		LOD 200

		Stencil{
		Ref[_StencilMask]
		Comp equal
		Pass keep
	}

	Pass
	{
	CGPROGRAM
	#pragma vertex vert
	#pragma fragment frag

	struct appdata
	{
	float4 vertex : POSITION;
	};

	struct v2f
	{
	float4 pos : SV_POSITION;
	};

	fixed4 _Color;

	v2f vert(appdata v)
	{
	v2f o;
	o.pos = UnityObjectToClipPos(v.vertex);
	return o;
	}

	half4 frag(v2f i) : COLOR
	{
	return _Color;
	}
	ENDCG
	}






		/*
	void surf(Input IN, inout SurfaceOutputStandard o) {
		// Albedo comes from a texture tinted by color
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
		o.Albedo = c.rgb;
		// Metallic and smoothness come from slider variables
		o.Metallic = _Metallic;
		o.Smoothness = _Glossiness;
		o.Alpha = c.a;
	}
	ENDCG
	}
	*/
	//FallBack "Diffuse"
	}
} 