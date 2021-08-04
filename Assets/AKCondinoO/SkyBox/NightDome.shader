// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'
// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
Shader "Unlit/NightDome" {
Properties{
_Alpha("Alpha",Range(0,1))=1 _MainTex("Albedo (RGB)",2D)="white"
_VerticalOpacityThreshold("Vertical Opacity Threshold",Float)=0.5 _VerticalTransparencyThreshold("Vertical Transparency Threshold",Float)=0.1
}
SubShader{Tags{"Queue"="Transparent" "RenderType"="Transparent"}
ZWrite Off
Cull Back
Blend SrcAlpha OneMinusSrcAlpha
Lighting Off
//  A single pass in our subshader
Pass{
CGPROGRAM
#pragma   vertex vert
#pragma fragment frag
//  Adding fog...
#pragma multi_compile_fog
#include "UnityCG.cginc"
float _Alpha;
float4 _Color;
uniform sampler2D _MainTex;
uniform float4    _MainTex_ST;
half _VerticalOpacityThreshold;
half _VerticalTransparencyThreshold;
struct input{
float2     uv:TEXCOORD0;
float4 vertex:POSITION;
};
struct output{
float4 position:SV_POSITION0;
float2       uv:TEXCOORD0;
float4   vertex:POSITION1;
};
output vert(input i){
output o;//  Convert the vertex to world space: 
	   o.vertex=mul(unity_ObjectToWorld,i.vertex); 
	   o.position=UnityObjectToClipPos(i.vertex);
	   o.uv=i.uv.xy*_MainTex_ST.xy+_MainTex_ST.zw;
return o;}
fixed4 frag(output i):SV_Target{
fixed4 color=tex2D(_MainTex,i.uv);//  Calculate alpha according to the world Y coordinate:
	   color.w=1-saturate((i.vertex.y-_VerticalOpacityThreshold)/(_VerticalTransparencyThreshold-_VerticalOpacityThreshold));
	   color.w=color.w*_Alpha;
return color;}
ENDCG
}
}
FallBack"Diffuse"
}