Shader "Penalty/Stadium Surface"
{
 Properties {
  _BaseMap("Surface",2D)="white" {} _BaseColor("Tint",Color)=(1,1,1,1)
  _Mode("0 Surface 1 Pitch 2 Fabric 3 Football",Float)=0 _Smoothness("Smoothness",Range(0,1))=.2
  _Tiling("Pitch texture scale",Float)=.75 _Emission("Emission",Color)=(0,0,0,0)
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
  Pass {
   Name "Forward" Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile_fog
   #pragma multi_compile_instancing
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST,_BaseColor,_Emission; float _Mode,_Smoothness,_Tiling;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;float2 uv:TEXCOORD2;half4 color:COLOR;half fog:TEXCOORD3;};
   V Vert(A a) {UNITY_SETUP_INSTANCE_ID(a);V o;VertexPositionInputs p=GetVertexPositionInputs(a.positionOS.xyz);o.world=p.positionWS;o.positionCS=p.positionCS;o.normal=TransformObjectToWorldNormal(a.normalOS);o.uv=TRANSFORM_TEX(a.uv,_BaseMap);o.color=a.color;o.fog=ComputeFogFactor(p.positionCS.z);return o;}
   float Line(float distance,float width) {return 1-smoothstep(width,width+fwidth(distance)*1.5,abs(distance));}
   half4 Frag(V i):SV_Target {
    float2 uv=_Mode==1?i.world.xz*_Tiling:i.uv;
    half3 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).rgb*_BaseColor.rgb*i.color.rgb;
    half3 normal=normalize(i.normal);half smoothness=_Smoothness;
    if(_Mode==1) {
     float x=i.world.x,z=i.world.z;
     float stripe=sin(z*3.14159265/5.25);albedo*=1+stripe*.09;
     float detailFade=saturate(1-max(fwidth(x),fwidth(z))*95);
     float grassGrain=sin(x*321+sin(z*119))*sin(z*281+sin(x*143));
     albedo*=1+grassGrain*.16*detailFade;
     float wear=exp(-dot(float2(x/.85,(z-11)/.55),float2(x/.85,(z-11)/.55))*2)+exp(-dot(float2(x/2.1,(z-.5)/.8),float2(x/2.1,(z-.5)/.8))*2);
     float noise=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv*.173+.25).g;
     albedo=lerp(albedo,half3(.17,.115,.052)*(noise*.7+.6),saturate(wear*.9)*(1-noise*.4));
     float paint=max(Line(z,.055),Line(z-105,.055));
     paint=max(paint,Line(abs(x)-34,.055)*step(0,z)*step(z,105));
     paint=max(paint,Line(z-5.5,.055)*step(abs(x),9.16));
     paint=max(paint,Line(abs(x)-9.16,.055)*step(0,z)*step(z,5.5));
     paint=max(paint,Line(z-16.5,.055)*step(abs(x),20.16));
     paint=max(paint,Line(abs(x)-20.16,.055)*step(0,z)*step(z,16.5));
     paint=max(paint,Line(z-52.5,.055));
     paint=max(paint,Line(length(float2(x,z-52.5))-9.15,.055));
     paint=max(paint,Line(length(float2(x,z-11))-9.15,.055)*step(16.5,z));
     paint=max(paint,1-smoothstep(.09,.12,length(float2(x,z-11))));
     albedo=lerp(albedo,half3(.77,.8,.72)*(noise*.18+.82),paint*.96);
     float a=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv+float2(.001,0)).g;
     float b=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv+float2(0,.001)).g;
     float c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).g;
     normal=normalize(normal+float3((c-a)*1.4,0,(c-b)*1.4)
                       +float3(grassGrain,0,sin(z*277+x*228))*.11*detailFade);
     smoothness=lerp(.15,.29,saturate(c*1.7))*(1-wear*.22);
    } else if(_Mode==2) {
     float weave=sin(uv.x*2800)*sin(uv.y*2800);
     albedo*=1+weave*.012*saturate(1-fwidth(uv.x)*2800);smoothness*=.85+weave*.03;
    } else if(_Mode==3) {
     float2 p=i.uv*170;
     float2 cell=frac(p+float2(floor(p.y)*.5,0))-.5;
     float fade=saturate(1-max(fwidth(p.x),fwidth(p.y))*1.3);
     float pore=1-smoothstep(.08,.27,length(cell));
     albedo*=1-pore*.22*fade;
     smoothness*=1-pore*.55*fade;
    }
    InputData data=(InputData)0;data.positionWS=i.world;data.normalWS=normal;data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);data.shadowCoord=TransformWorldToShadowCoord(i.world);data.bakedGI=SampleSH(normal);data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);data.shadowMask=1;
    data.vertexLighting=VertexLighting(i.world,normal);
    SurfaceData s=(SurfaceData)0;s.albedo=albedo;s.alpha=1;s.metallic=0;s.specular=.04;s.smoothness=smoothness;s.normalTS=half3(0,0,1);s.occlusion=1;s.emission=_Emission.rgb;
    half4 result=UniversalFragmentPBR(data,s);result.rgb=MixFog(result.rgb,i.fog);return result;
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
