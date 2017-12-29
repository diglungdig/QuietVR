// Copyright 2017 Google Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

Shader "Brush/DiffuseSingleSided" {
Properties {
  _Color ("Main Color", Color) = (1,1,1,1)
  _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
  _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
}

SubShader {
  Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
  LOD 200
  Cull Back

CGPROGRAM
#pragma surface surf Lambert vertex:vert alphatest:_Cutoff addshadow
#pragma multi_compile __ TBT_LINEAR_TARGET
#include "../../../Shaders/Include/Brush.cginc"

sampler2D _MainTex;
fixed4 _Color;

struct Input {
  float2 uv_MainTex;
  float4 color : COLOR;
};

void vert (inout appdata_full v) {
  v.color = TbVertToNative(v.color);
}

void surf (Input IN, inout SurfaceOutput o) {
  fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
  o.Albedo = c.rgb * IN.color.rgb;
  o.Alpha = c.a * IN.color.a;
}
ENDCG
}


// MOBILE VERSION
SubShader {
  Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
  LOD 100

CGPROGRAM
#pragma surface surf Lambert vertex:vert alphatest:_Cutoff
#pragma multi_compile __ TBT_LINEAR_TARGET
#include "../../../Shaders/Include/Brush.cginc"

sampler2D _MainTex;
fixed4 _Color;

struct Input {
  float2 uv_MainTex;
  float4 color : COLOR;
};

void vert (inout appdata_full v) {
  v.color = TbVertToNative(v.color);
}

void surf (Input IN, inout SurfaceOutput o) {
  fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
  o.Albedo = c.rgb * IN.color.rgb;
  o.Alpha = c.a * IN.color.a;
}
ENDCG
}

Fallback "Transparent/Cutout/VertexLit"
}
