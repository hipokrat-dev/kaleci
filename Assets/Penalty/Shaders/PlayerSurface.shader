Shader "Penalty/Player Surface"
{
 Properties {
  _BaseMap("Albedo",2D)="white" {} _BaseColor("Tint",Color)=(1,1,1,1)
  _BumpMap("Normal",2D)="bump" {} _BumpScale("Normal strength",Float)=1
  _ClothDetail("Cloth relief",2D)="gray" {} _ClothAmount("Cloth relief amount",Range(0,1))=0
  _SurfaceKind("0 Skin 1 Cloth 2 Leather 3 Hair 4 Eye",Float)=0
  _Smoothness("Smoothness",Range(0,1))=.3
  _FaceDetail("Face detail",Range(0,1))=0
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
   #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);TEXTURE2D(_ClothDetail);SAMPLER(sampler_ClothDetail);
   CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST,_BaseColor;float _BumpScale,_Smoothness,_SurfaceKind,_ClothAmount,_FaceDetail;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;half4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;float2 uv:TEXCOORD2;half4 tangent:TEXCOORD3;half4 color:COLOR;half fog:TEXCOORD4;};
   V Vert(A a){UNITY_SETUP_INSTANCE_ID(a);V o;VertexPositionInputs p=GetVertexPositionInputs(a.positionOS.xyz);o.positionCS=p.positionCS;o.world=p.positionWS;o.normal=TransformObjectToWorldNormal(a.normalOS);o.tangent=half4(TransformObjectToWorldDir(a.tangentOS.xyz),a.tangentOS.w*GetOddNegativeScale());o.uv=TRANSFORM_TEX(a.uv,_BaseMap);o.color=a.color;o.fog=ComputeFogFactor(p.positionCS.z);return o;}
   half4 Frag(V i):SV_Target {
    half3 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb*i.color.rgb;
    half3 baseNormal=normalize(i.normal),tangent=normalize(i.tangent.xyz),bitangent=normalize(cross(baseNormal,tangent))*i.tangent.w;
    half3 normalTS=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv),_BumpScale);
    half smoothness=_Smoothness;
    if(_SurfaceKind<.5){
     float fade=saturate(1-max(fwidth(i.uv.x),fwidth(i.uv.y))*1600);
     float pores=sin(i.uv.x*7151+sin(i.uv.y*3871))*sin(i.uv.y*6281);
     normalTS.xy+=float2(pores,sin(i.uv.x*4973-i.uv.y*7127))*.022*fade;smoothness+=pores*.025*fade;
     // The dark beard is rougher than skin; the lip and forehead retain a soft highlight.
     half luminance=dot(albedo,half3(.2126,.7152,.0722));
     smoothness=lerp(smoothness,lerp(.18,.39,smoothstep(.035,.19,luminance)),_FaceDetail);
    }else if(_SurfaceKind<1.5){
     half detail=SAMPLE_TEXTURE2D(_ClothDetail,sampler_ClothDetail,i.uv).r;
     albedo*=lerp(1,clamp(detail*2,.65,1.24),_ClothAmount);
     float fade=saturate(1-max(fwidth(i.uv.x),fwidth(i.uv.y))*1800);
     float weave=sin(i.uv.x*9000)*sin(i.uv.y*7500);normalTS.xy+=float2(weave,-weave)*.055*fade;smoothness+=weave*.025*fade;
    }else if(_SurfaceKind<2.5){
     float grain=sin(i.uv.x*2100)*sin(i.uv.y*1800);float fade=saturate(1-max(fwidth(i.uv.x),fwidth(i.uv.y))*500);
     normalTS.xy+=float2(grain,-grain)*.018*fade;
    }else if(_SurfaceKind<3.5){
     float strand=sin(i.uv.x*250+sin(i.uv.y*15)*2);albedo*=.93+strand*.07;smoothness+=strand*.025;
    }
    half3 normal=normalize(tangent*normalTS.x+bitangent*normalTS.y+baseNormal*normalTS.z);
    InputData data=(InputData)0;data.positionWS=i.world;data.normalWS=normal;data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);data.shadowCoord=TransformWorldToShadowCoord(i.world);data.bakedGI=SampleSH(normal);data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);data.shadowMask=1;data.vertexLighting=VertexLighting(i.world,normal);
    SurfaceData s=(SurfaceData)0;s.albedo=albedo;s.alpha=1;s.smoothness=saturate(smoothness);s.specular=.04;s.normalTS=normalTS;s.occlusion=1;
    half4 result=UniversalFragmentPBR(data,s);Light key=GetMainLight(data.shadowCoord);
    if(_SurfaceKind<.5){
     // A restrained wrapped diffuse lobe softens skin under stadium floodlights.
     half wrap=saturate((dot(normal,key.direction)+.4)/1.4)-saturate(dot(normal,key.direction));
     result.rgb+=albedo*half3(1,.65,.48)*wrap*key.color*.16*key.shadowAttenuation;
     // Soft fill recovers facial details under the brow without flattening the key light.
     result.rgb+=albedo*half3(.18,.19,.22)*_FaceDetail;
    }else if(_SurfaceKind>2.5&&_SurfaceKind<3.5){
     half3 h=normalize(key.direction+data.viewDirectionWS);half th=dot(bitangent,h);
     result.rgb+=half3(.022,.015,.009)*pow(saturate(1-th*th),24)*saturate(dot(normal,key.direction))*key.shadowAttenuation;
    }
    result.rgb=MixFog(result.rgb,i.fog);return result;
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}
