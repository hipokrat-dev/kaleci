Shader "Penalty/Stadium Glow"
{
 Properties {_BaseColor("Glow",Color)=(1,1,1,1)}
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Blend SrcAlpha One ZWrite Off Cull Off
  Pass {
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial) half4 _BaseColor; CBUFFER_END
   struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
   struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
   V Vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;o.color=a.color;return o;}
   half4 Frag(V i):SV_Target {float d=length(i.uv*2-1);return half4(_BaseColor.rgb*i.color.rgb,saturate(1-d)*saturate(1-d)*_BaseColor.a*i.color.a);}
   ENDHLSL
  }
 }
}
