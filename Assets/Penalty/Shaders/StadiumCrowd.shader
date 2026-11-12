Shader "Penalty/Stadium Crowd"
{
 Properties {_BaseMap("Spectator atlas",2D)="white" {} _BaseColor("Night balance",Color)=(.7,.7,.7,1)}
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" "RenderType"="TransparentCutout"}
  Cull Off ZWrite On
  Pass {
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial) half4 _BaseColor; CBUFFER_END
   struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
   struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;half fog:TEXCOORD1;};
   V Vert(A a){V o;float phase=a.positionOS.x*2.1+a.positionOS.z;float top=frac(a.uv.y*4);a.positionOS.x+=sin(_Time.y*1.3+phase)*.018*top;a.positionOS.y+=sin(_Time.y+phase)*.009*top;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;o.color=a.color;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   half4 Frag(V i):SV_Target {half4 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);clip(color.a-.5);return half4(MixFog(color.rgb*_BaseColor.rgb*i.color.rgb,i.fog),1);}
   ENDHLSL
  }
 }
}
